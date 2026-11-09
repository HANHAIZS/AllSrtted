---
name: develop-foundation-feature
description: Implement or revise Unity project features that consume the local Game Foundation packages and QFramework architecture. Use for new screens, popups, flows, settings, save-backed state, SDK-triggered behavior, hot-update UI, commands, models, systems, or view controllers in a consuming game project; do not use to change the shared com.nn555k.gamefoundation packages themselves.
---

# Develop Foundation Feature

Build project-owned features on the shared infrastructure without moving product logic into Foundation.

## Required context

Read repository `AGENTS.md`, `.gamefoundation/project.json`, `Docs/Foundation/Architecture.md`, the related `Docs/Features/<feature-id>.md`, and the project architecture entry before editing. Read `references/capability-routing.md` for module ownership and `references/verification.md` before choosing tests. For folder and QFramework patterns, read `Docs/Foundation/ProjectLayout.md` and `references/project-architecture.md`. Before deleting unused models, audit runtime references, string paths and Editor generation inputs; keep recovery outside Assets.

## Workflow

1. Confirm the Unity project root, active architecture, related Feature Spec, relevant scenes or prefabs, and whether the task changes runtime code, editor tooling, or assets.
2. If a behavior-changing request has no Feature Spec, use `$specify-foundation-feature` to create one before implementation. Formatting-only edits and evidence-only diagnostics do not require a new spec.
3. Set the Feature Spec status to `Implementing`, then classify every new class by responsibility: Command, Model, System, Event, ViewController, Utility, or vendor adapter. Keep the class in the project's centralized role folder.
4. Reuse Foundation contracts for cross-project infrastructure. Keep gameplay, progression, economy, rank, shop, IAP, page-specific state, and vendor policy in the project layer.
5. Register dependencies only in the project's `Architecture<T>.Init`. Cache QFramework dependencies during initialization; commands may resolve into local variables at execution start.
6. Route state mutation through Commands. Controllers and UI callbacks only collect input, send commands, and render observable state or events.
7. Preserve Inspector references and existing prefab hierarchy unless the request explicitly authorizes structure changes. For Figma imports, switch to `$normalize-figma-unity-ui`.
8. Implement the smallest coherent change and add focused tests around the changed boundary. Keep the Feature Spec architecture and acceptance sections synchronized with material decisions.
9. Set status to `Verifying`, run `python3 Scripts/validate_game_foundation_project.py --project .`, recompile Unity, inspect errors, and validate the exact runtime or asset behavior affected.
10. Mark the spec `Verified` only after its acceptance criteria and Definition of Done have evidence. Leave failed or unavailable checks unchecked and report them.

## Decision rule

For SDK initialization, attribution or identity work, read `Docs/Foundation/SdkInitialization.md` and update `Docs/Integrations/SdkInitializationChecklist.md`. If the project uses ThinkingData with Adjust, implement explicit readiness/identity gates before Adjust initialization, test failure and identity changes, and distinguish client evidence from real server postback ingestion. Do not install unused vendors or treat ordering alone as readiness.

For UI/prefab/art work, read `Docs/Foundation/ResourceLoading.md`. Use Resources for path loading; put its targets under `Assets/Resources`. Direct-reference prefabs belong under `Assets/Prefabs`, non-Sprite art under `Assets/Art`, and only Sprite images/assets under `Assets/Sprites`. New popups use `Assets/Resources/Prefabs/UI/Popups`, first-use loading, cached instances and explicit failure handling; do not preload all popups from the startup HUD. Record paths, load keys/reference owners and lifecycle in the Feature Spec before creation. Preserve existing loaders and paths; do not auto-migrate them to YooAsset.

Change `Packages/com.nn555k.gamefoundation.*` only when the requirement is demonstrably generic across projects and the task explicitly includes evolving the shared framework. When that is true, switch to `$maintain-game-foundation` and validate compatibility separately.

For UI mounting and authoring, follow `Docs/Foundation/UiLayers.md`, the project adoption in `UiLayers.Project.md` and `UiAuthoring.md`. Use the layer owner's sorting/input boundary, keep failure from disturbing the visible modal stack, and preserve unrelated Inspector values. Enter Play Mode for changed runtime boundaries; documentation-only edits do not require a runtime run.

## Completion report

Link the Feature Spec and report changed scripts and assets, affected scenes/prefabs/components, why the ownership is correct, convention/compile/test evidence, performance considerations, and any Inspector fields that still require confirmation.
