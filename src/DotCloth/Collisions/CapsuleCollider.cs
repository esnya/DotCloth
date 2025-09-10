using System.Numerics;

namespace DotCloth.Collisions;

/// <summary>Collision with a capsule formed by sweeping a sphere along a segment.</summary>
public sealed class CapsuleCollider : ICollider
{
    private readonly Vector3 _a;
    private readonly Vector3 _b;
    private readonly float _radius;
    private readonly Vector3 _ab;
    private readonly float _abLenSq;

    /// <summary>Creates a capsule spanning from <paramref name="a"/> to <paramref name="b"/> with <paramref name="radius"/>.</summary>
    public CapsuleCollider(Vector3 a, Vector3 b, float radius)
    {
        _a = a;
        _b = b;
        _radius = radius;
        _ab = b - a;
        _abLenSq = _ab.LengthSquared();
    }

    /// <inheritdoc />
    public void Resolve(ref Vector3 position, ref Vector3 velocity)
    {
        var ap = position - _a;
        float t = 0f;
        if (_abLenSq > 0f)
        {
            t = Vector3.Dot(ap, _ab) / _abLenSq;
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
        }

        var closest = _a + t * _ab;
        var diff = position - closest;
        var distSq = diff.LengthSquared();
        var rSq = _radius * _radius;
        if (distSq >= rSq)
        {
            return;
        }

        var dist = MathF.Sqrt(distSq);
        Vector3 normal;
        if (dist > 0f)
        {
            normal = diff / dist;
        }
        else if (_abLenSq > 0f)
        {
            normal = Vector3.Normalize(_ab);
        }
        else
        {
            normal = Vector3.UnitY;
        }

        position = closest + normal * _radius;
        var vn = Vector3.Dot(velocity, normal);
        if (vn < 0f)
        {
            velocity -= vn * normal;
        }
    }
}
