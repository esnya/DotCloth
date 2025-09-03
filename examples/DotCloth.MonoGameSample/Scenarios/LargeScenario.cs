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
        // Grid of cloth instances with per-instance moving colliders
        int gx = 5, gz = 4; // 20 instances
        for (int z = 0; z < gz; z++)
        for (int x = 0; x < gx; x++)
        {
            Geometry.MakeGrid(18, 18, 0.1f, out var pos, out var tri);
            var vel = new Vector3[pos.Length];
            // Offset cloth in world
            var offset = new Vector3((x - (gx-1)*0.5f) * 2.2f, 1.6f, (z - (gz-1)*0.5f) * 2.2f);
            for (int i = 0; i < pos.Length; i++) pos[i] += offset;
            var sim = new PbdSolver();
            sim.Initialize(pos, tri, DefaultParams());
            // Pin top row
            int n = 18; var pins = new int[n];
            for (int i = 0; i < n; i++) pins[i] = (n - 1) * n + i;
            sim.PinVertices(pins);
            _cloths.Add(new ClothSim(sim, pos, vel, tri));
        }
    }

    public void Reset() => Initialize();
    public void UpdatePreStep(float elapsedSeconds) { }

    public void GetCollidersFor(int clothIndex, List<DotCloth.Simulation.Collision.ICollider> dst)
    {
        dst.Clear();
        dst.Add(new DotCloth.Simulation.Collision.PlaneCollider(new Vector3(0, 1, 0), 0f));
        // Per-instance moving sphere around its local origin
        int gx = 5;
        int ix = clothIndex % gx;
        int iz = clothIndex / gx;
        var basePos = new Vector3((ix - (gx-1)*0.5f) * 2.2f, 0.3f, (iz - (4-1)*0.5f) * 2.2f);
        float t = (float)(0.7 * clothIndex);
        var c = basePos + new Vector3(0.5f*MathF.Sin(t), 0.1f+0.15f*MathF.Cos(1.1f*t), 0.5f*MathF.Cos(0.9f*t));
        dst.Add(new DotCloth.Simulation.Collision.SphereCollider(c, 0.25f));
    }

    public void GetColliderVisualsFor(int clothIndex, List<ColliderViz> dst)
    {
        dst.Clear();
        dst.Add(new ColliderViz { Kind = ColliderKind.Plane, Normal = new Vector3(0,1,0), Offset = 0f });
        int gx = 5;
        int ix = clothIndex % gx;
        int iz = clothIndex / gx;
        var basePos = new Vector3((ix - (gx-1)*0.5f) * 2.2f, 0.3f, (iz - (4-1)*0.5f) * 2.2f);
        float t = (float)(0.7 * clothIndex);
        var c = basePos + new Vector3(0.5f*MathF.Sin(t), 0.1f+0.15f*MathF.Cos(1.1f*t), 0.5f*MathF.Cos(0.9f*t));
        dst.Add(new ColliderViz { Kind = ColliderKind.Sphere, Center = c, Radius = 0.25f });
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
