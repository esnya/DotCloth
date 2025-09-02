API Overview (initial)
======================

Parameter Model
- `ClothParameters` mirrors UnityCloth concepts: damping, stretch/bend/tether stiffness (or compliance), friction, thickness, gravity toggle/scale, external/random acceleration, air drag.

Core Interfaces
- `IClothSimulator`
  - `void Initialize(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, ClothParameters parameters)`
  - `void Step(float deltaTime, Span<Vector3> positions, Span<Vector3> velocities)`
  - `void UpdateParameters(ClothParameters parameters)`

Solver Settings
- `ClothParameters.Iterations` — XPBD iterations per substep (default 8).
- `ClothParameters.Substeps` — substeps per `Step` (default 1).
- `ClothParameters.ComplianceScale` — maps [0..1] stiffness → XPBD compliance alpha.

Collision Hooks
- Implement `ICollider.Resolve(...)` and pass to `PbdSolver.SetColliders(...)`.
- Included: `PlaneCollider` (infinite plane) for basic testing.

Threading Contract
- Each simulator instance is independent. Methods are safe to call from multiple threads on different instances. Concurrent calls on the same instance require the caller to synchronize unless the implementation documents otherwise.

Determinism
- For a fixed time step, parameters, and topology, sequences of steps should be deterministic on a given architecture.

UnityCloth Mapping (WIP)
- Damping ↔ `Damping`
- Stretching Stiffness ↔ `StretchStiffness` (XPBD compliance derived internally)
- Bending Stiffness ↔ `BendStiffness` (XPBD compliance derived internally)
- Tether Stiffness ↔ `TetherStiffness`
- Use Gravity ↔ `UseGravity`; Gravity Scale ↔ `GravityScale`
- External/Random Accel ↔ `ExternalAcceleration`/`RandomAcceleration`
- Friction/Thickness ↔ `Friction`/`CollisionThickness`
