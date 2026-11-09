---
id: leave-confirmation
title: Pause Quit leaving confirmation
status: Verified
owner: Project
created: 2026-10-28
updated: 2026-10-28
---

## Goal

Implement the supplied complete 弹窗素材3.png layout for Pause -> Quit -> Leaving confirmation. Gold/purple panel and header reuse existing PauseSettings sprites; top-right Close; cream inset contains broken heart and lower-right -1; white rounded Leaving title with purple outline follows Are You Sure？.png font style; below inset You will lose 1 life and red Leave button. No new payment or booster flow.

## User-visible behavior

Quit opens confirmation, keeps board paused and does not debit. Close returns to the same pause instance, still paused, with no debit; pause Close subsequently resumes. Leave uses existing TryReturnHome and wallet transaction, preserving one debit per attempt, at-zero exit and save-failure retry. Existing result HOME remains direct. Fixed user-requested -1/copy is display only; ledger caps at0 and never charges a previously settled attempt twice.

Presentation is a focused partial of existing SortGameViewController; no new runtime controller/Canvas/System/Model. All wallet changes stay behind existing commands. New prefab Assets/Resources/Prefabs/UI/Popups/LeavingPopup.prefab, configurable key Prefabs/UI/Popups/LeavingPopup, lazy first Resources load, dictionary cache and same controller teardown. Existing UiPopupRouter Replace and Popups host; original pause instance retained. Only top/open view and current attempt can act; stale/hidden callbacks rejected. Missing/invalid confirmation resource retains pause and permits retry; unavailable Home retains confirmation. Decorative graphics do not intercept input. Close must work with0 energy.

## Ownership and architecture

New Assets/Sprites/UI/Leaving/{Heart,LeaveButton,Inset}.png directly referenced by new prefab. Heart checker removal and blank button generated with built-in imagegen; source/outputs/prompts recorded in evidence. Inset source copied unchanged. Existing Background/Header/Close sprites and Fredoka font reused. No edits to original PausePopup, ResultPopup, HomeScene or GameScene are required; new key defaults in source. No save-schema, SDK, network or Foundation package change. Prefab-only authoring via Unity APIs. New static sprites have mipmaps/readability off and bounded texture import. One lazily cached modal instance, destroyed with controller.

## Acceptance criteria

- [x] Actual Quit/Close/Leave flow, reuse, frozen timer, gated old callbacks, debit once.
- [x] Close and Leave work at0; missing-resource and missing-Home recovery.
- [x] Updated existing navigation/wallet tests retain assertions through new confirmation step.
- [x] Unity compile, focused/full relevant runtime checks, convention validator, independent review.
- [x] Phone/iPad screenshots match layout; old native scenes/prefabs byte-identical; art/ownership/performance and true-device limitations recorded.

## Implementation steps

1. Back up affected scripts/scenes/prefabs; new flow tests RED.
2. Append confirmation partial, change Quit callback and restrict pause restart gating to real pause modal.
3. Author new prefab with Editor tool and transparent art, no rebuild of old assets.
4. Update old flow tests, verify all relevant boundaries, review, record final evidence.

## Out of scope

Restart confirmation, result-screen flow changes, resource rules or save schema changes, payment and ad features, new level design, Foundation changes.

## Verification matrix

| Boundary | Evidence | Status |
| --- | --- | --- |
| Quit / Close / Leave and pause timer | LeavingPlayModeTests real pointer checks | 3/3 focused passed |
| Navigation, wallet and prior UI | Full AllSorted.PlayModeTests | 44/44 passed |
| Missing prefab/Home and0 energy | Focused and existing recovery tests | Passed in full suite |
| Visual/native protection | Phone/iPad screenshots and file hash comparison | Inspected; four protected assets byte-identical |
| Compilation/conventions | Console and project validator | 0 errors/warnings; validator 0 warnings |

## Definition of Done

- [x] Acceptance checks pass with final native prefab and runtime source.
- [x] No original protected scene/prefab changes; resources and lifecycle documented.
- [x] Final screenshot, art prompt/source records, independent review and limitations delivered.

## Final delivery evidence

- Runtime: 44/44 PlayMode (job829e6a4315fd47deb88702d599ce6e1b), including3 new checks. Red baseline 0/3 demonstrated immediate old Quit behavior and absent confirmation capability. Full results, protected file checks, prompts and source manifests: `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/leaving/`.
- Screenshots `leaving-720.png` and `leaving-768.png` show actual Unity camera720×1280 /768×1024. Reference font treatment uses existing Fredoka Bold with white fill/purple outline; Leave and -1 dark-red outline. Content620×822 at.85 scale, centered; right-top Close is a separate button above art.
- Changed scripts: new ViewControllers/Sorting/SortGameViewController.Leaving.cs, new Editor/Sorting/LeavingPopupArtwork.cs; existing GameViewController.cs restricts restart availability to pause and UI.cs redirects Quit; new LeavingPlayModeTests and existing HomeNavigation/WalletPlayModeTests include new explicit confirmation step. No rule/state/storage changes.
- New Resources prefab and3 Sprite files as documented above; original PausePopup, ResultPopup, GameScene and HomeScene remain byte-identical. Scene-bound Inspector requires no manual changes; LeavingPopupKey has the source default. Old missing-Home assertions now retain the active confirmation and still recover; failed-save and energy assertions remain.
- First opening yields2 cached modals (original pause + confirmation), reopen reuses the same instance, controller teardown unregisters/destroys both. New textures512×512,512×190,564×409 RGBA32, no mips/readable CPU copy; base pixel payload about2.25MiB, shared old art reused. No new update loop or Canvas.
- Independent read-only reviewer found no important issues in callback, pause, wallet, cache and teardown boundaries. Ordinary Play showed0 Console warnings/errors. Convention validation passed with0 warnings.
- Final editor: saved GameScene, Edit Mode, no dirty scene. Device screenshots are editor render evidence; real iPhone/iPad safe areas/performance have not been tested.
