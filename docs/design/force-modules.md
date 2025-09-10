# Force Modules

## Purpose
Provide swappable force-based cloth models that avoid patented XPBD/PBD/tether approaches. Each algorithm implements `IForce` and can be combined within the solver.

## Boundaries
- Operates on particle positions owned by the solver.
- No allocations per step.
- Does not mutate shared global state.

## Public APIs
```csharp
public interface IForce
{
    void Accumulate(Vector3[] positions, Vector3[] forces);
}
```
Algorithms:
- `EdgeSpringForce`: Hooke springs for structural edges.
- `DiscreteShellForce`: Bending force based on dihedral angle.
- `CoRotationalFemForce`: Linear FEM with co-rotation for triangle elements.
- `StrainLimiter`: Optional post-integrate step clamping edge stretch.

## Dependencies
- `System.Numerics.Vector3`
- Parallel loops (`System.Threading.Tasks`) for per-particle operations.

## Tests
- Rest shape preservation.
- Non divergence under perturbation.
- Convergence toward rest.
- No over-contraction with `StrainLimiter`.

