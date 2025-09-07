using Godot;
using System;

namespace DotCloth.GodotSample;

/// <summary>
/// ColliderDefinition with built-in oscillating motion.
/// </summary>
public partial class ColliderMover : ColliderDefinition
{
    [Export]
    public Vector3 MotionAmplitude { get; set; } = new(0.15f, 0.05f, 0.15f);

    [Export]
    public Vector3 MotionFrequency { get; set; } = new(0.9f, 1.2f, 0.7f);

    [Export]
    public float MotionPhase { get; set; } = 0f;

    private Vector3 _basePosition;
    private float _time;

    public override void _Ready()
    {
        _basePosition = GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        _time += (float)delta;
        var x = _basePosition.X + MotionAmplitude.X * MathF.Sin(MotionFrequency.X * _time + MotionPhase);
        var y = _basePosition.Y + MotionAmplitude.Y * MathF.Sin(MotionFrequency.Y * _time + MotionPhase);
        var z = _basePosition.Z + MotionAmplitude.Z * MathF.Cos(MotionFrequency.Z * _time + MotionPhase);
        GlobalPosition = new Vector3(x, y, z);
    }
}
