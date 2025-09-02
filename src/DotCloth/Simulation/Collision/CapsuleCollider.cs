using System.Numerics;

namespace DotCloth.Simulation.Collision;

/// <summary>Capsule collider defined by segment [p0, p1] and radius.</summary>
public sealed class CapsuleCollider : ICollider
{
    private Vector3 _p0;
    private Vector3 _p1;
    private float _radius;

    /// <summary>Constructs a capsule collider from segment endpoints and radius.</summary>
    public CapsuleCollider(Vector3 p0, Vector3 p1, float radius)
    {
        _p0 = p0;
        _p1 = p1;
        _radius = MathF.Max(0f, radius);
    }

    /// <inheritdoc />
    public void Resolve(ReadOnlySpan<Vector3> prevPositions, Span<Vector3> positions, Span<Vector3> velocities, float deltaTime, float thickness, float friction)
    {
        float r = _radius + MathF.Max(0f, thickness);
        var seg = _p1 - _p0;
        float segLen2 = seg.LengthSquared();
        for (int i = 0; i < positions.Length; i++)
        {
            var x = positions[i];
            float t = 0f;
            if (segLen2 > 1e-12f)
            {
                t = Vector3.Dot(x - _p0, seg) / segLen2;
                t = Math.Clamp(t, 0f, 1f);
            }
            var c = _p0 + seg * t; // closest point on segment
            var d = x - c;
            float dist = d.Length();
            if (dist < 1e-9f) dist = 1e-9f;
            float pen = r - dist;
            if (pen > 0f)
            {
                // Simple swept: if previous point was outside, try moving to boundary along prev->curr
                var xp = prevPositions[i];
                float tp = 0f;
                if (segLen2 > 1e-12f)
                {
                    tp = Vector3.Dot(xp - _p0, seg) / segLen2;
                    tp = Math.Clamp(tp, 0f, 1f);
                }
                var cp = _p0 + seg * tp;
                if ((xp - cp).Length() >= r - 1e-6f)
                {
                    var dir = x - xp;
                    // Step toward exit point in one shot by projecting onto local normal at new closest point
                    // (approximate; keeps implementation simple)
                    // No explicit time solve; final snap to surface below ensures separation.
                }
                var n = d / dist;
                positions[i] = c + n * r;
                var v = velocities[i];
                var vn = Vector3.Dot(v, n) * n;
                var vt = v - vn;
                vt *= MathF.Max(0f, 1f - friction);
                velocities[i] = vt;
            }
        }
    }
}
