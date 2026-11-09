---
id: single-level-sort-prototype
title: Single Level Primitive Sorting Prototype
status: Verified
owner: Project
created: 2026-10-26
updated: 2026-10-26
---

# Single Level Primitive Sorting Prototype

## Goal

Implement the user's one-level core sorting game in All Sorted!, using the installed Foundation/QFramework architecture, Unity primitives and original project-authored layout. No extracted Sort Express assets or configuration are permitted.

## User-visible behavior

- Six shelves with three slots each; 24 pieces across four colored primitive types; 180 seconds; eight matching triples to win.
- Drag the front piece to an empty front slot. Invalid release or cancellation returns it without mutation. A committed removal leaves a vacant front; the next layer advances only after all three front positions of that shelf are empty. See shelf-row-refill.md for the superseding rule and current evidence.
- Three equal front pieces on one shelf disappear atomically. Newly revealed triples settle until stable. Background/focus loss pauses and cancels drag.
- Pause/resume, timeout, win and replay form a complete offline loop. Mac mouse and iOS single-touch inputs are supported.
- Video reference: 18–20s matching. The former per-slot interpretation at 26–29s was superseded by the user’s explicit whole-row refill correction in shelf-row-refill.md. Locked shelves and other advanced video mechanisms are excluded.

## Out of scope

Original game assets/configuration; more levels; procedural generation; obstacles; boosters; lives; economy; shop; advertising; IAP; account; live services. Device signing and actual device performance remain separately reported checks.

## Ownership and architecture

| Responsibility | Owner | Planned type/path | Notes |
| --- | --- | --- | --- |
| State mutation | Command | Commands/Sorting/SortCommands.cs | Start, move, pause and tick commands |
| Observable or persistent state | Model | Models/Sorting/SortBoardModel.cs and SortBoardState.cs | Local game only; no save schema |
| Shared project rules | System | Systems/Sorting/SortGameSystem.cs | Owns deterministic game orchestration |
| One-shot notification | None | None | Revision and phase properties are sufficient |
| Unity presentation | ViewController | ViewControllers/Sorting/SortGameViewController*.cs, SortUiLayers.cs, SortPopupView.cs | All gameplay-facing behaviours use AllSortedController |
| Adapter or repository | Utility | Utilities/Sorting/PrototypeLevel.cs | Project-authored initial data only |
| Reusable infrastructure | Foundation | Core, UI and generated module registrations | Shared packages are unchanged |

## State and persistence

- Runtime state: per-slot stacks of uniquely identified pieces, phase, remaining time, match count and revision.
- Save schema/version/migration: None. Restart/relaunch begins the single level again.
- State mutation path: Controller -> QFramework Command -> SortGameSystem -> SortBoardModel. Pure board operations are model-owned and tested independently.
- Registration exists only in AllSortedApp.Init; controllers cache dependencies during Awake.

## UI, prefab, scene, and Inspector impact

- Affected assets: new Assets/Scenes/AllSortedPrototype.unity, Assets/Art/Materials/Sorting, Assets/Resources/Prefabs/UI/Popups/PausePopup.prefab and ResultPopup.prefab.
- Loading mode and final paths: scene-owned board/HUD direct references; popup Resources keys Prefabs/UI/Popups/PausePopup and Prefabs/UI/Popups/ResultPopup.
- Resources keys or direct reference owners: SortGameViewController owns scene camera, material palette, UI layer root, popup keys and HUD references.
- Load/cache/instance cleanup responsibility: controller lazily loads, caches and unregisters popup views, clears router and destroys owned instances when destroyed.
- UI layer contract/adoption: Docs/Foundation/UiLayers.md / UiLayers.Project.md; seven registered hosts, Pages HUD, Popups modal, non-interactive Effects/Toasts.
- Semantic layer and actual host: PrototypeUI/{Pages,Panels,Popups,Guides,Effects,Toasts,Blockers}.
- Modal open/return behavior: Replace via Foundation UiPopupRouter. Stack/Queue not used by this feature.
- Input gate owner/release paths and non-uGUI input routing: SortUiLayers gates lower layers; controller checks modal ownership and phase before world dragging. Close, reset and destroy release owned gates.
- Protected authored properties and allowed changes: only newly created scene/material/UI assets; unrelated template scene and Inspector values are preserved.
- Popup first-use/reuse/missing-resource recovery and startup residency: no popup hard reference or eager load; a failed manual pause leaves the game playable; mandatory suspension always cancels input and freezes time, with a HUD Resume fallback. A missing result key leaves a HUD Retry action and retries at most once per second, logging once per failed key and retrying immediately on a changed key.
- Model deletion dependency audit (runtime, configuration, Editor inputs) and recovery: no original model deletion.
- Hierarchy or binding changes: new scene generated through Editor APIs, selected as enabled build scene; existing SampleScene retained.
- Inspector confirmation: read back camera, palette, modal keys, seven hosts and HUD bindings after generation.

## Integrations and failure behavior

- SDK/consent/lifecycle: Foundation safe defaults only; no new vendor or network flow.
- SDK versions, initialization dependencies, identity source/sync owner: None.
- SDK prerequisite failure gate and local/device/server evidence: None.
- Content update/offline/rollback: installed Foundation tooling retained; gameplay uses no hot-update content or CDN.
- Cancellation, timeout, retry: drag cancellation is non-mutating; only playing ticks; terminal states are final until a start command; reset invalidates presentation callbacks.

## Performance considerations

- Cache components, QFramework services, piece views and UI references. No scene-wide searches in Update. Piece reconciliation occurs on board revision, not each frame.
- At most 24 piece instances plus transient match effects; shared materials; no original atlases or full export directory loaded.
- Only pointer interactions perform world raycasts. UI/camera resize work runs only when dimensions change.

## Acceptance criteria

- [x] Initial layout contains 24 pieces, six per type, eight total triples and a verified solution.
- [x] Legal/illegal movement, whole-row refill, repeated matching, conservation, pause, deadline, terminal state and reset pass focused tests.
- [x] One playable scene provides HUD, pause/resume, win/loss and replay with native primitives only.
- [x] Popup loading, reuse, modal raycast/world-input blocking and teardown have runtime evidence.
- [x] No extracted Sort Express art/config references enter the target project.

## Verification matrix

| Boundary | Evidence required | Result |
| --- | --- | --- |
| Project conventions | validate_game_foundation_project.py | Passed, 0 warnings on 2026-10-26 |
| Unity compilation | build log and no new compiler errors | Passed official onboarding, scene builder, final EditMode and PlayMode launches |
| Focused tests | Rules EditMode + scene PlayMode | 22/22 EditMode and 10/10 PlayMode passed; TestResults/sorting-editmode.xml and sorting-playmode.xml, 2026-10-26: EditMode 13:23 CST / PlayMode 13:20 CST |
| Runtime or asset behavior | generated scene readback and rendered screenshot | Saved scene Inspector/dependency readback passed; actual camera captures prototype-gameplay.png (720×1280), prototype-ipad.png (768×1024) and prototype-win.png inspected |
| UI layers and authoring | seven hosts, modal gates, lazy reuse, lifecycle | Passed seven-host readback, installed Foundation popup validator, pause reuse, world gate, hidden callbacks, missing resource recovery and forced UI-first teardown |
| Performance | allocation/lookup review; Editor capture | Source review confirms cached dependencies/materials and revision-only reconciliation. No allocation or device frame-time measurement claimed |

## Definition of Done

- [x] Ownership and QFramework roles match the implemented files.
- [x] Public behavior and acceptance criteria are implemented.
- [x] Relevant UI, scene and Inspector impacts are handled; save/SDK/live updates remain unchanged.
- [x] Convention validation and Unity compilation pass.
- [x] Focused tests and exact behavior checks pass.
- [x] Performance risks and manual platform checks are reported.
- [x] Verified status is used only for checks that actually passed; actual iOS verification remains explicitly separate.

## Decision log

- 2026-10-26: User narrowed scope to one simple level, primitives only, independent data, and required Foundation architecture. This supersedes the previous 10-level/original-resource proposal.
- 2026-10-26: Video corrected the earlier whole-layer reveal assumption to per-slot reveal after committed removal.
- 2026-10-26: Target directory is not a Git repository; work stays in the user's requested live project, with pre-onboarding manifest/lock backups in task work/.


## Final evidence and review

- Verified scope: this one-level Mac Editor prototype. This status does not certify iOS builds, signing, real touch delivery, device safe areas or device performance.
- Foundation version: v0.7.0. Official onboarding setup/verify completed and generated localReady=true. No shared Foundation package modifications.
- Rule evidence: original layout has a tested 12-move solution; final-move-before-expiry and expiry-before-final-move both preserve exactly one terminal outcome.
- Runtime evidence: actual authored scene loaded for all ten tests; off-board/occupied/canceled moves preserve slots and revision, pause during drag cancels input, timeout/win replay returns all 24 pieces.
- Independent final code review returned three P2 findings: destroyed Unity wrappers retained by the interface router, missing result resource blocking replay, and suspension depending on pause resource success. All three were fixed and the corresponding scene regressions pass.
- Teardown regression explicitly destroys PrototypeUI and its active popup before the controller, confirms ActiveView=null, then unloads the scene.
- Project content audit: scene dependency closure permits only native/package assets and project-owned scripts/materials; both popup dependencies use built-in UI/font assets and the project controller. No extracted game art/config paths appear in the feature source.
- Actual device verification remains pending. Current selected developer directory is /Library/Developer/CommandLineTools; full Xcode/iOS toolchain, signing and connected devices were not set up in this task.


## Presentation update — pause button (2026-10-26)

- User requested only the pause button artwork replacement, with all other content preserved.
- Source: /Users/zhangshuai/Documents/欧卡风格/关卡界面之暂停按钮.png. Its checkerboard was baked into an opaque PNG. Built-in imagegen removed the checkerboard to make an alpha-transparent Sprite; no original-game art was imported.
- Final direct-reference Sprite: Assets/Sprites/UI/PauseGear.png. The existing scene-owned Pause Image references the cropped Sprite; the obsolete II caption is blank. Image tint is white and type Simple. The original transform, 64×64 hit area, fitting flag, Button transitions and pause command path are preserved.
- Scene readback: position (-36,-167), size (64,64), 231 unrelated component serializations unchanged. Actual scene starts with 24 pieces, Pause click enters Paused with modal input gate, and Continue resumes Playing. TestResults/pause-artwork-verification.txt records PASS; actual camera screenshot prototype-pause-gear.png inspected.
- Existing scene builder retains this selected artwork on future explicit rebuilds. The saved-asset dependency audit admits this one authorized Sprite path. No runtime logic changed.
- Built-in imagegen prompt: remove only the baked gray/white checkerboard, retain purple button/cream gear, outlines, highlights, shadows, colors, proportions and internal details, output true alpha transparency without new elements.


## Presentation update — level background (2026-10-26)

- User requests the supplied level background PNG. Scope is background only; shelves, pieces, HUD, pause gear and gameplay are preserved.
- Direct-reference Sprite path: Assets/Sprites/Backgrounds/LevelBackground.png. Copy the original image byte-for-byte; no generation or pixel edits.
- Scene-owned SpriteRenderer behind the board, referenced by the existing controller, scales proportionally to cover the viewport on camera resize. The image is centered and may crop at the edges on different aspect ratios. It has no collider or raycast input.
- Verified: original source/destination SHA-256 hashes match. Saved Sprite reference and z=4 backdrop depth read back. 233 existing component serializations match before/after, excluding only the authorized Background reference. Actual scene runs with 24 pieces; viewport cover passes at 720×1280 and 768×1024; drag input and pause/resume pass. Both screenshots inspected. TestResults/background-artwork-verification.txt records PASS.
- Performance scope: one scene-owned SpriteRenderer and one directly referenced texture; fitting runs on existing camera resize/start paths. No per-frame background asset lookup, extra collider or UI input blocker. No device performance claim.
- Unity compiled without new compiler errors; project convention validation passes. Temporary import/probe source removed after verification.


## Presentation update — simplified HUD and four bottom button bases (2026-10-26)

- User requests old upper/lower decorative text removed, LEVEL 01 at top left, countdown restored at top center per follow-up, pause gear at top right, supplied bottom toolbar background, and four evenly spaced supplied green/gold button bases using the third image only as a placement reference.
- Hide the legacy title, slogans, match count, progress track and lower instructions; preserve the visible countdown between the two corner controls. Retain their serialized controller references so existing gameplay can keep updating internally. The 180-second rule and all gameplay remain unchanged.
- Direct-reference sprites: Assets/Sprites/UI/BottomToolbarBackground.png (original copy) and BottomFunctionButton.png (transparent cutout of the supplied opaque checkerboard source). Four bases are visual controls only; no booster, commerce, icon, price or purchase flow is requested.
- Top controls and bottom button row use the existing SafeArea. Toolbar background bleeds to the screen bottom. Buttons overlap the toolbar upper trim, at horizontal anchors 0.125, 0.375, 0.625, 0.875.
- Verified saved and live HUD: only LEVEL 01 and countdown text visible; timer anchored at top center, level at top left and pause at top right. Exactly four bases at the specified anchors. Both 720×1280 and 768×1024 actual-camera captures inspected with no overlap. Drag, pause and resume pass with 24 pieces. 150 non-UI component serializations and the runtime controller source are unchanged. TestResults/toolbar-artwork-verification.txt records PASS.
- Toolbar source copied byte-for-byte. Built-in imagegen removed only the baked checkerboard from the button-base source; prompt locks the green gradient, gold frame, rounded shape and highlights, and forbids icons, text, prices or symbols. Final saved asset is Assets/Sprites/UI/BottomFunctionButton.png.
- Only Editor authoring source, saved HUD and selected art changed; no per-frame layout code, gameplay command or purchase implementation added. Existing timer/score/progress bindings remain valid while non-requested displays are inactive.
- Project conventions pass with zero warnings. Temporary importer and rendering probe removed after live verification. Actual device safe-area checks remain unverified.


## Scope extension — two additional levels (2026-10-26)

The user subsequently requested two more levels. Current three-level behavior and final evidence are governed by Docs/Features/three-level-progression.md. The original first-level layout and all selected UI artwork are preserved.

## Whole-row refill correction — 2026-10-26

The user explicitly changed all three levels to wait for an entirely vacant shelf front before promoting hidden stock. [shelf-row-refill.md](shelf-row-refill.md) supersedes the earlier individual-slot rule and its solution fixtures. Content and art remain unchanged; solutions are now 14/16/16 moves.
