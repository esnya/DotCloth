API Overview (initial)
======================

Parameter Model
- `ClothParameters` mirrors UnityCloth concepts: damping, stretch/bend/tether stiffness (or compliance), friction, thickness, gravity toggle/scale, external/random acceleration, air drag.
  - `RandomAcceleration` with deterministic `RandomSeed` influences integration; default 0 disabled.

Core Interfaces
- `IClothSimulator`
  - `void Initialize(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, ClothParameters parameters)`
  - `void Step(float deltaTime, Span<Vector3> positions, Span<Vector3> velocities)`
  - `void UpdateParameters(ClothParameters parameters)`
  - `void SetInverseMasses(ReadOnlySpan<float> inverseMasses)` — 0 fixes a vertex (pinning)
  - `void PinVertices(ReadOnlySpan<int> indices)` — convenience pin API
  - `void ResetRestState(ReadOnlySpan<Vector3> positions)` — recompute rest values
  - `void SetColliders(IEnumerable<Collision.ICollider> colliders)`
  - `void SetTetherAnchors(ReadOnlySpan<int> anchors)` — define anchors for tethers

Solver Settings
- `ClothParameters.Iterations` — XPBD iterations per substep (default 8).
- `ClothParameters.Substeps` — substeps per `Step` (default 1).
- `ClothParameters.ComplianceScale` — maps [0..1] stiffness → XPBD compliance alpha.

Collision Hooks
- Implement `ICollider.Resolve(...)` and pass to `PbdSolver.SetColliders(...)`.
- Included: `PlaneCollider` (infinite plane) for basic testing.
- Included: `SphereCollider` (center + radius)
 - Included: `CapsuleCollider` (segment + radius)

Constraints
- Stretch: unique edges from triangles, XPBD with per-edge lambdas.
- Bending: distance across opposite vertices of adjacent triangles (XPBD). Future: dihedral angle.
- Tether-to-rest: pulls vertices toward rest position using XPBD; not identical to UnityCloth’s “tethers” but offers stabilizing behavior. Mapping notes pending.
- Tether-to-anchor: nearest anchor per vertex with rest length = initial distance × `TetherLengthScale`.

Batching (internal)
- Greedy batching groups constraints that do not share vertices; solver processes batches sequentially for determinism.
- This prepares for future parallelization while keeping the public API unchanged.

Threading Contract
- Each simulator instance is independent. Methods are safe to call from multiple threads on different instances. Concurrent calls on the same instance require the caller to synchronize unless the implementation documents otherwise.

Determinism
- For a fixed time step, parameters, and topology, sequences of steps should be deterministic on a given architecture.
 - Randomness uses `RandomSeed`; when non-zero and all other inputs fixed, results remain deterministic.

UnityCloth Mapping (WIP)
- Damping ↔ `Damping`
- Stretching Stiffness ↔ `StretchStiffness` (XPBD compliance derived internally)
- Bending Stiffness ↔ `BendStiffness` (XPBD compliance derived internally)
- Tether Stiffness ↔ `TetherStiffness`
- Use Gravity ↔ `UseGravity`; Gravity Scale ↔ `GravityScale`
- External/Random Accel ↔ `ExternalAcceleration`/`RandomAcceleration`
- Friction/Thickness ↔ `Friction`/`CollisionThickness`

Migration
- 0.x breaking: `IClothSimulator` extended with `SetInverseMasses`, `ResetRestState`, and `SetColliders` for clarity and integration needs. Update implementations accordingly.
 - 0.x extension: `PinVertices` convenience added; existing pinning via `SetInverseMasses` remains valid.
 - 0.x extension: `SetTetherAnchors` and `ClothParameters.TetherLengthScale` added for Unity-like tether behavior.
