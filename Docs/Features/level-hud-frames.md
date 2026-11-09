---
id: level-hud-frames
title: Illustrated normal and frozen timer and level name HUD
status: Verified
owner: Project
created: 2026-10-29
updated: 2026-10-29
---

# Illustrated normal and frozen timer and level name HUD

## Goal
Replace the in-level title and timer UI using the three supplied references, retaining live level number and remaining time.

## User-visible behavior
Normal timer uses the supplied dark teal rounded panel with white live MM:SS numerals. Freeze Watch uses the ice-crystal timer frame exactly while Board.FreezeSeconds>0, including paused frozen play; expiration and restart/next level restore the normal panel. Manual pause without Watch does not display ice. Level name uses its dark teal panel, rounded white letters with purple glow/outline and live LEVEL01/02/03. Sample02:48/LEVEL01 are not baked into runtime art. Top row keeps its left-title/center-timer/right-pause arrangement; new panel proportions align across phone/tablet. Existing animations and timer/booster logic remain unchanged.

## Out of scope
Time/stock/price/reward changes, new timer mechanics, gameplay/popup redesign and font licensing/source changes.

## Ownership and architecture
Existing SortGameViewController.Boosters renders a new optional FrozenTimerPanel Image from the cached model freeze state, using the same bounded UI refresh path. Gold gradient is a stateless Unity UI mesh effect in Utilities/Presentation; no architecture registration or Model/System/Command changes. Editor LevelHudArtwork authors only level/timer rects, effects and sprites plus one scene controller binding. PrototypeHudLayout calls the focused helper for reproducible future UI authoring.

## State and persistence
No save or gameplay state changes. The existing Freeze Watch Command/Board owns15-second freeze and countdown. Static art prepared from supplied images: remove external white/checker and sample text, preserve panels, true alpha; render live text separately. Existing legacy scenes without new frame keep their previous tint fallback.

## UI, prefab, scene, and Inspector impact
GameScene existing SafeArea/LevelPill and ClockPill remain bound to existing Text components, with directly referenced Assets/Sprites/UI/HudFrames/{Frozen,Normal,Level}.png. No Resources key or new popup. New FrozenFrame Image beneath Timer Text toggles visible; enlarged outer ice frame keeps the same inner timer text position. Input raycasts remain off for decoration. Allowed authored edits: only the two top HUD panel geometry, image refs and corresponding text/font/outline/gradient style plus new frozen-frame binding. Preserve pause button, board, toolbar, popups, entry animations and unrelated references.

## Acceptance criteria
- [x] Dynamic level and time reflect live state with reference-style art/colors; no fixed sample digits remain.
- [x] Watch enables ice while frozen, expiry and restart clear it; pause alone leaves normal frame.
- [x] White text remains legible in both states and HUD does not overlap pause/title on phone/tablet.
- [x] Native writes use Editor APIs and protected UI remains unchanged.

## Verification matrix
| Boundary | Evidence |
| --- | --- |
| Runtime | Freeze use/held timer/expiry/pause/restart and level labels |
| Visual | Phone/tablet normal and frozen renders |
| Compilation/conventions | Unity console and project validator |
| Performance | No extra per-frame lookup/subscription, gradient mesh only updates with Text |

## Definition of Done
Acceptance checked against real scene render/readback and tests; no unverified completion claims. Restore HomeScene clean Edit Mode. Device touch/safe-area/frame performance remains real-device validation.

## Verified evidence (2026-10-29)
- Full EditMode189/189 passed, job3835f9dde0754911a8d9983c8a4b6620. Focused runtime17/17 passed, joba99254e297b543a989137e4c62c0703c, covering HUD, actual Watch, entry and restart. After soft-glow refinement final HUD3/3 passed, job25a3491a4ee1449e91fc978dad8b1bbe.
- Runtime verifies live02:48/gold Text, ice enabled by Watch and held through pause, normal restored on freeze expiry and restart, dynamic LEVEL01/02/03, manual pause creates no ice, restart entrance still gates time. Existing Text references retained. All tests use memory wallet.
- Phone720x1280 and tablet768x1024 screenshots/results/native audit: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/level-hud-frames/. Final capture fits the actual camera/background to each target aspect.
- Native scene audit against backup: prior changed blocks belong only to Level/Timer/LevelPill/ClockPill and Sorting Game new FrozenTimerPanel binding. New FrozenFrame and text effect components allowed by scope. Pause, shelves, bottom buttons, other UI and prefab assets unchanged. Scene saved via focused Editor API helper; font/texture imports use Unity native APIs.
- Asset dependency audit now explicitly allows only the3 new HUD PNG paths. Art removes sample text and external background; white/purple title remains dynamic with cached radial glow mesh, timer gold uses a vertical vertex gradient over live builtin bold glyphs with outline/shadow. The supplied font appearance is approximated with existing project/builtin fonts; no unlicensed font files added.
- Changed production sources: Game Boosters partial (one optional Image binding and state refresh); SortHudGoldGradient/SortHudTextGlow Unity presentation mesh helpers; LevelHudArtwork plus focused hook in PrototypeHudLayout. New3 directly referenced Sprite textures under HudFrames. No Model/System/Command/save change and no new subscriptions or per-frame hierarchy/dependency lookups.
- Gold effect loops cached vertices with no list allocation; title glow uses cached buffers and rebuilds only when Text mesh changes. Three shared textures max512/1024. Device memory/frame cost and actual safe-area hardware remain device checks.
- Convention validation passed zero warnings; compilation successful. HomeScene returned clean Edit Mode.

## White timer revision (2026-10-29)
User requests white countdown numerals in normal and frozen states. Remove gold-gradient effect from the existing Timer Text, retain dark outline/shadow and live binding. Update focused authoring helper; no panel/position/timing/state changes. Verified9/9 focused PlayMode, job512adac890c54e5f83d0da5701fd8bab, covers white timer in normal/frozen modes and shop grants/failure feedback. Native API removes only Timer gold-gradient component and changes its outline to dark teal; white Text binding/panels remain unchanged. New screenshots and JSON at /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/white-timer-clean-shop/. No new textures, subscriptions or gameplay state.
