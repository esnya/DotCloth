# Simulation CLI

Purpose: run Minimal-like cloth simulations from the command line and emit numeric metrics for internal analysis.

## Scope
- Grid-only cloth with top row pinned.
- Configurable size, solver parameters, steps, and time step.
- Outputs average stretch ratio and edge angle variance to CSV.

## Dependencies
- Uses existing `PbdSolver` from `DotCloth`.
- No external libraries; minimal argument parsing.

## Testing
- Covered by existing unit tests for core solver.
- Manual invocation from developers.
