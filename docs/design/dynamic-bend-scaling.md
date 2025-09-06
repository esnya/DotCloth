Dynamic Bend Scaling
====================

Purpose
- Reduce angle variance by scaling bend stiffness and softness with mesh resolution.

Scope and Boundaries
- Applies to `VelocityImpulseSolver`.
- No public API changes.

Approach
- Compute average edge length at initialization.
- Scale `BendBetaScale` inversely and `CfmBend` directly with the average edge length.

Test Strategy
- `dotnet test`.
