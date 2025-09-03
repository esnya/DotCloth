using System.Collections.Generic;
using System.Numerics;
using DotCloth.Simulation;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;

namespace DotCloth.MonoGameSample.Scenarios;

internal sealed class CollidersScenario : IScenario
{
    public string Name => "Colliders";
    public IReadOnlyList<ClothSim> Cloths => _cloths;
    private readonly List<ClothSim> _cloths = new();
    private float _time;

    public void Initialize()
    {
        _cloths.Clear();
        int n = 24;
        Geometry.MakeGrid(n, n, 0.12f, out var pos, out var tri);
        // Lower the cloth so it can contact colliders
        for (int i = 0; i < pos.Length; i++) pos[i].Y -= 1.0f; // from 1.5 -> 0.5
        var vel = new Vector3[pos.Length];
        var sim = new PbdSolver();
        sim.Initialize(pos, tri, DefaultParams());
        // Pin two top corners only
        sim.PinVertices((n-1)*n + 0, (n-1)*n + (n-1));
        _cloths.Add(new ClothSim(sim, pos, vel, tri));
        _time = 0f;
    }

    public void Reset() => Initialize();

    public void UpdatePreStep(float elapsedSeconds)
    {
        _time += elapsedSeconds;
    }

    public void GetCollidersFor(int clothIndex, List<DotCloth.Simulation.Collision.ICollider> dst)
    {
        dst.Clear();
        // Floor
        dst.Add(new DotCloth.Simulation.Collision.PlaneCollider(new Vector3(0, 1, 0), 0f));
        // Moving sphere
        var r = 0.35f;
        var c = new Vector3(MathF.Sin(_time) * 0.6f, 0.4f + 0.2f * MathF.Cos(_time*0.7f), 0.0f);
        dst.Add(new DotCloth.Simulation.Collision.SphereCollider(c, r));
        // Oscillating capsule sweeping under the cloth
        var p0 = new Vector3(-0.7f + 0.2f*MathF.Sin(_time*0.5f), 0.25f, -0.2f);
        var p1 = new Vector3( 0.7f + 0.2f*MathF.Sin(_time*0.5f), 0.25f,  0.2f);
        dst.Add(new DotCloth.Simulation.Collision.CapsuleCollider(p0, p1, 0.15f));
    }

    public void GetColliderVisualsFor(int clothIndex, List<ColliderViz> dst)
    {
        dst.Clear();
        dst.Add(new ColliderViz { Kind = ColliderKind.Plane, Normal = new Vector3(0,1,0), Offset = 0f });
        var r = 0.35f;
        var c = new Vector3(MathF.Sin(_time) * 0.6f, 0.4f + 0.2f * MathF.Cos(_time*0.7f), 0.0f);
        dst.Add(new ColliderViz { Kind = ColliderKind.Sphere, Center = c, Radius = r });
        var p0 = new Vector3(-0.7f + 0.2f*MathF.Sin(_time*0.5f), 0.25f, -0.2f);
        var p1 = new Vector3( 0.7f + 0.2f*MathF.Sin(_time*0.5f), 0.25f,  0.2f);
        dst.Add(new ColliderViz { Kind = ColliderKind.Capsule, P0 = p0, P1 = p1, Radius = 0.15f });
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
