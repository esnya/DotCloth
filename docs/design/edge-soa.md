# Edge constraint SoA layout

## Purpose
Reduce cache misses and prepare for SIMD in `VelocityImpulseSolver` by storing stretch edge data in separate arrays instead of an `Edge` struct.

## Scope and boundary
- Applies only to stretch edge constraints.
- Bend and tether data structures are unchanged.
- Public API remains stable.

## Placement
- `VelocityImpulseSolver` now keeps `_edgeI`, `_edgeJ`, `_edgeRestLength`, `_edgeWi`, `_edgeWj`, `_edgeWSum` arrays.
- Solver loops read from these arrays, improving linear memory access.

## Test strategy
- Rely on existing unit tests and performance harness.

## Migration
- Internal change only; no consumer action required.
