---
id: booster-purchase
title: Shared booster purchase popup and persistent inventory
status: Verified
owner: Project
created: 2026-10-28
updated: 2026-10-28
---

## Goal

Replace empty booster green-badge refill with the supplied shared purchase popup. Four variants reuse the existing purple/gold panel/header/close, cream inset, matching booster icon and lower-right ×3, exact requested title/description, green purchase button with coin and1600.

## User-visible behavior

- Empty allowance green badge opens purchase; red positive-count badge remains non-purchase. Opening pauses the board and cancels drag, no coin/stock change or automatic booster effect. Close resumes the same attempt/time/freeze; parent main button cannot fire through the badge or modal.
- Vacuum: Vacuums up a set of three identical items.
- Shuffle Dice: Shuffles all remaining items into a new layout.
- Wildcard: Turns three groups of three items into identical wildcards.
- Freeze Watch: Stops the timer for 15 seconds while you keep sorting.
- Price1600 coins for3 uses of selected depleted booster. Purchase succeeds only once per open popup/current attempt, gives3 without activating effect, closes and resumes. Insufficient coins remains open with clear message and no grant/debit; no IAP/ad/payment SDK.
- User explicitly confirmed all remaining booster uses persist across application restarts. Default fresh counts3 each; old wallet v1 migrates to v2 retaining balances/timestamps and initializing counts3. The implementation migration snapshots the currently running legacy inventory before compilation; observed counts were[3,3,3,3], matching v1 migration defaults, so no spent live stock was discarded. General in-process architecture hot swapping is not a shipped feature.

## Out of scope

Real money, ads, store catalog, altered booster effects, bulk buying/stacking above3, new gameplay or level progression persistence.

## Ownership and architecture

Project-owned Wallet ledger stores coins/energy/four counts in one transaction. Existing SortBoosterModel presents inventory, existing GameSystem validates use/purchase, existing Commands mutate. No Foundation edits. Shared popup owned by existing GameViewController focused partial, no new runtime Canvas/manager. A BuySortBoosterCommand(booster,attempt) returns Accepted; only current paused attempt/depleted stock may purchase. Legacy developer +1 command may remain for compatibility but has no player UI route.

## Asset and lifecycle boundary

New Resources key Prefabs/UI/Popups/BoosterPurchasePopup, lazy first load and cached instance in existing controller dictionary, Popups layer Replace routing and modal input gate, teardown with existing controller. Four icons reuse scene sprite references. Green blank button and coin derived from supplied source art using built-in imagegen, transparent assets under Assets/Sprites/UI/BoosterPurchase. Existing Inset/Background/Header/Close/font reused. Missing/invalid prefab leaves active board and timer unchanged; stale/hidden, destroyed, terminal and wrong-attempt callbacks rejected. Existing Pause/Leaving/Result assets and scenes protected.

## Persistence and failures

Wallet schema2 contains booster counts; version1 migration validates old data before writing, newer or corrupt data remains protected. Atomic purchase saves both coin deduction and counts before publishing. Each accepted use saves one spent count before effect; save failure leaves board/count unchanged. No second inventory save slot or split transaction. Test storage and UTC isolated from real player data. Save failure surfaced, no silent free grant. Offline energy/once-per-attempt rules retained.

## Acceptance criteria

- [x] Four exact titles/descriptions, art, ×3,1600 price and close position.
- [x] Badge real pointer opens corresponding modal without parent use, timer/freeze suspended and restored.
- [x] Atomic1600→0 purchase and0→3 inventory, no repeat/stale grant; insufficient/save failure unchanged.
- [x] Remaining counts and coins survive reload; v1 migration/corrupt/future data safe.
- [x] Missing-resource, lifecycle and old wallet/navigation/booster regressions covered.

## Verification matrix

| Boundary | Required evidence | Status |
| --- | --- | --- |
| Ledger/persistence | migration, roundtrip, insufficient and failed writes | 14 new core cases passed; full129/129 EditMode |
| Game transactions | use failure leaves board unchanged; attempt/phase/depletion gates | Core and runtime suites passed |
| UI | four variants, actual pointer, close/purchase/pause gates | Full47/47 PlayMode; final3/3 visual rerun passed |
| Assets | phone/tablet renders, protected-file comparison, source/prompt manifest | Five screenshots inspected; five protected native assets byte-identical |
| Compile/conventions/review | Console, validator, relevant suites, independent review | No important review findings; ordinary Play0 errors/warnings; validator0 warnings |

## Definition of Done

- [x] Correct QFramework/state ownership and all accepted rules have evidence.
- [x] Assets authored with Unity APIs, originals protected, source/prompt paths documented.
- [x] Review important findings fixed and required tests pass.
- [x] Screenshots, performance boundaries and real-device limitations delivered.

## Implementation plan

1. Back up existing project files, write core transaction/persistence regression tests first and retain RED.
2. Add single-save persistent inventory and purchase command with safe migration; preserve existing energy and coin paths.
3. Build one configurable popup and connect depleted badge; reuse existing modal cache/teardown and four icon sprites.
4. Run focused then warranted full regressions, inspect four phone variants and tablet, review and record results.

## Verified evidence and delivery — 2026-10-28

- Evidence directory: `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/booster-purchase/`. `core_red.json`11/11 missing capabilities; `ui_red.json`3/3 old free-refill behavior; `edit.json`129/129 passed (job5b7676cc6db8464997240ed5d0d706e2), `play.json`47/47 passed (joba5b1565bd23f49b98b36367a210da6d6). Final UI screenshot rerun3/3 passed (job0fa9f812d5dd4d668f83bc6832e34048); added captures and title fitting only after full-suite pass.
- Actual Unity camera screenshots: purchase-0-720.png Vacuum, purchase-1-720.png Shuffle Dice, purchase-2-720.png Wildcard, purchase-3-720.png Freeze Watch; purchase-1-768.png tablet. Title font is fit once per open to remain inside header, description wraps at32 reference units; price1600 and ×3 remain readable. Both Close and purchase were exercised through actual pointer raycasts.
- Physical file roundtrip used only an isolated temporary directory `/var/folders/2w/c7xhq11d2fj3xhrtbktlcq1w0000gn/T/AllSorted-Purchase-3ca44445c4be42299ddfce0240a076cd`: after depleting Shuffle, purchasing3 for1600 and using1, a new ledger read Coins0 / Shuffle2 / Energy5. Tests and visual captures use memory saves, never grant/spend real player balances.
- Ordinary Play `readback.json`: real existing wallet loaded Coins6400 / Energy5 / counts[3,3,3,3], schema2, valid checksum, disk values match. Save location remains `/Users/zhangshuai/Library/Application Support/DefaultCompany/All Sorted!/GameFoundation/Saves/all-sorted-wallet.json`. Startup purchase instances0 proves lazy instantiation. Captured pre-compile legacy counts[3,3,3,3] in `legacy.json`; no special live-state import was required.
- New source/asset list in `changed-sources.json` / `art-manifest.json`: core Models/Wallet + read-through SortBoosterModel, existing WalletSystem/GameSystem, new Buy command; focused GameViewController.Purchase partial and badge/Update hook; new BoosterPurchaseArtwork tool and one new Resources prefab with two static PNGs. Existing Scene/popup files listed in `protected-assets.json` are byte-identical. No new scene-bound controller bindings; configurable prefab key has a source default. Reapply via All Sorted/Apply Booster Purchase Artwork only touches this new prefab/art metadata.
- New textures: Button512×191 RGBA32, Coin256×256 RGBA32, mip count1, readable=false; base pixel payload653,312 bytes (~0.62MiB), Editor runtime texture counters1,045,824 bytes (~1MiB). Shared panel/inset/close/font and four booster icons reused. One lazily cached popup, no new Canvas/Update component, no per-frame dependency lookups. Accepted use/purchase saves a small JSON transaction; actual mobile storage latency not profiled.
- Independent review found no confirmed Important/Critical defect. Reviewer questioned legacy in-memory bridging; before-compile snapshot proves all live counts were3, so normal v1 defaults preserve this session; production hot architecture replacement is explicitly outside scope. An existing asset allowlist was updated for the previously authorized exact Fredoka font path; no broad allowlist relaxation.
- Final editor is saved GameScene in Edit Mode. No manual Inspector work remains. Phone/tablet screenshots are editor render evidence; actual iPhone/iPad safe areas, disk interruption/power-loss and device performance remain untested.

## 金币图片替换 — 2026-10-28

[coin-art-refresh](coin-art-refresh.md) replaces supplied hexagon-coin art only. Existing dynamic balances, prices, quantities, grants/spending, saves and modal motion remain. Purchase button coin stays on LEFT per user correction.

## Icon replacement V2 — 2026-10-28

User requests image-only replacement: GoldenCard uses supplied 变形-v2.png, Vacuum uses 付费功能按钮之吸取商品V2.png, FreezeWatch uses 付费功能按钮之时间暂停V2.png. Preserve icon names/asset GUID/local file IDs so existing GameScene and shared purchase view refs continue to resolve. Direct copy transparent card, built-in imagegen background removal for RGB vacuum/watch. Replace only existing three PNGs, recalculate Sprite regions through Unity Editor API. No button Rect/Image fitting, stock/effect/price/save/animation/runtime code or Shuffle change. Originals backed up outside Assets; user sources untouched. Verified:6/6 existing booster effect/purchase PlayMode tests passed (0af7770e4e5745cbacaf62818632fff0), covering vacuum, shuffle, wildcard, freeze and purchase3/1600 without auto-use. Phone/tablet native renders inspected. All3 original GUID/local file ID pairs preserved;101 protected source/scene/popup files byte-identical. Only3 PNGs and native Sprite crop/import metadata changed; Shuffle untouched. GoldenCard raw PNG is an unmodified source copy; Vacuum/FreezeWatch use built-in transparent background edits with provenance. All3 textures512×512 RGBA32, mip1/readablefalse; no additional runtime texture slots/components/Canvas or Inspector setup. Foundation validator0 warnings. Evidence, backups, art-manifest, IDs, protected hash comparison and previews: `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/booster-icon-v2/`. Final saved HomeScene Edit Mode. Device checks unchanged/unprofiled.
