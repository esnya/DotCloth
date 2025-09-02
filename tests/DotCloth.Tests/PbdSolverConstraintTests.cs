using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Collision;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class PbdSolverConstraintTests
{
    private static (Vector3[] positions, int[] triangles) MakeQuad()
    {
        // 0----1
        // |  / |
        // | /  |
        // 2----3
        var positions = new[]
        {
            new Vector3(0, 0, 0), //0
            new Vector3(1, 0, 0), //1
            new Vector3(0, -1, 0), //2
            new Vector3(1, -1, 0), //3
        };
        var triangles = new[] { 0,1,2, 2,1,3 };
        return (positions, triangles);
    }

    [Fact]
    public void StretchConstraint_ReducesEdgeViolation()
    {
        var (pos0, tris) = MakeQuad();
        var p = new ClothParameters { UseGravity = false, StretchStiffness = 1.0f, Iterations = 20, Substeps = 1 };
        var solver = new PbdSolver();
        var v = new Vector3[pos0.Length];
        solver.Initialize(pos0, tris, p);

        // Impart velocity to stretch edge (0-1)
        var positions = (Vector3[])pos0.Clone();
        var velocities = (Vector3[])v.Clone();
        velocities[1] = new Vector3(5, 0, 0);

        // Predict unconstrained distance after dt
        float dt = 0.01f;
        var unconstrainedP1 = positions[1] + velocities[1] * dt;
        var restLen = Vector3.Distance(pos0[0], pos0[1]);
        var unconstrainedLen = Vector3.Distance(positions[0], unconstrainedP1);

        solver.Step(dt, positions, velocities);
        var constrainedLen = Vector3.Distance(positions[0], positions[1]);

        Assert.True(MathF.Abs(constrainedLen - restLen) < MathF.Abs(unconstrainedLen - restLen));
    }

    [Fact]
    public void MoreIterations_MonotonicallyReduceStretchViolation()
    {
        var (pos0, tris) = MakeQuad();
        var v0 = new Vector3[pos0.Length];
        float dt = 0.01f;

        float RunWithIterations(int iters)
        {
            var p = new ClothParameters { UseGravity = false, StretchStiffness = 1.0f, Iterations = iters, Substeps = 1 };
            var solver = new PbdSolver();
            var positions = (Vector3[])pos0.Clone();
            var velocities = (Vector3[])v0.Clone();
            velocities[1] = new Vector3(5, 0, 0);
            solver.Initialize(positions, tris, p);
            solver.Step(dt, positions, velocities);
            return MathF.Abs(Vector3.Distance(positions[0], positions[1]) - Vector3.Distance(pos0[0], pos0[1]));
        }

        var v10 = RunWithIterations(10);
        var v20 = RunWithIterations(20);
        Assert.True(v20 <= v10 + 1e-6f);
    }

    [Fact]
    public void Determinism_FixedInputs_YieldsSameResults()
    {
        var (pos0, tris) = MakeQuad();
        var p = new ClothParameters { UseGravity = true, GravityScale = 1.0f, StretchStiffness = 0.8f, Iterations = 10, Substeps = 2 };
        var aPos = (Vector3[])pos0.Clone();
        var bPos = (Vector3[])pos0.Clone();
        var aVel = new Vector3[pos0.Length];
        var bVel = new Vector3[pos0.Length];

        var s1 = new PbdSolver();
        var s2 = new PbdSolver();
        s1.Initialize(aPos, tris, p);
        s2.Initialize(bPos, tris, p);

        float dt = 0.008f;
        for (int k = 0; k < 10; k++)
        {
            s1.Step(dt, aPos, aVel);
            s2.Step(dt, bPos, bVel);
        }

        for (int i = 0; i < aPos.Length; i++)
        {
            Assert.Equal(aPos[i].X, bPos[i].X, 6);
            Assert.Equal(aPos[i].Y, bPos[i].Y, 6);
            Assert.Equal(aPos[i].Z, bPos[i].Z, 6);
            Assert.Equal(aVel[i].X, bVel[i].X, 6);
            Assert.Equal(aVel[i].Y, bVel[i].Y, 6);
            Assert.Equal(aVel[i].Z, bVel[i].Z, 6);
        }
    }

    [Fact]
    public void PlaneCollider_PushesOutFromPlane()
    {
        var positions = new[] { new Vector3(0, -0.1f, 0) };
        var velocities = new[] { Vector3.Zero };
        var tris = Array.Empty<int>();
        var p = new ClothParameters { UseGravity = false };
        var solver = new PbdSolver();
        solver.Initialize(positions, tris, p);
        solver.SetColliders(new [] { new PlaneCollider(new Vector3(0,1,0), 0f) });

        solver.Step(0.016f, positions, velocities);

        Assert.True(positions[0].Y >= -1e-6f);
    }
}
