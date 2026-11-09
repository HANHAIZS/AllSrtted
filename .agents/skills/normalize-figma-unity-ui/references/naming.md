# Semantic Naming

Use PascalCase names that describe runtime meaning, not appearance alone.

## Stable mappings

- Prefab: `SettingsPopup.prefab`
- Root: `SettingsPopup`
- Controller: `SettingsPopupViewController`
- Major nodes: `Header`, `TitleText`, `CloseButton`, `Body`, `MusicToggle`, `ConfirmButton`
- Repeated template: `RewardItem`, with instances named only when their role is fixed

Remove generator names such as `Frame 123`, `Group_42`, `Rectangle 18`, `Vector`, `Image 5`, or copied numeric suffixes only when semantics are known.

Avoid encoding color, coordinates, or transient copy into names. Prefer `DimBackground` to `BlackRect`, `ConfirmButton` to `GreenButton`, and `CurrencyIcon` to `Image7`.

Keep a mapping from Figma node ID to the chosen semantic Unity name. A later import may change display names or hierarchy but should still resolve the same source identity.

Before renaming, search AnimationClip transform paths, component string paths, code `Find` calls, prefab variants, and serialized object references. If a reference cannot be proven safe, report it instead of guessing.
