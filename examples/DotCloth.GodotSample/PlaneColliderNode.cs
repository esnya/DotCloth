using Godot;
using Vector3 = System.Numerics.Vector3;

namespace DotCloth.GodotSample;

public partial class PlaneColliderNode : ColliderNode
{
    public override void Resolve(ref Vector3 position, ref Vector3 velocity)
    {
        var planeY = GlobalTransform.Origin.Y;
        if (position.Y >= planeY)
        {
            return;
        }

        position.Y = planeY;
        if (velocity.Y < 0f)
        {
            velocity.Y = 0f;
        }
    }
}
