# Evaluation Metric Tests

## Purpose
Validate cloth solvers by checking average stretch and edge angle variance after a short simulation.

## Scope
- Run a small pinned grid for 30 steps.
- Thresholds may differ per integrator to cover explicit and semi-implicit behaviour.

## Testing Strategy
- Build and test against net9.0 and net8.0.
