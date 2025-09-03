using System.Collections.Generic;
using System.Numerics;
using DotCloth.MonoGameSample.Scenarios;
using DotCloth.Simulation;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;

namespace DotCloth.MonoGameSample.Scenarios;

internal sealed class MinimalScenario : IScenario
{
    public string Name => "Minimal";
    public IReadOnlyList<ClothSim> Cloths => _cloths;
    private readonly List<ClothSim> _cloths = new();
    private readonly List<DotCloth.Simulation.Collision.ICollider> _colliders = new();

    public void Initialize()
    {
        _cloths.Clear();
        // Single small grid with top row pinned
        Geometry.MakeGrid(20, 20, 0.1f, out var pos, out var tri);
        var vel = new Vector3[pos.Length];
        var sim = new PbdSolver();
        sim.Initialize(pos, tri, DefaultParams());
        // Pin top row
        var n = 20; var pins = new int[n];
        for (int i = 0; i < n; i++) pins[i] = (n - 1) * n + i;
        sim.PinVertices(pins);
        _cloths.Add(new ClothSim(sim, pos, vel, tri));
    }

    public void Reset() => Initialize();
    public void UpdatePreStep(float elapsedSeconds) { /* no-op */ }

    public void GetColliders(List<DotCloth.Simulation.Collision.ICollider> dst)
    {
        dst.Clear();
        dst.Add(new DotCloth.Simulation.Collision.PlaneCollider(new Vector3(0, 1, 0), 0f)); // floor at y=0
    }

    private static ClothParameters DefaultParams() => new()
    {
        VertexMass = 1.0f,
        Damping = 0.05f,
        AirDrag = 0.2f,
        StretchStiffness = 0.9f,
        BendStiffness = 0.1f,
        GravityScale = 1.0f,
        UseGravity = true,
        Substeps = 1,
        Iterations = 8
    };
}

