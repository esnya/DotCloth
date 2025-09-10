using Godot;
using DotCloth;
using DotCloth.MonoGameSample.Scenarios;

namespace DotCloth.GodotSample;

public partial class Main : Node3D
{
    private ForceCloth _cloth = null!;
    private int _size = 10;
    private string _model = "Springs";

    public override void _Ready()
    {
        _cloth = ClothFactory.Create(_size, _model);
        GD.Print($"Model: {_model}");
    }

    public override void _PhysicsProcess(double delta)
    {
        _cloth.Step((float)delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true } key)
        {
            string? next = key.Keycode switch
            {
                Key.Key1 => "Springs",
                Key.Key2 => "Shells",
                Key.Key3 => "FEM",
                Key.Key4 => "Springs+Strain",
                _ => null
            };
            if (next is not null && next != _model)
            {
                _model = next;
                _cloth = ClothFactory.Create(_size, _model);
                GD.Print($"Model: {_model}");
            }
        }
    }
}
