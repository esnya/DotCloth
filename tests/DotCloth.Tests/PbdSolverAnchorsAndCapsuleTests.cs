using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using DotCloth.Simulation.Collision;
using Xunit;

namespace DotCloth.Tests;

public class PbdSolverAnchorsAndCapsuleTests
{
    [Fact]
    public void TetherAnchors_PullsTowardAnchorWithScale()
    {
        var positions = new[]
        {
            new Vector3(0, 0, 0), // anchor candidate 0
            new Vector3(1, 0, 0),
            new Vector3(2, 0, 0),
        };
        var tris = new[] { 0,1,2 }; // degenerate single tri okay for building
        var p = new ClothParameters { UseGravity = false, TetherStiffness = 1.0f, TetherLengthScale = 0.5f, Iterations = 20 };
        var solver = new PbdSolver();
        var v = new Vector3[positions.Length];
        solver.Initialize(positions, tris, p);
        // Pin vertex 0, set as anchor
        solver.PinVertices(0);
        solver.SetTetherAnchors(new [] { 0 });

        var workPos = new[] { new Vector3(0,0,0), new Vector3(1.8f, 0, 0), new Vector3(2.6f, 0, 0) };
        var workVel = new Vector3[positions.Length];

        float initialD = Vector3.Distance(workPos[1], workPos[0]);
        float target = Vector3.Distance(positions[1], positions[0]) * p.TetherLengthScale;

        solver.Step(0.02f, workPos, workVel);

        float afterD = Vector3.Distance(workPos[1], workPos[0]);
        Assert.True(afterD <= initialD);
        Assert.True(afterD <= target + 1e-3f);
    }

    [Fact]
    public void CapsuleCollider_PushesOutOfSegmentRadius()
    {
        var positions = new[] { new Vector3(0.5f, 0.1f, 0) };
        var velocities = new[] { Vector3.Zero };
        var tris = Array.Empty<int>();
        var p = new ClothParameters { UseGravity = false };
        var solver = new PbdSolver();
        solver.Initialize(positions, tris, p);
        solver.SetColliders(new [] { new CapsuleCollider(new Vector3(0,0,0), new Vector3(1,0,0), 0.2f) });

        solver.Step(0.016f, positions, velocities);

        // Distance to segment should be >= radius
        var x = positions[0];
        var p0 = new Vector3(0,0,0);
        var p1 = new Vector3(1,0,0);
        var seg = p1 - p0;
        float t = Vector3.Dot(x - p0, seg) / seg.LengthSquared();
        t = Math.Clamp(t, 0f, 1f);
        var c = p0 + seg * t;
        var dist = (x - c).Length();
        Assert.True(dist >= 0.2f - 1e-5f);
    }

    [Fact]
    public void Initialize_ThrowsOnInvalidTriangleIndex()
    {
        var positions = new[] { new Vector3(0,0,0) };
        var tris = new[] { 0, 1, 2 }; // invalid
        var solver = new PbdSolver();
        var p = new ClothParameters();
        Assert.Throws<ArgumentOutOfRangeException>(() => solver.Initialize(positions, tris, p));
    }
}
