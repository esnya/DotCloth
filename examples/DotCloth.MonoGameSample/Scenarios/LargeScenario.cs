using System.Collections.Generic;
using System.Numerics;
using DotCloth.Simulation;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;

namespace DotCloth.MonoGameSample.Scenarios;

internal sealed class LargeScenario : IScenario
{
    public string Name => "Large";
    public IReadOnlyList<ClothSim> Cloths => _cloths;
    private readonly List<ClothSim> _cloths = new();

    public void Initialize()
    {
        _cloths.Clear();
        // Grid of small cloth instances with floor and a couple of colliders
        int gx = 4, gz = 3; // 12 instances
        for (int z = 0; z < gz; z++)
        for (int x = 0; x < gx; x++)
        {
            Geometry.MakeGrid(14, 14, 0.1f, out var pos, out var tri);
            var vel = new Vector3[pos.Length];
            // Offset cloth in world
            var offset = new Vector3((x - (gx-1)*0.5f) * 2.0f, 1.5f, (z - (gz-1)*0.5f) * 2.0f);
            for (int i = 0; i < pos.Length; i++) pos[i] += offset;
            var sim = new PbdSolver();
            sim.Initialize(pos, tri, DefaultParams());
            // Pin top row
            int n = 14; var pins = new int[n];
            for (int i = 0; i < n; i++) pins[i] = (n - 1) * n + i;
            sim.PinVertices(pins);
            _cloths.Add(new ClothSim(sim, pos, vel, tri));
        }
    }

    public void Reset() => Initialize();
    public void UpdatePreStep(float elapsedSeconds) { }

    public void GetColliders(List<DotCloth.Simulation.Collision.ICollider> dst)
    {
        dst.Clear();
        dst.Add(new DotCloth.Simulation.Collision.PlaneCollider(new Vector3(0, 1, 0), 0f));
        // Static spheres scattered
        dst.Add(new DotCloth.Simulation.Collision.SphereCollider(new Vector3( 1.8f, 0.3f, 0.0f), 0.25f));
        dst.Add(new DotCloth.Simulation.Collision.SphereCollider(new Vector3(-1.8f, 0.35f, 1.5f), 0.3f));
        dst.Add(new DotCloth.Simulation.Collision.SphereCollider(new Vector3( 0.0f, 0.25f,-1.5f), 0.22f));
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

