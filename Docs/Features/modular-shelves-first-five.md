---
id: modular-shelves-first-five
title: Shared modular recessed shelves for source stages one through five
status: Implementing
owner: Project
created: 2026-11-02
updated: 2026-11-02
---

## Goal
Continuation: user subsequently requested connected cabinets; final delivery and current visual acceptance are governed by connected-cabinets-first-five.md. Original modular assets remain the recovery path.
Implement stages A/B of the approved shelf plan for the real first five compiled stages. Reuse first-stage shelf art/UV/material, split fixed end caps, middle, front lip and independent back; merge static parts for runtime. All five use the same modular recessed three-slot design, with independent existing match locks. Preserve source data, legal solutions, merchandise, UI and economy.

## User-visible behavior
All source1–5 shelves use shared rounded shallow-color frames, gold front lip and dark recessed back; later stages no longer show five raw Cubes. Preserve0.94 slot pitch, existing source coordinate mapping and per-stage camera safety. Existing first-stage goods and source2–5 primitive goods retain identities and gameplay. Ground visual goods against shelf floor where necessary without moving model slots. Locks continue to show authoritative remaining match counts.

## Out of scope
Non-three-width mechanics, new levels, generators, door/dispenser/moving-shelf rules, replacing goods or UI, changing timers/solutions/coins/energy/booster rules, modifying original resource package or old art, hardware certification. No automatically invented shadow textures; reuse existing baked albedo and geometry first.

## Ownership and architecture
Editor/Sorting owns native mesh/prefab bake and explicit GameScene binding; Blender source/modules under workspace and Assets/Art/Models/Shelves. Utilities/Sorting owns pure shelf geometry constants/calculations. Existing SortGameViewController focused shelf/source partials instantiate shared direct-reference art, position anchors, render locks and own cleanup. No new runtime MonoBehaviour/architecture/system/command needed; model/system state and mutation unchanged.

## State and persistence
Presentation only, no saves/schema/state changes. Existing Commands, Source Board and lock state remain authoritative. Original JSON, Configs/Sorting/SortExpressFirstFive and solution witnesses hash-protected.

## Assets and UI impact
New Assets/Art/Models/Shelves/ source modules; shared static combined Mesh saved via Editor API. Existing first-stage1024 texture and material reused. New Assets/Prefabs/Shelves/PF_ModularShelf3.prefab with StaticBody, BackPanel and three front/rear anchor pairs, no Collider; GameScene adds only ModularShelfPrefab binding. Existing SourceLevel1ShelfPrefab/merchandise refs preserved for recovery. Fallback invalid/missing modular -> old source1 art if valid, otherwise current primitives. Legacy scenes unaffected. Existing Pages/Effects/Popups layer policy unchanged; no new Canvas/Resources path lookup; window/instance owner existing game controller.

## Failure and lifecycle
Validate both required mesh groups and anchors before instantiation. Inactive/rendererless/meshless/partial malformed prefab must fall back visibly with no orphan or invisible shelves. On stage reload owned shelves disposed; shared asset meshes/materials never destroyed. Native baker refuses dirty/playing state; old assets retained. Locks are disabled/cleared on reload based model, no duplicate subscriptions.

## Performance
One shared static body mesh/material plus separate back renderer; static cap/middle/lip authored as modules but combined offline, no per-frame Mesh.CombineMeshes or per-shelf unique materials. Reuse existing texture. Record renderer/triangle/material/instance counts, scene isolation and repeat-entry cleanup; do not claim device FPS without profiling.

## Acceptance criteria
- [ ] Real first-stage mesh split into left/middle/right/back/lip with UV intact; source artifacts recoverable.
- [ ] New prefab uses shared static body and independent back, consistent bounds near3.55x1.74x1.54 and no colliders.
- [ ] All source1–5 instantiate modular art, share mesh/material assets, keep slot positions and model state.
- [ ] All five actual pointer solution replays complete; locks/refill/retry/boosters and fallback remain valid.
- [ ] Phone/tablet views inspected for all5, no floating/clipping/overlap introduced.
- [ ] Source config/solutions/old art/native properties outside new binding protected, compilation/convention checks pass.

## Verification matrix
| Boundary | Required evidence | Result |
|---|---|---|
| Red/green | focused modular display assertion fails before binding | Pending |
| Native assets | UV/bounds/renderer/mesh/material/anchor readback | Pending |
| Runtime | first-five pointer solutions, invalid art fallback, locks, reload counts | Pending |
| Visual | phone/tablet stage1–5 captures | Pending |
| Protection | source/native comparison, validator, review | Pending |

## Definition of Done
All scoped checks pass and limitations disclosed; first five shelf visuals upgraded without claiming unsupported width/obstacle work complete.
