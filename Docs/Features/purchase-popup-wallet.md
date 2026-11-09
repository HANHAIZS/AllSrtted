---
id: purchase-popup-wallet
title: Coin balance above booster purchase popups
status: Verified
owner: Project
created: 2026-10-28
updated: 2026-10-28
---

# Coin balance above booster purchase popups

## Goal
Add the current game coin balance bar at the upper left above every booster purchase panel, matching the supplied overall popup reference.

## User-visible behavior
The four in-level offers and two prelevel offers share the current hexagon coin HUD image, live saved balance and the correct green plus badge at the coin icon lower-right. The bar sits outside the panel at its upper left, above the title, and follows the existing panel open/close animation. Plus reuses the existing +800 coin Command and persistence. Balance and purchase eligibility update immediately. Repeated popup use creates no extra listeners. Hidden, closing, replaced or stale-attempt callbacks cannot grant coins. Existing prices, rewards, descriptions, geometry and gameplay pause semantics stay intact.

## Out of scope
New art, billing, new wallet schema, unrelated Home/Shop HUD or popup layouts.

## Ownership and architecture
SortPopupView.PurchaseWallet is focused presentation/input binding only. Owners inject their existing modal gates and AddSortCoinsCommand action and refresh the displayed balance through existing purchase refresh paths. Ledger/SortWalletSystem continue owning saved state. New Editor PurchasePopupWalletArtwork authors only the new subtree and three bindings in BoosterPurchasePopup, and BoosterPurchaseArtwork invokes this focused helper on future rebuilds.

## State and persistence
No new saved fields. Existing AddSortCoinsCommand grants800 save-before-publish; failure retains balance and existing notice feedback. No additional architecture or per-frame dependency lookup/subscription.

## UI, prefab, scene, and Inspector impact
Assets/Resources/Prefabs/UI/Popups/BoosterPurchasePopup.prefab retains its key Prefabs/UI/Popups/BoosterPurchasePopup, lazy Resources loading/reuse and Popups host / UiPopupRouter Replace. Direct reference sprites reuse CoinReplacement/Hud and Home/Wallet/PlusBadge, no duplicate textures. New PopupContent/PurchaseWallet child inherits .25s/.15s motion and input/opacity gates. The existing Home/Shop layout, modal backdrop, content geometry and other prefabs stay unchanged. Owner destruction releases button listener; no observer added.

## Acceptance criteria
- [x] All six offers display current balance above left of title with no title/close overlap on phone/tablet.
- [x] Green plus has correct icon corner and grants800 once per click, persisted balance updates and purchase becomes affordable.
- [x] Hidden/stale/replaced/closing callbacks are ignored, reopening reuses one listener.
- [x] Save failure grants nothing and retains error feedback.
- [x] Existing purchase flows and popup motion remain functional.

## Verification matrix
| Boundary | Evidence |
| --- | --- |
| Compilation and project conventions | Unity compiled; validate_game_foundation_project.py passed with zero warnings |
| Runtime | Focused purchase/prelevel/motion PlayMode tests and new wallet pointer tests |
| Visual/native | Phone/tablet render, protected old prefab blocks compared with backup |
| Performance | Shared textures, no new per-frame lookups, existing refresh path |

## Definition of Done
Acceptance checks have concrete evidence; native API saves only; clean HomeScene Edit Mode restored. Real device checks remain outside Editor verification.

## Verification evidence
- PlayMode17/17 passed, job46dd7895ac4e483390822a1269b3a682: all four in-level variants, both entry variants, +800/affordability/reuse/hidden/closing/stale attempt, failed-save no grant and retry, Resources failures, existing popup animation. Expected missing-resource test errors are asserted with LogAssert.
- Actual720x1280 phone and768x1024 tablet captures: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/purchase-wallet/. New balance bar fits above panel/title; plus uses existing correct badge at coin corner. New tests use memory saves.
- Protected native prefab readback: only two prior blocks changed: SortPopupView's new balance/plus references and PopupContent's child list; original title, text, price, geometry, icons, backdrop and all other prefab blocks unchanged. Native write via focused Editor API helper.
- Changed sources: SortPopupView.PurchaseWallet.cs, both Home/Game purchase owner partials, PurchasePopupWalletArtwork.cs and future BoosterPurchaseArtwork hook; two PlayMode test fixtures; one purchase prefab. No Home scene or other popup/native asset writes.
- No new textures or model subscriptions. Cached callbacks/listeners owned by one modal instance and removed on destruction; no new GetModel/GetSystem/GetUtility calls in changed runtime files. Existing state refresh and motion own rendering.
- HomeScene ends clean in Edit Mode. Hardware safe areas/touch and device performance remain unverified outside Editor.
