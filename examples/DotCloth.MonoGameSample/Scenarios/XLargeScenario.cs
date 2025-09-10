using System.Numerics;
using DotCloth.Collisions;

namespace DotCloth.MonoGameSample.Scenarios;

public sealed class XLargeScenario : IScenario
{
    public string Name => "XLarge";
    public int GridSize => 30;
    public ForceCloth Create(ForceModel model)
    {
        var colliders = new ICollider[] { new PlaneCollider(Vector3.Zero, Vector3.UnitY) };
        return ClothFactory.Create(GridSize, model, colliders);
    }
    public void Update(float dt) { }
}
