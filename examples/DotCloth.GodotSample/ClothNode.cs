using Godot;
using System;
using System.Collections.Generic;
using DotCloth;
using DotCloth.MonoGameSample.Scenarios;
using Vector3 = System.Numerics.Vector3;

namespace DotCloth.GodotSample;

public partial class ClothNode : Node3D
{
    [Export]
    public int GridSize { get; set; } = 10;

    [Export]
    public ForceModel Model
    {
        get => _model;
        set
        {
            if (_model == value) return;
            _model = value;
            Initialize();
        }
    }

    private ForceModel _model = ForceModel.Springs;
    private ForceCloth _cloth = null!;
    private MultiMeshInstance3D _meshInstance = null!;
    private readonly List<ColliderNode> _colliders = new();
    private readonly System.Diagnostics.Stopwatch _sw = new();
    private double _perfAccum;
    private double _fpsSmooth;
    public string Metrics { get; private set; } = string.Empty;

    public override void _Ready()
    {
        Initialize();
    }

    private void Initialize()
    {
        _colliders.Clear();
        foreach (var child in GetChildren())
        {
            if (child is ColliderNode collider)
            {
                _colliders.Add(collider);
            }
        }

        var extras = _colliders.ToArray();
        _cloth = ClothFactory.Create(GridSize, Model, extras);

        if (_meshInstance == null)
        {
            _meshInstance = new MultiMeshInstance3D();
            AddChild(_meshInstance);
        }

        var mm = new MultiMesh
        {
            Mesh = new SphereMesh { Radius = 0.05f },
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            InstanceCount = _cloth.Positions.Length
        };
        _meshInstance.Multimesh = mm;
    }

    public override void _PhysicsProcess(double delta)
    {
        var dt = (float)delta;
        _sw.Restart();
        _cloth.Step(dt);
        var simMs = _sw.Elapsed.TotalMilliseconds;

        var mm = _meshInstance.Multimesh;
        var positions = _cloth.Positions;
        for (int i = 0; i < positions.Length; i++)
        {
            var p = positions[i];
            mm.SetInstanceTransform(i, new Transform3D(Basis.Identity, new Godot.Vector3(p.X, p.Y, p.Z)));
        }

        _perfAccum += delta;
        if (_perfAccum >= 0.25)
        {
            _perfAccum = 0.0;
            var fps = Engine.GetFramesPerSecond();
            _fpsSmooth = _fpsSmooth <= 0 ? fps : (_fpsSmooth * 0.9 + fps * 0.1);
            Metrics = $"Perf: Sim {simMs:F2} ms | FPS {(float)_fpsSmooth:F1} | Verts {positions.Length}";
        }
    }
}
