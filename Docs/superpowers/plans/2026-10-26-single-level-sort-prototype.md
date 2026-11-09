# Single Level Sorting Prototype Implementation Plan

> **For agentic workers:** Use executing-plans to implement this plan inline, task by task. Preserve the user's one-level, primitives-only scope.

**Goal:** A complete offline one-level primitive sorting game in the existing All Sorted! Unity project.

**Architecture:** Use generated AllSortedApp and AllSortedController with Foundation Core/UI services. The project owns pure board data, QFramework Model/System/Commands and scene presentation. No shared Foundation changes.

**Tech Stack:** Unity 2022.3.62f3c1, URP 14.0.12, QFramework, Game Foundation v0.7.0, uGUI, NUnit.

**Spec:** Docs/Features/single-level-sort-prototype.md.

## Global Constraints

- Only one level; 24 primitives, four distinct colored types, six three-slot shelves, 180 seconds.
- No extracted Sort Express art, audio, fonts or configuration.
- User target remains /Users/zhangshuai/All Sorted!; project is not Git-managed and is not copied into another Unity checkout.
- Roles are centralized under Assets/Scripts/Game; shared packages remain unchanged.
- No textual serialized scene/prefab/material edits; create with Editor APIs.
- Existing Foundation onboarding passed; keep its generated identity and architecture.

## Review Focus

- Drag release on an occupied/off-board slot must never reveal the source or lose a piece.
- Pause/focus loss and multiple touches must cancel only presentation input, without stale scene references.
- Last-match/timeout ordering must produce exactly one terminal result.
- Scene/popup restart and teardown must leave no stale router view, subscriptions or input gates.
- Primitive-only independent content must remain true in source and generated dependencies.

## Task 1 — Deterministic board and independently authored level

Files: Models/Sorting/SortBoardState.cs, Utilities/Sorting/PrototypeLevel.cs, Editor/Tests/SortBoardStateTests.cs.

Interfaces: SortBoardState.TryMove(int,int,int,int), Tick(float), SetPaused(bool), Reset(); GetFront(int,int), GetBehind(int,int), RemainingCount, Matches, RemainingSeconds, Phase, Revision.

- [x] Write NUnit tests before production logic, plus a standalone runner for rapid pure-C# red/green feedback.
- [x] Establish the API stub, then run the initial-count assertion and observe a real assertion failure.
- [x] Implement slot stacks, atomic move/match cascade, count invariants, terminal phases and deterministic reset.
- [x] Prove a solution by replaying legal actions in tests, not only by checking piece counts.
- [x] Cover invalid destination, same slot, per-slot reveal, mixed types, paused input, deadline, reset and victory finality.

Representative test contract:

```csharp
[Test] public void InvalidMoveDoesNotRevealOrLosePieces() {
    var board = PrototypeLevel.Create();
    var before = board.GetFront(0, 0);
    Assert.IsFalse(board.TryMove(0, 0, 1, 0));
    Assert.AreEqual(before.InstanceId, board.GetFront(0, 0).InstanceId);
    Assert.AreEqual(24, board.RemainingCount);
}
```

Verification: standalone C# test runner red then green; Unity EditMode runs the same behavioral contracts.

## Task 2 — Foundation state/command integration

Files: Models/Sorting/SortBoardModel.cs, Systems/Sorting/SortGameSystem.cs, Commands/Sorting/SortCommands.cs; modify Architecture/AllSortedApp.cs.

Interfaces: SortBoardModel.Board plus Revision/phase/time observables; SortGameSystem start/move/pause/tick called only through Commands. AllSortedApp.Init registers the one Model/System.

- [x] Add command-state integration tests for start, accepted/rejected movement, pause/tick and reset.
- [x] Implement the smallest wrappers around the tested board, cache dependencies during initialization.
- [x] Verify no UI callback writes state directly, no second Architecture and no per-frame service lookups.

## Task 3 — Primitive scene, presentation and layered UI

Files: ViewControllers/Sorting/SortGameViewController*.cs, SortUiLayers.cs, SortPopupView.cs; Editor/Sorting/PrototypeSceneBuilder.cs; generated AllSortedPrototype.unity, original primitive materials and lazy popup prefabs.

Interfaces: controller serializes camera, board root, palette, layer host, HUD fields and two Resources popup keys. Popup views implement IUiPopupView; Foundation UiPopupRouter uses Replace.

- [x] Create assets through Editor APIs and keep authored bindings serialized.
- [x] Reconcile unique piece views on revision, animate pick/drop/match and show next piece per slot.
- [x] Handle mouse/single active touch, invalid drops, cancellation and camera sizing.
- [x] Build seven semantic UI hosts, Safe Area HUD, progress, timer and lazy cached pause/result views.
- [x] Wire input to Commands; gate world input as well as uGUI while modal.
- [x] Use PlayMode tests for real resource load/reuse, modal gates, restart and missing-resource recovery.

## Task 4 — Compile, play, render and review

- [x] Run Python project convention gate and Unity EditMode/PlayMode suites.
- [x] Read back generated scene, palette, UI hosts and popup keys; scan for forbidden original resource references.
- [x] Capture an actual rendered gameplay screenshot, inspect it and repair material/layout problems.
- [x] Request one independent final code review, address critical/important findings with focused regression tests.
- [x] Update Feature Spec evidence and leave unavailable device checks explicitly unverified.

Execution ledger: .gamefoundation/single-level-progress.md. No Git commits are fabricated because the user project is not a repository.
