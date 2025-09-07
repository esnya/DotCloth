# Edge struct-of-arrays for solver hot loops

## Purpose
Improve cache locality and enable SIMD-friendly operations in `VelocityImpulseSolver` by storing edge constraint data in struct-of-arrays (SoA) form.

## Scope and boundary
- Applies to stretch constraint storage.
- Bend constraints remain in array-of-structs.
- Public API unchanged.

## Placement
- `VelocityImpulseSolver` holds separate arrays for edge endpoints, rest length and mass terms.
- Constraint iteration loops read from these arrays.

## Test strategy
- Existing unit tests.
- `perf/DotCloth.Perf` single and multi-instance runs before/after.

## Migration
None. Internal refactor only.
