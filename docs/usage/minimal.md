Minimal Usage
=============

Initialize
- Build positions/triangles, parameters (e.g., stiffness/iterations), and velocities (zero to start).
- Create `VelocityImpulseSolver`, call `Initialize(positions, triangles, parameters)`.

Optional setup
- Pin: `PinVertices(indices)` or `SetInverseMasses`.
- Tethers: `SetTetherAnchors(anchorIndices)` and adjust `TetherStiffness`/`TetherLengthScale`.
- Collisions: `SetColliders(new [] { new PlaneCollider(n, offset) /* ... */ })`.

Step
- Each frame/tick: `Step(deltaTime, positions, velocities)`.
- Positions/velocities are updated in-place.

Notes
- Determinism: Fixed inputs/time step/seed → reproducible.
- Safety: Triangle indices must be valid; parameters are clamped internally.
- Performance: Batching reduces data hazards; avoid unnecessary substeps/iterations.
