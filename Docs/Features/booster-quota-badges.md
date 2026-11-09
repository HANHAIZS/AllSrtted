---
id: booster-quota-badges
title: Session booster charges, corner badges and press feedback
status: Verified
owner: Project
created: 2026-10-26
updated: 2026-10-26
---

# Booster quota badges

## Goal

Give each existing bottom booster its own remaining-use allowance, supplied corner badge art and tactile visual press/release feedback.

## User-visible behavior

- Each of the four boosters starts a new game session with 3 uses. A successful activation consumes exactly 1 of that booster only. Rejected/invalid/paused/terminal attempts do not consume uses.
- At the button's bottom right, show the supplied red circular badge with its remaining number while the allowance is positive. At zero, replace it with the supplied green plus badge. The user chose replacement, not simultaneous display.
- Clicking the green plus adds exactly 1 use to its own booster, then it becomes a red 1. It must not also activate the parent booster or charge/launch anything else. Stale repeat callbacks cannot add again while the badge is a number.
- The red number is informational: it can show press feedback but does not add uses or activate the parent booster. Main booster clicks use the existing gameplay path.
- Zero allowance disables the main booster even if its gameplay condition is otherwise eligible. A plus remains usable during Playing even when that booster currently has no eligible target. Pause/results/input gates block both use and refill.
- Restart, next level, scene reload and Editor level selection preserve current session allowances. Restarting the application resets all four to 3. No disk save.
- Both the four main buttons and the corner badges shrink on press and rebound on release/click. Cancel/exit/disable/modal interruption restores authored scale without drift. The plus/count visual change must retain feedback correctly and not double-trigger.

## Out of scope

Money, ads, payment confirmations, item stores, permanent inventory, sound/haptic SDKs, original-game assets and changes to the four booster effects.

## Ownership and architecture

| Responsibility | Owner | Path | Notes |
| --- | --- | --- | --- |
| Session allowance state | Model | Models/Sorting/SortBoosterModel.cs | Four independent counts and observable revision |
| Atomic use/refill policy | System | Systems/Sorting/SortGameSystem.cs | Spend only after accepted effect, add only at zero while Playing |
| Mutation requests | Command | Existing UseSortBoosterCommand; new AddSortBoosterChargeCommand | No direct model writes from UI |
| Registration | Architecture | Architecture/AllSortedApp.cs | Register one project model in the existing architecture |
| Presentation | ViewController | SortGameViewController.Boosters.cs and new SortButtonPressFeedback.cs | Cache model/buttons, bind/unbind listeners, state-driven badge text and art |
| Authoring | Editor | PrototypeQuotaArtwork.cs; PrototypeBoosterArtwork.cs rebuild integration | Targeted corner children and direct bindings, retain main button geometry |
| Foundation | None | None | Shared packages unchanged |

## State and persistence

Session-only allowance lives separately from replaceable board content. Initial 3/3/3/3 belongs to a newly initialized model. Normal level/reset commands do not reset it. No PlayerPrefs, save schema, disk or migration. Preserve existing controller -> Command -> System ownership.

## UI, prefab, scene, and Inspector impact

- Existing scene: Assets/Scenes/GameScene.unity. Existing main bottom button bases, icons and anchor/size/layout remain. Add one 44x44 corner badge button per booster at bottom-right, with count text overlay and state-selected Sprite.
- Direct-reference Sprite assets: Assets/Sprites/UI/Boosters/AddChargeBadge.png and RemainingChargeBadge.png. Use built-in imagegen for true-alpha extraction of only the supplied checkerboard backgrounds; preserve subject details. Keep aspect and crop Sprite metadata around the visible badge.
- Main and badge buttons receive a project-owned reusable press feedback component, with base scale captured before animation, unscaled-time interpolation and original scales restored on disable/cancellation.
- Host remains Pages/SafeArea/BottomButtons; no new Canvas, layer, popup or resource key. Existing modal gates apply. Badge input consumes its own clicks; child count text does not intercept raycasts.
- Controller owns listeners and cached model references. Scene owns Sprite/direct references and feedback components. Destroyed UI must not be refreshed or leaked.
- Shift only the existing watch FreezeCounter rect 12 units left and reduce its width from 72 to 60 to avoid the new corner badge. Its text, behavior and height remain unchanged.
- Serialized Unity assets are edited only through Editor APIs, retaining unrelated Inspector references and image fitting.

## Failure and recovery

Unknown booster, no quota, no valid target, active freeze stacking, pause/terminal or stale hidden badge attempts are rejected without a charge mutation. Plus only changes quota; it does not mutate the board, consume a use or alter time. Main and badge pointer handlers respect actual Selectable/CanvasGroup eligibility, one pointer owner and cancellation. At UI-first teardown, cleanup must not access destroyed buttons.

## Performance considerations

Four counters, revision-driven badge updates, cached dependencies/listeners. Eight simple press components do no allocation and skip idle animation. No per-frame service lookup or texture modification. Two shared small Sprite textures, imported without mipmaps.

## Acceptance criteria

- [x] Fresh session has four independent allowances of 3 and red 3 badges.
- [x] Exactly one allowance is spent only after successful effect; zero blocks use.
- [x] At zero the green plus appears; one click changes it to red 1 and does not trigger the parent or accept stale repeats.
- [x] Retry, all level transitions, scene reload and Editor selection retain allowances; fresh model starts at 3.
- [x] Pause/result gates and failed target conditions do not spend or refill incorrectly.
- [x] Main/badge press, release, pointer exit/cancel and disable restore authored scales without drift or cross-triggering.
- [x] Supplied badge art fits bottom-right on phone/iPad, with legible numeric counts and unchanged main button layout.
- [x] Full project tests, Unity compilation, authoring readbacks and conventions pass.

## Verification matrix

| Boundary | Evidence | Result |
| --- | --- | --- |
| Model/System/Command | 8 SortBoosterQuotaTests; standalone Mono harness 67/67 including existing rule cases | Passed |
| Actual scene | BoosterQuotaPlayModeTests: zero/plus/one, actual GraphicRaycaster pointer isolation, retry/Editor selection/scene reload, pause and timeout gates; existing UI-first teardown test | Passed |
| Feedback | Main/badge pointer-down/up/exit/cancel/disable and eight repeated presses restore exact scale | Passed |
| Assets/layout | Saved-scene PrototypeAssetTests validates 44x44 bottom-right anchors, bindings and Sprite imports; quota-authoring.txt confirms 273 protected components unchanged; quota-phone/ipad/empty/refilled.png visually reviewed | Passed |
| Regression | TestResults/quota-green-edit.xml 72/72; quota-green-play.xml 25/25; Unity compiles; project validator zero warnings; independent read-only review no actionable findings | Passed |
| Device | Physical iPhone/iPad touch/haptics/performance | Not available; no claim |

## Definition of Done

- [x] State and mutation ownership match project conventions.
- [x] Required local criteria have concrete evidence.
- [x] No unrelated art/layout, booster behavior, Foundation or commerce changes.
- [x] Performance and manual platform limits reported.
- [x] Temporary verification tooling cleaned up; status Verified only after checks pass.

## Verification records and limits

- TDD: quota-red-play.xml recorded 4 expected failures before badge authoring; final full suites passed after integration. Supplemental model/policy red-green logs are under workspace work/booster-quota-rules.
- Delivered preview/evidence copy: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/all-sorted-prototype. Badge generation prompts, source/output paths and alpha observations are recorded in quota-badges/manifest.json. Both PNGs retain the supplied design with genuine transparent backgrounds, but the generation tool produced 1254px images, slightly translucent subject pixels and very faint outer alpha noise; Sprite metadata crops to visible alpha bounds. No claim of pixel-identical extraction.
- No money, ad request or network call is attached to refill. Allowances are in-memory only. Four model counters and eight idle-skipping animation components; no runtime texture reads or new Resources loads.
- Physical iPhone/iPad input/performance remain untested. Multi-pointer ownership and interrupted-modal paths are guarded in code; automated feedback checks cover release/exit/cancel/disable, rather than every concurrent-touch/modal ordering.
- A new application model is tested to start 3/3/3/3; no disk data survives process restart. Editor domain-reload settings were not changed.

## Decision log

- 2026-10-26: User supplied plus/count badge images and requested default 3, add-one click and feedback.
- 2026-10-26: User explicitly selected red count while positive and green plus only at zero; selected retention across retry/level changes and reset to 3 only for a new game application session.

## Follow-up — 2026-10-28

The green empty-stock badge now opens the paid purchase popup:1600 coins for3 uses; remaining uses persist across restarts. This supersedes the old player +1 free-refill/session-reset behavior. See booster-purchase.md. Existing booster effects, red count badges, press feedback and scene/retry retention remain.
