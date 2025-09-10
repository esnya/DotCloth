using Godot;

namespace DotCloth.GodotSample;

public partial class Main : Node3D
{
    private ClothNode _cloth = null!;

    public override void _Ready()
    {
        _cloth = GetNode<ClothNode>("Cloth");
        GD.Print($"Model: {_cloth.ModelName} Size: {_cloth.Size}");
    }
}
