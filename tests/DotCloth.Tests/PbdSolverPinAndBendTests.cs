using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class PbdSolverPinAndBendTests
{
    private static (Vector3[] positions, int[] triangles) MakeTwoTris()
    {
        // 0---1
        //  \  |
        //   \ |
        //     2
        var positions = new[]
        {
            new Vector3(0, 0, 0), //0
            new Vector3(1, 0, 0), //1
            new Vector3(1, -1, 0), //2
        };
        var triangles = new[] { 0,1,2, 0,2,1 }; // share edges in different winding
        return (positions, triangles);
    }

    [Fact]
    public void Pinning_KeepsVertexStationary()
    {
        var (pos, tris) = MakeTwoTris();
        var p = new ClothParameters { UseGravity = true };
        var solver = new PbdSolver();
        var v = new Vector3[pos.Length];
        solver.Initialize(pos, tris, p);

        // Pin vertex 0
        var inv = new float[pos.Length];
        for (int i = 0; i < inv.Length; i++) inv[i] = 1.0f / p.VertexMass;
        inv[0] = 0f; // pinned
        solver.SetInverseMasses(inv);

        var positions = (Vector3[])pos.Clone();
        var velocities = (Vector3[])v.Clone();

        solver.Step(0.02f, positions, velocities);

        Assert.Equal(pos[0].X, positions[0].X, 6);
        Assert.Equal(pos[0].Y, positions[0].Y, 6);
        Assert.Equal(0f, velocities[0].Length(), 6);
    }

    [Fact]
    public void Bending_RestoresOppositeDistanceTowardRest()
    {
        // Quad from earlier test for clearer bend
        var positions = new[]
        {
            new Vector3(0, 0, 0), //0
            new Vector3(1, 0, 0), //1
            new Vector3(0, -1, 0), //2
            new Vector3(1, -1, 0), //3
        };
        var tris = new[] { 0,1,2, 2,1,3 }; // shared edge (1,2) with opposite vertices 0 and 3
        var p = new ClothParameters { UseGravity = false, BendStiffness = 1.0f, Iterations = 20 };
        var solver = new PbdSolver();
        var v = new Vector3[positions.Length];
        solver.Initialize(positions, tris, p);

        // Move vertex 3 to bend the strip
        var workPos = (Vector3[])positions.Clone();
        var workVel = (Vector3[])v.Clone();
        workVel[3] = new Vector3(0, -5, 0);

        float rest = Vector3.Distance(positions[0], positions[3]);
        float dt = 0.01f;
        var unconstrained = Vector3.Distance(workPos[0], workPos[3] + workVel[3]*dt);

        solver.Step(dt, workPos, workVel);
        float after = Vector3.Distance(workPos[0], workPos[3]);

        Assert.True(MathF.Abs(after - rest) < MathF.Abs(unconstrained - rest));
    }
}

