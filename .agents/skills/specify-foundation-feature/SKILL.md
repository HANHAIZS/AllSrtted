---
name: specify-foundation-feature
description: Standardize a new or changed Unity game requirement into a project-owned Feature Spec before implementation. Use for behavior changes, screens, flows, gameplay, save data, SDK integrations, hot-update behavior, or UI/prefab work in projects that consume Game Foundation; do not use for formatting-only edits or shared package maintenance.
---

# Specify Foundation Feature

Turn a product request into an implementable, verifiable project work item without moving business policy into Foundation.

## Required context

Read repository `AGENTS.md`, `.gamefoundation/project.json`, `Docs/Foundation/Architecture.md`, and `Docs/Features/FEATURE_TEMPLATE.md`. Read `references/feature-spec-schema.md` when creating or repairing a Feature Spec.

## Workflow

1. Resolve the user-visible goal, acceptance criteria, exclusions, affected platforms, and unresolved choices. Ask only when a missing choice materially changes behavior; otherwise record a reasonable assumption.
2. Classify ownership before naming classes:
   - Cross-project infrastructure may consume a Foundation contract.
   - Gameplay, progression, economy, rank, store, IAP, concrete navigation, product analytics events, and vendor policy stay in the project.
3. Map project work to QFramework roles: Command for mutation, Model for observable/persistent state, System for reusable rules, Event for one-shot notification, ViewController for Unity presentation, and Utility for stateless adapters or repositories.
4. Record save/versioning, SDK consent and failure behavior, hot-update offline/rollback behavior, UI/prefab/Inspector impact, and performance risks only when relevant.
5. Define the smallest verification matrix that proves the acceptance criteria, including failure and recovery paths where the capability has them.
6. Create or update `Docs/Features/<feature-id>.md`. Use a stable lowercase kebab-case ID and keep the status current: `Draft`, `Ready`, `Implementing`, `Verifying`, `Verified`, or `Blocked`.
7. When implementation is requested in the same task, continue with `$develop-foundation-feature` using the approved spec as the working contract.

## Boundary

For SDK initialization, attribution or identity requirements, read `Docs/Foundation/SdkInitialization.md`. Record the actual vendor versions, initialization dependencies, identity sources and synchronization owner, consent/failure behavior, and separate local/device/server acceptance evidence in the Feature Spec and `Docs/Integrations/SdkInitializationChecklist.md`. Do not impose ThinkingData/Adjust on projects that do not use them.

For UI/prefab/art requirements, read `Docs/Foundation/ResourceLoading.md` and record whether each asset is path-loaded through Resources or directly referenced. Specify its typed root (Prefabs / Art / Sprite-only Sprites, or Resources), final path, load key/reference owner, cache and instance-cleanup responsibility. For popups, include first-use loading, reuse, missing-resource recovery and startup residency checks. For model removal, include runtime and Editor dependency evidence. Do not select YooAsset by default merely because it is installed.

A Feature Spec is not permission to broaden the request. Record deferred ideas under out-of-scope instead of implementing them. Do not mark a feature `Verified` until the listed evidence actually exists.

For UI work, read `Docs/Foundation/UiLayers.md`, `Docs/Foundation/UiLayers.Project.md` and `Docs/Foundation/UiAuthoring.md`. Record the semantic host, Replace/Stack choice, input-gate owner, instance lifetime, failure recovery and which authored properties the request may change. Select layers by interaction responsibility; gameplay pause and product presentation policy stay project-owned.

## Completion report

Link the Feature Spec, summarize ownership and acceptance criteria, state unresolved assumptions, and identify the next implementation or verification action.
