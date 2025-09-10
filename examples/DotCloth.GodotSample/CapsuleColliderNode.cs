using Godot;
using System;
using Vector3 = System.Numerics.Vector3;

namespace DotCloth.GodotSample;

public partial class CapsuleColliderNode : ColliderNode
{
    [Export]
    public Godot.Vector3 P0 { get; set; }
    [Export]
    public Godot.Vector3 P1 { get; set; }
    [Export]
    public float Radius { get; set; } = 0.75f;

    public override void Resolve(ref Vector3 position, ref Vector3 velocity)
    {
        var p0 = ToNumerics(P0);
        var p1 = ToNumerics(P1);
        var ab = p1 - p0;
        var ap = position - p0;
        float t = 0f;
        var abLenSq = ab.LengthSquared();
        if (abLenSq > 0f)
        {
            t = Vector3.Dot(ap, ab) / abLenSq;
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
        }

        var closest = p0 + t * ab;
        var diff = position - closest;
        var distSq = diff.LengthSquared();
        var rSq = Radius * Radius;
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
        else if (abLenSq > 0f)
        {
            normal = Vector3.Normalize(ab);
        }
        else
        {
            normal = Vector3.UnitY;
        }

        position = closest + normal * Radius;
        var vn = Vector3.Dot(velocity, normal);
        if (vn < 0f)
        {
            velocity -= vn * normal;
        }
    }

    protected static Vector3 ToNumerics(Godot.Vector3 v) => new(v.X, v.Y, v.Z);
}
