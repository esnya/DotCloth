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
    private float _time;
    private const int Gx = 5, Gz = 4;
    private const int Nx = 18, Ny = 18;
    private const float GridSpacing = 0.1f;
    private const float InstancePitch = 2.2f;

    public void Initialize()
    {
        _cloths.Clear();
        _time = 0f;
        // Grid of cloth instances with per-instance moving colliders
        for (int z = 0; z < Gz; z++)
        for (int x = 0; x < Gx; x++)
        {
            Geometry.MakeGrid(Nx, Ny, GridSpacing, out var pos, out var tri);
            var vel = new Vector3[pos.Length];
            // Offset cloth in world
            var offset = new Vector3((x - (Gx-1)*0.5f) * InstancePitch, 0.0f, (z - (Gz-1)*0.5f) * InstancePitch);
            for (int i = 0; i < pos.Length; i++) pos[i] += offset;
            var sim = new PbdSolver();
            sim.Initialize(pos, tri, DefaultParams());
            // Pin top row
            int n = Nx; var pins = new int[n];
            for (int i = 0; i < n; i++) pins[i] = (n - 1) * n + i;
            sim.PinVertices(pins);
            _cloths.Add(new ClothSim(sim, pos, vel, tri));
        }
    }

    public void Reset() => Initialize();
    public void UpdatePreStep(float elapsedSeconds) { _time += elapsedSeconds; }

    public void GetCollidersFor(int clothIndex, List<DotCloth.Simulation.Collision.ICollider> dst)
    {
        dst.Clear();
        dst.Add(new DotCloth.Simulation.Collision.PlaneCollider(new Vector3(0, 1, 0), 0f));
        // Per-instance moving sphere around its local origin
        int ix = clothIndex % Gx;
        int iz = clothIndex / Gx;
        var basePos = new Vector3((ix - (Gx-1)*0.5f) * InstancePitch, 0.4f, (iz - (Gz-1)*0.5f) * InstancePitch);
        // center under pinned row (top row in +Z direction)
        float pinnedZ = (Ny - 1) * 0.5f * GridSpacing;
        basePos.Z += pinnedZ;
        float phase = (float)(0.5 * clothIndex);
        float t = _time + phase;
        var c = basePos + new Vector3(0.5f*MathF.Sin(t), 0.1f+0.15f*MathF.Cos(1.1f*t), 0.5f*MathF.Cos(0.9f*t));
        dst.Add(new DotCloth.Simulation.Collision.SphereCollider(c, 0.25f));
    }

    public void GetColliderVisualsFor(int clothIndex, List<ColliderViz> dst)
    {
        dst.Clear();
        dst.Add(new ColliderViz { Kind = ColliderKind.Plane, Normal = new Vector3(0,1,0), Offset = 0f });
        int ix = clothIndex % Gx;
        int iz = clothIndex / Gx;
        var basePos = new Vector3((ix - (Gx-1)*0.5f) * InstancePitch, 0.4f, (iz - (Gz-1)*0.5f) * InstancePitch);
        float pinnedZ = (Ny - 1) * 0.5f * GridSpacing;
        basePos.Z += pinnedZ;
        float phase = (float)(0.5 * clothIndex);
        float t = _time + phase;
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
