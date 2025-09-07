using Godot;

namespace DotCloth.GodotSample;

/// <summary>
/// Defines a collision shape for cloth interaction.
/// Supported shapes are sphere and capsule.
/// </summary>
public partial class ColliderDefinition : MeshInstance3D
{
    public enum ShapeKind { Sphere, Capsule }

    [Export]
    public ShapeKind Shape { get; set; } = ShapeKind.Sphere;

    // Sphere radius or capsule radius.
    [Export]
    public float Radius { get; set; } = 0.4f;

    // Capsule height. Ignored for spheres.
    [Export]
    public float Height { get; set; } = 0.8f;
}
