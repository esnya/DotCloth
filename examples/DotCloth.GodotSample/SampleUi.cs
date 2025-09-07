using System;
using Godot;

namespace DotCloth.GodotSample;

/// <summary>
/// Handles user interaction and forwards parameter changes to <see cref="Main"/>.
/// </summary>
public partial class SampleUi : PanelContainer
{
    private Main _main = default!;
    private OptionButton _scenarioOption = default!;
    private Label _scenarioDesc = default!;
    private HSlider _iter = default!;
    private HSlider _stretch = default!;
    private HSlider _bend = default!;
    private VBoxContainer _scenarioControls = default!;

    public VBoxContainer ScenarioControls => _scenarioControls;

    public override void _Ready()
    {
        _main = GetTree().CurrentScene as Main ?? throw new InvalidOperationException("Main node not found");
        _scenarioOption = GetNode<OptionButton>("VBox/ScenarioSelector/ScenarioOption");
        _scenarioDesc = GetNode<Label>("VBox/ScenarioDesc");
        _iter = GetNode<HSlider>("VBox/Iterations/IterationsSlider");
        _stretch = GetNode<HSlider>("VBox/Stretch/StretchSlider");
        _bend = GetNode<HSlider>("VBox/Bend/BendSlider");
        _scenarioControls = GetNode<VBoxContainer>("../ScenarioPanel/ScenarioVBox");

        _scenarioOption.ItemSelected += idx => _main.SelectScenario((int)idx);
        _iter.ValueChanged += v => _main.SetIterations((int)v);
        _stretch.ValueChanged += v => _main.SetStretch((float)v);
        _bend.ValueChanged += v => _main.SetBend((float)v);
    }

    public void SetScenarioOptions(string[] names, int selected)
    {
        _scenarioOption.Clear();
        for (int i = 0; i < names.Length; i++) _scenarioOption.AddItem(names[i], i);
        _scenarioOption.Selected = selected;
    }

    public void SetScenarioDescription(string text) => _scenarioDesc.Text = text;

    public void SetParameterValues(int iter, float stretch, float bend)
    {
        _iter.Value = iter;
        _stretch.Value = stretch;
        _bend.Value = bend;
    }
}
