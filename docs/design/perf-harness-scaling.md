# Perf harness scaling

## Purpose
Benchmark multiple cloth sizes and instance counts to characterize force-model scaling.

## Scope and boundaries
- In: `perf/DotCloth.Perf` benchmark enumerating edge-spring, shell, FEM, and strain-limited force models.
- Out: GPU execution, collision benchmarking.

## Public API
The harness accepts optional `--maxSize` and `--maxInstances` arguments. It enumerates force models while increasing cloth size and instance count until a case falls below 60 FPS or the limits are reached. Metrics are recorded as `total / frame / FPS` within a single cell per model. The .NET runtime and observed CPU core/thread counts are printed once.

## Test strategy
- Run `dotnet run -c Release -f net9.0 --project perf/DotCloth.Perf -- --maxSize <N> --maxInstances <M>`.
- Capture output and populate `docs/perf/perf-results.md` with runtime version, CPU core/thread counts, and combined `total / frame / FPS` metrics.

## Migration
None.
