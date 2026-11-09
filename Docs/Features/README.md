# Feature Workflow

Every behavior-changing requirement has one project-owned Feature Spec in this directory. The spec is the shared contract between product intent, AI implementation, code review, and verification.

## Lifecycle

1. Create a spec with `Game Foundation > Feature Workflow > New Feature Specification` or the batch entrypoint.
2. Resolve intent and set status to `Ready`.
3. Set status to `Implementing` while code or assets change.
4. Set status to `Verifying` while convention, compile, test, prefab, runtime, and device evidence is collected.
5. Set status to `Verified` only when acceptance criteria and Definition of Done are satisfied.

Allowed statuses are `Draft`, `Ready`, `Implementing`, `Verifying`, `Verified`, and `Blocked`.

Formatting-only edits and evidence-only diagnostics may update an existing spec instead of creating a new one. Any change to runtime behavior, editor behavior, serialized data, UI structure, SDK behavior, hot-update behavior, or player-visible assets requires a spec.

Run the local gate before completion:

```bash
python3 Scripts/validate_game_foundation_project.py --project .
```

CI also checks that a pull request changing governed code or Unity assets updates at least one Feature Spec.
