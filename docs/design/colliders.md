# Collider Basics

## Purpose
Provide pluggable environment collision shapes for cloth particles.

## Boundaries
- Operates on particle position and velocity individually.
- Static shapes only; no broadphase or self collision.
- No allocations per step.

## Public API
```csharp
public interface ICollider
{
    void Resolve(ref Vector3 position, ref Vector3 velocity);
}
```
Shapes:
- `PlaneCollider`: infinite plane.
- `SphereCollider`: solid sphere.
- `CapsuleCollider`: segment swept sphere.
- `SweptSphereCollider`: convenience wrapper for a moving sphere (capsule under the hood).

## Dependencies
- `System.Numerics.Vector3`

## Tests
- Unit tests for plane, sphere, and capsule projection.
- Existing cloth tests updated to use a plane collider instead of built-in ground check.

## Migration
The solver no longer clamps particles to `Y >= 0`. Supply a `PlaneCollider` to reproduce the previous ground plane.
