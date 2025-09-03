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
        // Slightly larger grid to interact with tube
        Geometry.MakeGrid(26, 26, 0.11f, out var pos, out var tri);
        var vel = new Vector3[pos.Length];
        var sim = new PbdSolver();
        sim.Initialize(pos, tri, DefaultParams());
        // Pin a row near one edge to suspend as it drops
        var n = 26; var pins = new int[n];
        for (int i = 0; i < n; i++) pins[i] = i; // top row
        sim.PinVertices(pins);
        _cloths.Add(new ClothSim(sim, pos, vel, tri));
    }

    public void Reset() => Initialize();
    public void UpdatePreStep(float elapsedSeconds) { }

    public void GetColliders(List<DotCloth.Simulation.Collision.ICollider> dst)
    {
        dst.Clear();
        dst.Add(new DotCloth.Simulation.Collision.PlaneCollider(new Vector3(0, 1, 0), 0f));
        // Build an approximate vertical cylinder around Y-axis using capsules
        float radius = 0.6f; float halfHeight = 0.8f; float segRadius = 0.08f;
        int sides = 12;
        for (int s = 0; s < sides; s++)
        {
            float a0 = (s * 2f * MathF.PI) / sides;
            float x = radius * MathF.Cos(a0);
            float z = radius * MathF.Sin(a0);
            var p0 = new Vector3(x, -halfHeight, z);
            var p1 = new Vector3(x, halfHeight, z);
            dst.Add(new DotCloth.Simulation.Collision.CapsuleCollider(p0, p1, segRadius));
        }
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

