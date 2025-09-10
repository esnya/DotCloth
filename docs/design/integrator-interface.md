# Integrator Interface

## Purpose
Allow switching time integration strategies without touching the force model, enabling explicit and semi-implicit Euler paths while avoiding XPBD-style projections.

## Scope
- Introduce `IIntegrator` with `Integrate(ref Vector3 position, ref Vector3 velocity, Vector3 acceleration, float damping, float dt)`.
- Provide `SemiImplicitEulerIntegrator` (default) and `ExplicitEulerIntegrator`.
- `MassSpringCloth` accepts an `IIntegrator` in its constructor.
- Console sample toggles integrators via command-line argument.

## Testing
- `dotnet format --verify-no-changes`
- `dotnet build -f net9.0`
- `dotnet test -f net9.0`
- `dotnet build -f net8.0`
- `dotnet test -f net8.0`
