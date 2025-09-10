using System;
using System.Collections.Generic;
using Godot;
using DotCloth.MonoGameSample.Scenarios;

namespace DotCloth.GodotSample;

public partial class ClothPanel : Control
{
    [Export]
    public NodePath ScenariosPath { get; set; } = null!;

    private readonly List<ClothNode> _scenarios = new();
    private ClothNode _active = null!;
    private OptionButton _modelOption = null!;
    private OptionButton _scenarioOption = null!;
    private Label _perfLabel = null!;

    public override void _Ready()
    {
        _modelOption = GetNode<OptionButton>("VBoxContainer/ModelOption");
        _scenarioOption = GetNode<OptionButton>("VBoxContainer/ScenarioOption");
        _perfLabel = GetNode<Label>("VBoxContainer/PerfLabel");

        foreach (ForceModel model in Enum.GetValues<ForceModel>())
        {
            _modelOption.AddItem(model.ToString(), (int)model);
        }

        var root = GetNode<Node>(ScenariosPath);
        foreach (var child in root.GetChildren())
        {
            if (child is ClothNode cloth)
            {
                _scenarios.Add(cloth);
                _scenarioOption.AddItem(cloth.Name);
                cloth.Visible = false;
                cloth.SetProcess(false);
                cloth.SetPhysicsProcess(false);
            }
        }

        if (_scenarios.Count > 0)
        {
            ActivateScenario(0);
        }

        _modelOption.ItemSelected += OnModelSelected;
        _scenarioOption.ItemSelected += OnScenarioSelected;
    }

    public override void _Process(double delta)
    {
        if (_active != null)
        {
            _perfLabel.Text = _active.Metrics;
        }
    }

    private void ActivateScenario(int index)
    {
        for (int i = 0; i < _scenarios.Count; i++)
        {
            var node = _scenarios[i];
            var active = i == index;
            node.Visible = active;
            node.SetProcess(active);
            node.SetPhysicsProcess(active);
            if (active)
            {
                _active = node;
            }
        }

        _modelOption.Selected = (int)_active.Model;
    }

    private void OnModelSelected(long index)
    {
        _active.Model = (ForceModel)index;
    }

    private void OnScenarioSelected(long index)
    {
        ActivateScenario((int)index);
    }
}
