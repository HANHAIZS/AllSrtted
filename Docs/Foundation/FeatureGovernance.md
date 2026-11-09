# Feature Governance

Game Foundation separates reusable infrastructure from project behavior and gives every behavior-changing requirement one traceable Feature Spec.

## Installed contract

`Game Foundation > Project Setup` installs these project-owned governance assets by default:

- `.gamefoundation/project.json`: machine-readable project identity and architecture paths.
- A managed block inside `AGENTS.md`: AI routing, architecture, Unity asset, and completion rules.
- `.agents/skills`: requirement specification, project feature implementation, and Figma UI normalization workflows.
- `Docs/Features`: canonical template and one spec per behavior-changing work item.
- `Scripts/validate_game_foundation_project.py`: local and CI convention gate.
- `.github/workflows/game-foundation-governance.yml`: pull-request diff enforcement.

Existing project instructions are preserved. Setup only replaces the content between `GAME FOUNDATION MANAGED` markers; other generated files are preserved unless overwrite is explicitly enabled.

Setup records the installed managed-block SHA-256 in `.gamefoundation/managed-agents.sha256`. If the block has local edits or comes from an unrecognized legacy installation, repair preserves AGENTS and writes `.gamefoundation/AGENTS.managed.proposed.md` for manual review, even with overwrite enabled. Keep project rules outside the markers; after a reviewed merge matches the current template, the next repair records its receipt. Existing `.gamefoundation/project.json` is preserved byte-for-byte, including extension metadata; identity changes require a separate migration. Missing governance documents are added; existing documents/Skills still need reviewed merging.

## Requirement lifecycle

```text
User request
  -> Feature Spec (Draft)
  -> ownership + acceptance resolved (Ready)
  -> project implementation (Implementing)
  -> convention + Unity + behavior evidence (Verifying)
  -> Definition of Done complete (Verified)
```

Create a spec from `Game Foundation > Feature Workflow > New Feature Specification`, or in BatchMode with `GameFoundation.Setup.Editor.FoundationFeatureSpecBatch.GenerateFromEnvironment` and:

- `GAME_FOUNDATION_FEATURE_ID`
- `GAME_FOUNDATION_FEATURE_TITLE`
- `GAME_FOUNDATION_FEATURE_SUMMARY`

Feature IDs use lowercase kebab-case and remain stable after code, tests, and reviews link to them.

## Enforcement

Local validation:

```bash
python3 Scripts/validate_game_foundation_project.py --project .
```

Unity validation:

- `Game Foundation > Validate Project Conventions`
- BatchMode: `GameFoundation.Setup.Editor.FoundationProjectConventionValidator.ValidateBatch`

The validator checks the generated project contract, centralized QFramework role folders, architecture entries, project scripts placed outside the configured code root, installed Skills and instructions, Feature Spec structure, and high-confidence role violations. The pull-request workflow also rejects code, project settings, package dependencies, runtime/editor tooling, or any non-meta Unity asset change when no Feature Spec changed in the same diff. Only the initial governance Bootstrap is exempt because the base revision has no project contract yet.

Warnings identify ambiguous ownership for review; errors block completion. Unity compilation, tests, runtime behavior, screenshots, device SDK checks, and store/CDN verification remain separate evidence because static convention checks cannot prove them.

UI/prefab/art requirements also follow `ResourceLoading.md`: record Resources keys or direct-reference owners before creation. Both validators reject non-Sprite assets in Assets/Sprites, duplicate Resources keys and Resources nested inside the direct-reference Sprites tree; they do not infer asset loading intent. Runtime load and dependency checks remain required for path-loaded assets.

UI work also follows `UiLayers.md`, the project-owned `UiLayers.Project.md` mapping and `UiAuthoring.md`. The Feature Spec records the semantic host, modal mode, gate/lifetime owner, failure recovery and protected authored properties. These documents are design and verification contracts; installing them is not proof of runtime adoption.

## Definition of Done

A feature is complete only when its spec records:

- observable acceptance criteria and explicit exclusions;
- project/Foundation ownership and QFramework roles;
- persistent-data, UI/prefab/Inspector, SDK, hot-update, and performance impact where relevant;
- passing convention and Unity compilation evidence;
- focused automated and exact runtime or asset checks;
- remaining platform or manual verification.

Do not mark a Feature Spec `Verified` while it contains `TBD`, `Pending`, or unchecked Definition of Done items.
