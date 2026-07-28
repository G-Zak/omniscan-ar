# Contributing / Working Process

This is a solo capstone project, run with a real team-style process on purpose — see [Project Charter §8](documentation/01-planning/00-project-charter.md#8-governance--working-process) for the reasoning.

## Branching

- No direct commits to `main`.
- One branch per issue: `feature/sX-YY-short-description` (e.g., `feature/s3-03-subgraph-endpoint`).
- Open a PR against `main` even solo — write a short self-review comment before merging. This is deliberate discipline, not overhead.
- Merge with a merge commit (not squash/rebase) so the PR history stays visible in `git log`.

## Commit messages

Explain *why*, not just *what* — the diff already shows what changed.

## Definition of Done

An issue is done when:
1. Code is merged to `main` via a reviewed PR.
2. It builds/runs via the documented setup (`docker compose up`, or the relevant client build).
3. Every acceptance criterion in the issue is satisfied.
4. Any doc whose behavior changed (architecture, API contract, data model) is updated in the same PR.

## Documentation discipline

Architecture and data-model docs are updated *before* a sprint that changes them starts. See [documentation/](documentation/) for the full plan.

## Attribution

This repository's commits and history belong to the project owner. AI coding assistants may be used during development, but commits are not co-authored by, and no collaborator access is granted to, any AI agent or tool account.
