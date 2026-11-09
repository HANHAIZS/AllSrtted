---
id: prelevel-boosters
title: Level entry popup and selected starting boosters
status: Verified
owner: Project
created: 2026-10-28
updated: 2026-10-28
---

## Goal

Add a selected-booster preparation popup before Home-triggered level entry.

## User-visible behavior
Home Start opens a purple/gold modal using supplied pause background/header/close, two green item plates, provided golden absorption box and alarm clock. Header current LEVEL NN, lower PLAY button. User confirmed each begins with3 uses, independent persistent counts. Click item toggles selection without spending; zero cannot select. At0 the corresponding green+adds1 without currency cost. With stock the right-lower badge is red and displays its count. Both can be selected, each once per level. PLAY enters selected level: strong vacuum removes three matching groups (9 items, including hidden layers), extra time adds30 seconds. Neither selection mandatory. Close returns Home unchanged; reopened selection resets. No energy consumed on popup open/close. Existing zero-energy start restriction retained. Effects apply only once to new attempt, never again on restart/next. Counts persist across restarts; existing four booster inventory separate.

## Out of scope
Real money/ads, extra costs, new levels, automatic boosters on retries, altered in-level four booster effects, changes to Home/Shop/settings/navigation art.

## Ownership and architecture
Project wallet ledger owns two EntryCharges, schema3 migrates v1/v2 while preserving coins, energy, deadlines and old charges. WalletSystem and Commands own grants/spending. Navigation model carries one-shot level+mask intent. GameSystem prepares board, validates effects, durably spends both selected counts atomically, then publishes board/attempt. Saving failure does not publish effects or deduct any counts. Home focused partial owns lazy popup lifecycle and presentation; SortPopupView reused. No new architecture/Canvas/Foundation edits.

## State and persistence
Same wallet slot all-sorted-wallet; schema3 adds EntryCharges[2] default[3,3]. Nonnegative int counts, overflow grant rejected. v2 migration validates all old resources/inventory before adding defaults. Future/corrupt/failed saves retain existing protection. Opening/cancelling has no ledger mutation beyond existing time recovery. Failed scene preflight keeps popup/selection and stock. If GameScene loads but selected-charge commit fails, return Home with saved stock intact and resource error. In-memory selection cleared on cancellation/scene loss. Direct/editor level entries keep existing no-booster path.

## UI, prefab, scene, and Inspector impact
New Assets/Resources/Prefabs/UI/Popups/PrelevelPopup.prefab, key Prefabs/UI/Popups/PrelevelPopup. Home keeps key only, first-use Resources.Load and cache1 instance; close hides/reuses; destroy unregisters/destroys, releases gate. Host Layers.Hosts[2] Popups, Foundation UiPopupRouter Replace; gate SortUiLayers on Pages/Panels. Hidden/stale/transition/modal callbacks rejected. Missing/invalid resource leaves page/input intact, retry possible. New prefab created from shared art through Editor APIs; old scene and prefab bindings untouched. Title, count, selection and notice dynamic. New textures under Assets/Sprites/UI/Prelevel, direct referenced by popup. Background/header/close/green button/base/plus use existing Sprite assets. Green button reads PLAY (same supplied style, no Free text).

## Integrations and failure behavior
No network/SDK; offline grant/use supported. All charges save before publication. Double PLAY rejected by loading gate and consumed entry intent. Lifecycle teardown clears router and owned instance without leftover modal input block.

## Performance considerations
Two new icons <=512 textures, no mipmaps/readback; single cached instance; existing wallet revision refresh updates labels, no per-frame resource lookup/loading. No eager startup residency.

## Acceptance criteria
- [x] Reference layout, actual supplied art, current level, two independent plus/count/select controls and PLAY/close.
- [x] Selected effects apply once; no selection/close has no use debit; zero energy blocks; stale callbacks rejected.
- [x] Initial3, grants+1, atomic spend, persistence, v1/v2 migration/future/corrupt/save failure protection.
- [x] Lazy loading/reuse/missing resource recovery/cleanup; preserved native assets.
- [x] Compile, focused core/UI tests, phone/tablet visuals and project validator.

## Verification matrix
Core: wallet migration/add/spend/failure/roundtrip; board exact9 removal/+30; command entry intent consumption. UI: actual pointer select/plus/close/PLAY, cache/gates, no-count/no-energy/failure, render phone/tablet, actual level effects. Existing regression suites covering changed boundaries. Verified; see delivery evidence below.

## Definition of Done
- [x] Acceptance criteria and ownership verified, evidence recorded, device limits stated.

## Implementation plan
1. Add focused regression cases and inventory/effect/intent path.
2. Author popup via Editor API and shared art, lazy Home controller bindings.
3. Verify migration, saves, cancellation/gates, effects and screenshot/protected asset readback.

## User refinement
User confirmed right-lower badge: existing in-game red RemainingChargeBadge with plain number when count>0, green+only at0. Remove ×3 and separate left badge. Red count is display-only; selecting the item remains on the main button. Green+grants1.

## Verified delivery — 2026-10-28

- Existing HomeScene and GameScene are byte-identical to the pre-feature backup (`native-scenes.json`). Added one lazy Resources prefab `Assets/Resources/Prefabs/UI/Popups/PrelevelPopup.prefab`, no new scene binding/Canvas/manager. Reused supplied pause art, green button/base and exact in-game red/green badge sprites. Two icon assets under Assets/Sprites/UI/Prelevel; source/generation manifest recorded. Text: LEVEL NN and PLAY; independent selected markers. Counts appear only in right-lower red badges;0 shows green+and no count.
- Core regression11 cases failed before capability, then passed. Full EditMode161/161 passed (bb4a0ad282dd4b7cb1cf3cdc84241f53). Full PlayMode56/56 passed (4e1ba090bc1d4efda58adc4e28245318), including4 new entry cases. Final badge binding validation was followed by focused4/4 (d0be8b3056f949be9c68a8bd05c72a41). Focused tests use memory saves and actual raycast pointer events, never spend real player resources.
- Verified initial3, independent grants at0, saved reload, v1/v2 migration preserving balances/timing/old booster inventory, atomic both-charge spend/rejection, save failure, invalid mask, all3 levels' three sets/+30sec, normal no-selection path, cancel/reopen/cache/gates,0 energy, missing resources/scene, failed entry return with error, no repeated effects on restart. Other existing tests retain prior save protection, navigation, pause, purchase and energy cases.
- Read-only review findings resolved: modal PLAY checks its own gate; selected-save failure uses guarded Home recovery and transfers error to Home; stale booster callbacks and lifecycle pause are gated during failure; destroyed-popup unregister does not close an already destroyed Unity object; badge dependencies validated before cache publication.
- Shared wallet schema3 adds EntryCharges[2]. New StartSortEntryCommand/AddSortEntryChargeCommand and navigation intent/error commands mutate only through cached Systems/Models. Home Entry partial owns resource loading/cache/listeners/teardown; GameSystem prepares an unpublished board and saves both count deductions before publishing attempt/effects. Retry/next/editor entry retain original nonselected behavior.
- Evidence folder `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/prelevel/`: backed up sources/scenes, art-manifest, native-scenes, phone/tablet captures and test summaries. Phone720×1280/tablet768×1024 inspected. New two icon textures each512×512 RGBA32, no mips/readable copies; nominal2MiB base pixels, existing modal art shared; one cached popup and revision-driven labels, no frame-by-frame loads or storage writes.
- Foundation validator0 warnings; Unity compile no new errors. Saved HomeScene Edit Mode, no extra Inspector setup. Actual device safe areas, storage latency/GPU profiling and process interruption during selected-save/start gap remain untested. No real payment/advertising added.

## Two visual states — 2026-10-28

User requests unselected orange-yellow plate with red stock badge; selected green plate with green white-check badge replacing numeric count. Tap item again restores orange/red without spending.0 stays orange and has green+; refill1 restores red1 and remains unselected. Close/reopen resets selection. Existing item art, modal layout/labels/PLAY, grant/save/effect rules unchanged. New OrangePlate/CheckBadge Sprite assets, active green plate uses existing BottomFunctionButton. Only owned prefab, editor author and Home Entry rendering change. Verified: new state-transition check failed before rendering change (402b95a27bea4a49ae0a25f95b07ccab); all5 entry PlayMode tests pass after change (fa45d72c180a407e9b9b0f3a8e9129f3). Real pointers verify toggle/deselect/reopen without spending, both-effects PLAY, missing/failing entry and0 refill. Phone720×1280 and tablet768×1024 inspected, contrasting one green checked item and one orange item. Source/backup/art-manifest/screenshots in `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/prelevel-states/`. No old scene bytes changed. Only Home Entry rendering, PrelevelPopupArtwork, owned PrelevelPopup prefab, two new Sprite textures and focused test changed; no Model/System/Command/save changes. New OrangePlate512 and CheckBadge256, shared old green plate, no mip/CPU read copies; nominal1.25MiB additional base texture pixels. Existing cached instance/revision refresh retained. Foundation validator0 warnings, compiler errors0, HomeScene saved Edit Mode. Device checks remain untested.

## Entry booster purchases and blue idle state — 2026-10-28

User confirms0-charge green+now opens a purchase offer instead of free refill:1600 coins buys3 uses of that specific entry booster. Positive stock keeps selection behavior and cannot open/rebuy. Vacuum Box description exactly `Vacuums up three identical items and clears them in one go`; Time Alarm description exactly `Adds 30 seconds to the timer`. Descriptions follow supplied wording; existing9-item vacuum effect unchanged. Reuse existing BoosterPurchasePopup prefab/key/layout with corresponding icon/×3/coin1600; Home lazily owns one cached clone, runtime Id EntryBoosterPurchasePopup, returns to parent entry modal on cancel/success, preserving other selections. No automatic activation/use; cancel costs nothing; insufficient funds disables purchase with notice; save failure changes neither coins nor stock; stale/double callbacks rejected. Purchase save atomic in existing schema3/slot; no migration changes.

Entry base becomes blue when inactive; activated green/check and0 green+retain prior behavior. Add BluePlate Sprite, retain old unused OrangePlate for recoverability. Entry purchase ownership: new Home focused purchase partial, cached router/model, new wallet BuyEntryBooster method+system+command. No new prefab/Canvas, no eager purchase load. Validate all cloned view bindings before modal replacement; teardown unregisters both views including destroyed wrappers. Main scenes and in-level purchase prefab protected. Verified by6 new atomic-purchase core cases (missing-method RED6/6 then GREEN), focused wallet/booster/shop/navigation EditMode57/57 (a6a51301368d4a9c93f3e4a56feeb4af), entry PlayMode7/7 (cd4a871245194497bb325edee0ba0c6b), final offer capture/pointer test1/1 after waiting for normal color-tint transition (029be98837d741f4a1918538c31c2c9a). Core prelevel fixture17/17 also passed. User cancellation, unaffordable purchase, exact title/body/icon, both-item cache reuse, parent selection restoration, stale callbacks, failed-save retry and destruction while child offer is top are exercised with actual pointers and memory saves. Native protection verifies HomeScene, GameScene and reused in-level purchase prefab byte-identical. No real player purchases made in tests. New BluePlate512 RGBA32, mip1/readablefalse, nominal1MiB base pixels; OrangePlate retained but no longer referenced by this popup. Shared existing green/check/offer art, one lazy child offer cache, no new per-frame lookup/disk writes. Read-only reviewer found no important issues. Foundation validator0 warnings; compile errors0; phone720×1280 and tablet768×1024 captures inspected. Final HomeScene saved Edit Mode. Sources: Home Entry partial + new EntryPurchase partial; wallet ledger/system + new BuySortEntryBoosterCommand; PrelevelPopupArtwork + owned PrelevelPopup prefab; focused tests. Evidence, art provenance, backups and protected hashes: `/Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/prelevel-purchase/`. Device/GPU/storage latency checks remain untested.
