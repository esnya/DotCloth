using System.Numerics;

namespace DotCloth.Simulation.Collision;

/// <summary>
/// Infinite plane collider: dot(n, x) &gt;= offset (outside half-space).
/// </summary>
public sealed class PlaneCollider : ICollider
{
    private readonly Vector3 _normal;
    private readonly float _offset;

    /// <summary>Creates a plane with normal (normalized internally) and offset.</summary>
    public PlaneCollider(Vector3 normal, float offset)
    {
        var n = normal;
        var len = n.Length();
        _normal = len > 0 ? n / len : new Vector3(0, 1, 0);
        _offset = offset;
    }

    /// <summary>Unit normal of the plane (read-only).</summary>
    public Vector3 Normal => _normal;

    /// <summary>Offset of the plane: points satisfy dot(n, x) = offset (read-only).</summary>
    public float Offset => _offset;

    /// <inheritdoc />
    public void Resolve(ReadOnlySpan<Vector3> prevPositions, Span<Vector3> positions, Span<Vector3> velocities, float deltaTime, float thickness, float friction)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            var x = positions[i];
            float d = Vector3.Dot(_normal, x) - _offset;
            float minDist = thickness - d;
            if (minDist > 0f)
            {
                // Swept push-out using previous position if outside
                var xp = prevPositions[i];
                float dp = Vector3.Dot(_normal, xp) - _offset;
                float t = 1f;
                if (dp >= thickness)
                {
                    // Solve xp + t*(x-xp) lies on plane at distance thickness
                    var vx = x - xp;
                    float denom = Vector3.Dot(_normal, vx);
                    if (MathF.Abs(denom) > 1e-9f)
                    {
                        t = (thickness - dp) / denom;
                        t = Math.Clamp(t, 0f, 1f);
                    }
                }
                var xi = Vector3.Lerp(xp, x, t);
                positions[i] = xi + _normal * (thickness - (Vector3.Dot(_normal, xi) - _offset));
                // Simple friction: remove velocity component along tangent proportionally
                var v = velocities[i];
                var vn = Vector3.Dot(v, _normal) * _normal;
                var vt = v - vn;
                vt *= MathF.Max(0f, 1f - friction);
                velocities[i] = vt; // kill normal velocity for stability
            }
        }
    }
}
