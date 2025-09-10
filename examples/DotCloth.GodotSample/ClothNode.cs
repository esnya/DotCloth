using Godot;
using DotCloth;
using DotCloth.MonoGameSample.Scenarios;
using System.Diagnostics;
using System.Collections.Generic;

namespace DotCloth.GodotSample;

public partial class ClothNode : Node3D
{
    [Export]
    public int ScenarioIndex
    {
        get => _scenarioIndex;
        set
        {
            if (_scenarioIndex == value)
            {
                return;
            }
            _scenarioIndex = value;
            LoadScenario();
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
            LoadScenario();
        }
    }

    public string ModelName => _model.ToString();
    public string ScenarioName => _scenarios[_scenarioIndex].Name;
    public IReadOnlyList<string> ScenarioNames => _scenarioNames;

    private ForceCloth _cloth = null!;
    private ForceModel _model = ForceModel.Springs;
    private int _scenarioIndex;
    private readonly IScenario[] _scenarios =
    {
        new MinimalScenario(),
        new LargeScenario(),
        new XLargeScenario(),
        new CollidersScenario()
    };
    private readonly string[] _scenarioNames;
    private readonly Stopwatch _sw = new();
    private double _perfAccum;
    private double _fpsSmooth;

    public string Metrics { get; private set; } = string.Empty;

    public ClothNode()
    {
        _scenarioNames = System.Array.ConvertAll(_scenarios, s => s.Name);
    }

    public override void _Ready()
    {
        LoadScenario();
    }

    public override void _PhysicsProcess(double delta)
    {
        _sw.Restart();
        var dt = (float)delta;
        _scenarios[_scenarioIndex].Update(dt);
        _cloth.Step(dt);
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

    private void LoadScenario()
    {
        _cloth = _scenarios[_scenarioIndex].Create(_model);
    }
}
