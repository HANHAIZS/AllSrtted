---
id: space-toys-first-level
title: Redesigned space toys and three-shelf tutorial
status: Draft
owner: Project
created: 2026-10-30
updated: 2026-10-30
---
## Goal
Use Blender to significantly redesign three source meshes into Star Ring Tower, Star Bloom, Cosmic Apple, replace first level with3 shelves/9items tutorial confirmed by user.
## User-visible behavior
Level1 single centered column of3 shelves. Source arrangement: ring _ ring / flower _ flower on top shelf; apple then ring then flower in middle slot of middle shelf; apple _ apple bottom shelf. Eachkind3; four moves solve. Keep current180second timer and existing progression/wallet/boosters/UI. Levels2/3 retain their layouts/primitives. Models visibly edited vertex shape plus new star/planet decorations, new toy colors. Preserve source OBJ files; deliver .blend and per-model FBX. No final preview requested.
## Ownership and architecture
PrototypeLevel owns project board data, same SortBoardState handles3shelves. ViewController uses board shelf count and layout-specific slot positions for render/input/shelf visibility. Three lazy Resources merchandise prefabs at Prefabs/Merchandise/SpaceToys, per-scene cache loaded only on level1; original fallback primitives if missing, warning once. Mesh/material/texture direct references live Assets/Art/SpaceToys and Assets/Prefabs/Merchandise as appropriate; native assets authored Editor API. No Foundation changes/save migration. Original source resources from local sort-express2165, edited derivatives traced in manifest, user responsible for rights when distribution planned.
## Out of scope
Full source game restoration, other levels, original code or shaders, extra services, untouched source import, UI redesign.
## Acceptance criteria
- [ ]3 edited models authored in live Blender, exported and native Unity materials/prefabs valid.
- [ ]Level1 has3shelves/9items,4move win, reenter/restart/next preserve correct layout and rendering.
- [ ]All child renderers participate in entrance fade, input targets use matching geometry, boosters remain functional.
- [ ]Compile, focused tests, native resource checks and convention validation pass.
## Verification matrix
Model bounds/UV/faces/material audit, exact Unity lazy resource load, renderer/binding readback; focused board and actual scene PlayMode win/restart/level2 regression. Device/manual appearance/performance untested.
## Definition of Done
Acceptance evidence recorded, sources and scene user changes preserved, feature status reflects actual result.

## Superseded 2026-10-30
User supplied new README and requested re-execution. This star-theme derivative approach is archived, not active. Runtime/data/test edits restored to pre-attempt backups; stage0 reference binding now occurs only in Sort-Rework outside Unity. No claim of completed feature.

Stage0 reference rebind completed in /Users/zhangshuai/Documents/Sort-Rework, validation and3views saved; explicit README0.7 user confirmation gate pending. Unity restored6shelves24pieces,23/23 EditMode tests passed29a274d094994bf4841353b2cc7a5f8d. StageB Resources/Art/model loader removed via Editor API after external backup. No reference assets in Unity.
