# Level entry transition implementation plan

Goal: .15s Home fade + .25s eased goods/HUD arrival, timer held, conditional actual-loading reference.
Architecture: project presentation bridge plus command-owned runtime board entry gate. Unity2022.3/uGUI/QFramework.
Spec: ../Features/level-entry-transition.md. Execute inline in this shared non-Git Unity checkout; preserve native authored assets.

- [x] Pin timing/readiness and timer/action gates with deterministic EditMode tests.
- [x] Implement cached GameSystem entry gate and Game presentation cleanup with transparent material variants.
- [x] Implement lazy resource bridge, capped snapshot, async request, conditional loading and failure recovery; connect Home.
- [x] Author transition prefab using Editor APIs, with supplied Loading sprite and current background.
- [x] Test actual Home entry, booster/energy semantics, duplicate/failure/lifecycle paths and existing popup regressions. Record phone/tablet frames, run conventions and review changed boundary.

Review focus: pending entry consumed once; timer cannot tick through external Commands; no mandatory loading dwell; no lost material/shared asset mutation; no orphan overlay on failure/scene interruption.
