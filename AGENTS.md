Project Agent Rules (DotCloth)
==============================

Scope and Source
- This file extends your user‑scope defaults (`~/.codex/AGENTS.md`). Project‑specific clarifications below prevail for this repo.

Core Principles (project‑specific focus)
- Language: Code identifiers, docstrings, and public docs are in English. Chat/PR text matches contributor language (JP/EN acceptable).
- Static safety: Nullable enabled everywhere. No unnecessary Optional/nullable values; absence must be meaningful and handled explicitly.
- Naming: Favor clear, domain‑accurate names aligned with UnityCloth terminology (see `docs/glossary.md`).
- Small and cohesive: Keep modules focused; avoid speculative abstractions (YAGNI) and duplication (DRY).
- Design‑first: Add/adjust a short design note before non‑trivial implementation (see `docs/design/mini-design.md`).
- Force modules: Each algorithm (springs, shells, FEM, strain limiting, etc.) lives in its own class implementing a dedicated interface so combinations remain swappable.
- Collisions: Environment shapes implement `ICollider` with `Resolve(ref Vector3 position, ref Vector3 velocity)` and remain allocation-free per step.

.NET/C# Baseline
- Target frameworks: `net9.0`.
- C# language version: `latest`.
- Warnings as errors, analyzers at latest, nullable enabled.
- Thread safety: No shared global mutable state. Instance methods must be safe for concurrent use per documented contract.
- Math types: Prefer `System.Numerics.Vector3`/`Vector2`.

Testing and CI
- Test framework: xUnit.
- CI runs: format/lint/typecheck/test as required checks.
- Task completion commands:
  - `dotnet format --verify-no-changes`
  - `dotnet build -f net9.0`
  - `dotnet test -f net9.0`
  - `dotnet build -f net8.0`
  - `dotnet test -f net8.0`

Samples
- Sample projects maintain scenario parity and include a ground `PlaneCollider` so cloth particles collide with a Y=0 floor.

Performance Optimization Playbook
- Measure-first: Add/adjust a perf harness, run representative single/multi-instance cases, and record results before/after.
- Keep it simple: Prefer low-risk, low-diff optimizations with clear wins; revert quickly if gains are within noise or regressions appear.
- Determinism and safety: Optimizations must preserve determinism for fixed inputs and not weaken safety checks.
  - Adopted patterns (current code):
    - Avoid per-step allocations (reuse internal buffers like previous positions); build topology once at Initialize.
- Deferred/rolled back patterns (not retained):
  - SoA hot loop for positions (Vector3→x/y/z arrays) increased overhead in multi-instance runs; rolled back to keep simplicity and performance.
- Experiment flags: If parallelism or heavier changes are explored, hide behind an opt-in flag with clear defaults and remove if gains are not material.
- Result reporting: Note the .NET runtime version and include frame time (ms) and frame rate (FPS) alongside total run time; document CPU/OS context outside tables.
- Benchmark matrix: Maintain multiple cloth sizes and instance counts; compress `total / frame / FPS` metrics into one cell and dedicate a column to each force model.
- Benchmark search: Perf harnesses accept `--maxSize` and `--maxInstances` limits and increase complexity until a case drops below 60 FPS or the limits are hit. Record observed CPU core/thread limits alongside OS notes.

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
