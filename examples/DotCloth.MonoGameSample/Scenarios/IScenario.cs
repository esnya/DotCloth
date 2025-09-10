using DotCloth;
using System.Numerics;
using System.Collections.Generic;

namespace DotCloth.MonoGameSample.Scenarios;

public interface IScenario
{
    string Name { get; }
    int GridSize { get; }
    ForceCloth Create(ForceModel model);
    void Update(float dt);
}

public interface IColliderScenario : IScenario
{
    void CollectColliderVisuals(List<ColliderViz> dst);
}

public enum ColliderKind { Sphere, Capsule }

public readonly struct ColliderViz
{
    public ColliderKind Kind { get; init; }
    public Vector3 Center { get; init; }
    public float Radius { get; init; }
    public Vector3 P0 { get; init; }
    public Vector3 P1 { get; init; }
}
