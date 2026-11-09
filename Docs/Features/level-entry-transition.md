---
id: level-entry-transition
title: Home to level entry transition
status: Verified
owner: Project
created: 2026-10-29
updated: 2026-10-29
---

# Home to level entry transition

## Goal
A continuous Home-to-Game transition with no artificial loading delay and timer/input beginning only after arrival.

## User-visible behavior
Home snapshot fades over the actual level background preview in .15s. After that, each merchandise item scales around its own fixed center from0 to1.15 over.18s with cubic ease-out, then settles to1 over.10s with smooth ease-in/out, alongside fade-in. Shelves and all item centers remain stationary; the merchandise phase totals.28s; top information and bottom boosters fade concurrently. Fast entry completes in approximately .43s. Slow load shows supplied 转场动画.png (purple Vacuum Box Loading page) only after .25s if Game is not ready. It disappears as soon as Game is ready; no minimum dwell or fake progress. Game entrance then takes only the remaining presentation phase. Existing .25/.15s elastic popup motion stays unchanged.

## Out of scope
Level design, popup animation changes, rewards/prices/energy policy, fake percentage/progress and mandatory loading-page dwell. Direct editor GameScene entry and result/fallback replay keep their current behavior; pause Restart now reuses the merchandise pop entrance.

## Ownership and architecture
Home validates scene and a lazy transition prefab before publishing an entry intent, then starts LoadSceneAsync Single. A project presentation-only SortLevelTransitionView owns a temporary persistent Blockers-domain overlay Canvas (order600), Home RenderTexture, background preview, loading art and async lifetime; it cleans up textures/object on success or failure. It owns no economy or level rules. Game controller Entrance partial caches renderers/material variants/HUD groups and animates them; restores exact authored endpoints/materials. SortBoardModel has runtime-only IsEntering; SetSortEntryPresentationCommand/SortGameSystem set/release that gate. Tick and gameplay actions reject entry-period mutations, wallet UTC refresh continues. No new Architecture or Foundation changes.

## State and persistence
No save version change. Existing pending level/selected boosters are consumed once at actual scene start. Entry boosts reserve once through existing save-before-publish Command. No additional energy charge. Failure clears pending entry, releases owned blockers and preserves recoverable Home/error behavior. Invalid transition resources keep Home usable; scene-load failure restores Home controls and dismisses overlay. A completed load without a valid Game controller recovers Home with an error instead of infinite loading. Disabling/destroying Game during entrance restores owned visuals and releases timer gate and bridge.

## UI, prefab, scene, and Inspector impact
New Assets/Resources/Prefabs/UI/Transitions/LevelTransition.prefab, key Prefabs/UI/Transitions/LevelTransition, loaded first use and released after entry. Direct Sprite references: existing LevelBackground and new Assets/Sprites/UI/Transitions/Loading.png copied from supplied reference. Snapshot is transient GPU RenderTexture capped1280px. Overlay is a scene-boundary Blockers host, never a second resident UI root; no popup/page sorting edits. Only UI creation via Editor APIs. Game/Home scene bindings need no native rewrites; controller default key is configurable. Runtime material copies restore originals after .28s, no asset material changes. Popup keys/assets/motion protected.

## Acceptance criteria
- [x] Home .15 fade, merchandise0→1.15→1 in.28s about stationary item centers, static shelves, HUD synchronized; fast overall~.43s.
- [x] Loading art appears only while actual readiness is delayed beyond .25s; immediate ready means no artificial wait.
- [x] Timer and input remain blocked until entrance completion; wallet real-time recovery remains independent.
- [x] Same selected level/boosters start once, repeated PLAY blocked, no extra life debit.
- [x] Failure, missing resource, cleanup and repeated transitions recover; authored visuals/materials restored.
- [x] Popup elasticity unchanged, direct scene/restart behavior preserved.

## Verification matrix
| Boundary | Evidence |
| --- | --- |
| Pure timing and domain gate | EditMode timing boundaries, external tick/action blocking, release |
| Runtime | Actual async Home entry, selected boosters, countdown hold/release, duplicate click/failure/lifecycle tests |
| Visual | Phone/tablet entrance frames and supplied Loading art |
| Conventions and compilation | Validator and Unity console |
| Performance | Snapshot/material cleanup, no per-frame dependency or hierarchy lookup |

## Definition of Done
Checks above have concrete evidence; native asset readback and protected scene/popup checks; end with HomeScene clean Edit Mode. Real slow-device throughput and memory/frame timing remain hardware validation.

## Verified evidence (2026-10-29)
- Full EditMode188/188 passed, job4b55a0aeedd24778a01902a6220cbd3d. Full PlayMode78/78 passed, job07abb502c6074fdb90ff5bb713382317. Initial timing4/4 and focused async/Home/prelevel/wallet29/29 also passed.
- Actual asynchronous entry tests verify pending selected level/boosters consume once, time stays210 during animation with Time boost and HUD shows03:30, external Tick999 cannot advance clock, gameplay/pause click blocked until ready, entry settles without energy debit, materials/positions/HUD restore exactly. Lifecycle suspension restores full visual endpoints then pauses synchronously before any clock tick; disable releases owned resources. Direct entry and all existing purchase/popup tests pass.
- Loading readiness boundaries are exercised with a test-controlled clock only; production has no artificial delay/readiness override. Reference art appears at elapsed>=.25 while not ready and hides synchronously at MarkSceneReady, with no minimum dwell.
- Rendered720x1280 and768x1024 entry/Loading frames plus test JSONs: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/level-transition/. Loading PNG is byte-identical to supplied source.
- HomeScene and GameScene SHA256 match backups; no native scene edits. Existing popup prefab/motion scripts were not modified. New LevelTransition prefab and Sprite import authored via Editor APIs.
- Code review resolved lifecycle suspension, originally opaque rear-item alpha endpoint and boosted initial timer label; second review found no important outstanding issues.
- Cleanup readback after tests: Active bridgefalse, HomeEntryCapture RenderTextures0, EntryFade materials0. Snapshot max1280px long edge, transient capture/depth GPU memory; one .25s temporary material copy per shared source, all restored/destroyed. No per-frame GetModel/GetSystem/GetUtility or hierarchy lookup added; render loops use cached collections.
- Convention validator passed, zero warnings. Expected missing-resource errors are asserted test cases; no compiler errors. Unity returned to clean HomeScene Edit Mode.
- Affected source: Home BeginGame now async bridge handoff; new ViewControllers/Navigation/SortLevelTransitionView, Game entrance partial/lifecycle and update hooks, runtime SortBoardModel.IsEntering and command/System gates, timing Utility, Editor LevelTransitionArtwork; tests await actual scene readiness. Implementation plan: ../Plans/2026-10-29-level-entry-transition.md. No save version, pricing, rewards or popup-motion changes.
- Hardware loading stalls, mobile safe-area/interrupt timing and actual GPU frame/memory measurements remain real-device checks. Editor captures and tests validate behavior, not device throughput.

## Current revision: per-item pop entrance (2026-10-29)
User approved replacing lateral merchandise motion with0%→115%→100% scale and fade, first.18s expansion plus.10s settle, static shelves. Cache each item target scale, apply animation per item (never scale the parent, which would move slot positions). Preserve timer/input gating until the full.28s settles. No material/scene/popup asset changes. Current revision verified:5/5 focused EditMode (job0b7f651fa82640fbb4a855e8d6294407) and25/25 focused PlayMode (job8b57a420ecf84db6b822197001b4d8b8), covering actual selected-level entry, loading, timer/input gating, pause lifecycle, wallet/Home navigation and popup motion. Earlier evidence above refers to previous implementation.

- Current pop rendering evidence: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/entry-pop/ includes phone/tablet115% peak frames, start/end frames and result JSONs. Runtime assertions compare every item center before/at peak and every scale against its cached target×1.15. Endpoints restore per-item intended normal scale, including rear-item ratios.
- The new revision changes only Timing Utility and Game Entrance partial plus focused tests/spec; shelf traversal and lateral offsets removed. All merchandise items animate independently; no parent scaling/slot drift. Both native scene hashes remain unchanged. Loading art, popup source/assets, resource/economy state and existing gates retained. Device motion/performance remains a hardware check.

## Pause Restart pop extension (2026-10-29)
After an accepted Restart from the top, open pause-settings popup, run the same per-item0→1.15→1 scale/fade over.28s. The existing popup first closes with its unchanged.15s elastic motion, then the visible goods run their.28s pop; shelf geometry stays fixed. Reuse the Game controller entrance presentation/gate with a local unscaled clock: no Home snapshot, scene load, Loading page or additional Home-fade delay. The only lead-in is the actual authored pause-panel close duration, so the panel cannot obscure the goods pop. Rejected Restart (zero lives/save failure/stale/disabled callback) keeps the pause modal and board intact, with no entrance started. Input and timer remain blocked until settle; existing once-per-attempt life cost and booster inventory/reset policy remain authoritative. Result/fallback replay and direct programmatic restart without a pause modal remain unchanged. Lifecycle interruption restores visuals and pauses as before.

Implementation touches only Game Entrance/UI partials, tests and this spec; no new assets/resources/native changes. Verified pause Restart scope: full PlayMode81/81 passed (jobcbc8e0f28b8b4abf91d620de8df7482b); prior focused15/15 also passed. New tests physically click Restart, assert original scene stays loaded with no bridge/Loading, gate direct ticks and duplicate callbacks, verify115% item scale and stationary centers, preserve spent booster counts, and verify last-life1→0 plus failed debit/retry. Existing pause/three-level tests now await actual local animation completion.

- Restart evidence: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/pause-restart-pop/ contains phone/tablet peak frames, backups and full test results. Both scene assets still match original SHA256. No new runtime asset/prefab or native edits; only Game Entrance/UI partials and test await helpers changed.
- Final cleanup: no EntryFade materials, no HomeEntryCapture textures, no active transition bridge; HomeScene clean Edit Mode. Convention validator passed zero warnings. No new subscriptions or per-frame dependency lookups; existing cached entrance loops reused. Real device feel/performance remains a device check.
