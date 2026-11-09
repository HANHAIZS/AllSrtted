# Main menu three-page navigation implementation plan

> **For agentic workers:** Execute inline with executing-plans, task by task. Keep evidence in the workspace ledger and obtain one independent final review.

**Goal:** Build the supplied Shop/Home/Setting interface, smooth tab transitions and current-level start/return flow.

**Architecture:** Keep one AllSortedApp. A navigation Model and Commands own selected tab and one-shot level entry; the Home controller owns scene-bound presentation and transitions. Existing gameplay Commands and shared settings/quotas retain their ownership.

**Tech Stack:** Unity 2022.3, QFramework, Game Foundation UI, uGUI, original Sprite metadata partitions, Unity MCP, built-in imagegen for required raster edits.

**Spec:** Docs/Features/home-screen.md

## Global Constraints

- No Foundation package edits or second architecture.
- Edit Unity serialized assets through Editor APIs only; preserve GameScene authored layout and existing popup identities/keys.
- Shop is the supplied background without products; Setting has the three shared session switches.
- Tabs remain responsive during a 0.25s retargetable transition; page input opens only after settling; short 0.12s body crossfade.
- Level/quotas/preferences persist within the current run only. Direct GameScene entry defaults to 1 unless an explicit menu or one-shot Home request exists.
- Use live project with external backups: this folder is not a Git repository; do not initialize one or create a disconnected worktree.

## Review Focus

- Fast Home/Shop/Setting retargeting must preserve current visual state and select only the last target.
- Hidden and transitioning Start/settings callbacks must not mutate state or enter a game.
- Failed scene availability checks must retain the current screen/modal and clear pending entry.
- One-shot level entry must not leak into a later direct scene load or editor preview.
- Shared settings and spent quotas must survive repeated scene round trips without duplicate listeners.

## Task 1: Navigation state and Commands

Files: Models/Navigation/SortHomeNavigationModel.cs; Commands/Navigation/SortHomeNavigationCommands.cs; Architecture/AllSortedApp.cs; Editor/Tests/HomeNavigationTests.cs.

Interfaces: enum SortHomeTab {Shop=0,Home=1,Settings=2}; model SelectedTab (BindableProperty<SortHomeTab>), PendingLevel (read-only externally); commands SelectSortHomeTabCommand(SortHomeTab), RequestSortHomeEntryCommand(int), ConsumeSortHomeEntryCommand (LevelNumber output), ClearSortHomeEntryCommand. Selection/request expose Accepted.

- [x] Write focused tests that assert missing navigation capability first, using reflection only until production types exist.
- [x] Run HomeNavigationTests; expect failures for the missing registered model.
- [x] Implement validated selection (same target rejected), exclusive pending request, consume-once and clear. Register model in AllSortedApp.Init.
- [x] Verify invalid enum/level do not mutate; duplicate request does not overwrite; consume twice returns level then zero; settings/quotas/board unchanged. Run focused tests green.

Representative acceptance:
```csharp
app.SendCommand(new RequestSortHomeEntryCommand(3));
var first=new ConsumeSortHomeEntryCommand(); app.SendCommand(first);
var second=new ConsumeSortHomeEntryCommand(); app.SendCommand(second);
Assert.AreEqual(3,first.LevelNumber); Assert.AreEqual(0,second.LevelNumber);
```

## Task 2: Home presentation and artwork

Files: ViewControllers/Home/SortHomeViewController.cs and .Transition.cs; Editor/Home/HomeSceneBuilder.cs; Assets/Sprites/UI/Home; Assets/Scenes/HomeScene.unity.

Consumes Task 1 commands/model plus shared SortSettingsModel and existing SortButtonPressFeedback/SortUiLayers. Produces Home controller fields HomeCamera, Layers, Pages[3], TabButtons[3], TabIcons[3], TabLabels[3], NavigationArea, SelectionPlate, rail segments, StartButton/LevelLabel, SettingButtons[3]/OffMarks[3], SettingsArt; configurable GameSceneName.

- [x] Add PlayMode smoke coverage that loads HomeScene and asserts an actual controller/pages; run red against missing scene.
- [x] Inspect original PNGs, edit white backgrounds of Shop/Home and remove Level 18 from Start base using built-in imagegen; record exact prompts and saved paths. Inspect generated alpha/content.
- [x] Copy originals/selected outputs to Sprite-only Home directory. Author native Sprite partitions for navigation cap/flat trim and settings row without redrawing original pixels.
- [x] Create HomeScene with one camera/EventSystem and seven ordered hosts via Unity MCP. Build 3 page backgrounds, top-aligned settings art, real toggle buttons, stable navigation hit regions, a single moving selected piece and dynamic start label.
- [x] Implement cached bindings, command-driven selection/settings, 0.25s retargeting from actual current values, 0.12s page alpha, hidden/transition input rejection, resize target refresh and lifecycle listener cleanup.
- [x] Check real pointer hits and final transforms/labels/page gates on all six directions; capture phone/iPad pages.

Acceptance sequence:
```csharp
home.TabButtons[0].onClick.Invoke(); yield return new WaitForSecondsRealtime(.08f);
var before=home.SelectionPlate.anchoredPosition;
home.TabButtons[2].onClick.Invoke(); Assert.AreEqual(before,home.SelectionPlate.anchoredPosition);
yield return new WaitForSecondsRealtime(.35f);
Assert.IsTrue(home.Pages[2].interactable);
Assert.IsFalse(home.Pages[0].blocksRaycasts); Assert.IsFalse(home.Pages[1].blocksRaycasts);
```

## Task 3: Start/Quit scene flow

Files: SortGameViewController.cs, .Editor.cs, .UI.cs, new .Navigation.cs; existing PauseSettingsPlayModeTests; new HomeNavigationPlayModeTests; HomeScene Build Settings.

Consumes Task 1 one-shot request and Task 2 Start. Produces Home->Game current-level fresh board; visible top Pause Quit->Home while preserving session quota/preferences. Original direct GameScene and explicit editor preview priorities remain.

- [x] Add failing round-trip test: play level 3, spend freeze, toggle Music, Pause Quit returns Home and reads Level 3; Start enters fresh level 3 with 180s, zero freeze, same spent quota and Music state.
- [x] Check GameScene availability before Request command; guard repeat start; retain Home with visible error and clear pending intent on load failure.
- [x] Consume pending Home entry once during Game.Start; explicit Editor launch choice takes priority, clears unused entry; otherwise preserve fresh/direct default 1.
- [x] Bind Quit only for active top pause popup, scene availability preflight, no premature router clearing on failure. Scene teardown owns cleanup. Preserve Close and Restart.
- [x] Update old inert-Quit integration coverage to the new behavior, retaining meaningful no-refund restart assertions. Add missing HomeScene recovery test and hidden callbacks/duplicate navigation tests.
- [x] Set HomeScene first in enabled build scenes through EditorBuildSettings API, GameScene second, SampleScene disabled; read back.

## Task 4: Verification and delivery

- [x] Force-refresh only outside running tests; confirm stable compilation. Run focused Home tests then full AllSorted.Tests and AllSorted.PlayModeTests, inspect all failures.
- [x] Run `python3 Scripts/validate_game_foundation_project.py --project .`, expect 0 warnings.
- [x] Render 720x1280 and 768x1024 Home/Shop/Setting and shared OFF state; capture actual navigation transition frames and encode a local preview using installed imageio_ffmpeg.
- [x] Compare protected existing scene/prefab hashes and unchanged gameplay sources. Inspect dependency lookup locations and cleanup.
- [x] Request one independent whole-change review, fix important findings with reproducing tests, then run affected/full verification as warranted.
- [x] Update feature spec to Verified only with evidence, copy spec/plan/manifest/results/previews to outputs/all-sorted-prototype/home-screen, leave an inspectable HomeScene open. Report device limits and session-only progress.

## Execution authorization

2026-10-27: User said 开始执行 after reviewing the written three-page design. Proceed inline without another permission handoff. No Git commit/worktree operations apply to this live non-Git Unity project; backups and a source/hash ledger preserve recovery and review boundaries.
