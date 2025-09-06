PBD/XPBD Outline (experimental XPBD)
===================================

References (conceptual)
- Position Based Dynamics (PBD), Müller et al.
- Extended PBD (XPBD), Macklin et al. (compliance parameterization, better stabilities).

Constraints (initial set)
- Stretching: edge length preservation. Default solver uses sequential impulses; XPBD variant uses compliance.
- Bending: dihedral/distance models. Default solver uses impulses; XPBD variant uses compliance.
- Tether: distance anchors to reduce drift (both variants supported).
- Damping/drag: velocity damping, air drag.

Solver Loop (sketch)
1) External forces → integrate velocities.
2) Predict positions.
3) Sequential constraint solve (Gauss–Seidel). Default solver applies velocity impulses; XPBD uses lambdas/compliance.
4) Velocity update from position delta / dt and post‑stabilization (small position corrections).
5) Optional collision response hooks.

Parallelization
- Partition constraints (graph coloring or edge/face batching) to minimize conflicts.
- Use per‑constraint lambdas; atomic‑free when batched, atomic otherwise.
- Determinism: fixed ordering inside batches; avoid data races.

Data Layout
- SoA arrays for positions, velocities, inverse masses; constraint lists for edges/faces.

Tuning and Stability
- Use compliance rather than raw stiffness where applicable.
- Clamp extreme parameters; ensure time‑step stability.
