# Home wallet implementation plan

> Implement inline using executing-plans; use a fresh final reviewer as required by that skill. This existing project is not a Git repository; preserve recoverable workspace backups instead of inventing commits/worktrees.

**Goal:** Add shared Home/Shop resource HUD and the user-confirmed energy, save and offline rules.
**Architecture:** One existing AllSortedApp; stateful ledger owned by wallet Model, commands via wallet/game Systems; focused partial of existing Home controller; native Editor API authoring only.
**Spec:** Docs/Features/home-wallet.md
**Constraints:** no Foundation edits; preserve old Inspector fields and GameScene/popup bytes; isolate all tests from real player save data; retain old booster quotas.

## Review focus

- Ended-attempt identity and duplicate terminal callbacks must never charge a new attempt.
- One-energy restart must create a valid new board at0; rejected subsequent retry must leave it intact.
- UTC rollback, partial recovery/manual grants and long offline intervals must not award extra energy.
- Failed scene/save operations must not lose funds or clear the current UI; invalid/future saves must not be overwritten.
- Hidden HUD and lifecycle callbacks must not leak actions/subscriptions, collide with central Shop header, or overwrite old UI properties.

## Task 1 — transactional resource ledger and persistence

- [x] Add meaningful ledger tests first: defaults, +800/+1/cap, exact900s, partial intervals/manual changes, offline catchup/cap, rollback, roundtrip, corrupt backup, future schema, write failure, overflow.
- [x] Run EditMode and retain RED for missing ledger capability.
- [x] Implement Models/Wallet/SortWalletLedger.cs and SortWalletModel.cs; Systems/Wallet/SortWalletSystem.cs; Commands/Wallet/SortWalletCommands.cs. Slot all-sorted-wallet v1; Foundation Save/Clock utilities already installed.
- [x] Register wallet Model/System in Architecture/AllSortedApp.cs and run green core tests. Test fixtures swap architecture storage/clock to memory and restore it; no player slot writes.

## Task 2 — attempts and 0-energy gates

- [x] Write integration tests for failure/repeat, abandon token, restart from1 to0, duplicate/stale/hidden calls, 0 entry/restart and victory. Watch assertions fail before connecting flows.
- [x] Modify SortGameSystem.cs, SortCommands.cs, SortHomeNavigationCommands.cs and game UI/Navigation partials. Begin a token on real new boards, settle one debit per attempt; UI must validate/save outcome before clearing old popup/pieces. Quit settles after scene load succeeds.
- [x] Low-level content initialization stays a developer/test path; player Home requests and retry/advance routes are gated.
- [x] At0 terminal state use existing result secondary HOME action, no new popup. Verify original booster allowances and scene failure behavior.

## Task 3 — native resource HUD and art

- [x] Inspect imagegen outputs, record prompts, alpha and source/asset manifest; import under Assets/Sprites/UI/Home/Wallet/ with bounded Sprite regions and stable IDs.
- [x] Add HUD PlayMode tests before authoring: expected field bindings absent, real pointer plus changes values, cap, Settings/transition rejection, low-energy display,0 recovery route.
- [x] Add Editor/Home/HomeWalletArtwork.cs; author only new Panels safe-area subtree and new fields on original Home controller. Add future HomeSceneBuilder hook without invoking whole-scene Build.
- [x] Add focused Resources partial; cache dependencies, integer-second refresh, focus/pause flush, alpha/input gating, remove listeners at teardown. Render original art with dynamic text and the user-provided transparent green plus badge.

## Task 4 — verification and delivery

- [x] Run focused tests, full project suites when changed gameplay boundaries warrant them, convention validator and ordinary Play startup. Inspect720x1280/768x1024 screenshots and exact component protection comparison.
- [x] Request one fresh final code review against this spec/plan and backup diff. Fix Important/Critical findings with a failing regression then green verification; record any rulings.
- [x] Update feature status/evidence and hand back working scene with asset/save paths, previews and remaining device limitations. Do not delete workspace evidence because no Git history exists.

Execution complete: 115/115 EditMode, 41/41 PlayMode, convention validator 0 warnings. Final size steering uses CoinBar dimensions for both counters. Independent review findings resolved with pending settlement recovery and a strict project file-read boundary; see Docs/Features/home-wallet.md and final evidence.
