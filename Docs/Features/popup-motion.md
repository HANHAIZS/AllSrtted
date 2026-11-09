---
id: popup-motion
title: Elastic popup opening and closing
status: Verified
owner: Project
created: 2026-10-28
updated: 2026-10-28
---

## Goal
All existing game modals gain lightweight time-driven spring-like open/close presentation.

## User-visible behavior
Open0.25s: PopupContent scale relative to authored final localScale85%→103%→100%, with opacity fade-in and DimBackground gradually darkening to its authored final value. Reach overshoot at70% of open time, smoothly settle by completion. Close0.15s: fade content/dim out and shrink gently to85%, then hide for reuse. Unscaled time works while gameplay or Time.timeScale is paused. Existing modal text, art, final geometry, gameplay timing, costs, saves and routes preserved. Cancel/reopen during animation is supported without accumulated scale or late hides. Replacement keeps dim darkness continuous and new modal on top.

## Out of scope
New art, altered game economy/timer/pause rules, Foundation router changes, custom per-popup animations, scene transition animations.

## Ownership and architecture
Existing project SortPopupView owns presentation state; focused partial handles cached RectTransform/CanvasGroups and unscaled Update. No Model/System/Command mutation. Foundation IUiPopupView/UiPopupRouter interfaces unchanged. Existing owner callbacks/router close notification remain synchronous; visual close can finish afterwards. IsOpen is logical, becomesfalse at close so stale callbacks cannot repeat transactions. Closing visuals stop accepting input. Scene destruction cancels visual work and unregister remains with existing owner.

## State and persistence
Transient visual state only; no save schema, SDK, network or content changes. Capture authored scales and existing group settings once, never recapture in-flight values. Missing decorative motion children falls back to previous instant show/hide.

## UI, prefab, scene, and Inspector impact
Existing Resources keys/prefabs/lazy-loading/cache unchanged: PausePopup,LeavingPopup,BoosterPurchasePopup,PrelevelPopup,ResultPopup (also Home entry purchase clone). Use existing PopupContent/DimBackground only. Add two CanvasGroups to runtime instances if missing, no new Canvas/manager/native asset edit. Owner SortUiLayers remains responsible for logical Pages/Panels gate; noninteractive close visuals and top-modal callbacks preserve input policy. On replacement, transfer existing dim opacity from sibling closing view to new view to avoid duplicate backdrop fading. Preserve authored Image.Type, sprites, color, Rects, text and final localScale (0.85 for illustrated modals;1 for result).

## Integrations and failure behavior
Close callback fires exactly once per logical open, at close request; finishing animation does not send a second callback or clear a newer router view. Open interrupts/reverses a closing visual, disable/destroy clears animation and restores authored values. No coroutine survives scene teardown. Existing immediate gameplay/scene actions retain their timing; only show/close presentation animates.

## Performance considerations
Cache child refs/groups once per instance; update active animations only; sibling scan on open only. No per-frame allocation, resource load, model lookup or disk write. Two small runtime CanvasGroup components per cached modal, reused thereafter.

## Acceptance criteria
- [x] Exact85%/103%/100% milestones at0/.175/.25seconds and close completion at.15.
- [x] Opacity/dim fade, authored endpoints, unscaled operation.
- [x] Rapid close/reopen, replacement dim handoff, stale callbacks, disable/destroy/reuse.
- [x] Shared5 prefab coverage and existing interaction/purchase/navigation regressions.
- [x] Native assets unchanged, compile/validator and runtime visuals evidence.

## Verification matrix
Focused PlayMode deterministic milestones and real unscaled time, interruption/gates/Replace. Existing PlayMode regressions after shared interface change. Native hashes and captured sequence. Verified; see evidence below.

## Definition of Done
- [x] Acceptance criteria and evidence recorded; limits stated; status Verified after checks.

## Verified delivery — 2026-10-28

- Shared SortPopupView.cs + focused SortPopupView.Motion.cs cover all5 existing popup prefabs, including the Home purchase clone using the shared purchase prefab. No prefab, scene, Canvas, art, loader, model, economy, save or Foundation package change. Captured final scales preserved: illustrated panels0.85, Result1; actual spring scales are their authored values multiplied by0.85/1.03/1.
- Exact milestone/real unscaled/interruption/Replace3 cases RED3/3 before motion (2728e86dfd1249a985fdd6b247035479), GREEN3/3 (c66ec871752241569723294e94629ba8). Full existing+new PlayMode62/62 passed (fc90fe77e6a848a49e34761121485841). Native-frame recording1/1 passed afterwards (1a00cb6b441344deb57a6add6a616461). Core behavior is unchanged; no additional EditMode run needed for this presentation-only boundary.
- `outputs/popup-motion/` evidence: red/focused/full-play/capture JSON, source backup, native protection hash comparison,17 native360×640 PNG frames, samples.csv and popup-motion.gif. GIF only encodes the Unity captures as an animated preview; no product artwork altered. Open sample: factor0.85 at0s,1.03 at0.175s,1 at0.25s. Close root inactive at0.15s; cached scale/group properties restore after hide, so final CSV row contains restored values of an inactive root. Background values progress0→1→0 relative to unchanged authored black Image alpha0.97.
- Rendering starts at near-zero panel opacity0.005 to retain first-frame pointer registration during the otherwise fully transparent entry; backdrop starts0. Fade reaches authored group alpha exactly. Closing both groups stop raycasts and content interaction; logical close callback fires synchronously once and visual completion never sends another notification. Thus existing purchase/pause/navigation callback timing stays intact.
- Runtime instance reuse, rapid reopen during exit, disable/re-enable, new modal ordering and backdrop handoff all passed. OnDisable restores captured properties and clears pending motion. Sibling scan occurs only on open, animation updates use cached references and allocate no coroutine/listener per frame. Only2 runtime CanvasGroups per cached instance, no new textures or frame-by-frame loads/writes.
- All7 protected native assets (2 scenes+5 modal prefabs) byte-identical. Independent read-only review found no important issues. Foundation validator0 warnings, compilation no errors. Final saved HomeScene Edit Mode, no Inspector setup needed. Real device frame pacing/GPU performance remains unprofiled. Scene-transition animation is outside this feature; scenes still load on their existing business actions.
