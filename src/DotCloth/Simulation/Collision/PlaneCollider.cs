using System.Numerics;

namespace DotCloth.Simulation.Core;

/// <summary>
/// Infinite plane collider: dot(n, x) >= offset (outside half-space).
/// </summary>
public sealed class PlaneCollider : ICollider
{
    private readonly Vector3 _normal;
    private readonly float _offset;

    public PlaneCollider(Vector3 normal, float offset)
    {
        var n = normal;
        var len = n.Length();
        _normal = len > 0 ? n / len : new Vector3(0, 1, 0);
        _offset = offset;
    }

    public void Resolve(Span<Vector3> positions, Span<Vector3> velocities, float deltaTime, float thickness, float friction)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            var x = positions[i];
            float d = Vector3.Dot(_normal, x) - _offset;
            float minDist = thickness - d;
            if (minDist > 0f)
            {
                // Push out along normal
                positions[i] = x + _normal * minDist;
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

