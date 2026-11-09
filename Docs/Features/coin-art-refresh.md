---
id: coin-art-refresh
title: Supplied hexagon coin artwork replacement
status: Verified
owner: Project
created: 2026-10-28
updated: 2026-10-28
---

## Goal
Replace only supplied coin images in existing Shop, resource HUD and purchase popup.

## User-visible behavior
Six source coin piles map in order to existing1.000/5.000/10.000/25.000/50.000/100.000 packs; all card backgrounds, amount/price typography and buttons remain. New supplied resource HUD coin+bar replaces shared Home/Shop coin bar. Remove its baked1240 so the existing dynamic balance still renders. New single coin replaces purchase button LEFT icon (user corrected earlier right-side typo), shared in-level and prelevel purchase offers. No feature, balance, price, stock, click handler, persistence or animation changes.

## Out of scope
New payment logic, changed quantities/prices/positions/sizes/fonts, new currencies or balances, changed modal motion or other art.

## Ownership and architecture
Editor-only CoinImageReplacementArtwork imports8 transparent Sprite assets and swaps only7 Home Image.sprite refs and1 shared purchase-prefab Coin Image.sprite. Existing ShopCoinArtwork/HomeWalletArtwork/BoosterPurchaseArtwork author tools prefer new sprites to retain replacement on future authored builds. Runtime Models/Systems/Commands/Controllers unchanged. No new runtime components, Canvas, lookup or load.

## State and persistence
No mutation/schema change. HUD runtime text retains actual coins; never displays source image1240. Existing tests isolate wallet memory from player data.

## UI, prefab, scene, and Inspector impact
New Assets/Sprites/UI/CoinReplacement/{Pack1..Pack6,Hud,Purchase}.png, with built-in imagegen transparent cutouts of user-supplied RGB sources. HUD additionally removes baked number. Native Sprite regions trim alpha; HUD padded region preserves old1122:429 ratio so existing shared-bar layout remains. All scene/prefab writes via Unity Editor API. Preserve image type/aspect/color/raycast, Rects, labels, callbacks and other refs. Existing resources and lazy lifecycle unchanged; popup reuses existing BoosterPurchasePopup prefab/key. Original assets kept for recovery, no user source overwritten.

## Integrations and failure behavior
No SDK/network at runtime. Editor validates required images and target bindings before swap. Backup sources/native assets and compare protected properties after replacement. Missing input aborts authoring before any native save.

## Performance considerations
Six pile textures max512, HUD512, purchase256; uncompressed RGBA32, no mips/readable copy. Direct references replace existing textures, shared icons reused, no new per-frame behavior. Device profile unchanged/unmeasured.

## Acceptance criteria
- [x] Correct8 image mappings with transparent backgrounds and preserved supplied hexagon emblems.
- [x] Actual HUD balance and existing prices/quantities/controls remain.
- [x] Only intended native sprite refs change, runtime sources unchanged.
- [x] Phone/tablet Shop/HUD/offer visuals inspected; relevant pointer/save regression and validator pass.

## Verification matrix
Native sprite-reference diff and runtime hashes; focused Shop/wallet/entry offer pointer tests plus render captures. Verified; see delivery evidence below.

## Definition of Done
- [x] Asset mappings, preserved authoring/behavior, verification and performance limits documented.

## Verified delivery — 2026-10-28

- Imported8 new sprites, correct reading-order mappingPack1..Pack6; shared Home/Shop coin barHud keeps dynamic wallet label and no baked1240; purchase Coin Image at anchoredPosition(-115,5) now referencesPurchase (LEFT). Native readback and phone/tablet renders inspected; new orange hexagon emblem preserved across all sources. Source/prompt/generated-path manifest in evidence folder; source PNGs untouched, former art retained.
- Focused actual-pointer Shop packs/HUD/entry purchase tests3/3 passed (abcd2c83451d47289b816479e17aefb5), covering six exact grant/price/amount mappings, shared HUD updates and scene reload, hidden callbacks, purchase/cancel/affordability and saved balance. Final settled-popup capture/pointer case1/1 passed (e385b51fec0f46ef884181e35d73ba2c). Captures wait for prior0.25s motion to settle; assertions unchanged. No new tests for a reversible sprite-only replacement.
- `native-comparison.json`: HomeScene541 objects before/after;7 Image.m_Sprite references changed, Rects/text/buttons/Image fitting/input flags and all old references unchanged. Shared purchase prefab68 objects before/after, only left Coin Image.m_Sprite visually changed. Unity also persisted existing default Home EntryPopupKey/EntryPurchasePopupKey and shared popup OpenSeconds0.25/CloseSeconds0.15; these equal prior runtime defaults and do not change behavior. No object additions/deletions. `runtime-protection.json` confirms all hashed Model/System/Command/ViewController source files unchanged.
- Editor sources: new CoinImageReplacementArtwork importer/swap tool and3 focused fallback preferences in ShopCoinArtwork/HomeWalletArtwork/BoosterPurchaseArtwork, preventing future authored builds from restoring old images. Only new Sprite metadata and intended native references authored through Unity APIs.
- New textures RGBA32, mip1, readablefalse: six512×512 piles, Hud512×223, Purchase256×256. Nominal base pixels7,010,304 bytes (~6.69MiB) for the8 replacements; shared card/button textures unchanged, no runtime component, Canvas, subscription, resource key or per-frame behavior added. Device memory/GPU performance not profiled.
- Foundation validator passed0 warnings, compilation no errors. Final saved HomeScene Edit Mode; no additional Inspector setup. Evidence/art manifest/backups/native diffs/runtime hashes and phone/tablet previews: `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/coin-art-refresh/`.
