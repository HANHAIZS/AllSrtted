---
id: {{FEATURE_ID}}
title: {{FEATURE_TITLE}}
status: Draft
owner: Project
created: {{DATE}}
updated: {{DATE}}
---

# {{FEATURE_TITLE}}

## Goal

{{FEATURE_SUMMARY}}

## User-visible behavior

- TBD

## Out of scope

- TBD

## Ownership and architecture

| Responsibility | Owner | Planned type/path | Notes |
| --- | --- | --- | --- |
| State mutation | Command / None | TBD | UI must not mutate state directly |
| Observable or persistent state | Model / None | TBD | Record save version impact |
| Shared project rules | System / None | TBD | Keep gameplay policy project-owned |
| One-shot notification | Event / None | TBD | Use only when notification is transient |
| Unity presentation | ViewController / None | TBD | Preserve Inspector references |
| Adapter or repository | Utility / None | TBD | Vendor policy stays in project |
| Reusable infrastructure | Foundation / None | TBD | Requires explicit shared-package scope |

## State and persistence

- Runtime state: TBD
- Save schema/version/migration: None
- State mutation path: TBD

## UI, prefab, scene, and Inspector impact

- Affected assets: None
- Loading mode and final paths: None (popups -> Assets/Resources/Prefabs/UI/Popups; direct prefabs -> Assets/Prefabs; non-Sprite art -> Assets/Art; Sprites -> Sprite assets only)
- Resources keys or direct reference owners: None
- Load/cache/instance cleanup responsibility: None
- UI layer contract/adoption: `Docs/Foundation/UiLayers.md` / `Docs/Foundation/UiLayers.Project.md` when UI is affected
- Semantic layer and actual host: None
- Modal open/return behavior: None (Replace or Stack; record actual supported APIs)
- Input gate owner/release paths and non-uGUI input routing: None
- Protected authored properties and allowed changes: None (see `Docs/Foundation/UiAuthoring.md`)
- Popup first-use/reuse/missing-resource recovery and startup residency: None
- Model deletion dependency audit (runtime, configuration, Editor inputs) and recovery: None
- Hierarchy or binding changes: None
- Inspector confirmation: None

## Integrations and failure behavior

- SDK/consent/lifecycle: None
- SDK versions, initialization dependencies, identity source/sync owner: None
- SDK prerequisite failure gate and local/device/server evidence (link integration checklist): None
- Content update/offline/rollback: None
- Cancellation, timeout, retry: None

## Performance considerations

- Allocation, lookup, subscription, pooling, or main-thread impact: None

## Acceptance criteria

- [ ] User-visible behavior is stated as an observable result.
- [ ] Failure and recovery behavior is defined where relevant.
- [ ] Out-of-scope behavior is explicit.

## Verification matrix

| Boundary | Evidence required | Result |
| --- | --- | --- |
| Project conventions | `validate_game_foundation_project.py` | Pending |
| Unity compilation | No new compiler errors | Pending |
| Focused tests | EditMode/PlayMode test names or reason not applicable | Pending |
| Runtime or asset behavior | Exact readback, logs, hierarchy, screenshot, or device evidence | Pending |
| UI layers and authoring, when affected | Ordering, top-modal input, composed gates, failure/cleanup and preserved authored properties | Not applicable / Pending |
| Performance | Allocation/lookup/subscription review when relevant | Pending |

## Definition of Done

- [ ] Ownership and QFramework roles match the implemented files.
- [ ] Public behavior and acceptance criteria are implemented.
- [ ] Save, SDK, hot-update, UI, prefab, and Inspector impacts are handled where relevant.
- [ ] Convention validation and Unity compilation pass.
- [ ] Focused tests and exact behavior checks pass.
- [ ] Performance risks and manual platform checks are reported.
- [ ] Status is updated to `Verified` only after all required evidence exists.

## Decision log

- {{DATE}}: Feature Spec created.
