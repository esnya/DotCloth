using System.Numerics;
using DotCloth.Collisions;

namespace DotCloth.MonoGameSample.Scenarios;

public sealed class LargeScenario : IScenario
{
    public string Name => "Large";
    public int GridSize => 20;
    public ForceCloth Create(ForceModel model)
    {
        var colliders = new ICollider[] { new PlaneCollider(Vector3.Zero, Vector3.UnitY) };
        return ClothFactory.Create(GridSize, model, colliders);
    }
    public void Update(float dt) { }
}
