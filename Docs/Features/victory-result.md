---
id: victory-result
title: Victory settlement artwork and Epic Toon celebration
status: Verified
owner: Project
created: 2026-10-29
updated: 2026-10-30
---
# Victory settlement
## Goal
Show supplied SUCCESS ribbon, central coin stack and Continue button over dimmed completed gameplay; decorate with selected supplied effects.
## User-visible behavior
SUCCESS upper, coins central, Continue lower. Rainbow paper showers celebrate; restrained yellow sparkle twinkles around coins. Preserve popup85%-103%-100% .25s and .15s close. Continue uses existing next-level command; last authored level wraps to1. Existing zero-energy entry restriction/HOME recovery stays. Coins are decoration, no new reward amount/transaction requested. Failure result remains unchanged.
## Ownership and architecture
Project ViewControllers render/bridge effects and forward existing commands; no Foundation or economy changes. New lazy cached Resources key Prefabs/UI/Popups/VictoryPopup, existing ResultPopup kept for losses. Same Popups host, Replace router and modal gate. Dispose with scene/router; stop and clear effects on disable. No new startup references. Source particle presets selected: Epic Toon FX ConfettiShowerRainbow (one lightweight emitter; no explosion/clouds) and SparkleAreaYellow (star/tiny sparkle emitters). Import only selected prefabs and dependency closure. Native preset prefabs under Assets/Prefabs/Vfx/EpicToon; materials/textures under Assets/Art/Vfx/EpicToon. UI Graphic adapter uses their ParticleSystems on unscaled time, cached particle buffers, CanvasGroup fade, no raycast. Disable world renderer to avoid URP incompatibility; UI/Default material consumes original textures and particle colors.
## Out of scope
Real rewards, new levels, collection bar/mascot not supplied, sound autoplay, full516MB package import, failure UI redesign.
## Acceptance criteria
- [x] Correct artwork/layout/Continue; lazy loading and reuse.
- [x] Supplied rainbow and gold effect presets render in UI, no input blocking or retained particles after close; unscaled time.
- [x] Continue advances1->2->3->1 only once; hidden callbacks rejected; zero-energy and missing resources recover.
- [x] Existing failure and popup motion pass; convention validation/compile.
## Verification matrix
Focused PlayMode victory asset/lifecycle + existing ThreeLevel/Prototype/PopupMotion checks. Native prefab readback, particle texture/color/geometry/layout checks; user requested no final preview. Actual hardware untested.
## Definition of Done
All acceptance checks have recorded results; preserve old prefab GUID/key, game layout and unsaved Editor scene contents.

## Implementation evidence
- Added lazy VictoryPopup key; original ResultPopup prefab SHA256 unchanged. Only victory uses Continue; existing AdvanceSortLevelCommand handles1->2->3->1 and rejects repeated/hidden activations. Title metadata remains ALL SORTED! for existing result consumers, baked banner/Continue artwork are visible. Body shows only existing zero-energy warning, HOME recovery remains available. No coin grants added.
- Assets: Assets/Sprites/UI/Victory/{Artwork,Continue}.png; Artwork transparentized with built-in imagegen and native Sprite slices SuccessBanner/VictoryCoins. Prompt preserved exact SUCCESS! ribbon, coin stack, position and colors while removing fake checker background only. Continue copied unchanged from relocated /Users/zhangshuai/Documents/Sort素材/胜利结算确定按钮.png.
- Chosen presets: ConfettiShowerRainbow (single emitter, avoids explosion clouds) and SparkleAreaYellow (gold stars and initial tiny sparkle).8 dependency files448645bytes copied with native identities, manifest at outputs/victory-result/fx-manifest.json; no demo/scripts/sounds imported. Native readback: confetti25particles at1s, 3x3 atlas; sparkle3particles, RGBA255,226,106,255.
- Three Canvas particle bridges reuse preset simulations and source textures, world renderers disabled, max128/24/24 particles; cached256 buffers, no frame allocations, no raycasts, UI/Default with alpha textures supports current URP. OnDisable stops and clears; outer modal fade also fades effects.
- Inner SortVictoryLayout fits a660x1180 subtree to viewport while shared popup motion owns the outer.9 scale; allows optional HOME below Continue on short tablets. It checks cached viewport size only and does not recapture animation endpoints.
- Red resource test failed as intended before implementation (job2937b1221e1f4b01a5facb68fd3e2c24). Initial18/18 PlayMode tests passed, jobb7a2ef03e7054bbea10123cd83ea6f5f, covering Victory effects unscaled/clear/no-input, all three level transitions, original failure/replay, and all popup motion. Last geometry-only authoring fit is covered by additional short-viewport test. A rerun job5cfc3e45f30c41c89062d9ac2e76ed1d failed to initialize (no tests executed), without compilation errors; targeted retry pending.
- Static code review and separate layout follow-up found no P1/P2 issues. Convention validator passed0warnings. No final preview supplied per user preference; actual hardware rendering/performance untested.

## Revision: 30-coin reward and one-shot ribbons (2026-10-29)
User supersedes decoration-only policy. Replace shower with supplied ConfettiDirectionalRainbow ribbon emitters, one burst per victory opening, and ItemSparkleYellow brighter gold highlights. Add upper-left existing coin HUD (no refill requested), white/green outlined 30 beneath coin pile. Continue locks both actions, flies 10 pooled UI coins on staggered arcs (~0.9s unscaled), sends ClaimSortVictoryRewardCommand for the active won attempt, renders saved balance, briefly pulses HUD, then advances. Save failure unlocks retry and stays on result; hidden/destroyed animation cancels without later callback. Per-attempt claim is idempotent, stale/non-won rejected, persisted coin total uses existing schema4; no board/session persistence. No new startup hard references; existing VictoryPopup key/router/motion retained. Popup inner fitter extended upward for wallet; no final preview.
Verification: reward once/reload/save failure/stale attempt; animation delay and rapid double taps; one-shot ribbons, brighter gold settings, no input blocking; compact viewport; compile and foundation validation.

## Reward revision implementation
- ClaimSortVictoryRewardCommand validates Won then delegates to SortWalletSystem/Ledger. Per-attempt idempotence resets on BeginAttempt; coin total is saved before publishing; failed write leaves balance unchanged and allows retry. No schema change because gameplay attempts are not restored across app sessions.
- SortVictoryRewardView owns 10 reusable Image coins, unscaled staggered .58s flights (last arrives ~.87s), .2s HUD pulse, modal action lock, cancellation on close/disable, and post-save advance. Scene controller supplies cached wallet/model gates; stale attempt tokens rejected. Reward font Fredoka Bold, white with dark green outline, matching Continue style. Existing coin HUD and individual coin sprites reused.
- VictoryPopupArtwork authors upper-left balance, reward30, flying coin images,1340-height fit extent. Selected new source presets ConfettiDirectionalRainbow (root ribbon emitter only; clouds/glow inactive) and ItemSparkleYellow (larger .18-.38 stars, gold/cream color, two emitters14+9/s). Ribbons have one85-particle burst, no rate/loop,2.3-3.2s lifetime. Original particle textures preserved; all three UI bridges noninteractive/unscaled; transient cap100+32+32. Native dependencies only, manifest outputs/victory-reward/fx-manifest.json. No new Resources key or scene reference.
- Red tests3e9e08920a7d4fce8532058102c6eed9 failed for missing reward transaction. Green23/23 ledger EditMode tests3edb38609950483c8747b53068d89304 passed including reward roundtrip/idempotence/stale/save failure. PlayMode4/4a65b95aa53a449a184c5f4443a684acc passed including cancel/reuse/unscaled flight, one-shot ribbons/gold, compact viewport, effect teardown. Broad jobcdde2b7e11a54e108a217ac135e6da0c stalled before executing tests; stopped test-owned Play Mode, cleared orphaned job and reloaded runner domain, focused retry passed. No product compile errors.

- Final real-scene PlayMode2/2 job335199634274492eb5722a677b4fabbf passed: 1->2->3->1, wallet+30 each win, rapid repeated clicks ignored, no advance before flight. Prototype full win path passed. Foundation validator passed0warnings; Editor compilation idle, prefab readback has10 coin Images, white Fredoka-Bold30, no startup GameScene hard reference. HomeScene restored clean/EditMode. Console retains one Test Framework PlaymodeLauncher.cs:113 NullReferenceException from stalled launcher recovery; focused reruns succeeded, no gameplay/compiler exception found. Actual device appearance/performance remains user verification in Unity; no final preview generated or opened.
