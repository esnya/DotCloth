using System.Numerics;

namespace DotCloth.Collisions;

/// <summary>Collision with a sphere moving linearly between two points.</summary>
public sealed class SweptSphereCollider : ICollider
{
    private readonly CapsuleCollider _capsule;

    /// <summary>Creates a swept sphere moving from <paramref name="start"/> to <paramref name="end"/> with <paramref name="radius"/>.</summary>
    public SweptSphereCollider(Vector3 start, Vector3 end, float radius)
    {
        _capsule = new CapsuleCollider(start, end, radius);
    }

    /// <inheritdoc />
    public void Resolve(ref Vector3 position, ref Vector3 velocity)
    {
        _capsule.Resolve(ref position, ref velocity);
    }
}
