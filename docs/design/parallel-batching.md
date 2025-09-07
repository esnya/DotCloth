# Parallel constraint batching

## Purpose
Speed up `VelocityImpulseSolver` by executing independent constraint batches on multiple cores.

## Scope and boundary
- Applies only to velocity-level solver loops.
- `IClothSimulator.Step` now accepts `Vector3[]` positions/velocities so parallel loops operate in safe code.
- Internal behavior must remain deterministic.

## Placement
- `VelocityImpulseSolver.Step` runs `Parallel.For` over tethers and each stretch/bend batch with array indexing.
- Uses `System.Threading.Tasks`.

## Test strategy
- Existing unit tests.
- Performance harness `perf/DotCloth.Perf` before/after.

## Migration
Breaking: `Step(float, Span<Vector3>, Span<Vector3>)` → `Step(float, Vector3[], Vector3[])`.
Consumers passing spans must supply arrays.
