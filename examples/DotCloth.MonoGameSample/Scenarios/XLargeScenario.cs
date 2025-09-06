using System.Collections.Generic;
using System.Numerics;
using DotCloth.Simulation;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;

namespace DotCloth.MonoGameSample.Scenarios;

internal sealed class XLargeScenario : IScenario
{
    public string Name => "X Large";
    public IReadOnlyList<ClothSim> Cloths => _cloths;
    private readonly List<ClothSim> _cloths = new();
    private float _time;
    private const int Gx = 6, Gz = 5;
    private const int Nx = 20, Ny = 20;
    private const float GridSpacing = 0.1f;
    private const float InstancePitch = 2.4f;

    public void Initialize()
    {
        _cloths.Clear();
        _time = 0f;
        // Heavier grid: more instances and larger cloths
        // 30 instances
        for (int z = 0; z < Gz; z++)
            for (int x = 0; x < Gx; x++)
            {
                Geometry.MakeGrid(Nx, Ny, GridSpacing, out var pos, out var tri);
                var vel = new Vector3[pos.Length];
                // Only horizontal offset; keep height same as Minimal (~1.5)
                var offset = new Vector3((x - (Gx - 1) * 0.5f) * InstancePitch, 0.0f, (z - (Gz - 1) * 0.5f) * InstancePitch);
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
        int ix = clothIndex % Gx;
        int iz = clothIndex / Gx;
        var basePos = new Vector3((ix - (Gx - 1) * 0.5f) * InstancePitch, 0.45f, (iz - (Gz - 1) * 0.5f) * InstancePitch);
        float pinnedZ = (Ny - 1) * 0.5f * GridSpacing;
        basePos.Z += pinnedZ;
        float phase = (float)(0.4 * clothIndex);
        float t = _time + phase;
        var c = basePos + new Vector3(0.6f * MathF.Sin(1.1f * t), 0.12f + 0.18f * MathF.Cos(0.8f * t), 0.6f * MathF.Cos(0.9f * t));
        dst.Add(new DotCloth.Simulation.Collision.SphereCollider(c, 0.28f));
    }

    public void GetColliderVisualsFor(int clothIndex, List<ColliderViz> dst)
    {
        dst.Clear();
        dst.Add(new ColliderViz { Kind = ColliderKind.Plane, Normal = new Vector3(0, 1, 0), Offset = 0f });
        int ix = clothIndex % Gx;
        int iz = clothIndex / Gx;
        var basePos = new Vector3((ix - (Gx - 1) * 0.5f) * InstancePitch, 0.45f, (iz - (Gz - 1) * 0.5f) * InstancePitch);
        float pinnedZ = (Ny - 1) * 0.5f * GridSpacing;
        basePos.Z += pinnedZ;
        float phase = (float)(0.4 * clothIndex);
        float t = _time + phase;
        var c = basePos + new Vector3(0.6f * MathF.Sin(1.1f * t), 0.12f + 0.18f * MathF.Cos(0.8f * t), 0.6f * MathF.Cos(0.9f * t));
        dst.Add(new ColliderViz { Kind = ColliderKind.Sphere, Center = c, Radius = 0.28f });
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
