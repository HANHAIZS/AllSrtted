---
id: source-level1-artwork
title: Reworked first-stage merchandise and recessed shelf artwork
status: Verified
owner: Project
created: 2026-10-30
updated: 2026-11-02
---

## Goal
Implement the user-approved first-stage art batch: Blender-reworked Apple, Hanoi and Sunflower plus one recessed modular shelf, exported as FBX and connected to the current source-campaign display. Preserve all five stage definitions, solutions, economy, boosters and UI. Reuse the previously selected original silhouettes as source; source-game meshes/textures are reference only.

## User-visible behavior
First-stage goods use a broad twin-leaf apple, four separated stacking rings with rounded cap, and a faceless sixteen-petal flower in a rounded square pot. Refine profiles and use mint/coral/gold/blue/ivory materials with clearer broad highlights. First-stage shelves have rounded front lip, recessed dark interior and a lower ledge, fitted to the current three slots. This batch applies to source stage1 only; source2–5 retain their accepted primitive presentation. Legacy artwork remains untouched.

## Ownership and architecture
Presentation only in existing SortGameViewController partials: explicit stage1/family artwork selection, shared front/rear art materials and optional shelf prefab. No Command/Model/System/Utility registration or save changes. Existing piece roots, targets and input remain authoritative; artwork child compensation preserves authored model proportions while the parent runs existing pop/refill/fly/drag effects. Unknown/missing artwork retains the primitive fallback. GoldenKind stays primitive. Source kinds100+ are resolved by family, never used to index the legacy0–3 array.

## Assets and lifecycle
New files under Assets/Art/Models/SourceLevel1/SM_S1_{Apple,Hanoi,Sunflower,Shelf}.fbx; original1024 albedo under Assets/Art/Textures/SourceLevel1; shared URP front/rear materials under Assets/Art/Materials/Sorting/SourceLevel1. Direct-reference prefabs under Assets/Prefabs/Merchandise/SourceLevel1. GameScene Inspector owns four explicit prefab references plus three front/rear material pairs; no Resources lookup or new eager UI load. Scene serialized changes only through Unity Editor API. New instances are owned and destroyed by existing piece/source-shelf lifecycle. All old art remains recoverable and unmodified.

## State and persistence
No state mutation, save migration or progression changes. Source JSON, old PrototypeLevel, solution witnesses and gameplay files protected by hashes. UI properties/components, currencies and booster bindings protected by native readback and existing tests.

## Failure behavior
Missing prefab/material or malformed artwork falls back to the existing primitive without changing stock, timer or attempts. Inactive primitive renderer remains available for fallback but must not cast duplicate shadows. Native authoring refuses dirty or playing editor scenes rather than saving transient state. Restart/cancel/victory/next-level cleanup does not retain artwork or temporary materials.

## Out of scope
Models for28 other families, stages2–5 final art, original proprietary art import, changing layouts/solutions/timers/slot geometry, UI redesign, package changes and hardware profiling. No final preview requested.

## Acceptance criteria
- [x] Four FBX files have centered pivots, applied transforms, valid UVs, one material each and no degenerate faces; goods<=2500 tris, shelf<=2000 tris.
- [x] Fresh Blender reimport preserves model bounds, UV, material slot and triangle counts; original.blend saved outside Assets.
- [x] Native Unity prefabs/materials are complete, URP, textures1024 and rear multiplier0.68; no colliders or missing scripts in artwork.
- [x] Source1 actual front/rear goods display family-correct art, no numeric substitute labels, correct aspect and material after refill/pop/restart; shelf lips do not block pointer moves.
- [x] Missing art fallback, Golden, unchanged source2–5 and legacy artwork verified.
- [x] All five existing pointer solutions complete, wallet/booster/restart/UI regressions pass; protected content unchanged except approved GameScene references.
- [x] Native compile, full EditMode/PlayMode and Foundation validation pass; phone/tablet captures inspected internally.

## Verification matrix
Blender file/mesh/UV/triangle/bounds readback and FBX roundtrip; Unity native import/prefab/scene readback; test-first PlayMode stage1 artwork binding/fallback/restart and source five-pointer solutions; existing full suites; protected SHA256s, UI binding snapshot and phone/tablet render inspection. Hardware FPS/GC not claimed.

## Performance considerations
One mesh/material per visible good; three art material pairs shared per scene, no per-piece material creation. Single static shelf renderer preferred; locking UI remains separate. No frame-loop asset lookups. Preserve current primitives for fallback, disable their render/shadows when art is present.

## Definition of Done
All scoped acceptance criteria have recorded export/native/test evidence, original asset/UI content remains intact, final affected files and performance limits are documented.

## Completion evidence
Four original-rebuild FBX+1024 albedo files exported and native roundtripped: Apple1048/Hanoi2360/Sunflower1864/Shelf1080tris; no boundary or degenerate faces. Blender source and export/native verification in workspace outputs/level1-art-v2/. Family meshes and inverse-scale rear/refill/restart checked by source PlayMode tests; malformed rendererless/meshless prefabs reproduced red e48a478cd8424634931700ab7a66edb9 and repaired green e702999dfa1d48259e3ee0b6e4e321bb. Shelf importer corrects depth direction with child180Y and art child lowering0.14m grounds goods without changing slots. Phone/tablet images saved to TestResults/source-stage-1-*.png; internal visual inspection performed, no final preview opened. Independent final recheck: no remaining Critical/Important issues. Initial focused source suite4/4 and final complete PlayMode100/100 on2026-11-02 (job011d9c88ef1d4f92a1f6c4fe4134ac15), EditMode224/224 (jobda649dcaf3b54aee8bac91666d695b77). The interrupted2026-10-30 full-run job is no longer available; it is not claimed as passing.5019 pre-existing native files checked at art completion: only approved GameScene Inspector references changed; prior art and source JSON unchanged. No hardware FPS/GC claim.
