---
id: pause-settings-popup
title: Illustrated pause settings popup
status: Verified
owner: Project
created: 2026-10-27
updated: 2026-10-27
---

# Pause settings popup

## Goal

Restyle the existing level gear/pause popup with the supplied purple/gold background, top ribbon and Paused lettering, three embedded Music/Sound/Haptic buttons, red Quit, orange Restart and matching red close artwork found alongside the supplied files. Follow the supplied overall reference proportions.

Subsequent integration: [home-screen.md](home-screen.md) supersedes the temporary inert-Quit policy below. Quit now returns to HomeScene, preserves current level/session allowances/preferences, and retains the original modal on missing-scene failure. Close/Restart and all illustrated popup assets remain. The earlier no-op verification records the original feature delivery; HomeNavigationPlayModeTests covers the new Quit flow.

## User-visible behavior

- Gear opens the existing modal and pauses the board and freeze clock. Close X resumes the same board. Underlying shelves/boosters remain blocked while open.
- Music, Sound and Haptic buttons independently toggle ON/OFF. Per the user's later reference, OFF shows a rounded dim layer over the button face and a red diagonal from top-left to bottom-right, without darkening the purple surrounding row. Default ON; retain in-memory settings across close/reopen, restart, level and scene changes; new application defaults ON. No disk save. There are currently no audio clips/playback or vibration calls in the project: this scope supplies real toggle state for later consumers, without inventing media or SDKs.
- Quit intentionally does nothing apart from press feedback, retaining the popup, level, timer and allowances; the main screen is not built.
- Restart closes the popup and resets the current level, including its clock, board and temporary booster effects. Spent/added booster allowances remain unchanged by existing session policy. It never restores spent uses.
- Main/background/header/captions use the actual transparent source PNGs, without generated redraws. Slice the combined settings-row Sprite metadata into static strips and three button pieces so each embedded button has its own input and press feedback.

## Ownership and architecture

| Responsibility | Owner | Path |
| --- | --- | --- |
| Three session preference values/revision | Model | Models/Settings/SortSettingsModel.cs |
| Toggle mutation | Command | Commands/Settings/ToggleSortSettingCommand.cs |
| Existing pause/resume/restart | Existing System and Commands | SortGameViewController.UI.cs; SortGameSystem; existing Commands |
| Toggle input/rendering/listeners | ViewController | ViewControllers/Sorting/SortPauseSettingsView.cs |
| Registration | Existing architecture | Architecture/AllSortedApp.cs |
| Prefab/art authoring | Editor | Editor/Sorting/PausePopupArtwork.cs; existing PrototypeSceneBuilder rebuild hook |

No second architecture, gameplay change, new level, economy, sound/haptic content, SDK, Foundation package edit or main-screen navigation.

## Out of scope

Main-screen navigation, audio or vibration media/SDK integration, permanent preferences, new gameplay mechanisms and changes to existing booster allowances.

## Assets, lifetime, and UI ownership

- Preserve Resources key Prefabs/UI/Popups/PausePopup and original prefab GUID. Author Assets/Resources/Prefabs/UI/Popups/PausePopup.prefab through PrefabUtility/Unity Editor APIs only.
- Direct-reference Sprite images under Assets/Sprites/UI/PauseSettings/: Background, Header, PausedText, SettingsRow, Quit, Restart, Close. Source files are /Users/zhangshuai/Documents/欧卡风格/暂停界面之*.png. They already have real alpha. SettingsRow contains native multiple Sprite regions; pixel data unchanged.
- Retain Foundation Replace modal routing, lazy first-use Resources load and cached instance reuse. Hosts are existing Popups under SortUiLayers, sorting order 200. DimBackground intercepts input; artwork does not. No new Canvas, popup loader, or eager hard reference from the HUD.
- Existing SortPopupView.Primary becomes the close X; Secondary remains restart; keep caption/body bindings for compatibility but hide pause-only old text. ResultPopup remains unchanged. Keep required root DimBackground/PopupContent structural names/profile.
- New settings controller caches Model/router once in Awake, subscribes/binds once, renders on revision/enable, unregisters on destroy and rejects hidden/stale/non-top callbacks. Reuse SortButtonPressFeedback for close, quit, restart and toggles.
- Layout target: centered 620x822 backdrop with PopupContent uniformly scaled to 0.85 per the later compact-size request (effective backdrop 527x698.7 in reference units); ribbon above its top, embedded row below ribbon, Quit then Restart. The full-screen dim/input blocker remains unscaled. Fit portrait phone and iPad through existing host scaler; no changes to level HUD, base buttons or camera.

## Failure and recovery

Prepare the lazy prefab and all settings bindings before pausing/gating. Missing/invalid popup must not pause invisibly or block input. Reopen reuses one instance without duplicate listeners. Close/restart clear gates through existing owner. Destruction releases listeners and model observer; UI-first teardown remains safe. Quit must remain a deliberate no-op, including repeated invocations.

## Performance

Seven shared Sprite textures, settings row regions share one texture; no per-frame texture work or dependency lookup. Three bools and revision; event-driven UI. Small existing press animations use unscaled time. Local runtime/visual checks do not certify physical-device audio, haptics or performance.

## Acceptance criteria

- [x] Real gear opens an illustrated, correctly proportioned modal, clock/freeze/input gated.
- [x] Three independent toggles change state once, show OFF clearly, retain state across reopen/restart/scene reload, hidden callbacks ignored.
- [x] Close resumes; Quit leaves everything unchanged; Restart resets current level without refunding any booster.
- [x] Existing popup key/GUID, lazy reuse, result popup, HUD and all three level rules remain.
- [x] Phone/iPad renders, saved-prefab readbacks/profile and focused/full Unity regressions pass; project validator passes.
- [x] Final sources/assets/evidence and any manual/device limits recorded, temporary tooling cleaned.

## Verification matrix

| Boundary | Evidence | Result |
| --- | --- | --- |
| Settings Model/Command | Six new EditMode cases: independent toggles, revision, invalid input, session retention, board/quota untouched | Supplemental 75/75; full Unity EditMode 80/80 |
| Actual pause popup | Three new integration cases: artwork, pause/close, pointer hit, reuse, hidden input, Quit and Restart without refunds | Red: 3 expected missing-view failures; final full Unity PlayMode 28/28 |
| Assets/layout | Original PNG hashes, prefab profile/GUID, phone/iPad and OFF captures | Passed; all seven PNGs byte-identical; GUID 744dc3a36c91c4d1c856a83918ca9171 preserved |
| Regression | Full EditMode/PlayMode, existing lazy popup/teardown/missing-resource/three-level/booster cases and validator | 80 + 28 passed; validator zero warnings; independent review no actionable findings |
| Physical devices/audio/haptics | No media/integration in project, no device access | Not claimed |

## Definition of Done

Required local criteria have fresh evidence; project architecture and UI conventions preserved; no package edits; no device/audio/haptic claim beyond implemented state. Status Verified only after these checks.

## Decision log

- 2026-10-27: User explicitly requests Quit no-op and current-level restart with no booster refunds. Reuse existing quota model.
- 2026-10-27: Existing close art is present in the same user-supplied source folder. Use it for the reference's X control. Session-only toggle state is the smallest behavior for three settings while no playback/haptic content exists.
- 2026-10-27: User supplied the matching Close PNG explicitly during implementation; this is the same source used for X.
- 2026-10-27: User provided an OFF-state reference showing dim green buttons with red top-left-to-bottom-right strokes, and requested a smaller overall popup. Use native uGUI rounded dim Images and red rectangular strokes, scale PopupContent to 0.85, retain full-screen DimBackground and all actions.
- 2026-10-27: User emphasized reference layout and X placement. Align the native Close image 40 units inside the right anchor and 70 below the top, rect 102x122; adjust ribbon/label, settings row and Quit/Restart spacing to the reference's relative proportions. Overall content scale remains 0.85.

## Final evidence, boundaries and limits

- Evidence and previews: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/all-sorted-prototype/pause-settings/{verification.json,art-manifest.json,phone.png,ipad.png,off.png}. Supplemental state/command TDD logs: workspace work/pause-settings-model; baseline backups: work/pause-settings-backup.
- Unity MCP authored the prefab and Sprite import metadata. Primary/Secondary stable references now represent Close/Restart; Quit has no navigation listener. Added seven direct Sprite sources and one settings component in the existing cached popup. Row slices share the original texture and allow independent press/rebound without duplicate art beneath the interactive pieces.
- Source files changed: AllSortedApp.cs; new SortSettingsModel.cs and ToggleSortSettingCommand.cs; new SortPauseSettingsView.cs and a pre-open bindings guard in SortGameViewController.UI.cs; new PausePopupArtwork.cs plus existing scene-builder rebuild hook. Tests: six EditMode settings cases, three PlayMode popup cases, existing asset whitelist extended only for PauseSettings sprites.
- Saved GameScene, ResultPopup and existing sorting/booster model/system hashes remain identical. Seven source PNG hashes match imported originals; no generated art, image redrawing, Foundation/package modification, new Resources key or gameplay policy was introduced.
- Actual pointer raycast hits SettingButton_2 ahead of DimBackground. OFF uses a rounded dim Image and native red backslash stroke matching the latest reference; original Text-based geometry validation is superseded. A same-frame UI layout query and a stale compiled-test cache were resolved by waiting for layout and force-refreshing before rerunning; final full suites pass. One intermediate full run failed an existing second world-drag assertion after a realtime wait; it did not reproduce in the final unchanged full suite, and its root cause is not established. No gameplay change was made. Details are retained in verification.json rather than hiding intermediate results.
- Final checks include quitting without board/clock/quota mutation, close/resume/gate release, current-level restart with 2/3 spent allowances remaining, freeze reset, setting retention across scene reload, lazy reuse and teardown. No temporary test runner source or pending flag was added.
- Music/Sound/Haptic are real independent session preference switches with visible OFF marks. Actual audio playback/haptic content is absent and outside this feature; no physical iPhone/iPad, native vibration, audio mixer or device performance claim is made. Runtime adds three bools, a revision observer and event-driven presentation; no per-frame preference scanning or texture work.

- Final reference alignment verified on 720x1280 and 768x1024 renders. Compact content scale 0.85; Close anchor (1,1), position (-40,-70), size (102,122), overlapping the upper right panel border. Final Unity runs: EditMode bd90f74b1a984e1daea2d424101b62cc (80/80), PlayMode 9d2796a1235949f0b262b2e97c3a4fe1 (28/28); validator zero warnings.
