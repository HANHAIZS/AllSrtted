---
id: ci-events-directory
title: Preserve the Events directory in Git
status: Verifying
owner: Project
created: 2026-11-10
updated: 2026-11-10
---

# Preserve the Events directory in Git

## Goal

Keep the required Events role directory available in a fresh Git checkout.

## User-visible behavior

GitHub's Game Foundation conventions check can find the Events directory even before the project adds an event source file.

## Out of scope

Unity behavior, scenes, and runtime event definitions.

## Ownership and architecture

The project tracks an empty `Assets/Scripts/Game/Events/.gitkeep` file. The existing Game Foundation validation policy applies.

## Acceptance criteria

- A fresh checkout includes the Events directory.
- The project validator passes both full-project and push-diff checks.

## Verification matrix

| Check | Command |
| --- | --- |
| Fresh checkout | Run the project validator against the committed snapshot |
| Push diff | Run the project validator with `--base` |

## Definition of Done

The conventions check succeeds on GitHub for the repair commit.
