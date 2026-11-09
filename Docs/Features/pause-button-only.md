---
id: pause-button-only
title: Open the pause popup only from the level settings button
status: Verified
owner: Project
created: 2026-11-02
updated: 2026-11-02
---

## Goal
Only an explicit click on the existing top-right gear opens the level pause settings popup. Remove Escape and automatic focus/app-suspension popup triggers. Preserve popup appearance, settings, resume, restart, leaving confirmation, purchases and all five stage rules.

## User-visible behavior
The gear opens the normal cached pause popup; X resumes. Escape neither opens nor dismisses it. Backgrounding or desktop focus loss cancels unfinished drags, saves wallet state and silently freezes the board clock without loading a popup. Returning resumes only that lifecycle-owned silent suspension; an existing manual pause/purchase/leaving popup remains open and paused. A gear click during a silent suspension explicitly opens the settings popup. Missing pause resources remain recoverable, and lifecycle suspension does not attempt resource loading.

## Ownership and architecture
Existing SortGameViewController.Input.cs removes the keyboard shortcut and routes app/focus return to a private lifecycle resume method. UI.cs separates silent suspension from RequestPause; paused state continues through PauseSortCommand, with dependencies already cached. The controller-owned suspendedWithoutPopup flag records ownership, so automatic return cannot close a manual or purchase modal. No new state owner, model, save field or package changes.

## Assets and UI ownership
No prefab, scene, art, Inspector, Resources key or popup-layout changes. PausePopup remains lazy/reused in existing Popups host and existing Replace router; explicit modal gating is unchanged. Silent suspension relies on paused board input rejection and does not add an invisible modal gate. Popup animation85→103→100 stays unchanged.

## Failure and lifecycle
Repeated focus/pause notifications are idempotent. App-paused and focus-lost reasons are tracked separately; either return order resumes only when both clear. Suspension during entrance finishes existing presentation and silently pauses without starting the gameplay clock. No automatic return resumes a lost/won board or an already manually paused modal. Missing UI cannot prevent lifecycle pause or cancel/save behavior. No currency, stock or energy changes.

## Out of scope
Changing purchase-popup pause behavior, timer durations, layouts, art, store/settings pages, SDK or device performance work.

## Acceptance criteria
- [x] Gear opens the pause popup and X resumes, retaining reuse and normal modal input blocking.
- [x] Escape call path removed; no lifecycle callback opens RequestPause.
- [x] App suspension and focus loss do not create/show/cache a pause popup; silent clock pause, drag cancellation and return recovery verified.
- [x] Focus/background return does not dismiss an existing manual modal; missing pause UI is not loaded during suspension.
- [x] Entrance, pause/restart/leave/purchase and first-five artwork/gameplay regressions pass; protected native assets unchanged.
- [x] Unity compilation and Foundation validator pass.

## Verification matrix
Test-first lifecycle PlayMode tests against real authored GameScene, existing missing-resource/drag/entry/pause suites; full project EditMode/PlayMode; direct call-site audit for RequestPause and Escape; native asset hashes and console. No screenshots or final preview needed for trigger-only change.

## Performance considerations
Removes a frame-loop key poll and automatic popup preparation; adds no per-frame lookup or allocation. Existing cache/cleanup retained.

## Definition of Done
Scoped criteria checked only after recorded red/green lifecycle behavior and final regression evidence. Any unrelated failures reported explicitly.

## Completion evidence
2026-11-02: Full PlayMode100/100, job011d9c88ef1d4f92a1f6c4fe4134ac15; full EditMode224/224, jobda649dcaf3b54aee8bac91666d695b77. Initial lifecycle red99250895b73a4f39811540ff54462826 reproduced automatic popup/failing missing-resource access; overlapping-return red7ab4b2e5562f4e169dbfbacbe221febf reproduced premature resume. Final full suite covers both repairs. Focused pause/entry18/18 also passed before the overlap repair. Independent recheck found no remaining Critical/Important issues. Foundation validator0warnings. All39 protected scene/popup/model/prefab files match their pre-change hashes. No new compiler errors; console has expected missing-resource messages emitted by passing failure-path tests, not runtime deployment failures. Escape removal is verified by call-site audit; no synthetic OS keypress test is claimed. Evidence: /Users/zhangshuai/Documents/Codex/2026-10-26/lia/outputs/pause-trigger/.
