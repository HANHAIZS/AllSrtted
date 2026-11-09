---
id: editor-level-selector
title: Select any of the three levels from the Unity Editor menu
status: Verified
owner: Project
created: 2026-10-26
updated: 2026-10-26
---

# Editor level selector

## Goal

Provide Unity menu entries All Sorted > 关卡选择 > 第 1 关 / 第 2 关 / 第 3 关.

## User-visible behavior

- In Edit Mode, choosing a level opens the existing prototype scene and enters Play Mode at the chosen level. If switching scenes would discard unsaved work, retain Unity's standard save/cancel prompt.
- In Play Mode, choosing a level immediately restarts that level with a fresh board and 180 seconds. Close owned pause/result popups, cancel drags and release gameplay gates; resume Editor pause if needed.
- The launch choice is consumed once. Normal subsequent Play or scene entry starts level 1. No saved progress or persistent override.
- Menu entries are unavailable during compilation, asset import and Play Mode transitions.
- Game UI, art, level contents, shelf-row-refill rules, progression and device builds remain unchanged.

## Out of scope

In-game selector UI, unlocks, saves, keyboard shortcuts, new content or Foundation changes.

## Ownership and architecture

- Editor/Sorting/PrototypeLevelMenu.cs owns menu callbacks, safe scene opening and entering Play Mode.
- The existing ViewController gains an Editor-only partial bridge for a one-shot SessionState launch choice and explicit level reset through the existing ReloadLevel -> StartSortLevelCommand path.
- No Model, System, registration, package or serialized schema change. Startup default stays level 1 outside the Editor.

## State and persistence

Unity SessionState only carries the next requested level across Play Mode domain reload. Consume and erase on the prototype controller's first Start. Stopping/cancelling Play clears any unconsumed choice. No disk save.

## UI, prefab, scene and Inspector impact

No serialized asset/binding/layout change or new resource. Existing Assets/Scenes/AllSortedPrototype.unity and SortGameViewController are reused. No Inspector setup required. Existing modal and primitive cleanup remain owned by the controller. The Editor menu bridge is excluded from player builds.

## Failure and recovery

Cancel the standard save dialog without entering play or setting a launch override. Missing target scene/controller reports a clear Editor error without gameplay mutation. Invalid launch overrides fall back to level 1. Pause/loss/win do not prevent an explicit Editor selection.

## Performance

No per-frame selector polling. One controller lookup per menu action and existing reset/reconciliation only. One additional SessionState read on scene startup in the Editor.

## Acceptance criteria

- [x] All three actual Unity menu callbacks switch the active level, label, contents and fresh timer.
- [x] Switching from pause/result state cancels modals and returns input correctly.
- [x] Selected startup level survives domain reload and is consumed once; ordinary scene entry returns to 1.
- [x] Real Edit Mode menu selection starts the chosen level.
- [x] Full project tests, Unity compilation and convention validator pass.
- [x] Authored scene/content/art and player behavior are preserved.

## Verification matrix

| Boundary | Required evidence | Result |
| --- | --- | --- |
| Menu/reset | Actual menu invocation in PlayMode, paused and failed states, all 3 entries | Passed EditorMenusSelectAllThreeLevelsAndReleaseOldModals |
| Startup | One-shot launch choice, subsequent scene entry defaults to 1 | Passed EditorLaunchChoiceIsConsumedOnceThenSceneDefaultsToFirstLevel |
| Native Editor | Read native menu and execute the actual menu from Edit Mode, across domain reload | Native AX lists all 3 entries; Editor API probe launched LEVEL 03 with 30 items, 28 visible pieces and consumed key |
| Regression | Full EditMode/PlayMode suites and compilation | 37/37 EditMode + 16/16 PlayMode, no failures |
| Authoring/conventions | Scene unchanged; validator 0 warnings | Scene matches backup bytes; validator passed with 0 warnings |

## Definition of Done

- [x] Editor ownership and existing Command mutation path preserved.
- [x] Required local acceptance criteria have evidence.
- [x] No authored asset or player-facing UI change.
- [x] Status becomes Verified after checks pass; no device certification claimed.

## Decision log

- 2026-10-26: User requests direct 1/2/3 selection in Unity Editor. Use three clearly labelled menu entries supporting both stopped and running states; retain standard scene save protection.

## Final evidence

- Before implementation the two new scene tests failed for missing behavior: launch choice remained level 1, and the requested menu was unregistered. After implementation the full suites passed.
- TestResults/editor-selector-green-edit.xml: 37 passed, 0 failed, 2026-10-26 12:05:51Z–12:05:52Z.
- TestResults/editor-selector-green-play.xml: 16 passed, 0 failed, 2026-10-26 12:05:57Z–12:06:07Z. Tests invoke the actual three menu callbacks, use paused and failed boards, and verify the next plain scene entry defaults to 1.
- TestResults/editor-selector-launch.txt: actual Edit Mode ExecuteMenuItem followed by a real Play Mode/domain reload produced LEVEL 03, 30 total pieces, 28 front/rear visuals, and an erased one-shot key. The native macOS menu tree independently displayed all three labelled entries.
- Scene file equals pre-change backup. Level content, art, popup assets and Foundation packages were not modified. Production changes: one conditional Start branch, an Editor-only controller partial, and Editor/Sorting/PrototypeLevelMenu.cs. No new component or Inspector binding.
- Independent read-only review found no actionable defect. The reviewer highlighted the need for a real Edit Mode launch check; the subsequent probe above supplies that evidence.
- Temporary verification/probe sources and meta files were backed up outside Assets and removed after saving evidence. No pending flag remains.
- Standard save/cancel protection is retained through Unity's SaveCurrentModifiedScenesIfUserWantsTo API; its native dialog was not separately clicked. No iOS build/device certification is claimed. Player startup excludes the Editor branch via preprocessor and remains level 1.
