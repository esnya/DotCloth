using System.Numerics;

namespace DotCloth.Collisions;

/// <summary>Collision with a solid sphere.</summary>
public sealed class SphereCollider : ICollider
{
    private readonly Vector3 _center;
    private readonly float _radius;

    /// <summary>Creates a sphere centered at <paramref name="center"/> with the given <paramref name="radius"/>.</summary>
    public SphereCollider(Vector3 center, float radius)
    {
        _center = center;
        _radius = radius;
    }

    /// <inheritdoc />
    public void Resolve(ref Vector3 position, ref Vector3 velocity)
    {
        var diff = position - _center;
        var distSq = diff.LengthSquared();
        var rSq = _radius * _radius;
        if (distSq >= rSq)
        {
            return;
        }

        var dist = MathF.Sqrt(distSq);
        var normal = dist > 0f ? diff / dist : Vector3.UnitY;
        position = _center + normal * _radius;
        var vn = Vector3.Dot(velocity, normal);
        if (vn < 0f)
        {
            velocity -= vn * normal;
        }
    }
}
