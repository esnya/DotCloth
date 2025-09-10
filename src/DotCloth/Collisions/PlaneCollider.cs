using System.Numerics;

namespace DotCloth.Collisions;

/// <summary>Collision with an infinite plane.</summary>
public sealed class PlaneCollider : ICollider
{
    private readonly Vector3 _point;
    private readonly Vector3 _normal;

    /// <summary>Creates a plane passing through <paramref name="point"/> with outward <paramref name="normal"/>.</summary>
    public PlaneCollider(Vector3 point, Vector3 normal)
    {
        _point = point;
        _normal = Vector3.Normalize(normal);
    }

    /// <inheritdoc />
    public void Resolve(ref Vector3 position, ref Vector3 velocity)
    {
        var distance = Vector3.Dot(position - _point, _normal);
        if (distance >= 0f)
        {
            return;
        }

        position -= distance * _normal;
        var vn = Vector3.Dot(velocity, _normal);
        if (vn < 0f)
        {
            velocity -= vn * _normal;
        }
    }
}
