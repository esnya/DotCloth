DotCloth Usage Guide
====================

Setup
- Target `.NET 9.0` and reference the `DotCloth` project/package.
- Prepare vertex `positions` (`Vector3[]`), `triangles` (`int[]`) with 3 indices per triangle, and a `velocities` array (usually zeros initially).

Initialize
- Create `ClothParameters` aligned with your use case.
- Create `VelocityImpulseSolver` and call:
  - `Initialize(positions, triangles, parameters)`

Pinning
- Pin a subset of vertices to anchor the cloth:
  - `PinVertices(ReadOnlySpan<int>)` or `PinVertices(params int[])`.
  - For partial stiffness, prefer using tethers instead of non‑zero inverse masses.

Tethers
- Rest tethers: without anchors set, tethers pull each vertex toward its rest position (`ResetRestState` updates rest).
- Anchor tethers: define anchor indices (`SetTetherAnchors`); each vertex is tethered to its nearest anchor with rest length = initial distance × `TetherLengthScale`.
- Control strength with `TetherStiffness`.

Collisions
- Provide colliders via `SetColliders(IEnumerable<ICollider>)`.
- Built‑ins: `PlaneCollider`, `SphereCollider`, `CapsuleCollider`.
- The collider API receives previous positions for simple swept push‑out to reduce tunneling.

Stepping
- Each frame: `Step(deltaTime, positions, velocities)`. Arrays are updated in place.
- Deterministic for fixed inputs, time step, and random seed.

Determinism and Threading
- Each solver instance is independent. Calls on different instances may run concurrently.
- Calls on the same instance must be synchronized by the caller.
- Determinism holds for fixed inputs/parameters/seed and batch order.

Tuning (quality vs. cost)
- Iterations: primary quality knob. Start with 8–10. Higher values reduce constraint violation but cost scales roughly linearly.
- Substeps: prefer not to increase unless necessary for fast motion/collisions.
- ComplianceScale: global stiffness mapping factor; reduce only if high stiffness still stretches.

Parameter Tips
- Typical ranges: see `docs/usage/recommendations.md`.
- Start conservative (lower stiffness, moderate damping, minimal drag) and adjust iteratively.

Multi‑Instance Patterns
- Create one solver per cloth. Reuse arrays to minimize allocations.
- For many avatars (e.g., 40), prefer shorter iterations per cloth and avoid substeps when possible.

Updating Parameters
- Use `UpdateParameters` to reapply clamps and refresh derived values.
- Use `ResetRestState` after topology/pose changes that redefine “rest”.

Error Handling
- Triangle indices must be within [0, vertexCount). Invalid indices throw.
- Nulls are not allowed; parameters and spans must be non‑null.

Common Pitfalls
- Excessive substeps degrade performance; try higher iterations first.
- Using friction/thickness values that are too high can cause jitter; increase gradually.

