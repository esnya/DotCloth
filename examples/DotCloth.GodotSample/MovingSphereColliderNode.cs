using Godot;
using System;

namespace DotCloth.GodotSample;

public partial class MovingSphereColliderNode : SphereColliderNode
{
    private float _time;

    public override void _PhysicsProcess(double delta)
    {
        _time += (float)delta;
        GlobalPosition = new Vector3(
            MathF.Sin(_time) * 2f,
            2f + 0.5f * MathF.Cos(_time * 0.7f),
            0f);
    }
}
