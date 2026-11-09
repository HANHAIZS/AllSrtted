---
id: sort-express-first-five-runtime
title: Sort Express first five stages with primitive goods
status: Verified
owner: Project
created: 2026-10-30
updated: 2026-10-30
---
## Goal
Replace only playable stage content/board presentation with reference2165 first5 stages. Preserve user's Home/Shop/Settings/art/popup/purchase/wallet flows and authored assets. Use primitive silhouettes per family and stable color variations per full item ID. User explicitly accepts compatible, deterministic and solvable generation for stages4/5, not original RNG recovery.
## User-visible behavior
Source1/2/3 fixed placements, aligned empty cells, raw shelf positions, depths, item IDs and 300-second limit. Source4/5 retain source fixed cells/positions/lock-match thresholds and generator quotas; compatible weighting/materialization is marked in data, with recorded legal solutions. Shelf counts3/9/10/9/13; first3 items9/54/84; generated counts51 and81 including fixed30 in5. Front vacancies remain until whole front row clears, then advance one complete layer. Locks decrement on global triple completion. Hidden/locked pieces cannot be dragged or selected by in-game removal. Existing invalid-drop/pause/retry/result/progression lifecycle retained for5 stages. Finish5 returns1 on next action.
## Ownership and architecture
Project immutable level definitions/catalog Utility; catalog registered only in AllSortedApp.Init and cached by models/system. SortBoardState owns aligned layers, vacancies and lock state. Commands/System own gameplay mutations. Existing controller partial owns dynamic shelves, rendering/input and cached primitive materials. GoldenKind remains4; source kinds start100 to prevent collisions. Original PrototypeLevel remains as unchanged legacy regression content; test repositories explicitly exercise that legacy suite while new tests run source campaign.
## Source/asset audit
2200 archived JSON already present and byte-exact; no missing source stage files. Current runtime3 stages/6 shelves/4 artwork families only. First5 reference75 itemIDs/32 family prefixes. Final model assets intentionally substituted; no reference art import. Preserve all native scenes/prefabs/sprites/materials/models by hash. New compiled config only at Assets/Resources/Configs/Sorting/SortExpressFirstFive.json, loaded once/cached by catalog; source archive remains Editor-only and unmodified. Runtime shelves/material instances are view-owned and destroyed with owner. Native Unity objects authored through APIs, never YAML editing.
## Out of scope
Remote levels, original proprietary RNG identity, stages6–2200 runtime, reference models/textures, Home/Shop/Settings redesign, economy/save migration, special mechanics absent from first5. No deleting original assets.
## Failure and lifecycle
Invalid or missing runtime config fails with explicit diagnostic before attempt/stock mutation. Entry/restart use existing wallet guards; missing config never fabricates a blank board. Cancel/pause/owner destruction maintain authoritative state. Deterministic compiled generation and stored witness allow restart reproducibility. Legacy test content is selected through repository injection, not product test switches.
## Acceptance criteria
- [x] Source audit and documented75IDs/32families with no missing config files.
- [x] First3 fixed source data exact, all5 shelf/lock counts correct and original generator quotas conserved.
- [x] All5 solve through legal moves with no booster; locks reject move/pickup before threshold.
- [x] Dynamic input/render >6 shelves, aligned vacancies, hidden/refill/lock UI verified on phone/tablet.
- [x] Six booster effects match extracted descriptions and no item/stock loss on rejected use.
- [x] Source5-level navigation/restart/victory/Home compatibility verified; native unrelated assets unchanged.
- [x] Compilation, focused/full regression and Foundation validator pass; actual evidence recorded.
## Verification matrix
Source byte/hash/field audit; EditMode data/solvability/lock/refill/failure/booster tests; PlayMode all5 rendering/input/solve/restart/navigation and captures; legacy regression retained; native asset hashes before/after; console/convention gates. Hardware performance not claimed.
## Definition of Done
Check all scoped acceptance evidence, record compatibility generator algorithm and remaining fidelity limits, update status only after actual checks.

## Verified source interpretation and compatibility boundary
Installed metadata31 field definitions identify TimeLimit, LockMatchCount, ShelfItemCountWidth, SpawnItems, PredefinedDepth, SameShelfProbability, SameLayerWeight, BackLayerWeight and RandomWeight. This establishes structural names, not original native RNG/selection algorithms. User accepted compatible generation for4/5. Fixed cells (including blanks), raw x/y and lock counts match source exactly; runtime converts coordinates to shelf units x×1.25/y×2.1 and recenters uniformly per axis. First3 rows remain exact. Compiler preserves all51 generated item quotas and30 generated blanks in4/5;5 additionally retains30 fixed items and may have empty alignment padding at final generated row. Seed/rejection-sampling and witness are stored, so retry is deterministic. Generated initial placement is baked from accepted weighted compatibility distribution, not original live generator replay.

Runtime only permits five independently validated stages. Config schema/catalog appearance/occupied kinds and legal-solution witness validate before caching/publication. Missing iid mapping/family/color/shape regression cases reproduced failures before repair. Legacy regression uses explicit repository injection in test utilities; no product test-mode flag. PrototypeLevel and its three saved solution files remain unchanged.

Booster policy preserves project/extracted contracts: vacuum removes one matching accessible front triple; dice permutes all remaining occupied positions (including locked/hidden inventory); wildcard transforms three complete triples across all remaining inventory,9 IDs, preserving locked-stock identity until accessible. Watch keeps15s with existing flight/skin effect. Entry box clears9 across remaining inventory including locked stock; alarm adds30s. Locks forbid pickup/drop and direct vacuum, not non-removing permutation/conversion or entry setup. New tests caught entry4 failure and dice/wildcard exclusion and were fixed.

Source inspection/play captures found overlong lock captions; display now uses compact LOCK n and reuses three shelf materials for the controller lifetime. Source substitutes carry small stable numeric match IDs to distinguish75 variants during primitive prototyping; final item art is out of scope.

## Completion evidence
- Full EditMode224/224, jobdfd2c1052d1f41a6a5b6fc72a96db824; full PlayMode94/94, jobae8df5cc6da64920b4edf034f4cbdbec. TestResults/source-first-five-{editmode,playmode}.json.
- First3 legal source solutions3/18/28 pointer moves; compatible4/5 solutions49/104 pointer moves. Production Continue performs30coin flight/settlement then next, stage5 wraps1. Source5 watch/retry/stock/energy test passes. Ten phone/tablet captures TestResults/source-stage-{1..5}-{720x1280,768x1024}.png saved and inspected.
- All4692 protected native scene/prefab/sprite/material/model/source-archive files match pre-change SHA256. PrototypeLevel and three existing solution files unchanged. No final preview opened; No additional final preview was opened; the current Play Mode is left intact.
- Source compiler deterministic rebuild SHA256752b1b50c4f00bf68f9b9f8636ba99083ad001a3ae15c32a24fb5df9a7442a3d. Rebuild: python3 Scripts/build_sort_express_first_five.py. Validates all source quotas and legal witnesses before atomic publication. Resource size59613bytes.
- Red coverage: missing repository219223927a8f49228fde2220f4e7336b; malformed catalog/locked entryf3aaf40dcaeb4839bced9f8ecc9b2635; locked dice/wildcarda2e18967dd6e443086f13f9e6ba5d899. Repairs verified by full suites.
- Earlier full PlayModeb67e9e1215654ff3a038a376ef967b68 passed93/94; ActualAsyncEntryHoldsTimerAndInputThenReleasesWithSelectedBoostersOnce failed an entry/gate false assertion. Isolated repeat51dc78a60c704beab0eef2b2de42b1d3 passed. Added assertion diagnostics only; no runtime transition behavior change. Subsequent complete94/94 passed; initial transient failure remains recorded rather than hidden.
- Read-only independent review found config catalog cross-validation and locked-inventory booster issues; both reproduced by tests and repaired. Final recheck: no remaining critical/important findings. Shelf materials reused per owner. Foundation validator0warnings; no new compiler errors.
- Detailed audit, source comparison, protected hashes, code diff, review and test results at /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/first-five-runtime/.

## Remaining scope
Compatible generation preserves source quotas, fixed cells, shelf positions/depth alignment and lock thresholds but does not claim original native RNG sequence or identical difficulty distribution. Source6–2200 remain archive-only. Device hardware FPS/GC not profiled. Primitive substitutes use match IDs/colors instead of final family art.

Delivery report: `Docs/Verification/sort-express-first-five-runtime-report.md`.
