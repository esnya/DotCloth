using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using DotCloth.Simulation.Collision;
using Xunit;

namespace DotCloth.Tests;

public class PbdSolverTetherAndSphereTests
{
    [Fact]
    public void Tether_ReducesDistanceToRest()
    {
        var positions = new[] { new Vector3(0, 0, 0) };
        var velocities = new[] { Vector3.Zero };
        var tris = Array.Empty<int>();
        var p = new ClothParameters { UseGravity = false, TetherStiffness = 1.0f, Iterations = 10 };
        var solver = new PbdSolver();
        solver.Initialize(positions, tris, p);

        var rest = positions[0];
        var pos = new[] { new Vector3(1, 0, 0) };
        var vel = new[] { Vector3.Zero };

        // Allow several steps to observe reduction under velocity-level stabilization
        for (int i = 0; i < 10; i++) solver.Step(0.01f, pos, vel);

        float d0 = Vector3.Distance(rest, new Vector3(1, 0, 0));
        float d1 = Vector3.Distance(rest, pos[0]);
        Assert.True(d1 <= d0 - 1e-4f);
    }

    [Fact]
    public void SphereCollider_PushesOut()
    {
        var positions = new[] { new Vector3(0.1f, 0, 0) };
        var velocities = new[] { Vector3.Zero };
        var tris = Array.Empty<int>();
        var p = new ClothParameters { UseGravity = false };
        var solver = new PbdSolver();
        solver.Initialize(positions, tris, p);
        solver.SetColliders(new[] { new SphereCollider(Vector3.Zero, 0.5f) });

        solver.Step(0.016f, positions, velocities);

        Assert.True(positions[0].Length() >= 0.5f - 1e-5f);
    }
}
