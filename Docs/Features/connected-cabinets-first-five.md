---
id: connected-cabinets-first-five
title: Continuous connected cabinets for the first five source stages
status: Verified
owner: Project
created: 2026-11-02
updated: 2026-11-02
---

## Goal
User corrected the first-five shelf implementation with three connected-cabinet references and explicitly requested connected structures. Replace the separate per-row boxes with a continuous cabinet silhouette and shared dividers; preserve each stage's staggered relationships, source shelf IDs, contents, layer rules, locks, legal solutions, timers, economy, goods and UI. This continues and supersedes the independent-row presentation in modular-shelves-first-five.

## User-visible behavior
Use first-stage ivory/dark interior/gold lip palette and existing material/texture. Compact connected layout uses 3.55-wide cells, 1.6 row pitch, .94 slot pitch and existing floor support -.487. Source x scale is 3.55/3.13; source y scale is 1.6, except stage1's 1.5-source-unit row separation maps to the same1.6 pitch. Original JSON remains byte-identical. Slot input, goods, locks and camera all consume this same display mapping. Cabinet cell vertical bounds -.65..+.95; front/rear slot local offsets stay (0,0,-.5)/(0,.34,.35). Adjacent cells share their structural boundary, including partial staggered boundaries; do not join complete boxes with decorative thin lines.

## Out of scope
New levels, non-three-slot rules, new merchandise, new lock art or mechanics, UI redesign, save/economy changes, hardware certification and changing original source JSON. Keep original modular shelf and S1 resources as recovery assets.

## Ownership and architecture
Editor authors five connected cabinet variants from actual stage layout and current shelf art palette. Native prefab/mesh mutation only through Unity APIs. Each variant has shared StaticBody and BackPanel groups, no Collider, and Anchors/Shelf_i/Front0..2 + Rear0..2 at mapped positions. Offline union of structural cell regions removes duplicate interior faces and retains beveled exterior/compartment edges; gold lips remain part of the static artwork. All instance geometry/materials shared; no runtime mesh generation or per-frame dependencies.

Existing controller gets ConnectedShelfPrefabs[5] direct references. One ConnectedArtwork instance under Source Shelves, plus model-indexed SourceShelf_i roots for independent lock visuals. Legacy/source scenes outside compiled first5 keep existing behavior. Missing/invalid connected art falls back to valid modular row art, then valid old S1 art, then visible primitives; no blank stage or model mutation. Owner disposes its instances on reload/destroy and never destroys shared assets. Existing Commands/Models remain authoritative; no new runtime component, architecture or persistent state.

## Acceptance criteria
- [x] All five connected silhouettes and shared partitions inspected; adjacent cells form continuous borders, including stage3's stagger.
- [x] All5 at720x1280 and768x1024 inspected; final clean captures and overview saved under workspace outputs/connected-cabinets-first-five.
- [x] Native import verifies each body/back is one closed connected component, no boundary/nonmanifold edges or degenerate triangles, exact bounds, shared meshes/materials, zero colliders and front/rear anchors.
- [x] Actual pointer solutions complete all5; lock countdown, repeat entry/owner cleanup, restart, boosters, malformed connected art and modular/legacy fallback covered.
- [x] 4972-file protection baseline: only expected controller/test edits and new GameScene reference fields changed; no missing file. Original configs, solutions, goods art, UI/economy code and original materials/textures preserved. Validator0 warnings, compile0 errors.
- [x] Connected fixture RED4/4 missing bindings then GREEN4/4. Full PlayMode108/108 and EditMode224/224 passed; final shading revision followed by focused verification and fresh224/224. See the intermittent replay note below; no device FPS claim.

## Lifecycle and performance
Connected cabinet body/back do not cast realtime shadows: isolated before/after renders confirmed frame shadow bias caused diagonal streaks on the back. The back uses one shared material variant at Assets/Art/Materials/Sorting/Connected/M_ConnectedCabinetBack.mat, preserving the same atlas and lighting but disabling received realtime shadows to avoid large detached merchandise shadows on the deep back. The body/floors remain shadow receivers for contact. Original material, lights and goods shadow settings stay unchanged; no per-instance material cloning.
Scene-bound direct prefab references; instantiate one cabinet variant only for current stage, no Resources lookup/new manager/UI layer. Offline geometry shared by all entries. Record renderer/triangle/material counts, reload cleanup, old assets and recoverable source files. Device performance outside this local Editor verification; other widths/new mechanisms/new goods excluded.

## Verification matrix
| Boundary | Required evidence | Result |
|---|---|---|
| Native artwork | source preservation, connected topology, shared atlas, exact bounds/anchors | outputs/connected-cabinets-first-five/unity-import.json and export-validation.json |
| Runtime | connected fixture, modular fallback, all-five pointer solutions/restart/boosters | full-playmode.json108/108; connectedGreen.json4/4; connectedFinalFocused.json8/9 then connectedFinalReplay.json1/1 |
| Visual | all5 at720x1280 and768x1024 | captures/ and first-five-overview.png, final back material avoids detached shadow blobs |
| Protection | source hashes, exact scene diff, compile, validator, regression suites and review | protected-after-comparison.json; game-scene-native.diff; full-editmode.json224/224; review approved |

## Verification note and limits
One final combined replay produced an intermittent Pick5:1,2 failure. The prior full108 run and several prior all-five replays passed. Failure diagnostics now include move index, phase, input gate, lock, visual and slot position. The unchanged runtime passed the subsequent independent all-five replay; the cause was not reproduced and is not attributed to the material change. Preserve both failed and passed reports, rather than report a fictitious final9/9 run. Physical-device FPS/memory and future non-three-slot mechanics remain unverified.

Cabinet-only triangle totals stage1..5:2232/7808/8382/9488/10720; two renderers each. Original shelf atlas reused; one shared back material variant. No runtime mesh rebuild or per-instance material cloning. Total scene draws/frame time were not measured.

## Definition of Done
All scoped acceptance checks have fresh evidence, firstfive connected cabinets are saved/bound and playable, original rules/config/resources preserved, no unrelated native changes, and local verification limits disclosed.
