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
    void GetColliders(List<DotCloth.Simulation.Collision.ICollider> dst);
}

