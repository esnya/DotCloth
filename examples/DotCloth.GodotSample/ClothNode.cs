using System;
using Godot;
using DotCloth;
using DotCloth.MonoGameSample.Scenarios;
using System.Diagnostics;

namespace DotCloth.GodotSample;

public partial class ClothNode : Node3D
{
    [Export]
    public int Size
    {
        get => _size;
        set
        {
            if (_size == value)
            {
                return;
            }
            _size = value;
            _cloth = ClothFactory.Create(_size, ToModelString(_model));
        }
    }

    [Export]
    public ForceModel Model
    {
        get => _model;
        set
        {
            if (_model == value)
            {
                return;
            }
            _model = value;
            _cloth = ClothFactory.Create(_size, ToModelString(_model));
        }
    }

    public string ModelName => ToModelString(_model);

    private ForceCloth _cloth = null!;
    private ForceModel _model = ForceModel.Springs;
    private int _size = 10;
    private readonly Stopwatch _sw = new();
    private double _perfAccum;
    private double _fpsSmooth;

    public string Metrics { get; private set; } = string.Empty;

    public override void _Ready()
    {
        _cloth = ClothFactory.Create(_size, ToModelString(_model));
    }

    public override void _PhysicsProcess(double delta)
    {
        _sw.Restart();
        _cloth.Step((float)delta);
        var simMs = _sw.Elapsed.TotalMilliseconds;

        _perfAccum += delta;
        if (_perfAccum >= 0.25)
        {
            _perfAccum = 0.0;
            var fps = Engine.GetFramesPerSecond();
            _fpsSmooth = _fpsSmooth <= 0 ? fps : (_fpsSmooth * 0.9 + fps * 0.1);
            Metrics = $"Perf: Sim {simMs:F2} ms | FPS {(float)_fpsSmooth:F1} | Verts {_cloth.Positions.Length}";
        }
    }

    private static string ToModelString(ForceModel model) => model switch
    {
        ForceModel.Springs => "Springs",
        ForceModel.Shells => "Shells",
        ForceModel.Fem => "FEM",
        ForceModel.SpringsWithStrain => "Springs+Strain",
        _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
    };
}

public enum ForceModel
{
    Springs,
    Shells,
    Fem,
    SpringsWithStrain
}
