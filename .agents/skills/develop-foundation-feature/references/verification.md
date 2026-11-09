# Feature Verification

Choose the smallest relevant set, then expand only when evidence requires it.

- Code-only change: Unity compile plus focused EditMode tests.
- Model/System/Command flow: test state before and after the command and emitted events.
- Save change: round trip, version migration, corrupt primary, backup recovery, missing data.
- Hot update UI: offline, no update, progress, cancellation, verification failure, rollback.
- SDK behavior: consent denied, timeout, prerequisite failure or empty identity blocking dependents, cancellation/late completion, repeated initialization, login/logout/account switches, and lifecycle after disposal. For ThinkingData/Adjust use `Docs/Foundation/SdkInitialization.md` and the integration checklist; distinguish local ordering tests, real device callbacks, and actual server postback ingestion. Never mark backend checks passed from client diagnostics alone.
- UI code: null references, duplicate subscriptions, reopening, navigation back stack.
- Prefab/scene change: read back exact hierarchy/component values and run target resolution screenshots.
- High-frequency path: inspect allocations, repeated lookups, subscriptions, collection capacity, and main-thread calls.

Do not hide unrelated project errors. Separate them from failures caused by the change.
