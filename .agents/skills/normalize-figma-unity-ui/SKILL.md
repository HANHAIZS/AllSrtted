---
name: normalize-figma-unity-ui
description: Inspect, rename, restructure, bind, and validate Figma-imported Unity uGUI assets into production prefab conventions. Use when a Figma plugin creates frames or prefabs with generated names, wrong roots, Container nodes, missing dim layers, unsafe layout, or when AI must convert a design import into Popup, Page, Hud, Overlay, ListItem, or Component prefabs while preserving synchronization IDs and references.
---

# Normalize Figma Unity UI

Convert disposable design-import structure into a stable production prefab through a previewable Unity Editor workflow.

## Required context

Read repository `AGENTS.md`, `Docs/Foundation/FigmaUiWorkflow.md`, the related `Docs/Features/<feature-id>.md`, `references/prefab-structure.md`, `references/naming.md`, `references/figma-sync.md`, and `references/verification.md`. Also read the installed Unity MCP workflow skill before operating Unity assets.

## Workflow

1. Confirm the real project root, Figma file/frame/node IDs, imported source path, target production prefab path, active scene or Prefab Stage, and the applicable `UiPrefabConventionProfile`.
2. Inspect the actual hierarchy, components, bindings, variants, animation clips, sprites and serialized references. Treat user-provided names as hints until resolved to exact objects.
3. Classify the asset as Popup, Page, Hud, Overlay, ListItem or Component. Stop and ask only when this choice materially changes the structure and cannot be inferred.
4. Use existing imported assets, or the developer's independently installed design tool when download/sync is in scope. Foundation ships no download/sync tool; do not install one implicitly. Pull into a temporary source tree and apply only opted-in visual fields. Never expose the local token in logs or generated assets.
5. Build a semantic rename map keyed by stable Figma node ID where available. Do not invent names for ambiguous artwork.
6. Call `GameFoundation.UI.Editor.FigmaPrefabNormalizer.Preview(prefabPath, profile, kind)` through Unity MCP `execute_code` or use the preview menu. Report destructive-looking moves or reference risks before applying.
7. Apply with `FigmaPrefabNormalizer.Apply` through Unity Editor APIs. Never edit prefab YAML with shell tools.
8. Rename semantic nodes, restore serialized bindings, and attach the project ViewController only after the structural schema is stable. UI callbacks send project Commands rather than mutating state.
9. Run `UiPrefabValidator.Validate`, read back the hierarchy and exact component values, then compile.
10. Verify Safe Area, representative resolutions, text overflow, localization, reopening, sorting, raycast blocking, animation paths and Figma re-sync behavior.

## Default popup invariant

Before mounting normalized content, read `Docs/Foundation/UiLayers.md`, `UiLayers.Project.md` and `UiAuthoring.md`. Prefab kind is not a layer: select the interaction role and use the layer owner to constrain imported Canvas sorting and input. Preserve button Image fitting (both Preserve Aspect values are valid), static captions, unrelated visual fields and prefab overrides; validate the changed structural boundary and actual rendered input.

Before choosing a production path, read `Docs/Foundation/ResourceLoading.md`. Raw design sources go under `Assets/Art/DesignImports`, imported prefab drafts under `Assets/Prefabs/DesignImports`, path-loaded UI prefabs under `Assets/Resources/Prefabs/UI` (popups under `Popups`), and directly referenced prefabs under `Assets/Prefabs/UI`. Extracted Sprite images go under `Assets/Sprites/UI`; materials and ordinary textures belong under `Assets/Art`. A Resources prefab can reference both without copying dependencies. Popups load on first use and reuse their instance. Record the load key or reference owner in the Feature Spec, and verify runtime loading after normalization. Do not migrate existing assets without explicit scope.

The direct root structure is `DimBackground` and `PopupContent`, plus only the optional roots declared by the active profile. Move body and script-bound nodes under `PopupContent`. `DimBackground` is full stretch, black, uses the configured alpha, and has raycast target enabled. Never retain `Container` as a permanent alias.

## Safety boundary

Do not mass-normalize a directory without per-prefab preview. Do not change scenes, prefab variants, animation paths or generated source assets outside the explicitly confirmed target set. Preserve all stable sync identifiers even when display names change.

## Completion report

List every prefab changed, its old and new root shape, renamed/moved/created nodes, affected components or variants, validation and screenshot results, and Inspector references that require manual confirmation.
