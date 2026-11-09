---
id: shop-no-ads
title: Shop no-ads presentation
status: Verified
owner: project
created: 2026-10-29
updated: 2026-10-29
---
# Shop no-ads presentation

Status: Verified

## Goal
Add supplied blue NO-ADS card immediately above Small Bundle in existing scrolling Shop. Use existing green purchase button at bottom right, caption US$6.99 in existing purchase font. User explicitly selected display and click feedback only. No purchase/grant, persistence, ad SDK or entitlement. Preserve all existing card descendants, amounts, prices, wallet and navigation; shift bundle and coin sections down together and extend scroll content.

## Ownership and architecture
HomeScene ShopScroll/Content/NoAdsOffer on existing Pages host; same page CanvasGroup/input gating and scene lifetime. No new layer/popup/Resources key. Direct Sprite Assets/Sprites/UI/Home/Bundles/NoAds.png uses built-in imagegen to remove source checker background only. Source: /Users/zhangshuai/Documents/欧卡风格/商城界面之去广告.png. Existing green CoinShop button and Fredoka font reused. Existing Button and SortButtonPressFeedback own tint/press animation; no action callbacks. Optional NoAdsOffer reference controls additional 260 logical units of height, preserving legacy scene fallback. Focused authoring method owns new card and section offsets only. No model, command or system changes necessary.

## Verification matrix
Native hierarchy/position/button caption/raycast and unchanged existing bundle descendants; phone and tablet renders and scroll reachability via existing Shop tests. Press and release feedback must return to authored scale, clicking must leave wallet unchanged. Convention validation and Unity console compilation. No real payment or advertisement behavior is implemented.

## User-visible behavior
One blue NO-ADS offer appears before Small Bundle. Bottom-right green button reads US$6.99, provides existing press/release tint and bounce, and performs no transaction.

## Out of scope
Purchases, entitlements, real billing, ad removal SDK, wallet mutation and save schema changes.

## Acceptance criteria
- [x] Correct offer ordering, art and price; existing product descendants retained.
- [x] Phone/tablet render and bottom coin packs reachable.
- [x] Button press feedback and no wallet changes.
- [x] Compilation and convention validator pass.

## Definition of Done
Record native readback, rendered evidence and focused runtime checks; preserve pre-existing unsaved scene edits.

## Verified evidence
- Unity Play Mode phone720x1280 and tablet768x1024 captures at /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/shop-no-ads/{phone,tablet,bottom}.png. NO-ADS offer precedes Small Bundle, existing six coin packs remain scrollable; last pack center inside viewport at scroll bottom.
- Pointer-down scale0.91; pointer-up/click settles1.0. Coin and energy labels unchanged before/after click. Price US$6.99, zero persistent purchase callbacks, existing feedback component reused. No saved entitlement or payment path.
- Only four pre-existing scene serialization blocks changed: controller NoAdsOffer binding, scroll content children/height, BundleRoot y, CoinPacks y. All prior card descendants and pricing unchanged; no objects removed. User's pre-existing unsaved light/camera edits were kept in the open Editor scene, not saved into the production asset.
- Unity imported Sprite via Editor API; no source compile errors. validate_game_foundation_project.py passed zero warnings. No new tests added for this reversible presentation change; existing button behavior verified directly in Play Mode.
- Files: HomeScene, SortHomeViewController.Bundles.cs (optional offer reference and scroll offset), ShopBundleArtwork.cs (focused offer authoring and future builder hook), NoAds.png+native meta. No model, commands, wallet, existing offer content or resource keys changed. One shared1024-max static Sprite and one existing-style button; no new update loops or runtime hierarchy searches.
- Built-in imagegen prompt: remove fake checker background only; preserve blue banner, gold border, crossed ADS icon, exact Remove pop-up / and banner ads / NO-ADS text and layout. Transparent output copied into final Sprite path. Physical device touch verification remains outside Editor QA.
