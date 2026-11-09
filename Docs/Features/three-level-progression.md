---
id: three-level-progression
title: Three independently authored sorting levels
status: Verified
owner: Project
created: 2026-10-26
updated: 2026-10-26
---

# Three-level progression

## Goal

Add two playable levels to the existing level, with increasing sorting complexity, while preserving the selected primitives, blue background, corner controls, centered timer and four bottom button bases.

## User-visible behavior

- Three levels total. Level 1 is unchanged; level 2 uses 24 more-mixed pieces; level 3 uses 30 pieces, deeper stacks and four initial empty slots. Six three-slot shelves and four existing primitive types remain.
- Every level has 180 seconds and a verified legal solution. No new obstacle, booster or purchase mechanic.
- Top label updates LEVEL 01 / 02 / 03. Clear levels 1 and 2 to use NEXT LEVEL. Clear level 3 to START OVER at level 1.
- Pause restart and timeout replay restart the current level, not the first. Next-level intent is accepted only after a win; repeated stale popup callbacks cannot skip levels.
- Fresh scene entry starts at level 1. No disk progression persistence is added.

## Out of scope

Level selector screen, unlocks, saves, rewards, extra art, reference-game configuration, obstacles, working bottom boosters, economy and actual device certification.

## Ownership and architecture

| Responsibility | Owner | Type/path | Notes |
| --- | --- | --- | --- |
| State mutation | Command | Commands/Sorting/SortCommands.cs | Explicit start/retry and guarded advance |
| Observable state | Model | Models/Sorting/SortBoardModel.cs, SortBoardState.cs | Current level and initial/remaining counts |
| Rules/progression | System | Systems/Sorting/SortGameSystem.cs | Start valid content, advance only Won |
| One-shot notification | None | None | Existing revision/phase observables suffice |
| Presentation | ViewController | SortGameViewController*.cs | Dynamic level/progress text and terminal primary action |
| Content adapter | Utility | Utilities/Sorting/PrototypeLevel.cs | Three independent fixed layouts |
| Infrastructure | Foundation | Existing registrations and popup router | Shared packages unchanged |

## State and persistence

- Runtime current level number, board, match target and the existing clock/phase properties. Model is authoritative.
- Controller -> Command -> System -> Model. Invalid level numbers fail before state mutation. Advance rejects Playing, Paused and Lost.
- Save schema/version/migration: None. Fresh application/scene entry begins at level 1.

## UI, prefab, scene, and Inspector impact

- Affected scene: Assets/Scenes/AllSortedPrototype.unity. Add only a serialized reference to the existing LEVEL label; all authored transforms, sprites, palettes and UI hosts remain.
- Loading mode/final paths: existing scene direct references and existing lazy Resources popup keys remain.
- Resources keys/reference owners: SortGameViewController owns LevelLabel and existing pause/result keys. No new Resources asset.
- Loading/cache/cleanup: unchanged lazy popups. Board replacement clears all old piece instances before rendering reused IDs.
- UI layer contract: Docs/Foundation/UiLayers.Project.md. Pages HUD and Popups modal, Replace routing, existing world-input gates.
- Allowed authored changes: dynamic level number and result primary/title/body text. All unrelated authored UI properties protected.
- First-use/reuse/failure: existing recovery retained; missing result UI still offers retry of current level. No blank modal or unsolicited advance.
- Model deletion audit: None; no model removed.
- Inspector confirmation: read back LevelLabel binding and unchanged existing references.

## Integrations and failure behavior

- SDK, consent, identity, server, content update and rollback: no change; fully offline local content.
- Retry resets current layout, clock, match count and visuals. Only winning advances. Final win wraps to the first level only on explicit START OVER.
- Pause, suspension cancellation and terminal timeout priority remain unchanged.

## Performance considerations

No extra per-frame service lookup. Six shelves and front/one-behind rendering remain; deeper pieces instantiate only when exposed. Level reset clears reused unique IDs. No asset duplication or new live service.

## Acceptance criteria

- [x] All three independent layouts can be won by replaying legal moves; level 1 remains identical.
- [x] Current level, target groups, retry and guarded 1->2->3->1 progression are correct through Commands.
- [x] Real scene updates labels, cleans old views, supports timeout/pause replay of level 2/3 and prevents repeated hidden callbacks.
- [x] Existing art/layout and input gates are preserved; project conventions, compile and relevant suites pass.

## Verification matrix

| Boundary | Required evidence | Result |
| --- | --- | --- |
| Content/rules | Three explicit solution replays and fresh board instances | Passed 12/15/18-move production-rule replays; counts 24/24/30, groups 8/8/10 |
| Commands | Guarded advance, retry current, invalid index leaves state unchanged | Passed six command scenarios, including complete 1->2->3->1 and duplicate advance rejection |
| Actual scene | Three-level run, final wrap, level 2/3 replay, label and piece counts | Passed all 13 PlayMode tests. Real next-level popup callbacks, level 3 pause/timeout replay, fresh scene reset and 24/28 front-plus-preview render counts verified |
| Authoring | Bound existing label; existing UI properties untouched | Scene diff adds only LevelLabel reference. Actual LEVEL 02/03 camera captures inspected |
| Compilation/conventions | Unity compile and project validator | 32/32 EditMode + 13/13 PlayMode passed 2026-10-26 18:08 CST; conventions 0 warnings |
| Performance/device | Cached service and bounded-view review; device checks reported separately | Cached dependencies and revision-only reconciliation retained; no device profiling or actual iOS run claimed |

## Definition of Done

- [x] Ownership and QFramework roles match implementation.
- [x] Acceptance criteria have test/runtime evidence.
- [x] Existing UI art and references remain, with only LevelLabel added.
- [x] Compile, tests and conventions pass; unavailable device checks remain unverified.
- [x] Status changes to Verified only after required local evidence exists.

## Decision log

- 2026-10-26: User requested two more levels. Assume gradual complexity, keep all three offline and the current visual design. No further product choices require blocking implementation.


## Final review and evidence

- Pure new-content tests first failed 4/4 against the missing-level stub, then all 18 pure rule/content tests passed. Command tests first produced 27/32 with five expected missing-progression failures, then Unity EditMode passed 32/32.
- Unity PlayMode passed 13/13 using the actual authored scene, with new full-campaign and current-level retry scenarios. Reports: TestResults/three-level-green-edit.xml and three-level-green-play.xml.
- Independent read-only review found no important findings or blockers. Minor optional coverage suggestions were same-frame duplicate popup invocation and adding level-2 pause restart to the scene fixture; duplicate advance and level-2 timeout retry already have command evidence.
- Existing sprites, materials, layout and popup paths remain. New scene binding is only LevelLabel; no shared Foundation package modifications. Temporary in-editor verification runner is removed after saving test evidence.
- Actual iPhone/iPad device behavior and performance remain unverified, unchanged from the prior prototype delivery.

## Subsequent rule correction — 2026-10-26

[shelf-row-refill.md](shelf-row-refill.md) supersedes immediate per-slot reveal across all three levels. Original layouts and 24/24/30 counts remain unchanged. Current explicit winning solutions take 14/16/16 moves; full Unity suites pass 37 EditMode and 14 PlayMode tests. Earlier 12/15/18-move evidence above records the previous rule only.

## Subsequent booster extension

[four-gameplay-boosters.md](four-gameplay-boosters.md) adds the requested four toolbar boosters across all three levels. Original level content and whole-row refill remain. The user renamed the scene to GameScene; menus, builder, tests and build settings now use that scene. Current complete suite: 64 EditMode + 21 PlayMode passed. Arbitrary card draws can strand leftovers as expressly accepted by the user; the original no-booster solutions remain valid.

## 关卡标题字体样式修订 — 2026-10-28

按用户 `关卡界面之关卡名字.png` 参考，将 GameScene 的 LevelLabel 改为圆润粗体、白色填充、紫色描边。使用 Google Fonts 官方 Fredoka 的700字重静态实例（SIL OFL 1.1，许可随字体保存），以动态文字保留 LEVEL 01/02/03。仅修改标题文字样式及编辑器对应作者化入口；位置、底板和运行逻辑不变。已验证：LEVEL 01/02/03正常自动更新，含描边最大宽119.34，小于原130宽容器；720×1280和768×1024相机截图已保存。Unity编译及Console无新错误，规范校验0警告。仅新增一个Outline，不增加运行时脚本或Canvas；字体源与许可在 Assets/Art/Fonts/Fredoka/。未做真机验证。
