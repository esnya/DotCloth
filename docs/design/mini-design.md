DotCloth Mini Design (initial)
==============================

Purpose
- Provide a UnityCloth‑compatible, high‑performance cloth solver for .NET 9. The default path is a velocity‑level sequential‑impulses solver. An XPBD variant is experimental and available only behind a compile flag for research/verification.

Scope and Boundaries
- In: cloth simulation core (velocity‑level default), optional XPBD variant (experimental), parameter mapping to UnityCloth, collision hooks, deterministic update path, multithread‑friendly API.
- Out (initial): GPU acceleration, soft‑body volume constraints, cloth‑rigid broadphase, authoring tooling.

Public API / Data Model (initial sketch)
- `ClothParameters`: UnityCloth‑like fields (damping, stretch/bend stiffness, tether, friction, thickness, gravity scale, external accel, etc.).
- `IClothSimulator`: `Initialize`, `Step(deltaTime, positions, velocities)`, `UpdateParameters`.
- Data layout: `System.Numerics.Vector3` for positions/velocities; triangle indices as `int` array.

Placement and Responsibilities
- `src/DotCloth/Simulation`: parameters, interfaces, solver core.
- `tests/DotCloth.Tests`: unit tests and property checks for stability/invariants.

Dependencies
- Runtime: .NET 9 BCL only. No external runtime deps.
- Test: xUnit.

Test Strategy
- Unit tests for parameter validation, deterministic stepping, basic energy behavior (damping reduces velocity magnitude).
- Property tests where feasible (e.g., constraints reduce violation over iterations).

Migration / Compatibility
- If diverging from UnityCloth semantics, document mapping and migration notes in `docs/api/overview.md`.
