using System;
using Godot;

namespace DotCloth.GodotSample;

public partial class ClothPanel : Control
{
    [Export]
    public NodePath ClothPath { get; set; } = null!;

    private ClothNode _cloth = null!;
    private OptionButton _modelOption = null!;
    private SpinBox _sizeInput = null!;
    private Label _perfLabel = null!;

    public override void _Ready()
    {
        _cloth = GetNode<ClothNode>(ClothPath);
        _modelOption = GetNode<OptionButton>("VBoxContainer/ModelOption");
        _sizeInput = GetNode<SpinBox>("VBoxContainer/SizeInput");
        _perfLabel = GetNode<Label>("VBoxContainer/PerfLabel");

        foreach (ForceModel model in Enum.GetValues<ForceModel>())
        {
            _modelOption.AddItem(model.ToString(), (int)model);
        }

        _modelOption.Selected = (int)_cloth.Model;
        _sizeInput.Value = _cloth.Size;

        _modelOption.ItemSelected += OnModelSelected;
        _sizeInput.ValueChanged += OnSizeChanged;
    }

    public override void _Process(double delta)
    {
        _perfLabel.Text = _cloth.Metrics;
    }

    private void OnModelSelected(long index)
    {
        _cloth.Model = (ForceModel)index;
    }

    private void OnSizeChanged(double value)
    {
        _cloth.Size = (int)value;
    }
}

