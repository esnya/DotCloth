using Godot;
using DotCloth.Collisions;
using Vector3 = System.Numerics.Vector3;

namespace DotCloth.GodotSample;

public abstract partial class ColliderNode : Node3D, ICollider
{
    public abstract void Resolve(ref Vector3 position, ref Vector3 velocity);
}
