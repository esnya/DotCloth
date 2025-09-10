using System;
using Godot;
using DotCloth.MonoGameSample.Scenarios;

namespace DotCloth.GodotSample;

public partial class ClothPanel : Control
{
    [Export]
    public NodePath ClothPath { get; set; } = null!;

    private ClothNode _cloth = null!;
    private OptionButton _modelOption = null!;
    private OptionButton _scenarioOption = null!;
    private Label _perfLabel = null!;

    public override void _Ready()
    {
        _cloth = GetNode<ClothNode>(ClothPath);
        _modelOption = GetNode<OptionButton>("VBoxContainer/ModelOption");
        _scenarioOption = GetNode<OptionButton>("VBoxContainer/ScenarioOption");
        _perfLabel = GetNode<Label>("VBoxContainer/PerfLabel");

        foreach (ForceModel model in Enum.GetValues<ForceModel>())
        {
            _modelOption.AddItem(model.ToString(), (int)model);
        }

        foreach (var name in _cloth.ScenarioNames)
        {
            _scenarioOption.AddItem(name);
        }

        _modelOption.Selected = (int)_cloth.Model;
        _scenarioOption.Selected = _cloth.ScenarioIndex;

        _modelOption.ItemSelected += OnModelSelected;
        _scenarioOption.ItemSelected += OnScenarioSelected;
    }

    public override void _Process(double delta)
    {
        _perfLabel.Text = _cloth.Metrics;
    }

    private void OnModelSelected(long index)
    {
        _cloth.Model = (ForceModel)index;
    }

    private void OnScenarioSelected(long index)
    {
        _cloth.ScenarioIndex = (int)index;
    }
}

