# Prefab Structure

The active `UiPrefabConventionProfile` is authoritative. Defaults are:

| Kind | File/root suffix | Required content root | Dim |
| --- | --- | --- | --- |
| Popup | `Popup` | `PopupContent` | Required |
| Page | `Page` | `Content` | No |
| Hud | `Hud` | `Content` | No |
| Overlay | `Overlay` | `Content` | No |
| ListItem | `Item` | None | No |
| Component | None | None | No |

File name and root GameObject name must match. Required and optional structural nodes are direct children of the root. Repeated content belongs under the content root, not beside it.

Projects may declare direct optional roots in their own profile. The default dim layer is full screen with anchors `(0,0)` to `(1,1)`, zero offsets, black color, alpha `0.97`, and raycast target enabled.

The normalizer may create required structural nodes, rename direct `Container` to `PopupContent`, and move unknown direct children into the content root when the profile permits. It does not infer business bindings or redesign layouts.
