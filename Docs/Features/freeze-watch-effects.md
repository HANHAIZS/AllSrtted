---
id: freeze-watch-effects
title: Watch flight frost impact and snow
status: Verified
owner: Project
created: 2026-10-30
updated: 2026-10-30
---
# Freeze Watch presentation
## Goal
Make Freeze Watch activation legible through icon flight, impact, ice HUD and full-screen frost/snow throughout its existing15-second freeze.
## User-visible behavior
Click stocked watch: icon flies from bottom dock to just below clock .28s, dial glows; frost pulse starts .22s, hits .4s and vanishes .4s later. At impact execute existing UseSortBoosterCommand, debit once, start existing15-second freeze, white flash/iced HUD; frame sweeps inward and fades in .35s, snow begins. On expiry clock immediately returns normal, frame and snow fade2.5s. Pause holds animation and existing model countdown. Restart, win/loss, exit and scene disable Clear immediately. Repeated launches rejected. No final preview requested.
## Ownership and architecture
SortWatchFreezeView project ViewController owns only transforms/alpha/pooled snow. Scene SortGameViewController holds cached reference and sends command at hit. Model/System retain existing freeze duration and inventory rules; no save migration or Foundation edits. UI Effects layer4 is noninteractive below modal Popups? Existing host sort order400 means abovePopups200; all effects raycastfalse, hide/dim as needed on pause. No additional scene/camera. Source frame/snow normalized to true RGBA with imagegen, original sources preserved; final Sprite assets Assets/Sprites/UI/WatchFreeze. One directly referenced scene effect instance, no Resources key.32 cached snow Images, no per-frame allocations/lookups; teardown clears without late callback.
## Acceptance criteria
- [x] Watch flight -> .4s hit -> flash/frozen skin/edge sweep/snow.
- [x] Model freeze15s starts at hit, no repeated spend, pause holds, thaw2.5s.
- [x] Restart/terminal/disable clears all; no raycast blocking; true transparent assets.
- [x] Compile, focused tests, native bindings and convention gate pass.
## Verification matrix
Focused PlayMode actual scene timeline/stock/debit/pause/expiry/clear and existing HUD rules. Source pixel alpha and asset readback. Device performance/manual appearance untested. Changes restricted to new effects, related UI flow and GameScene direct binding; preserve other scene authored data. Save errors leave launch uncommitted and hide flight. No sound added.

## Out of scope
Other boosters, freeze duration changes, sound, extra wallet mutations, fullscreen input blockers.
## Definition of Done
Focused effect and HUD tests pass; correct native bindings/alpha, successful convention validation and compiler checks. Device appearance/performance is manual verification.

## Evidence
- Runtime/editor scripts: SortWatchFreezeView, WatchFreezeArtwork, SortGameViewController.Boosters partial. GameScene direct reference in Effects host4;32 cached Snow Images, input raycasts disabled. UI VFX pauses along with gameplay; other boosters/inventory unchanged.
- Generated true RGBA frame from supplied fake checkerboard source (preserved ice edges/corners, transparent center), copied to Assets/Sprites/UI/WatchFreeze/Frame.png; native pixel readback centerAlpha0.0. White snowflake normalized similarly, Pulse.png code-authored radial wave/glow. No materials or new camera. Frame2048max, Snow256, Pulse128, no mipmaps.
- PlayMode5/5 joba1c018de8a0741fa9b9933df9b43d7fb passed. Earlier7a42ad8a2337442a85a3ec6d26d14073 passed4/5; old fixed14.8s HUD assertion crossed actual expiry after new .4s flight/real frame time, changed test to use current remaining freeze for deterministic within-freeze and expiry boundaries. Effect tests already passed, production duration unchanged. Initial resource test c1b5ef0ac1e54493bbf9b8b1e95d5462 returned0 tests during compilation, not treated as verification.
- Native readback:32flakes, all Graphics nonblocking, correct dock origin and FrozenTimerPanel references. No new lookup in callback/frame methods. Clear allocates one5-element Image array only on lifecycle reset; frame updates reuse transforms.
- Console contains existing Unity Test Framework PlaymodeLauncher.cs113 exception from test launcher recovery, no gameplay/compiler errors found; focused tests completed. No final preview supplied. Hardware rendering/performance remains manual.

- Final foundation convention gate passed0warnings. Scene returned to HomeScene/EditMode without dirty edits. Frame generated exec-80657d5b-6ea4-4128-93c6-4d1b268f82bd.png and snow exec-8f20874d-e262-4bd9-a831-2163dbff7a7d.png preserve source motifs with true alpha. Additional snow refinement was inspected but not substituted; chosen existing bound snow remains stable. Built-in edit prompts removed baked checkerboard only, retained source ice/snow style; sources kept unchanged.
