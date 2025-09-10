# Sample Floor Collider

## Purpose
Demonstrate environment collisions in sample projects by adding a ground plane.

## Boundaries
- Only a static plane at Y=0 is provided.
- Uses shared `ClothFactory` to keep scenarios aligned across samples.

## Public API
No new public types. `ClothFactory` now supplies a `PlaneCollider` when constructing `ForceCloth`.

## Dependencies
- `DotCloth.Collisions` for `PlaneCollider`.
- `System.Numerics.Vector3`.

## Tests
- Covered indirectly by existing collider unit tests; samples rely on manual verification.

## Migration
Sample applications now collide with the ground without additional user code.
