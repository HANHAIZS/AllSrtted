# Feature Spec Schema

Use `Docs/Features/FEATURE_TEMPLATE.md` as the canonical structure.

## Required frontmatter

- `id`: stable lowercase kebab-case identifier.
- `title`: user-facing feature or change name.
- `status`: `Draft`, `Ready`, `Implementing`, `Verifying`, `Verified`, or `Blocked`.
- `owner`: normally `Project`; use `Foundation` only when shared package evolution is explicitly in scope.
- `created` and `updated`: ISO dates.

## Required decisions

- Goal and observable user behavior.
- Explicit out-of-scope items.
- Ownership classification and QFramework roles.
- State mutation path and persistent-data impact.
- UI, prefab, scene, and Inspector impact.
- Relevant failure, offline, cancellation, and lifecycle behavior.
- For SDK initialization/attribution/identity work: actual versions, dependency order and readiness gates, identity sources/synchronization owner, plus separate local/device/server acceptance evidence and an integration checklist link.
- Acceptance criteria and verification evidence.
- Definition of Done with unchecked work left visible.

Do not invent class names or file paths when the project has not been inspected. Use `TBD` for a genuine unresolved choice and keep status `Draft` or `Blocked` until it is resolved.
