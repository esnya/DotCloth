# Parallel constraint batching

## Purpose
Speed up `VelocityImpulseSolver` by executing independent constraint batches on multiple cores.

## Scope and boundary
- Applies only to velocity-level solver loops.
- No public API changes; internal behavior must remain deterministic.

## Placement
- `VelocityImpulseSolver.Step` adds `Parallel.For` over tethers and each stretch/bend batch.
- Uses `System.Threading.Tasks`.

## Test strategy
- Existing unit tests.
- Performance harness `perf/DotCloth.Perf` before/after.

## Migration
None. No public surface changes.
