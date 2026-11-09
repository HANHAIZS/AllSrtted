---
id: shelf-row-refill
title: Refill a shelf only after its entire front row is empty
status: Verified
owner: Project
created: 2026-10-26
updated: 2026-10-26
---

# Shelf row refill

## Goal

Apply the user's corrected reference rule to all three levels: removing a front item leaves a usable empty slot. Hidden items advance together only when all three front positions on that shelf are empty.

## User-visible behavior

- A committed move never reveals only the source slot's rear item. Rear previews remain behind and cannot be dragged.
- Any front vacancy can accept a moved item even when hidden stock remains behind it. That stock retains its identity and order.
- After a move empties the last front position, advance each column by one layer together. Initially empty columns do not block the condition.
- Clearing a matching triple also empties its whole row, so the next layer advances together. Newly formed triples still cascade until stable.
- Apply the same rules to levels 1–3; preserve independent content where solvable, 24/24/30 items, the 180-second clocks and guarded progression/retry.
- Count only real pieces; front vacancies never count as items. Reset restores every layer and identity.

## Out of scope

New art, UI layout, shelf geometry, boosters, purchases, saves, reference-game assets/configuration, Foundation changes, iOS certification.

## Ownership and architecture

| Responsibility | Owner | Existing path | Notes |
| --- | --- | --- | --- |
| State mutation | Command/System | Commands/Sorting/SortCommands.cs, Systems/Sorting/SortGameSystem.cs | Existing move path retained |
| Board state/rule | Model | Models/Sorting/SortBoardState.cs | Vacant fronts are separate from hidden stock; atomic settling |
| Content | Utility | Utilities/Sorting/PrototypeLevel.cs | Three project-owned layouts |
| Presentation/input | ViewController | ViewControllers/Sorting/SortGameViewController*.cs | Existing front/behind getters remain authoritative |
| Foundation/Event | None | None | No new registration or event |

## State and persistence

Runtime-only vacancies and hidden layers. Existing Controller -> Command -> System -> Model ownership. No save schema or migration.

## UI, prefab, scene, and Inspector impact

No asset, hierarchy, binding, resource path or Inspector change. Existing primitives follow authoritative front/behind positions and input only selects GetFront. Existing view cleanup remains. Scene: Assets/Scenes/AllSortedPrototype.unity, SortGameViewController component.

## Integrations and failure behavior

No SDK or network changes. Invalid drops, cancelled drags, pause and timeout do not reveal stock. Current-level retry restores the original row layers. Existing input gates and terminal ordering remain.

## Performance considerations

Six shelves, three columns per shelf. Refill/settling only on accepted moves. Reuse slot storage; no per-frame allocation or service lookup added.

## Acceptance criteria

- [x] Moving one/two items leaves front vacancies and preserves rear identities.
- [x] Removing the last front item reveals exactly the next layer for that shelf together.
- [x] Dropping into a vacancy with hidden stock is legal and preserves that stock.
- [x] Triple clear/refill/cascade, counts, reset and invalid-input gates remain correct.
- [x] All three levels have explicit legal winning solutions under the new rule.
- [x] Actual scene input/visuals enforce delayed refill and block rear-piece dragging.
- [x] Full project EditMode/PlayMode suites, compilation and conventions pass.

## Verification matrix

| Boundary | Evidence | Result |
| --- | --- | --- |
| Model | Regression cases for partial row, final vacancy, hidden-stock target, cascade/reset | Passed: pure model/content 23/23; Unity full EditMode 37/37 |
| Three levels | Explicit solutions through real rules, Commands and actual scene | Passed: original content unchanged; legal solutions 14/16/16 moves, all three wins and 1->2->3->1 |
| Runtime | Front vacancy, fixed preview identity/position, hidden input rejection, grouped advance | Passed: all three levels through screen-space drag; full PlayMode 14/14 |
| Assets | Scene/art/bindings preserved | Original scene and content byte-for-byte identical to backup; only runtime model and tests changed |
| Compilation/conventions | Full project tests and validator | Unity 2022.3: 37/37 EditMode + 14/14 PlayMode; project validator 0 warnings |
| Device | iPhone/iPad device smoke/performance | Not available; not claimed |

## Definition of Done

- [x] Rules and ownership match this spec.
- [x] All required local acceptance evidence exists.
- [x] Unrelated authored assets and UI are preserved.
- [x] Performance and unavailable device checks are reported.
- [x] Status becomes Verified only after required checks pass.

## Decision log

- 2026-10-26: User explicitly requests changing the first three levels. Interpret adjacent three as the three front slots of one existing shelf. Keep initial empty slots usable and allow refill after either moves or a triple clear. This supersedes the former immediate per-slot reveal rule.

## Verification evidence

- TDD: original model produced three expected failures in the five new row-refill regression tests (partial vacancy, dropping onto hidden stock, pause/reset layer boundary); the revised model passed all five. Updated model/content suite passed 23/23.
- Full Unity EditMode: TestResults/row-refill-green-edit.xml, 37 passed, 0 failed, 2026-10-26 11:26:28Z–11:26:29Z.
- Full Unity PlayMode: TestResults/row-refill-green-play.xml, 14 passed, 0 failed, 2026-10-26 11:26:34Z–11:26:44Z. Includes all three full-level wins/progression, current-level replay and actual front/rear input/visual checks.
- Inspected TestResults/row-refill-waiting.png and row-refill-promoted.png: bottom-left shelf retains its rear cube with a vacant front after the first move, then promotes both rear pieces together after the final occupied front moves away.
- Production changes are confined to Models/Sorting/SortBoardState.cs. Lists retain an empty front sentinel instead of popping immediately; only an entirely vacant shelf advances all column lists together. No per-frame lookup/allocation or new component.
- Scene and PrototypeLevel content equal pre-change backup bytes. Exact render/input paths consume the same GetFront/GetBehind APIs. No Inspector action required.
- Mac Editor verification complete; iPhone/iPad device behavior and performance remain unverified.

- Final independent read-only code review: no actionable Critical, Important or Minor findings. Reviewer checked the model diff, updated solution/cascade tests, rendering/input consumers and both XML reports.
- Temporary in-editor verification runner and its meta file were backed up outside Assets and removed; no pending check flag remains. Final Unity refresh/domain reload completed without compiler errors. Final project convention validation: 0 warnings.
