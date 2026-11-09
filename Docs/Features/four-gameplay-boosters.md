---
id: four-gameplay-boosters
title: Four functional gameplay boosters with supplied toolbar icons
status: Verified
owner: Project
created: 2026-10-26
updated: 2026-10-27
---

# Four gameplay boosters

## Goal

Activate the existing four green bottom buttons, insert the user's supplied vacuum/dice/card/frozen-watch icons in that order, and implement their gameplay effects in all three levels.

## User-visible behavior

- Vacuum: randomly choose an eligible kind with at least three front items, randomly choose three matching front pieces across shelves and merge/remove them as one match. Hidden pieces cannot be sucked out. Normal whole-row refill and resulting cascades apply afterward. Without a front triple, disable the button.
- Dice: randomly permute remaining items across occupied front and hidden positions, retaining vacancies, stack lengths, IDs and each kind's total count before normal match settling. No refill of a partly occupied front row. Disable if no meaningful rearrangement is possible.
- Card (updated 2026-10-27): randomly choose three complete groups of three same-kind non-gold remaining items, including hidden stock, and turn all nine into the matching gold-sphere kind. Each group contains three distinct items of one original kind; separate groups may have the same kind if enough items remain. Preserve IDs/positions before normal front-triple settling. Disable when the remaining non-gold inventory cannot supply three complete groups, even if nine individual items remain. Existing gold and vacancies are never selected. Rejected uses do not spend quota or mutate the board. This replaces the earlier unrestricted nine-item draw.
- Watch: freeze the main countdown for 15 active gameplay seconds while dragging, matching and other boosters remain available. Show a blue timer and remaining freeze seconds on the watch button. Manual/lifecycle pause freezes both clocks. Repeated watch clicks while active do not stack. A tick crossing expiry charges only its unfrozen remainder to the level clock.
- Booster activation is available only while Playing and free of modal/input gates. Cancel any uncommitted drag. Restart, advance and Editor selection clear ongoing effects. All three levels use the same mechanics.
- Booster quotas and badge input follow booster-quota-badges.md: each successful use spends one session allowance; no price, ads or purchases.

## Out of scope

Economy, payment, ad rewards, new levels, new art style, sound, device certification and a global solvability solver. Grouped card draws preserve each original kind's count modulo three; they do not promise every board arrangement is solvable.

## Ownership and architecture

| Responsibility | Owner | Existing/new paths | Notes |
| --- | --- | --- | --- |
| Mutation requests | Command | Commands/Sorting/UseSortBoosterCommand.cs | Accepted result and affected IDs for presentation |
| Board state/rules | Model | Models/Sorting/SortBoardState.Boosters.cs, SortBoardState.cs | Random effects, new gold kind, freeze clock, existing whole-row refill |
| Orchestration/random source | System | Systems/Sorting/SortGameSystem.cs | Cached random source and observable publication |
| Presentation | ViewController | SortGameViewController.Boosters.cs, SortGameViewController.cs | Bind four buttons once, animations, gold mesh/material, freeze feedback |
| Authoring | Editor | Editor/Sorting/PrototypeBoosterArtwork.cs, PrototypeHudLayout.cs | Direct-reference Sprite imports and targeted scene edits |
| Foundation | None | No shared package change | Existing layers, commands, router retained |

## State and persistence

Runtime-only kind changes, piece identity/order, freeze remaining time and last affected IDs. No save schema. Controller -> Command -> System -> Model remains authoritative. Seeded System.Random passed to pure rules permits reproducible tests; runtime caches its random source.

## UI, prefab, scene, and Inspector impact

- Artwork revision 2026-10-27: replace Shuffle.png, GoldenCard.png and FreezeWatch.png using the user's new Sort素材 images (dice, upright card, thick frozen-watch block). Extract real transparency with built-in imagegen, then reimport the same asset paths preserving GUIDs and Sprite names/local IDs. Update their Sprite crop metadata. User subsequently requested the card slightly smaller: change only FunctionButton_3/Icon sizeDelta from 98x98 to 86x86 (about 12% reduction), retaining its centered anchor, offset, aspect and all base-button geometry. Update the existing authoring size choice to retain this on rebuild. Other icons, all badges, listeners, game rules and quotas remain.

- Scene: Assets/Scenes/GameScene.unity, existing SortGameViewController and bottom four buttons. Keep all button base positions/sizes, authored backgrounds, timer/level/pause positions, shelf geometry and existing references.
- Direct-reference sprites: Assets/Sprites/UI/Boosters/Vacuum.png, Shuffle.png, GoldenCard.png, FreezeWatch.png. Built-in imagegen removes only the baked checkerboard; transparent outputs preserve each supplied subject. Import as Sprite with alpha, no mipmaps; keep aspect; child icon raycastTarget false. Icons fit within existing green faces.
- New gold sphere materials: Assets/Art/Materials/Sorting/GoldBall.mat and GoldBallBack.mat, direct references on the scene controller. Existing four palettes remain unchanged.
- Pages host: existing SafeArea/BottomButtons. Non-interactive status/freeze feedback uses existing host descendants; no Canvas sorting change. Existing Popups Replace policy and SortUiLayers gameplay gate continue.
- Runtime owner: SortGameViewController subscribes once and removes listeners on destruction. Scene owns direct Sprite/material references; controller owns transient primitive/effect instances. Effects never intercept input.
- Scene/art mutations use Unity Editor APIs only. No text-editing serialized Unity assets. No new Resources key, popup or eager loading.

## Integrations and failure behavior

No SDK/network/save change. Missing bindings fail explicitly before enabling controls. Invalid/paused/terminal uses do not change board, clock or IDs. Freeze expires precisely; restart cancels it; cancelled drags do not mutate state. Existing timeout-before-input ordering remains. Card availability counts complete matching groups across front and hidden stock, rather than total non-gold items alone.

## Performance considerations

All random selection/permutation allocation is action-scoped, with at most 30 pieces. No random or service lookup per frame. Reconcile only board revisions. Freeze text changes at second boundaries. Cache shared gold materials; replace a visual only when that piece changes kind, and release old visual/collider correctly.

Card availability now makes a short counting pass with a small kind-count dictionary when availability is refreshed; random group/position selection runs only on accepted activation. No per-frame random draw or service lookup is added.

## Acceptance criteria

- [x] Four transparent supplied icons appear inside the unchanged four green bases in the requested order.
- [x] Each button routes through Commands and activates the correct effect with visible feedback.
- [x] Vacuum removes exactly three same-kind fronts before cascades; partial-row hidden stock stays hidden.
- [x] Shuffle preserves real pieces, identities, kinds, vacancies and layers before matching; visible arrangement changes.
- [x] Card draws three complete matching triples (nine distinct non-gold IDs), preserves remaining kind remainders, supports hidden stock/repeated groups of one kind, rejects insufficient groups without spending quota, and renders true gold spheres.
- [x] Watch freezes exactly 15 active seconds, accepts item manipulation, handles pause/retry/expiry and rejects stacking.
- [x] Three-level progression, whole-row refill, pause/result gates and Editor selection remain correct.
- [x] Full Unity suites, compilation, authored readbacks and phone/iPad camera captures pass.

## Verification matrix

| Boundary | Evidence | Result |
| --- | --- | --- |
| Pure rules | Seeded effect tests, conservation, grouped card draws, insufficient-group rejection, precise freeze and phase/reset gates | Updated 2026-10-27: 24 booster rule cases; standalone combined Mono rules/commands 69/69 |
| Commands | Real architecture publishes booster changes and availability | Passed 5 new command cases in actual Unity architecture |
| Actual scene | All four actual buttons, gold shape replacement, freeze drag, modal/retry gates | Passed all 5 new scene cases plus 16 existing regressions |
| Assets/UI | Direct Sprite paths, alpha, exact bindings/rects; phone/iPad screenshots | Passed; authoring report validates 270 protected component serializations, original 128x128 button rects and anchors |
| Regression | Full project EditMode/PlayMode suites and convention validator | Updated 2026-10-27: 74/74 EditMode + 25/25 PlayMode, validator 0 warnings |
| Device | iPhone/iPad actual build and touch/performance | Unavailable; not claimed |

## Definition of Done

- [x] Project roles and mutation ownership preserved.
- [x] All required local acceptance criteria have evidence.
- [x] Existing unrelated art/layout/scene properties preserved.
- [x] No shared package, commerce or original-game resource introduced.
- [x] Performance/manual platform limits reported and temporary verification tools cleaned up.
- [x] Mark Verified only after required local evidence exists.

## Decision log

- 2026-10-27: User supplied replacement dice/card/watch art and explicitly kept functionality unchanged. Input source PNGs contain opaque checkerboards. Generated transparent cutouts and exact prompts are saved in task outputs/all-sorted-prototype/booster-icons-v2/manifest.json; original supplied files are retained.
- 2026-10-27: After seeing the new card, user requested a smaller card icon. Set its existing centered rect to 86x86, about 12% below the previous 98x98. No gameplay or button hit-area change.

- 2026-10-27: User changed the card to select three groups of three matching items. This supersedes the earlier arbitrary-nine decision. Same-kind groups may repeat; hidden-stock scope, gold result, existing quota and UI remain.

- 2026-10-26: User supplied four icons and described each effect. Button order follows attachment order. Keep existing toolbar and fit icons within its green faces.
- 2026-10-26: User explicitly rejected grouping the card draw into complete matching triples and selected arbitrary random nine, accepting unmatched leftovers. This takes precedence over guaranteed-solvability safeguards.
- 2026-10-26: Default to directly usable prototype boosters; watch duration counts active gameplay time and cannot stack while active. No new payment or inventory subsystem.
- 2026-10-26: Retain the user's renamed GameScene and original GUID. Update obsolete menu, builder, test paths and EditorBuildSettings through Editor APIs; do not recreate the old scene.

## Final evidence and changed boundary

### Three-icon artwork revision, 2026-10-27

- Scope: Assets/Sprites/UI/Boosters/Shuffle.png, GoldenCard.png and FreezeWatch.png plus their importer crop metadata; GameScene FunctionButton_3/Icon sizeDelta 86x86; matching card-size choice in Editor/Sorting/PrototypeBoosterArtwork.cs. Source images: /Users/zhangshuai/Documents/Sort素材/付费功能之随机排序按钮.png, 付费功能之变形按钮.png and 付费功能按钮之时间暂停.png, respectively. Direct Sprite owners remain the existing FunctionButton_2/3/4 Icon Images on the Pages HUD.
- Backup: task workspace work/booster-icons-v2-backup. Unity MCP performed imports, Sprite metadata changes and targeted scene save. import-report.json confirms real alpha and the three original GUIDs/local IDs retained; bindings-readback.json records unchanged references/rects before the later card-size request; card-resize.json records 98x98 to 86x86 with 314 other components unchanged. Subsequent MCP readback confirms all references, all base-button rects and the other three icon rects unchanged.
- Five existing PrototypeAssetTests and nine BoosterPlayModeTests/BoosterQuotaPlayModeTests passed through MCP. Phone, iPad and toolbar renders were visually checked. Final direct Play/Stop smoke check: Playing with 24 pieces, card 86x86, Console zero errors. Project validator zero warnings. Protected hashes confirm all gameplay sources, vacuum and badges unchanged; the sole source-size edit is Editor-only. No physical-device claim.
- An intermediate test run was interrupted by script reload, causing a hot-reloaded controller with a null cached model. It was stopped, its orphaned MCP status cleared after confirming no actual Unity jobs, and rerun successfully. Unity Test Framework 1.1.33 then logged a BackgroundWatcher cleanup NullReferenceException at PlaymodeLauncher.cs:113. Investigation found one nonpersistent DontSave test controller with a null runner; it was removed via MCP, without touching any saved asset. Final Play/Stop had zero Console errors. Detailed evidence is in outputs/all-sorted-prototype/booster-icons-v2/verification.json. No package/gameplay patch was introduced.
- Cutouts were produced by built-in image_gen; exact prompts and original/generated/final paths are in booster-icons-v2/manifest.json. Outputs are 1254px RGBA with authentic transparency; extraction is generative and may have small edge variations/faint alpha noise, so pixel-identical preservation is not claimed. Existing 512px import limit and no-mipmap settings remain. Three textures replace the old textures in place; no extra runtime texture loading or per-frame work.

### Card revision, 2026-10-27

- Runtime change is confined to Models/Sorting/SortBoardState.Boosters.cs: count full triples for availability; choose an eligible original kind and three distinct positions per group; remove chosen positions before the next draw; commit all nine before normal settling. Existing Command/System/quota/presentation interfaces remain.
- TDD evidence: workspace work/golden-triplets-backup/red.log records 3 expected failures against the old mixed draw/count-only gate; green.log records 69/69 actual rule/command tests passing after the change.
- TestResults/golden-triplets-green-edit.xml: 74 passed, 0 failed. TestResults/golden-triplets-green-play.xml: 25 passed, 0 failed. Scene button test checks original kinds for each selected triple, gold Sphere meshes and one charge spent. Insufficient-group rejection test checks full board/quota snapshots.
- Source tests changed: SortBoosterRulesTests.cs, SortBoosterQuotaTests.cs and PlayMode/BoosterPlayModeTests.cs. No art, prefab, level config, Foundation package or production UI source changes. GameScene byte identity is checked against the pre-change SHA-256.
- Existing seed reproducibility, all three levels, gold exclusion, hidden stock, settling, zero/plus badges, pause/timeout, progression and teardown tests remain green. Grouped removal preserves kind-count remainders; no global solvability guarantee or physical iPhone/iPad validation is claimed.
- Independent read-only review found no actionable findings in the new group-count gate and nine-position selection. Temporary runner/meta were backed up outside Assets and removed. Project validator passed with zero warnings; final scene SHA-256 remained b7c9a4a73658dc95033c6499388a2e5e535032e22b6026ab064fb8acb6381a40.

### Original four-booster rollout, 2026-10-26

- Pure TDD: 21 new rules failed before their API existed; five new command cases failed before command integration. A further deterministic hidden-only shuffle case failed before front-change fallback. Final standalone suite passed 59/59. Logs/reproduction: task workspace work/boosters-rules/.
- Scene TDD: initial new-button suite passed one gate scenario and failed four expected inactive/unimplemented button scenarios. The first full integration run passed 64 EditMode and 20/21 PlayMode; the existing UI-first teardown test exposed a new refresh touching destroyed buttons. Update now stops UI work when its layer owner is gone, while OnDestroy still performs cleanup. Final full run passes that regression too.
- TestResults/boosters-green-edit.xml: 64 passed, 0 failed, 2026-10-26 13:11:08Z–13:11:09Z.
- TestResults/boosters-green-play.xml: 21 passed, 0 failed, 2026-10-26 13:11:14Z–13:11:24Z. Includes all three full-level wins, Editor selector, whole-row refill, actual four button callbacks, gold sphere meshes, freeze drag/pause/expiry/retry and modal/teardown failure paths.
- TestResults/boosters-authoring.txt: original button rects/anchors preserved; 270 protected component serializations unchanged on final apply. Scene edits use Editor APIs and keep the renamed GameScene GUID. Existing four palettes remain unchanged; two new direct-reference gold materials added.
- Inspected real camera captures: boosters-phone.png (720x1280), boosters-ipad.png (768x1024), boosters-gold.png, boosters-freeze.png and boosters-vacuum.png. All four icons fit inside their bases; gold is a true Sphere mesh; freeze shows a blue main timer and 15s on the watch.
- Four cutouts use built-in imagegen background extraction, not a style redesign. Final project sprites are Assets/Sprites/UI/Boosters/{Vacuum,Shuffle,GoldenCard,FreezeWatch}.png. Original supplied files are unchanged. Exact prompts and output metadata are in task outputs/all-sorted-prototype/booster-icons/manifest.json. Unity imports use 512 maximum dimension, alpha and no mipmaps.
- Production files: SortBoardState.cs and new .Boosters.cs; SortGameSystem.cs; UseSortBoosterCommand.cs; SortGameViewController.cs/.UI.cs and new .Boosters.cs; PrototypeBoosterArtwork.cs; PrototypeHudLayout.cs. Old scene paths in PrototypeLevelMenu/PrototypeSceneBuilder and related tests now target GameScene.
- Dependency lookup review: Controller/System resolve once in Awake/OnInit; Command resolves once at execution setup. No new per-frame allocation or service lookup. Four shared icon textures and two shared gold materials; no device profiling claimed.
- Independent read-only review found no actionable findings. Explicitly excluded from its verdict: guaranteed solvability after the user-approved arbitrary card draw, commerce and physical-device certification.
- Temporary in-editor verification runner and meta were backed up outside Assets and removed. No pending verification flag or recurring work remains. Actual iPhone/iPad build, input and performance remain unverified.
