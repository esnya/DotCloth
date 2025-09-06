using System.Collections.Generic;
using System.Numerics;
using DotCloth.Simulation;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;

namespace DotCloth.MonoGameSample.Scenarios;

internal sealed class CylinderScenario : IScenario
{
    public string Name => "Cylinder";
    public IReadOnlyList<ClothSim> Cloths => _cloths;
    private readonly List<ClothSim> _cloths = new();

    public void Initialize()
    {
        _cloths.Clear();
        // Closed tube cloth: roll a grid around Y-axis and stitch seam
        Geometry.MakeTube(radial: 32, heightSeg: 24, radius: 0.6f, height: 1.6f, out var pos, out var tri);
        var vel = new Vector3[pos.Length];
        var sim = new PbdSolver();
        sim.Initialize(pos, tri, DefaultParams());
        // Pin top ring to keep tube hanging
        var pins = new int[32];
        for (int i = 0; i < 32; i++) pins[i] = i; // first ring indices
        sim.PinVertices(pins);
        _cloths.Add(new ClothSim(sim, pos, vel, tri));
    }

    public void Reset() => Initialize();
    public void UpdatePreStep(float elapsedSeconds) { }

    public void GetCollidersFor(int clothIndex, List<DotCloth.Simulation.Collision.ICollider> dst)
    {
        dst.Clear();
        dst.Add(new DotCloth.Simulation.Collision.PlaneCollider(new Vector3(0, 1, 0), 0f));
    }

    public void GetColliderVisualsFor(int clothIndex, List<ColliderViz> dst)
    {
        dst.Clear();
        dst.Add(new ColliderViz { Kind = ColliderKind.Plane, Normal = new Vector3(0, 1, 0), Offset = 0f });
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
        Iterations = 10
    };
}
