using Godot;
using System;

namespace DotCloth.GodotSample;

public partial class MovingCapsuleColliderNode : CapsuleColliderNode
{
    private float _time;
    private Godot.Vector3 _baseP0;
    private Godot.Vector3 _baseP1;

    public override void _Ready()
    {
        _baseP0 = P0;
        _baseP1 = P1;
        UpdateVisual();
    }

    public override void _PhysicsProcess(double delta)
    {
        _time += (float)delta;
        var sweep = 0.5f * MathF.Sin(_time * 0.5f);
        P0 = new Godot.Vector3(_baseP0.X + sweep, _baseP0.Y, _baseP0.Z);
        P1 = new Godot.Vector3(_baseP1.X + sweep, _baseP1.Y, _baseP1.Z);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        var center = (P0 + P1) * 0.5f;
        GlobalPosition = center;
    }
}
