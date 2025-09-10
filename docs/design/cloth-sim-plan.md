Cloth Simulation Redesign Plan
================================

Purpose
-------
Restart the cloth module from zero with a focus on patent-neutral techniques. XPBD, PBD and tether-based constraint projection are excluded.

Prior Art Survey (non-patented)
-------------------------------
- Baraff & Witkin 1998: large-step implicit integration for cloth.
- Bridson 2002: robust collision and contact handling.
- Bouaziz et al. 2014: projective dynamics as an efficient solver.
- Li et al. 2020: ADMM-based simulation for nonlinear materials.
- Grinspun et al. 2003: discrete shells for bending.
- Etzmuß et al. 2003: co-rotational finite elements.
- Provot 1995: strain limiting to prevent overstretch.
No active patents were found for these methods in USPTO/EPO searches as of 2024-09.

Architecture Outline
--------------------
- **Integrator**: SemiImplicitEuler, ImplicitEuler (swappable via `IIntegrator`).
- **Force Model**: MassSpring, DiscreteShell, CoRotationalFem (implement `IForceModel`).
- **Constraint Solver**: GaussSeidel, ProjectiveDynamics, ADMM, StrainLimiting (implement `IConstraintSolver`).
- **Collision**: Modular `ICollider` implementations (BRep, IPC).
- **Simulator**: `IClothSimulator` composes the above modules; no global mutable state. Parallel loops may be introduced where safe.

Testing Strategy
----------------
- Shape preservation within tolerance.
- Non-divergence under extreme stiffness.
- Convergence to rest after forces cease.
- No over-contraction beyond rest lengths.

Next Steps
----------
1. Finalize module interfaces and baseline parameter types.
2. Implement a minimal mass-spring force model with SemiImplicitEuler.
3. Add unit tests exercising the above criteria.
4. Prototype DiscreteShell and CoRotationalFEM force models behind `IForceModel`.
5. Investigate strain limiting and ADMM solvers as optional `IConstraintSolver` implementations.
6. Parallelize per-instance stepping and measure scaling with the perf harness.
