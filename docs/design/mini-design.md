DotCloth Mini Design (initial)
==============================

Purpose
- Provide a UnityCloth‑compatible, high‑performance velocity‑level cloth solver for .NET 9. An XPBD solver is kept only for experiments behind a compile flag.

Scope and Boundaries
- In: cloth simulation core (XPBD), parameter mapping to UnityCloth, collision hooks, deterministic update path, multithread‑friendly API.
- Out (initial): GPU acceleration, soft‑body volume constraints, cloth‑rigid broadphase, authoring tooling.

Public API / Data Model
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
- Legacy XPBD solver available via `DOTCLOTH_EXPERIMENTAL_XPBD`. Default `PbdSolver` now wraps `VelocityImpulseSolver` to maintain samples.

