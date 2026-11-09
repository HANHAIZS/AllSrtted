# Figma Sync Contract

Inspect the independently installed tool's components to identify the imported file/frame source and stable node IDs. Preserve those components, IDs and references during normalization; Foundation does not ship or require any particular sync component.

Apply these conservative defaults only where the installed tool supports them:

- Sprite and visual color are normal sync targets.
- Text style and color may sync, but localized runtime text content should remain project-owned.
- Size, position and active state are opt-in because production layout may differ from the design frame.

Use two conceptual layers:

1. Raw import: safe to refresh or replace from Figma.
2. Production prefab: stable schema, semantic names, scripts, localization and runtime references.

When the plugin cannot update production prefabs without overwriting project structure, refresh the raw import and perform a reviewed merge. Do not make generated node display names the persistent identity.
