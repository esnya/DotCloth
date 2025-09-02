using System.Numerics;

namespace DotCloth.Simulation.Collision;

/// <summary>Sphere collider with center and radius.</summary>
public sealed class SphereCollider : ICollider
{
    private Vector3 _center;
    private float _radius;

    /// <summary>Constructs a sphere collider at <paramref name="center"/> with <paramref name="radius"/>.</summary>
    public SphereCollider(Vector3 center, float radius)
    {
        _center = center;
        _radius = MathF.Max(0f, radius);
    }

    /// <inheritdoc />
    public void Resolve(ReadOnlySpan<Vector3> prevPositions, Span<Vector3> positions, Span<Vector3> velocities, float deltaTime, float thickness, float friction)
    {
        float effectiveR = _radius + MathF.Max(0f, thickness);
        for (int i = 0; i < positions.Length; i++)
        {
            var x = positions[i];
            var to = x - _center;
            var dist = to.Length();
            if (dist < 1e-9f) dist = 1e-9f;
            float pen = effectiveR - dist;
            if (pen > 0f)
            {
                // Swept push: if previous was outside, move to intersection point
                var xp = prevPositions[i];
                if ((xp - _center).Length() >= effectiveR - 1e-6f)
                {
                    var d = x - xp;
                    var m = xp - _center;
                    float a = Vector3.Dot(d, d);
                    float b = 2f * Vector3.Dot(d, m);
                    float c = Vector3.Dot(m, m) - effectiveR * effectiveR;
                    float disc = b * b - 4f * a * c;
                    if (disc >= 0f && a > 1e-12f)
                    {
                        float t = (-b + MathF.Sqrt(disc)) / (2f * a); // exit time
                        t = Math.Clamp(t, 0f, 1f);
                        x = xp + d * t;
                        positions[i] = x;
                        to = x - _center;
                        dist = to.Length();
                        if (dist < 1e-9f) dist = 1e-9f;
                    }
                }
                var n = to / dist;
                positions[i] = _center + n * effectiveR;
                var v = velocities[i];
                var vn = Vector3.Dot(v, n) * n;
                var vt = v - vn;
                vt *= MathF.Max(0f, 1f - friction);
                velocities[i] = vt; // remove normal velocity
            }
        }
    }
}
