# AllSorted UI layer adoption

Adopt the contract in [UiLayers.md](UiLayers.md) when creating production UI. This file is project-owned and preserved by default during Setup repair. Setup installs guidance; it does not create or validate production UI hosts.

## Single-level prototype adoption

- Domain/lifetime: GameScene; SortGameViewController owns its scene-local UI and popup cache.
- Hosts: PrototypeUI/Pages, Panels, Popups, Guides, Effects, Toasts, Blockers; orders 0,100,200,300,400,500,600.
- SortUiLayers owns host Canvases and CanvasGroups. All hosts use the same Screen Space Camera; Pages uses a SafeArea subtree and Foundation UiSafeAreaFitter.
- Foundation UiPopupRouter Replace is used for pause and terminal result; Stack and Queue are not used. Popups lazily load declared Resources keys and are hidden for reuse, never eagerly referenced by HUD.
- This feature has one modal owner, gates Pages/Panels, and exposes GameplayBlocked for world pointer input. Effects/Toasts are always non-interactive. No interactive guide or blocker is implemented by this scope.
- Pause/resume/start/tick remain Commands and SortGameSystem policy, not Foundation navigation policy.
- Popup creation fails before changing state/gates; missing keys are logged; restoring a key permits retry. Scene destruction unregisters owned views and clears their cache/gate.
- Prefab text, button Image.Type, Preserve Aspect and bindings are authored by the project scene builder. Runtime only updates declared dynamic result text.
- Verification: PrototypeAssetTests and PrototypePlayModeTests; actual runtime rendering and device checks are recorded in Docs/Features/single-level-sort-prototype.md.
- Illustrated pause settings retain the same lazy PausePopup key and Popups host. SortPauseSettingsView owns only its toggle listeners and settings-model observer; existing close/resume/restart remains with the scene controller. Quit is intentionally inert. Original settings-row Sprite partitions provide independent pressable targets without a new Canvas. See Docs/Features/pause-settings-popup.md for session state, gate ownership and verification.

Original adoption checklist retained for future UI features:

## HomeScene three-page adoption

- HomeScene owns one camera/EventSystem and seven SortUiLayers hosts with the same 0/100/200/300/400/500/600 orders. Shop/Home/Setting page CanvasGroups live in Pages; a shared navigation bar and its safe-area subtree live in Panels.
- SortHomeViewController owns the three directly referenced page instances for the scene lifetime. Tab changes reuse them; no popup or gameplay scene is eagerly instantiated. No Foundation package edits or second architecture were introduced.
- Selected tab and one-shot game entry belong to SortHomeNavigationModel, mutated through Commands. Per-frame easing positions/alpha remain presentation-only. During transitions every page's input is closed; Panels navigation remains responsive, with the last target winning. Once settled only that page accepts input. Decorative selector/rails/dividers never intercept raycasts.
- Main-menu settings and the existing lazy pause popup share SortSettingsModel/ToggleSortSettingCommand. Hidden and transitional callbacks are rejected; listeners/observers are owned and removed on scene teardown.
- Start preflights GameScene before writing an entry intent. GameScene consumes it once, retaining editor-preview priority and direct-entry default1. Pause Quit now preflights HomeScene and returns to it; missing scene leaves the pause popup and gate intact. The prior temporary inert-Quit policy is superseded by Docs/Features/home-screen.md.
- Sprite metadata, including stable native slice identifiers, and all scene writes are authored through Unity Editor APIs. Two vertical dividers remain at the one-third boundaries while the composed selector/upper trim moves above them.
- Runtime/asset evidence, device limits and performance boundaries are tracked by Docs/Features/home-screen.md.

- UI domain, root asset/scene and lifetime owner.
- Actual hosts for Pages / Panels / Popups / Guides / Effects / Toasts / Blockers; default orders 0 / 100 / 200 / 300 / 400 / 500 / 600, or the centrally configured ordered equivalent.
- Layer mounting and Canvas/sorting owner; camera/rendering-domain choices, Safe Area and page scalers.
- Popup loader/cache owner, Replace/Stack interfaces and unsupported modes; top-window close/back behavior.
- Owner-scoped uGUI gates, release paths and routing for keyboard/gamepad/back/world input.
- Project Commands/Systems that decide gameplay pause/resume and navigation cleanup.
- Exact layer/asset/runtime verification entry points and recorded evidence.

Do not claim these bindings or runtime checks are complete until the project implements and verifies them. Keep product-specific buttons, prices, page geometry and business state out of the portable contract.

## Home prelevel modal

Home owns one lazy Prefabs/UI/Popups/PrelevelPopup instance in Popups host, UiPopupRouter Replace. SortUiLayers gates Pages/Panels; host remains scene-owned. Close hides/reuses, scene teardown unregisters even destroyed Unity wrappers, missing/invalid prefabs leave menu usable. See Docs/Features/prelevel-boosters.md for verification and exact asset/state policy.

## Popup visual motion

SortPopupView owns unscaled .25s/.15s panel/backdrop presentation under the same Popups host. Logical IsOpen/router notifications remain immediate; closing visuals no longer intercept input or emit late notifications. Replacement hands off backdrop opacity and places new modal last; owner SortUiLayers still owns page/panel business gates. Motion captures authored endpoints once, restores on disable and does not edit native assets. See Docs/Features/popup-motion.md for runtime tests and native recording.
