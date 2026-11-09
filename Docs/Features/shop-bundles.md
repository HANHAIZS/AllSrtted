---
id: shop-bundles
title: Shop bundles and timed unlimited lives
status: Verified
owner: Project
created: 2026-10-28
updated: 2026-10-28
---

# Shop bundles and timed unlimited lives

## Goal

Add the supplied bundle offers to the existing Shop.

## User-visible behavior
Three full-width illustrated bundle cards precede the existing six coin packs in one vertical scrolling Shop. Header, resource HUD and bottom navigation stay fixed. Each card uses the supplied purple/gold background; left bundle image and coins; middle four in-level boosters in a 2x2 slot; right Box/Alarm plus unlimited-life heart; footer white name and existing centered US$ price button. Numbers use Fredoka Bold (existing purchase font); English uses Lilita One (existing coin HUD font), white. All content remains readable without overlap on phone/tablet.

| Bundle | Coins | Each of six boosters | Unlimited lives | Simulated price |
| --- | --- | --- | --- | --- |
| Small Bundle | 5000 | 1 | 1h | US$9.99 |
| Medium Bundle | 10000 | 3 | 3h | US$18.99 |
| Big Bundle | 25000 | 6 | 12h | US$36.99 |

Clicking purchase grants all rewards atomically and persists them, using the already authorized no-charge simulation. Repeated purchases deliberately grant again. Unlimited lives stack from max(now, prior expiry), continue offline, permit entering/restarting at zero lives and prevent quit/restart/failure life debits while active. Normal lives continue recovering underneath. The shared Home/Shop life HUD shows infinity and remaining HH:MM:SS; its add badge is hidden while unlimited. Expiration restores normal display and energy rules. One attempt still settles only once.

## Ownership and architecture
- ClaimSortBundleCommand -> SortWalletSystem -> SortWalletLedger transaction; no ViewController model mutation.
- SortBundleCatalog is immutable project-owned data; SortWalletSave schema4 adds UnlimitedUntilUtcTicks and permits inventory above3. Migration from v3 keeps balances and inventory, initializes deadline0. v1/v2 migrations remain. Valid unknown newer saves stay protected.
- SortHomeViewController.Bundles partial owns cached buttons/listeners, scroll geometry and notices. Existing CoinShop partial delegates viewport layout when bundle scroll is present. Commands and model/system references remain cached at initialization.
- Editor ShopBundleArtwork authors only Shop subtree, reparents existing CoinPacks and adds bindings. No shared Foundation changes or extra architecture.

## State, failures and integrations
Save before publish: insufficient representable capacity, invalid product, corrupt/future save or write failure grants nothing. Inventory/coin/deadline overflows reject the complete bundle. Retry remains possible. Save and reload preserve six inventory counts and absolute UTC expiry; current monotonic clock policy applies. Real billing, receipts, backend clock/anti-cheat and offers/timers are out of scope.

## UI and assets
HomeScene Shop page (Pages host), same SortUiLayers/page gates. ScrollRect has clipped viewport above Panels navigation. Three direct-referenced card instances for Home scene lifetime; no Resources keys/new popups. Assets/Sprites/UI/Home/Bundles/{Card,Small,Medium,Big,Infinite}.png are new transparent sprites derived from supplied art; reuse current booster sprites and CoinShop/Button. Preserve all popup/native assets and unrelated authored Image settings, fonts, navigation, wallet layout and coin pack descendants. Missing bundle bindings disable only bundle section and log clear error; coin page fallback stays usable. Disposal removes listeners. No per-frame hierarchy searches or QFramework lookups.

## Out of scope

Real IAP, receipt validation, backend anti-cheat, unrelated popup artwork.

## Acceptance criteria
- [x] Order and latest amounts/prices/quantities/durations match table, card slots and text match reference.
- [x] Six reward counts, coins, stacked offline deadline persist atomically; failures/overflow/migrations tested.
- [x] Zero-life entry, unlimited no-debit, expiry and normal recovery tested; one settlement per attempt preserved.
- [x] Scroll reaches existing six packs, header/nav remain usable; hidden/transitional/modal purchases rejected.
- [x] Phone/tablet render and lifecycle/command tests; no real player save mutations during tests.
- [x] Unity compilation, focused EditMode/PlayMode, convention validator, review.

## Performance and limitations
Three static cards add Images/Text and five shared textures, one cached ScrollRect. Countdown reuses existing one-second refresh; no per-second save. Text/geometry updates occur only on state/layout changes. Editor verification is not device performance or payment validation.

## Verification matrix

| Check | Evidence |
| --- | --- |
| EditMode | 99/99 focused wallet/shop tests, job7cc65f9171d24c2eb34da2b995565b58 |
| Runtime and visuals | Full PlayMode67/67 job542b3f5ddc234d1da2554b348d12940b; final Shop9/9 job246b122002294775a0069cb514633cee including missing scroll/content/notice fallback; phone720x1280 + tablet768x1024 captures |
| Conventions | validate_game_foundation_project.py passed, zero warnings |

## Definition of Done
All checkboxes above have recorded evidence, final affected scripts/art/scene and remaining device checks recorded here. Status becomes Verified only after successful verification.

## Verified implementation and evidence (2026-10-28)

- Full EditMode184/184: job3e51df17d66e4861b36988ef9b769c2a. Focused initial wallet/purchase99/99: job7cc65f9171d24c2eb34da2b995565b58.
- Full PlayMode67/67, followed by final Shop9/9 after completing missing-reference fallback; final9 covers all new bundle UI tests and all existing coin-shop UI tests. The expected3 logged errors are deliberate missing-binding tests checked via LogAssert. No new compile errors.
- Screenshots and JSON results: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/shop-bundles/. Phone/tablet cards match slots, names, quantities, duration, prices. Coin packs remain reachable at scroll bottom; Home/Shop infinity and countdown fit the existing HUD. All tests use in-memory wallets, not the real player save.
- Native comparison against backup: only3 prior HomeScene serialization blocks changed (Shop root child list, controller new bindings, CoinPacks parent/top anchors/position). All existing coin-card descendants, header/HUD/nav assets and popups unchanged. Added bundle scene objects and Sprite imports were authored with Unity Editor APIs.
- Code review fixed missing scroll/content/notice fallback; now the existing coin shop reparents to its fixed layout, bundles disable, and a clear error logs once. Top anchors corrected after pointer tests exposed a content-center offset.
- Affected production code: SortBundleCatalog, SortWalletLedger.Bundles (plus schema4 ledger), SortWalletModel, SortWalletSystem, ClaimSortBundleCommand, Home controller Bundles/CoinShop/Resources and lifecycle hooks. Editor ShopBundleArtwork plus HomeSceneBuilder hook; HomeScene and5 new Bundles Sprite assets. Existing migration test assertions updated to schema4.
- Five direct-reference textures import at max1024/512/256 with shared icons and buttons. ScrollRect and subscriptions are scene-owned; countdown uses existing1Hz refresh with no per-second save; QFramework lookups remain initialization/command setup only. Missing-scroll GetComponentInParent occurs only once in recovery, not normal frame updates.
- Actual iOS/Android touch feel, safe-area hardware and device memory/frame timing remain device checks. No real billing/receipt service added. HomeScene restored to clean Edit Mode after tests.

## Remove bundle footer caption (2026-10-29)
User requests removal of the small white Simulated purchases · No charge line below Big Bundle. Clear/hide the initial caption and successful-purchase footer, retain the bound Text only for actionable failed-purchase feedback; all simulated grant, saved inventory and prices remain unchanged. No card geometry/scroll changes. Verified9/9 focused PlayMode, job512adac890c54e5f83d0da5701fd8bab. Native readback confirms empty/inactive BundleNotice. Successful grants leave caption hidden; actionable failure feedback still works and existing tests pass. Prices, awards and save logic unchanged. See /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/white-timer-clean-shop/ for screenshots and results.

## Small Bundle icon replacement (2026-10-29)
Replace only Small.png with a transparent cutout of user-supplied /Users/zhangshuai/Documents/UI素材V2/小礼包.png. Preserve existing asset GUID/Sprite identity, Icon RectTransform, preserveAspect, other card art, all labels, prices, grants and persistence. Built-in imagegen removes the white background only; no runtime code changes. Verified: Sprite GUID 47e157f31a3aa40be94ffe9e1b8678a4 and local ID 7250873890289514549 preserved. Native Icon position (-195,35), size (236,180), preserveAspect true unchanged. HomeScene disk hash unchanged (existing unsaved Editor state preserved). Native isolated card render checked at outputs/small-bundle-v2/card.png. Unity import succeeded; convention validator passed with zero warnings. No runtime scripts or buying behavior changed. Prompt: remove surrounding white background only; preserve lavender bag, tan right-side rope, hexagon coins and proportions. Built-in imagegen transparent output copied into Small.png.
