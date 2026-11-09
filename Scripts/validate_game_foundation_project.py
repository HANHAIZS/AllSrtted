#!/usr/bin/env python3
"""Validate a consuming Unity project's Game Foundation governance contract."""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path, PurePosixPath


ROLE_FOLDERS = (
    "Architecture",
    "Commands",
    "Events",
    "Models",
    "Systems",
    "Utilities",
    "ViewControllers",
)
ALLOWED_STATUSES = {
    "Draft",
    "Ready",
    "Implementing",
    "Verifying",
    "Verified",
    "Blocked",
}
REQUIRED_SPEC_FIELDS = ("id", "title", "status", "owner", "created", "updated")
REQUIRED_SPEC_HEADINGS = (
    "## Goal",
    "## User-visible behavior",
    "## Out of scope",
    "## Ownership and architecture",
    "## Acceptance criteria",
    "## Verification matrix",
    "## Definition of Done",
)
SPEC_ID = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
FRONTMATTER = re.compile(r"\A---\s*\n(.*?)\n---\s*\n", re.DOTALL)


@dataclass
class ValidationReport:
    """Collect deterministic errors and warnings for CLI and unit-test callers."""

    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)

    def error(self, message: str) -> None:
        """Record a convention violation that must fail the gate."""
        self.errors.append(message)

    def warning(self, message: str) -> None:
        """Record review guidance that does not block the gate."""
        self.warnings.append(message)


def parse_arguments() -> argparse.Namespace:
    """Parse the project root and optional Git comparison base."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", default=".", help="Unity project root")
    parser.add_argument(
        "--base",
        default="",
        help="Optional Git base revision used to require a changed Feature Spec",
    )
    return parser.parse_args()


def load_configuration(project_root: Path, report: ValidationReport) -> dict[str, object]:
    """Load the generated project contract and report malformed or missing JSON."""
    config_path = project_root / ".gamefoundation" / "project.json"
    if not config_path.is_file():
        report.error("Missing .gamefoundation/project.json. Run Game Foundation Project Setup.")
        return {}
    try:
        configuration = json.loads(config_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exception:
        report.error(f"Invalid .gamefoundation/project.json: {exception}")
        return {}

    for field_name in ("projectName", "rootNamespace", "codeRoot", "architectureFile", "controllerFile"):
        if not isinstance(configuration.get(field_name), str) or not str(configuration[field_name]).strip():
            report.error(f"Project configuration field '{field_name}' is missing or empty.")
    return configuration


def validate_governance_files(project_root: Path, report: ValidationReport) -> None:
    """Verify that AI instructions, workflow templates, skills, scripts, and CI are installed."""
    required = (
        "AGENTS.md",
        ".agents/skills/specify-foundation-feature/SKILL.md",
        ".agents/skills/develop-foundation-feature/SKILL.md",
        ".agents/skills/normalize-figma-unity-ui/SKILL.md",
        "Docs/Foundation/Architecture.md",
        "Docs/Features/README.md",
        "Docs/Features/FEATURE_TEMPLATE.md",
        "Scripts/validate_game_foundation_project.py",
        ".github/workflows/game-foundation-governance.yml",
    )
    for relative_path in required:
        if not (project_root / relative_path).is_file():
            report.error(f"Missing governance file: {relative_path}")

    agents_path = project_root / "AGENTS.md"
    if agents_path.is_file():
        content = agents_path.read_text(encoding="utf-8", errors="replace")
        if "<!-- GAME FOUNDATION MANAGED START -->" not in content or "<!-- GAME FOUNDATION MANAGED END -->" not in content:
            report.error("AGENTS.md is missing the Game Foundation managed block.")


def normalized_project_path(project_root: Path, value: object) -> Path | None:
    """Resolve a project-relative path while rejecting absolute and escaping values."""
    if not isinstance(value, str) or not value.strip():
        return None
    relative = Path(value.replace("\\", "/"))
    if relative.is_absolute() or ".." in relative.parts:
        return None
    return project_root.joinpath(relative)


def validate_architecture(
    project_root: Path,
    configuration: dict[str, object],
    report: ValidationReport,
) -> None:
    """Validate the centralized QFramework role tree and generated architecture entry files."""
    code_root = normalized_project_path(project_root, configuration.get("codeRoot"))
    if code_root is None or not str(configuration.get("codeRoot", "")).replace("\\", "/").startswith("Assets/"):
        report.error("Configured codeRoot must be a non-escaping child path under Assets.")
        return
    if not code_root.is_dir():
        report.error(f"Configured code root does not exist: {configuration.get('codeRoot')}")
        return

    for role in ROLE_FOLDERS:
        if not (code_root / role).is_dir():
            report.error(f"Missing centralized role folder: {configuration.get('codeRoot')}/{role}")

    role_names = set(ROLE_FOLDERS)
    for directory in code_root.rglob("*"):
        if not directory.is_dir() or directory.name not in role_names:
            continue
        if directory.parent != code_root:
            report.error(
                "Nested role folder is forbidden: "
                + directory.relative_to(project_root).as_posix()
            )

    for config_field in ("architectureFile", "controllerFile"):
        source_path = normalized_project_path(project_root, configuration.get(config_field))
        if source_path is None or not source_path.is_file():
            report.error(f"Configured {config_field} does not exist: {configuration.get(config_field)}")

    validate_project_source_root(project_root, code_root, report)
    validate_role_sources(project_root, code_root, report)


def validate_project_source_root(
    project_root: Path,
    code_root: Path,
    report: ValidationReport,
) -> None:
    """Reject project C# files placed in sibling trees outside the configured role-based code root."""
    scripts_root = project_root / "Assets" / "Scripts"
    if not scripts_root.is_dir():
        return
    resolved_code_root = code_root.resolve()
    for source_path in scripts_root.rglob("*.cs"):
        try:
            source_path.resolve().relative_to(resolved_code_root)
        except ValueError:
            report.error(
                "Project source is outside configured codeRoot: "
                + source_path.relative_to(project_root).as_posix()
            )


def validate_role_sources(project_root: Path, code_root: Path, report: ValidationReport) -> None:
    """Check role ownership across partial declarations without treating comments as inheritance."""
    forbidden_utility_markers = ("AbstractCommand", "AbstractModel", "AbstractSystem")
    for source_path in (code_root / "Utilities").rglob("*.cs") if (code_root / "Utilities").is_dir() else ():
        content = csharp_declarations(source_path.read_text(encoding="utf-8", errors="replace"))
        for marker in forbidden_utility_markers:
            if marker in content:
                report.error(
                    f"Utility source owns a QFramework role ({marker}): "
                    + source_path.relative_to(project_root).as_posix()
                )

    for role, marker in (("Commands", "AbstractCommand"), ("Systems", "AbstractSystem")):
        role_root = code_root / role
        if not role_root.is_dir():
            continue
        declarations = []
        role_types = set()
        for source_path in sorted(role_root.rglob("*.cs")):
            content = csharp_declarations(source_path.read_text(encoding="utf-8", errors="replace"))
            namespaces = re.findall(r"\bnamespace\s+([\w.]+)\s*[;{]", content)
            # Ambiguous scopes retain manual review instead of borrowing another type's base.
            namespace = namespaces[0] if len(namespaces) == 1 else str(source_path)
            pattern = r"((?:(?:public|internal|private|protected|abstract|sealed|static|partial)\s+)*)class\s+(\w+)(?:\s*<[^>{}]+>)?\s*(?::\s*([^{}]+))?\s*\{"
            for match in re.finditer(pattern, content):
                modifiers, name, bases = match.groups()
                owns_role = bool(re.search(r"\b" + marker + r"\b", bases or ""))
                partial = "partial" in modifiers.split()
                key = (namespace, name)
                declarations.append((source_path, name, key, owns_role, partial))
                if owns_role and partial:
                    role_types.add(key)
        for source_path, name, key, owns_role, partial in declarations:
            if not owns_role and not (partial and key in role_types):
                report.warning(
                    f"Review {role} ownership; {name} has no {marker} declaration: "
                    + source_path.relative_to(project_root).as_posix()
                )


def csharp_declarations(content: str) -> str:
    """Mask comments and ordinary/verbatim strings before the conservative declaration scan."""
    return re.sub(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*.*?\*/',
                  lambda match: "\n" * match.group(0).count("\n") + " ", content, flags=re.DOTALL)


def parse_frontmatter(content: str) -> dict[str, str]:
    """Parse the flat key/value frontmatter used by Foundation Feature Specs."""
    match = FRONTMATTER.search(content)
    if not match:
        return {}
    fields: dict[str, str] = {}
    for raw_line in match.group(1).splitlines():
        if ":" not in raw_line:
            continue
        key, value = raw_line.split(":", 1)
        fields[key.strip()] = value.strip().strip('"')
    return fields


def validate_resource_layout(project_root: Path, report: ValidationReport) -> None:
    """Enforce the project Sprite root and reject ambiguous Resources keys without moving assets."""
    assets_root = project_root / "Assets"
    keys: dict[str, str] = {}
    ignored_extensions = {".meta", ".cs", ".asmdef", ".asmref", ".dll"}
    for path in sorted(assets_root.rglob("*")):
        parts = path.relative_to(project_root).parts
        if any(part.startswith(".") for part in parts):
            continue
        if "Editor" in parts and parts[:2] != ("Assets", "Sprites"):
            continue
        if path.is_dir():
            if path.name == "Resources" and parts[:2] == ("Assets", "Sprites"):
                report.error("Direct-reference Sprites tree must not contain Resources: " + "/".join(parts))
            continue
        if parts[:2] == ("Assets", "Sprites") and path.suffix.lower() != ".meta":
            validate_sprite_asset(project_root, path, report)
        if "Resources" not in parts or path.suffix.lower() in ignored_extensions:
            continue
        relative = "/".join(parts)
        if parts.count("Resources") > 1:
            report.error("Nested Resources folders are forbidden: " + relative)
            continue
        key = str(PurePosixPath(*parts[parts.index("Resources") + 1:]).with_suffix("")).lower()
        if key in keys:
            report.error(f"Duplicate Resources key '{key}': {keys[key]} and {relative}")
        else:
            keys[key] = relative


def validate_sprite_asset(project_root: Path, path: Path, report: ValidationReport) -> None:
    """Accept imported Sprite images or standalone serialized Sprites, never unrelated art files."""
    relative = path.relative_to(project_root).as_posix()
    if path.suffix.lower() == ".asset":
        # Unity native Sprite assets use class ID 213; mixed serialized assets require a different root.
        headers = re.findall(r"^--- !u!(\d+)\s", path.read_text(encoding="utf-8", errors="replace"), re.MULTILINE)
        if headers and all(value == "213" for value in headers):
            return
    else:
        image_extensions = {".png", ".jpg", ".jpeg", ".tga", ".psd", ".psb", ".tif", ".tiff", ".bmp", ".exr", ".hdr"}
        if path.suffix.lower() in image_extensions:
            meta_path = Path(str(path) + ".meta")
            metadata = meta_path.read_text(encoding="utf-8", errors="replace") if meta_path.is_file() else ""
            if "TextureImporter:" in metadata and re.search(r"^\s*textureType:\s*8\s*$", metadata, re.MULTILINE):
                return
    report.error("Assets/Sprites only accepts Sprite assets; use Assets/Prefabs or Assets/Art: " + relative)


def validate_feature_specs(project_root: Path, report: ValidationReport) -> list[Path]:
    """Validate Feature Spec identity, lifecycle, required sections, and verified evidence state."""
    features_root = project_root / "Docs" / "Features"
    if not features_root.is_dir():
        report.error("Missing Docs/Features directory.")
        return []

    spec_paths = sorted(
        path
        for path in features_root.glob("*.md")
        if path.name not in {"README.md", "FEATURE_TEMPLATE.md"}
    )
    for spec_path in spec_paths:
        content = spec_path.read_text(encoding="utf-8", errors="replace")
        fields = parse_frontmatter(content)
        relative = spec_path.relative_to(project_root).as_posix()
        for field_name in REQUIRED_SPEC_FIELDS:
            if not fields.get(field_name):
                report.error(f"Feature Spec {relative} is missing frontmatter field '{field_name}'.")

        feature_id = fields.get("id", "")
        if feature_id and not SPEC_ID.fullmatch(feature_id):
            report.error(f"Feature Spec {relative} has invalid id '{feature_id}'.")
        if feature_id and spec_path.stem != feature_id:
            report.error(f"Feature Spec filename must match id '{feature_id}': {relative}")

        status = fields.get("status", "")
        if status and status not in ALLOWED_STATUSES:
            report.error(f"Feature Spec {relative} has unsupported status '{status}'.")
        for heading in REQUIRED_SPEC_HEADINGS:
            if heading not in content:
                report.error(f"Feature Spec {relative} is missing heading '{heading}'.")

        if status in {"Ready", "Implementing", "Verifying", "Verified"} and "TBD" in content:
            report.error(f"Feature Spec {relative} cannot contain TBD while status is {status}.")
        if status == "Verified":
            if "Pending" in content:
                report.error(f"Verified Feature Spec {relative} still contains Pending evidence.")
            if re.search(r"^- \[ \]", content, re.MULTILINE):
                report.error(f"Verified Feature Spec {relative} still has unchecked Definition of Done items.")
    return spec_paths


def governed_change(path: str, code_root: str) -> bool:
    """Return whether a changed path can alter governed runtime, editor, or serialized behavior."""
    normalized = PurePosixPath(path.replace("\\", "/")).as_posix()
    if normalized.startswith("Assets/") and not normalized.endswith(".meta"):
        return True
    if normalized.startswith("ProjectSettings/"):
        return True
    if normalized.startswith("Scripts/") and normalized.endswith((".py", ".sh", ".js", ".cs")):
        return True
    if normalized.startswith("Packages/com.nn555k.gamefoundation.") and not normalized.endswith(".meta"):
        return True
    if normalized in {"Packages/manifest.json", "Packages/packages-lock.json"}:
        return True
    return False


def git_path_exists(project_root: Path, revision: str, path: str) -> bool:
    """Check whether a project file existed at a Git revision without reading its contents."""
    completed = subprocess.run(
        ["git", "cat-file", "-e", f"{revision}:{path}"],
        cwd=project_root,
        check=False,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    return completed.returncode == 0


def changed_paths(project_root: Path, base: str, report: ValidationReport) -> list[str]:
    """Read a Git diff path list without interpreting file content or mutating the worktree."""
    try:
        completed = subprocess.run(
            ["git", "diff", "--name-only", f"{base}...HEAD"],
            cwd=project_root,
            check=True,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
    except subprocess.CalledProcessError as exception:
        report.error(f"Could not compare Feature Spec coverage with Git base '{base}': {exception.stderr.strip()}")
        return []
    return [line.strip().replace("\\", "/") for line in completed.stdout.splitlines() if line.strip()]


def validate_diff_coverage(
    project_root: Path,
    configuration: dict[str, object],
    base: str,
    report: ValidationReport,
) -> None:
    """Require a changed Feature Spec whenever a pull request changes governed behavior."""
    paths = changed_paths(project_root, base, report)
    if not paths:
        return
    if (
        ".gamefoundation/project.json" in paths
        and not git_path_exists(project_root, base, ".gamefoundation/project.json")
    ):
        return
    code_root = str(configuration.get("codeRoot", "Assets/Scripts/Game"))
    governed = [path for path in paths if governed_change(path, code_root)]
    changed_specs = [
        path
        for path in paths
        if path.startswith("Docs/Features/")
        and path.endswith(".md")
        and Path(path).name not in {"README.md", "FEATURE_TEMPLATE.md"}
    ]
    if governed and not changed_specs:
        report.error(
            "Governed code or Unity assets changed without an updated Feature Spec: "
            + ", ".join(governed[:8])
        )


def validate_project(project_root: Path, base: str = "") -> ValidationReport:
    """Run every project-level governance check and return a structured report."""
    report = ValidationReport()
    configuration = load_configuration(project_root, report)
    validate_governance_files(project_root, report)
    if configuration:
        validate_architecture(project_root, configuration, report)
    validate_resource_layout(project_root, report)
    validate_feature_specs(project_root, report)
    if base and configuration:
        validate_diff_coverage(project_root, configuration, base, report)
    return report


def main() -> int:
    """Print a concise validation report and return a CI-compatible exit code."""
    arguments = parse_arguments()
    project_root = Path(arguments.project).resolve()
    report = validate_project(project_root, arguments.base.strip())
    for warning in report.warnings:
        print(f"WARNING: {warning}")
    for error in report.errors:
        print(f"ERROR: {error}")
    if report.errors:
        print(f"Game Foundation project validation failed: {len(report.errors)} error(s).")
        return 1
    print(
        "Game Foundation project validation passed"
        f" with {len(report.warnings)} warning(s)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
