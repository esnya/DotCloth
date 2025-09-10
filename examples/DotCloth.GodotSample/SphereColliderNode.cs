using Godot;
using System;
using Vector3 = System.Numerics.Vector3;

namespace DotCloth.GodotSample;

public partial class SphereColliderNode : ColliderNode
{
    [Export]
    public float Radius { get; set; } = 1f;

    public override void Resolve(ref Vector3 position, ref Vector3 velocity)
    {
        var center = GlobalTransform.Origin;
        var centerVec = new Vector3(center.X, center.Y, center.Z);
        var diff = position - centerVec;
        var distSq = diff.LengthSquared();
        var rSq = Radius * Radius;
        if (distSq >= rSq)
        {
            return;
        }

        var dist = MathF.Sqrt(distSq);
        var normal = dist > 0f ? diff / dist : Vector3.UnitY;
        position = centerVec + normal * Radius;
        var vn = Vector3.Dot(velocity, normal);
        if (vn < 0f)
        {
            velocity -= vn * normal;
        }
    }
}
