using System;
using System.Collections.Generic;
using DotCloth.Simulation;

namespace DotCloth.MonoGameSample.Scenarios;

internal sealed class ClothSim
{
    public IClothSimulator Sim { get; }
    public System.Numerics.Vector3[] Pos { get; }
    public System.Numerics.Vector3[] Vel { get; }
    public int[] Tri { get; }
    public ClothSim(IClothSimulator sim, System.Numerics.Vector3[] pos, System.Numerics.Vector3[] vel, int[] tri)
    { Sim = sim; Pos = pos; Vel = vel; Tri = tri; }
}

internal interface IScenario
{
    string Name { get; }
    IReadOnlyList<ClothSim> Cloths { get; }
    void Initialize();
    void Reset();
    void UpdatePreStep(float elapsedSeconds);
    void GetCollidersFor(int clothIndex, List<DotCloth.Simulation.Collision.ICollider> dst);
    void GetColliderVisualsFor(int clothIndex, List<ColliderViz> dst);
}

internal enum ColliderKind { Plane, Sphere, Capsule }

internal sealed class ColliderViz
{
    public ColliderKind Kind { get; init; }
    public System.Numerics.Vector3 Center { get; init; }
    public float Radius { get; init; }
    public System.Numerics.Vector3 P0 { get; init; }
    public System.Numerics.Vector3 P1 { get; init; }
    public System.Numerics.Vector3 Normal { get; init; }
    public float Offset { get; init; }
}
