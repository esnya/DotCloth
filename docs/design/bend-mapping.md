Bend Mapping Adjustment
=======================

Purpose
- Ensure bend stiffness mapping does not vanish when stretch stiffness is zero and avoid excessive curling.

Scope and Boundaries
- Applies only to `VelocityImpulseSolver`.
- No API surface changes.

Approach
- Map bend stiffness directly via `MapStiffnessToBeta` with a dedicated scale `BendBetaScale`.
- Soften bend impulses by increasing `CfmBend` and tightening `LambdaClampBend`.
- Bending error sign ensures folded edges push apart.

Test Strategy
- `dotnet test` on the solution. Existing failing tests remain unchanged.
