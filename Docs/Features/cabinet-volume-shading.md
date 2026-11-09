---
id: cabinet-volume-shading
title: Connected cabinet volume and warm palette
status: Implementing
owner: Project
created: 2026-11-02
updated: 2026-11-02
---

# Connected cabinet volume and warm palette

## Goal
Restore depth in the five connected cabinets without cabinet shadow streaks, using cream frames, amber shelves, teal recesses, local AO and contact shadows.

## User-visible behavior
- Frames have lit bevels and darker inner edges; each recess has its own shading. Goods meet soft contact shadows on their support planes.
- All five stages retain the authored 8-degree downward pitch with zero yaw/roll, use Orthographic (L1 Size4.8, larger layouts auto-fit) and share the upper recess shading. Camera distance does not alter scale under orthographic projection. Existing input, slots, goods and every UI flow remain unchanged.

## Out of scope
Gameplay, configurations and solutions, UI/economy, model silhouettes, Foundation packages, reflection probes and global rim lights.

## Ownership and architecture
- Unity presentation: SortGameViewController.Cabinets/ContactShadows partials. No state mutation, new model/system/command or save migration.
- Editor: ConnectedCabinetArtwork and CabinetVolumeArtwork; Blender offline mesh material/color authoring. URP package sources remain untouched.

## State and persistence
Only transient owned visual instances/property blocks. No gameplay or persistent state changes.

## UI, prefab, scene, and Inspector impact
- Direct references: Assets/Prefabs/Shelves/Connected/PF_ConnectedStage1..5.prefab, existing ConnectedShelfPrefabs field. Two renderers, body two submeshes, back one; imported FBX meshes retain spatial geometry and anchors.
- Art: Assets/Art/Shaders/Sorting and Assets/Art/Materials/Sorting/Connected. Contact material and mesh directly bound on GameScene controller, cached once, disposed owned instances only.
- GameScene lights, camera post processing, separate CabinetEffects volume layer/profile. Home, Shop, Settings and uGUI Overlay authored properties protected.
- Assets/Settings/URP-Balanced MSAA2/SSAO off; HighFidelity MSAA4/SSAO tuned.
- No Resources keys, popups, modal gates or Sprite changes. Missing shadow resources gracefully omit shadows. Lock doors use the original Assets/Art/Textures/Sorting/LockDoor.png via a direct material reference on the scene controller; native font supplies live numeric thresholds. A world-space mesh and TextMesh have no collider or raycaster. Missing door art retains the old bar fallback. No collider or input policy changes.

## Integrations and failure behavior
No SDK/network/save changes. Cabinet cast shadows always off; back receive shadows off if A/B is unstable. Contact instances clean up on replacement, disappearance, death, restart and owner destruction; shared assets survive. Offline rebake retains new shading.

## Performance considerations
Three cabinet material draws instead of two. One shared unlit transparent mesh/material per visible contact shadow, cached property blocks and no per-frame allocation. Per-cell analytical AO supplements vertex AO where the existing large back triangles cannot interpolate a local corner gradient; at most 13 cells. Balanced SSAO disabled. Actual mobile 60fps requires target hardware and is not inferred from Editor results.

## Acceptance criteria
- [x] Three independent materials match approved colors, metallic and smoothness; preserved imported spatial geometry/anchors, no cabinet casts. No palette changes during the later camera/shadow/door work.
- [x] Inner AO, bevel rim and horizontal contact shadows work without UV0 AO. Visible upper-wall darkening is verified by pixel comparison; ceiling shadow property block is common to all five stages. Cabinet casting remains disabled; back reception stays off to avoid the previously diagnosed detached-shadow artifacts.
- [x] Contact shadows follow actual floor positions through drag/cancel/refill/match/boosters/restart/disposal and never fly with goods. Rear preview lift does not lift the shadow.
- [x] Five stages remain playable with unchanged configs/solutions; 9:16 and 3:4 captures checked.
- [x] Full EditMode 234/234, PlayMode 111/111 and convention checks pass; no unexpected runtime errors in the tests.
- [ ] Mobile Balanced frame-time budget verified on target hardware (unavailable locally unless a device is supplied).

- [x] Gray locks replaced with provided door image, live counts and unchanged thresholds. Camera orientation retained across all five levels.

## Verification matrix
| Boundary | Evidence | Result |
| --- | --- | --- |
| Materials/geometry/quality | CabinetVolumeAssetTests, Blender/native signatures | 234/234 full EditMode pass; ten spatial triangle sets and winding identical; corner normal deltas within FBX float tolerance 5e-5 |
| Lifecycle/input | ConnectedCabinetPlayModeTests + full source campaign pointer replay | Final full PlayMode 111/111; rear floor regression RED→GREEN, real five-stage pointer campaign and lock thresholds pass |
| Visual | Internal before/after phone/tablet captures; SSAO A/B | All five 720×1280/768×1024 captures inspected; upper shadow changes actual rendered pixels. Frame Debugger API returned zero events, so actual SSAO pass capture remains unverified; Balanced does not require SSAO |
| Conventions/compile | validator / Console | Foundation validation 0 warnings; shaders compile; no unexpected runtime logs in passing tests |
| Performance | allocations review and target phone CPU/GPU | Device check unavailable |

## Definition of Done
- [x] Presentation-only ownership preserved and native resources read back.
- [x] Local behavior, regression and convention verification passes.
- [ ] Device-only performance limitation reported; status remains Verifying until hardware evidence exists.

## Decision log
- 2026-11-02: User approved the concrete plan in workspace outputs/cabinet-volume-plan/改动计划.md; active saved project backed up before edits.

- 2026-11-02 user steering: stop further color/AO tuning, match the two newly supplied slightly elevated front views and soft down-right backdrop shadow. This explicitly supersedes the original zero-angle constraint. Horizontal contact quads are now viable. Keep cast shadows off and input/world anchors unchanged; validate pointer projection at both aspect ratios.

- 2026-11-02 final steering for this pass: first-level visual check takes priority. Preserve existing colors; L1 runtime pitch is 10 degrees and a local soft ceiling shadow is enabled only on its back panel. Other levels keep the previous 8-degree camera setting. Lock-door replacement deferred at user request. Full regression after these last angle/shadow changes remains pending.

- 2026-11-02 user approved current authored camera orientation (readback X8/Y0/Z0), requests Perspective/Base vertical60 near0.1 far50, automatic appropriate first-stage framing and all five stages to share these camera/shadow policies. Lock-door image is now authorized: replace gray bars with original supplied artwork and dynamic counts; do not change match thresholds or layouts. Current palette remains protected.

Evidence directory: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/cabinet-volume. Fresh review found one rear shadow support issue, reproduced and fixed; no other important findings. 109 protected Commands/Models/Systems/config/UI/source-art files unchanged.

Final local evidence: full-edit-result.json, full-play-final-result.json, geometry-comparison.json, protected-comparison.json, approved-camera-lock-native.json, final/ ten captures. Backdrop shadow A/B rendered 2740 darker background pixels with the supplied projection. All local checks pass; status stays Verifying only for target-device performance and actual Frame Debugger SSAO pass evidence, which were not available.
