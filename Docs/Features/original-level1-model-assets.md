---
id: original-level1-model-assets
title: Four original merchandise families across the first three levels
status: Verified
owner: Project
created: 2026-10-30
updated: 2026-10-30
---
## Goal
Apply FIRST-3-LEVELS-v2 §5 to the existing project. Add an original Pear as kind 3 and display Apple/Hanoi/Sunflower/Pear on L1/L2/L3. Preserve PrototypeLevel, all existing solution moves, 24/24/30 items, 8/8/10 matches and 180-second timers.

## User-visible behavior
Kinds 0/1/2/3 use coral twin-leaf Apple, four-ring Hanoi stacking toy, faceless sixteen-petal potted Sunflower and slender green single-leaf Pear. All three levels use these four families and the existing six original shelf visuals. OriginalArtwork replaces the primitive display; GoldenKind explicitly retains its gold primitive. Rear stock uses its own material at RGB ×0.68, smoothness 0.55, plus existing rear scale/offset. Drag, cancel, refill, pause, failure, restart, next-level and Home flows are unchanged.

## Ownership and architecture
Presentation-only runtime change in SortGameViewController.Merchandise.cs removes the level gate and three-kind cap from both decoration/material selection. Null/array bounds guards and explicit GoldenKind fallback remain. OriginalArtwork reciprocal local scale compensates existing primitive dimensions, including kind 3 cylinder, while PieceScale, SlotWorld and parent transforms remain unchanged. No Model/System/Command/Foundation or persistence changes.

OriginalLevel1AssetImport imports five models (four goods and shelf), authors one-renderer/one-material prefabs and URP Lit front/rear materials, validates native bounds/pivot/textures and creates the isolated validation scene. OriginalLevel1SceneBinding authors four direct prefab and four front/four rear material references through Unity Editor APIs. Existing UI/effect references and shelf transforms remain intact.

## Assets and lifecycle
- FBX: Assets/Art/Models/Level1/SM_L1_{Apple,Hanoi,Sunflower,Pear,Shelf}_A.fbx.
- Original albedo: Assets/Art/Textures/Level1/T_L1_*_Albedo.png, 1024×1024 with mipmaps.
- Materials: Assets/Art/Materials/Sorting/M_L1_*.mat and M_L1_*_Behind.mat.
- Direct prefabs: Assets/Prefabs/Merchandise/Level1/PF_L1_*.prefab, owned by GameScene Inspector references; no Resources key or new eager Resources load.
- Validation scene: Assets/Scenes/Validation/Level1ModelValidation.unity, excluded from build settings.
- Blender sources, original AO, FBX+GLB, preserved reference/whitebox scenes and validation: /Users/zhangshuai/Documents/Sort-Rework/. No source-game mesh, atlas or shader imported into Unity.
- Existing piece reconciliation instantiates/cleans artwork with its owning piece. Disabled primitive renderers remain available for fallback and do not draw.

## Out of scope
New layouts, tutorial simplification, new kind IDs, difficulty/timer changes, new source-game content, unrelated UI/economy/effects, LOD or device profiling. v2 supersedes the earlier three-family first-level-only scope. Existing Apple A / Hanoi B / Sunflower A selection remains; release names retain the _A suffix.

## Acceptance criteria
- [x] Original Pear: 0.48×0.70×0.48 m in Unity, 1,184 triangles / 598 Blender vertices, centered pivot, applied transforms, one material, one UV, 1024 albedo+AO. Closed mesh, no loose vertices or degenerate faces, zero UV interior overlap pixels at 1024.
- [x] Pear FBX/GLB native Blender roundtrips preserve bounds, triangles, pivot, material, UV and texture. Existing four assets also retain passing roundtrip checks.
- [x] Four-family front/three-quarter/side renders saved under Sort-Rework/renders/four-families-*.png and inspected.
- [x] Native Unity readback confirms four prefab and 4+4 material references; each prefab has one renderer, one URP Lit material. Rear RGB is 0.68, smoothness 0.55. Six shelf visuals, no missing scripts, watch binding retained.
- [x] AllFourFamiliesDecorateAllThreeLevels verifies every rendered normal piece has artwork and a disabled primitive, all four kinds appear in L1/L2/L3, Pear is not flattened, Golden remains primitive. Red job ef0369f8e3294e7483774576167f721c failed with array length 3; green focused suite 5d2d42fbcf944758b318a12cb8a65398 passed 5/5.
- [x] Existing 24-item drag/restart assertions retained and expanded to all four families. Existing solutions run unchanged; protected source/test SHA-256 values match the pre-change backup.
- [x] Six phone720×1280 / iPad768×1024 captures saved to TestResults/four-families-level{1,2,3}-{720x1280,768x1024}.png and inspected.
- [x] Full EditMode 191/191 (fafca55d023c4787a3fc57ac0f8f7813); full PlayMode 92/92 (687e94e517c3413581a7122cc07d9cc9). Exact results in TestResults/four-families-{editmode,playmode}.json.
- [x] Foundation validator: passed with 0 warnings. Read-only independent review: no critical/important issues.

## Regression test maintenance
Full regressions exposed stale asset whitelists, primitive material assertions and pre-flight watch expectations. The whitelist now permits only the five exact original model/texture/prefab paths and three existing watch effect images. Rear refill assertions read actual artwork materials. Watch/Home/Pause tests wait for the existing impact before checking spent stock; deterministic manual ticking remains tested. Gameplay code for these flows was not changed. PrototypeLevel.cs, ThreeLevelPlayModeTests.cs and SortBoardStateTests.cs remain byte-for-byte unchanged.

## Verification matrix
Blender: mesh topology, centered pivot/applied scale, triangle budget, UV raster, 1024 texture, FBX/GLB native roundtrip and three-view inspection. Unity: native scene/prefab/material readback, full EditMode/PlayMode regressions, Golden/refill/drag/restart/three-solution coverage, six device-aspect captures and Foundation convention validation. Hardware FPS/GC remains outside this verification scope.

## Verification limits and performance
Goods use 1048/2360/1864/1184 triangles, shelf 520; one renderer/material per visible artwork. Four families share prefab/material assets; no new runtime lookup or save dependency. Mesh/asset budgets and Editor render evidence are verified. Hardware FPS/GC was not profiled. The Editor console retains a historical Test Framework PlaymodeLauncher.cs:113 exception; the final complete runs succeeded and no new product compiler errors were found.

## Definition of Done
All scoped acceptance criteria have fresh test/native/render evidence. Export summary: /Users/zhangshuai/Documents/Sort-Rework/export/validation.json. Detailed delivery: /Users/zhangshuai/Documents/Sort-Rework/FINAL_REPORT.md. Backups, protected hashes, scene readback and test records: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/pear-v2/.
