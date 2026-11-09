---
id: shop-coin-packs
title: Six simulated Shop coin packs
status: Verified
owner: Project
created: 2026-10-28
updated: 2026-10-28
---

## Goal

Build six coin packs in Shop using the supplied card background, six corresponding coin pile images, green price-button design and three-column/two-row reference. Preserve existing header, resource bars, navigation and other pages.

## User-visible behavior

Packs in reading order:1.000/$1.99,5.000/$8.99,10.000/$16.99,25.000/$34.99,50.000/$64.99,100.000/$99.99. User explicitly chose simulated purchases: no payment, click grants full selected amount and persists it. Clearly label simulated/no-charge state. Each deliberate click may buy again. Hidden, transitioning, disabled or departing Shop callbacks are rejected. Save failure grants nothing and shows error. Coin overflow rejects the whole grant; no partial/repeated grant misstatement. Existing free+800 HUD behavior unchanged.

## Out of scope

Real payment, billing SDK, App Store/Google Play products, receipt/server verification, refunds, advertising, changed energy/inventory/level rules.

## Ownership and architecture

Immutable project catalog defines six amounts/prices. Existing wallet ledger performs save-before-publish grant under unchanged schema2; WalletSystem exposes operation, ClaimSortCoinPackCommand(index) returns Accepted. Existing Home controller focused partial owns six bindings/listeners, eligibility, notice and responsive layout; no new runtime manager/Canvas/architecture. Catalog class SortCoinPackCatalog: Count, GetCoins(index), GetPrice(index), GetDisplayAmount(index). Invalid indices rejected.

## Assets, lifecycle and protection

New Sprites under Assets/Sprites/UI/Home/CoinShop, all direct references from HomeScene Shop-only CoinPacks subtree. Supplied art needs white/checker background removal and blank price-button surface; built-in imagegen with source/prompt manifest. One shared card and button texture, six icons, dynamic amount/price labels. New Editor/Home/ShopCoinArtwork tool authors only new Shop subtree and new Home bindings via Unity APIs; original scene/prefab properties protected. New callbacks attached once and removed on destruction; existing page gates control input. No Resources loader or new popup. Existing QFramework dependencies cached at initialization.

## Persistence and failures

Reuse slot all-sorted-wallet schema2 and atomic store. Grant preserves energy, inventory and recovery state; invalid/future/corrupt/write-failed saves retain prior protection. Test architecture uses memory/temporary file storage, never modifies actual player coin balance. No SDK/network runtime dependency.

## Acceptance criteria

- [x] Six exact amount/price/icon mappings, supplied card/button styling and3×2 layout.
- [x] Pointer clicks grant exact selected amount, reflected in shared Home/Shop HUD and retained after reload.
- [x] Hidden/transition/disabled callbacks, invalid index, overflow and failed write do not grant.
- [x] Existing header/navigation/settings/resources and native scene references protected.
- [x] Phone/tablet renders, compilation, relevant tests, validator and review have evidence.

## Verification matrix

| Boundary | Required evidence | Status |
| --- | --- | --- |
| Wallet | all six grants, reload, failure, invalid/overflow | 21 new core cases passed; full150/150 EditMode |
| UI | actual pointer, shared counters, gates, lifecycle | 3 focused passed; full50/50 PlayMode |
| Native/visual | protected scene comparison, phone/tablet screenshots | Inspected; old native properties unchanged except new child reference and bindings |
| Completion | Console, convention validator, independent review | Ordinary Play0 errors/warnings, validator0 warnings, no important review findings |

## Definition of Done

- [x] Accepted simulation rules implemented without real billing.
- [x] Tests, asset/source manifests, protection evidence and previews recorded.
- [x] Important review findings resolved; device/performance limitations stated.

## Implementation plan

1. Back up project, prepare core/UI failing checks and art extractions.
2. Add six-pack catalog and atomic wallet command, preserving schema2.
3. Author responsive Shop-only grid and bind existing Home controller.
4. Verify all grant/gating/failure paths, inspect art/layout, review and document.

## Verified delivery — 2026-10-28

- Evidence folder `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/shop-coins/` contains source/prompt manifest for all8 built-in imagegen outputs, RED/GREEN results, native comparison, changed sources and phone/tablet screenshots. Project images saved in Assets/Sprites/UI/Home/CoinShop/{Card,Button,Coins1,Coins2,Coins3,Coins4,Coins5,Coins6}.png; output PNG alpha copied unchanged. Unity Sprite regions/import metadata only; original user sources untouched.
- `edit.json`:150/150 passed, jobbbbde12d29644e83a02461feeb5445f3. `play.json`:50/50 passed, job2e8dcbb9c0f848e98cd745232dafe11f. RED core21/21 missing capability, UI3/3 absent bindings retained. Actual pointer flow grants all6 amounts exactly, updates shared HUD, survives HomeScene reload; transitions/Home/Settings/disabled controller/hidden grid/disabled button reject stale callbacks, reenable does not duplicate listeners, failed write can retry on same card.
- File proof `disk.json`: isolated temporary directory, 100.000 pack from1600 reloads as101600, energy5 and booster inventory3 unchanged. No real player balance used for test purchases. Ordinary Play read existing4800 coins from schema2 file, matched HUD and disk; no reset to1600. Main save path remains `/Users/zhangshuai/Library/Application Support/DefaultCompany/All Sorted!/GameFoundation/Saves/all-sorted-wallet.json`.
- `shop-coins-phone.png`720×1280 and `shop-coins-ipad.png`768×1024 show all six card amounts/prices; labels use supplied dot thousands, card/button art and six coin pile variants. Simulated/no-charge notice remains visible. Runtime layout measured card bounds below header and above navigation before any capture-forced layout; `ordinary-play.json` records margins.
- `native-comparison.json`: HomeScene361→517 native objects; all32 new GameObjects under Shop/CoinPacks. Only two existing native objects changed: Shop parent child list and Home controller's three new fields (root, six-button array, notice). Original header/resource bars/navigation/settings/Rect/Image choices/Inspector refs retained; GameScene byte-identical. No prefab changes or full Home rebuild.
- Changed sources in `changed-sources.json`: immutable coin-pack catalog, existing wallet ledger/system + one claim command; existing Home focused CoinShop partial, initialization/layout/dispose hooks, new Editor/Home/ShopCoinArtwork and future HomeSceneBuilder hook; two new test fixtures. Model mutation stays behind command/system; runtime callbacks have no dependency lookups. No new Canvas, runtime controller or SDK.
- Texture readback: shared Card768×1024, Button512×256, six Coins512×512; RGBA32, mip count1, readable=false. Base pixels9,961,472 bytes (~9.50MiB); Editor runtime counters19,928,320 bytes (~19MiB). Shared textures reused across cards, layout updates only on initialization/enable/resize, no per-frame disk writes or texture generation. Actual device memory/GPU/storage latency unprofiled.
- Independent read-only reviewer found no important findings, and independently confirmed all new objects are inside the Shop subtree and protected old properties. Validator passed0 warnings; ordinary Play0 warnings/errors. Final state saved HomeScene in Edit Mode; no extra Inspector setup. Real iPhone/iPad safe areas and performance remain untested. This build has no real billing or payment processing.

## Typography refinement — 2026-10-28

User requested reference-matched amounts and prices plus US inside the left of all purchase buttons. Coin amounts use smaller light-gold rounded text with one thin brown outline; prices use smaller white text with thin blue outline. Separate US prefix preserves exact dollar price strings and pack mappings. Card, icon, purchase-button, header and navigation layouts and simulated grant/save behavior remain unchanged. Verified by six-label native readback (amount36, price28, US20; one amount outline), phone720×1280/tablet768×1024 captures, focused real-pointer six-pack grant/reload PlayMode test1/1 passed (job90fac642fdf34f5d8c6279943e43eea8), and Foundation validator0 warnings. Evidence in `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/shop-coins-text/`. No new textures/runtime callbacks; six extra native Text labels. Saved HomeScene in Edit Mode; device checks unchanged.

## Centered currency text — 2026-10-28

US and price both use size28, same font/color/outline, gap4. Measure their preferred widths to center the entire pair in each purchase button. Price strings, amounts, button art/size and purchase behavior remain unchanged. Verified: six native pairs use size28 and center0 with measured widths96.67–117.33 inside176-wide buttons; phone/tablet captures; real-pointer grant/reload PlayMode1/1 passed (a03b012ea4394db3b62343d815618ba1); Foundation validator0 warnings. Evidence: `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/shop-coins-centered-price/`. Only ShopCoinArtwork.cs and HomeScene text properties changed. No runtime allocation/behavior change; device checks unchanged.

## 金币图片替换 — 2026-10-28

[coin-art-refresh](coin-art-refresh.md) replaces supplied hexagon-coin art only. Existing dynamic balances, prices, quantities, grants/spending, saves and modal motion remain. Purchase button coin stays on LEFT per user correction.
