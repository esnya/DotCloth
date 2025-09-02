PBD/XPBD Outline
================

References (conceptual)
- Position Based Dynamics (PBD), Müller et al.
- Extended PBD (XPBD), Macklin et al. (compliance parameterization, better stabilities).

Constraints (initial set)
- Stretching: edge length preservation with XPBD compliance.
- Bending: dihedral angle constraints with XPBD compliance.
- Tether: distance anchors to reduce drift.
- Damping/drag: velocity damping, air drag.

Solver Loop (sketch)
1) External forces → integrate velocities.
2) Predict positions.
3) Sequential constraint solve (Gauss–Seidel) with XPBD lambdas per constraint.
4) Velocity update from position delta / dt.
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

