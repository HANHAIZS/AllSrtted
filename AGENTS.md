<!-- GAME FOUNDATION MANAGED START -->
# Game Foundation Project Rules

This project uses Game Foundation and one project-owned QFramework architecture.

Project configuration:

- Project: `AllSorted`
- Root namespace: `AllSorted`
- Code root: `Assets/Scripts/Game`
- Architecture: `AllSorted.AllSortedApp`
- Controller base: `AllSorted.AllSortedController`

Read before behavior-changing work:

- `.gamefoundation/project.json`
- `Docs/Foundation/Architecture.md`
- `Docs/Foundation/ProjectLayout.md`
- `Docs/Features/README.md`
- The related `Docs/Features/<feature-id>.md`

## Onboarding and repair

- For AI-driven initial setup, follow `Docs/Foundation/NewProjectSetup.md` and the framework repository's `Scripts/onboard_project.py` entry point. Installing UPM references alone does not complete onboarding.
- Existing architectures use `integrationMode: existing-architecture` and explicit architecture/controller paths and types in the project contract; repair preserves those sources and their asmdefs, including when overwrite is enabled.
- Keep project-specific rules outside the managed AGENTS block. Modified or unrecognized legacy managed text is preserved; review `.gamefoundation/AGENTS.managed.proposed.md` and move local rules outside the markers before manually merging framework updates.
- Reuse `.gamefoundation/project.json` on reruns. Never substitute `NewGame`, rename the project identity, or generate a second Architecture during setup/repair.
- Read `.gamefoundation/onboarding-report.json` after compilation and provider validation. Distinguish local readiness from Figma credentials, CDN, SDK, and device tasks.
- The configured content root is for explicitly selected hot-update content only; preserve existing YooAsset packages, groups, and collection rules. Default UI assets follow the Resources convention below.
- Design import tools are developer-managed. Do not install, replace, or upgrade them during default onboarding. Their absence is not a base-readiness failure; UI and prefab conventions still apply.

## Required feature workflow

- Every behavior-changing requirement must create or update one Feature Spec under `Docs/Features` before implementation.
- Use `.agents/skills/specify-foundation-feature` to classify scope, ownership, acceptance criteria, failure paths, and verification.
- Use `.agents/skills/develop-foundation-feature` for project code and `.agents/skills/normalize-figma-unity-ui` for imported Figma UI.
- Keep the Feature Spec status current and mark it `Verified` only after its acceptance criteria and Definition of Done have evidence.
- Run `python3 Scripts/validate_game_foundation_project.py --project .` before completion. CI applies the same check to pull-request diffs.

## Architecture

- Keep centralized role folders directly under `Assets/Scripts/Game`: `Commands`, `Models`, `Systems`, `Events`, `ViewControllers`, and `Utilities`.
- Use `Docs/Foundation/ProjectLayout.md` to classify model-owned data, stateless algorithms, Unity presentation helpers and Editor tools by responsibility, not by their caller.
- Add responsibility subfolders inside those role folders. Do not create feature folders containing another role tree.
- Project state mutation belongs in QFramework Commands. ViewControllers and UI callbacks send Commands and render state; they do not mutate Models or Systems directly.
- Shared project rules belong in Systems, persistent or observable state in Models, one-shot notifications in Events, Unity presentation in ViewControllers, and stateless bridges in Utilities.
- Register dependencies only in the configured Architecture's `Init`.
- Cache QFramework dependencies during initialization. Runtime callbacks, refresh methods, animations, and analytics helpers must not repeatedly call `GetModel`, `GetSystem`, or `GetUtility`.
- Commands resolve dependencies into local variables at execution setup. After edits, search changed files with `rg "GetModel<|GetSystem<|GetUtility<"` and inspect each remaining lookup.
- Keep scene-bound components stable; use focused partial files when needed instead of adding components merely to split code.
- Gameplay-facing MonoBehaviours should inherit the configured Controller base.

## Foundation boundary

- Foundation contains reusable Core, Save, content-update, SDK lifecycle, UI, setup, and optional provider infrastructure.
- Gameplay, levels, progression, economy, rank, store, IAP, rewards, concrete navigation, player save schemas, analytics event schemas, and vendor policy remain in the project.
- Do not edit `Packages/com.nn555k.gamefoundation.*` for a project-only requirement.
- Do not place vendor credentials, product URLs, production prefabs, or project state inside Foundation packages.

## SDK initialization and attribution

- For SDK initialization, attribution, or identity changes, read `Docs/Foundation/SdkInitialization.md` and update `Docs/Integrations/SdkInitializationChecklist.md` plus the related Feature Spec. Unused vendors do not need installing.
- When combining ThinkingData and Adjust, initialize ThinkingData, obtain its real identity, bind callbacks and synchronize Adjust callback parameters, then initialize Adjust. Explicitly block this dependent path on missing consent, failure, or empty distinct ID; `SdkHost.Order` alone is not a readiness gate.
- Keep a single identity synchronization owner, including login, logout and account switches. Keep vendor implementation and credentials in the project layer.
- Verify client startup, client attribution, and actual Adjust-to-ThinkingData server ingestion separately. Static convention checks or a client diagnostic event cannot prove server postback success. Leave unavailable device/backend checks unverified.

## Unity assets and UI

- Read `Docs/Foundation/ResourceLoading.md` before creating UI or prefabs. Default path-based loading uses `Resources.Load<T>` / `Resources.LoadAsync<T>`, not YooAsset.
- Use `Assets/Prefabs` for direct-reference prefabs, `Assets/Art` for models, meshes, materials, shaders, ordinary textures, fonts and licenses, and `Assets/Sprites` only for Sprite images/assets, including native serialized Sprites. Determine Sprite ownership from Unity import/type; never alter import settings just to pass folder checks.
- Path-loaded UI prefabs use `Assets/Resources/Prefabs/UI`; sprites use `Assets/Resources/Sprites/UI`. Design sources use `Assets/Art/DesignImports`, imported prefab drafts use `Assets/Prefabs/DesignImports`, and extracted Sprite images use `Assets/Sprites/UI`.
- New popups use `Assets/Resources/Prefabs/UI/Popups`, with traceable configurable keys, first-use Resources loading and instance reuse. Do not retain startup hard references to all popups or eagerly load them all. Handle missing resources without blank modals or state changes; clean up on owner destruction.
- Before authorized model deletion, audit reverse references, configuration, Resources strings and Editor source/baker paths. Hidden or optional configured assets are not unused. Keep a recoverable version or backup outside Assets.
- Record the final asset path, Resources key or direct reference owner, and lifecycle in the Feature Spec. A Resources prefab may reference art outside Resources; do not duplicate or move its entire dependency tree.
- Preserve existing resource paths unless migration is explicitly requested. Never automatically replace existing loaders or move Resources into YooAsset collectors.
- Do not edit `.prefab`, `.unity`, or `.asset` files as text. Use Unity Editor APIs, Prefab Mode, or Unity MCP and read back exact values.
- Treat Figma output as raw design data. Normalize naming and hierarchy, preserve stable Figma binding IDs and references, then run the project prefab validator.
- Preserve existing Inspector references and prefab overrides unless the Feature Spec explicitly authorizes structural changes.
- Read `Docs/Foundation/UiAuthoring.md`: existing button Preserve Aspect and Image Type are authored choices; do not normalize them or rewrite static captions outside the requested boundary. Disabled tint, price labels and visual animation endpoints remain project policies.
- Read `Docs/Foundation/UiLayers.md` and fill the project-owned `UiLayers.Project.md`: choose Pages / Panels / Popups / Guides / Effects / Toasts / Blockers by interaction responsibility. One layer owner controls Canvas sorting; Effects/Toasts never intercept input. Specify Replace/Stack, gate owners, lifecycle and failure recovery; gameplay pause/resume stays in project Commands/Systems.

## Completion gate

- Convention validation passes.
- Unity compiles without new errors.
- Focused EditMode/PlayMode tests and exact asset/runtime checks cover the changed boundary.
- Failure, offline, cancellation, lifecycle, persistence, and performance checks are included when relevant.
- The completion report links the Feature Spec and lists changed scripts/assets, affected scenes/prefabs/components, evidence, performance impact, and remaining Inspector or device checks.
<!-- GAME FOUNDATION MANAGED END -->
