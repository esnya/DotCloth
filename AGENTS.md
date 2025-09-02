Project Agent Rules (DotCloth)
==============================

Scope and Source
- This file extends your user‑scope defaults (`~/.codex/AGENTS.md`). Project‑specific clarifications below prevail for this repo.

Core Principles (project‑specific focus)
- Language: Code identifiers, docstrings, and public docs are in English. Chat/PR text matches contributor language (JP/EN acceptable).
- Static safety: Nullable enabled everywhere. No unnecessary Optional/nullable values; absence must be meaningful and handled explicitly.
- Naming: Favor clear, domain‑accurate names aligned with UnityCloth and PBD/XPBD terminology (see `docs/glossary.md`).
- Small and cohesive: Keep modules focused; avoid speculative abstractions (YAGNI) and duplication (DRY).
- Design‑first: Add/adjust a short design note before non‑trivial implementation (see `docs/design/mini-design.md`).

.NET/C# Baseline
- Target frameworks: `net9.0`.
- C# language version: `latest`.
- Warnings as errors, analyzers at latest, nullable enabled.
- Thread safety: No shared global mutable state. Instance methods must be safe for concurrent use per documented contract.
- Math types: Prefer `System.Numerics.Vector3`/`Vector2`.

Testing and CI
- Test framework: xUnit.
- CI runs: format/lint/typecheck/test as required checks.

Commits
- Conventional Commits + gitmoji: `type(scope)?: gitmoji Message` (single line). Use PR body for design/migration details.

PR Checklist (enforced by review/CI)
- Single cohesive reason for change.
- Public API minimal; no internal leakage.
- YAGNI/DRY respected; dependency direction not worsened.
- Names convey intent and constraints.
- Tests cover intent without brittle coupling.
- Migration section for breaking changes: 1‑line purpose → impact → rollback.
- Include a short “Design Summary” or link to ADR/notes.
- Optional/nullable usage justified with handling semantics.

