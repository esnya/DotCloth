Mass-Spring Solver
==================

Purpose
-------
Provide a patent-neutral cloth simulation kernel based solely on force accumulation and semi-implicit Euler integration.

Boundaries
----------
- Calculates spring forces between particle pairs.
- Integrates velocities and positions with gravity and damping.
- Resolves a static floor plane at y = 0.

Public API
----------
- `MassSpringCloth` with constructor `(Vector3[] positions, float[] invMass, Spring[] springs, Vector3 gravity, float damping)`
- `void Step(float dt)` advances simulation by `dt` seconds.
- `Spring` record defines `A`, `B`, `RestLength`, `Stiffness`.

Dependencies
------------
- `System.Numerics` for vectors.
- `System.Threading.Tasks` for parallel loops.

Tests
-----
- Preserves rest length.
- Does not diverge under perturbation.
- Converges to rest after motion.
- Preserves shape after floor collision.

Patent detour
-------------
This solver uses classical mass–spring forces and explicit integration; no constraint projection or tether schemes associated with PBD/XPBD are employed.
