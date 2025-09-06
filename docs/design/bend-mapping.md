Bend Mapping Adjustment
=======================

Purpose
- Ensure bend stiffness mapping does not vanish when stretch stiffness is zero.

Scope and Boundaries
- Applies only to `VelocityImpulseSolver`.
- No API surface changes.

Approach
- Map bend stiffness directly via `MapStiffnessToBeta` without cross-coupling with stretch.

Test Strategy
- `dotnet test` on the solution. Existing failing tests remain unchanged.
