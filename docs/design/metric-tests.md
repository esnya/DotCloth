# Evaluation Metric Tests

## Purpose
Validate cloth solvers by checking average stretch and edge angle variance after a short simulation.

## Scope
- Run a small pinned grid for 30 steps.
- Thresholds differ by compile-time flag to cover experimental and standard solvers.

## Testing Strategy
- Build and test against net9.0 and net8.0.
- Repeat with and without `DOTCLOTH_EXPERIMENTAL_XPBD`.
