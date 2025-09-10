using System.Numerics;
using DotCloth.Collisions;

namespace DotCloth.MonoGameSample.Scenarios;

public sealed class MinimalScenario : IScenario
{
    public string Name => "Minimal";
    public int GridSize => 10;
    public ForceCloth Create(ForceModel model)
    {
        var colliders = new ICollider[] { new PlaneCollider(Vector3.Zero, Vector3.UnitY) };
        return ClothFactory.Create(GridSize, model, colliders);
    }
    public void Update(float dt) { }
}
