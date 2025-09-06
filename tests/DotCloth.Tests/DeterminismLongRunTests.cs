using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class DeterminismLongRunTests
{
    [Fact]
    public void Determinism_HoldsOverManySteps()
    {
        var positions = new[]
        {
            new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(0,-1,0), new Vector3(1,-1,0)
        };
        var tris = new[] { 0,1,2, 2,1,3 };
        var p = new ClothParameters
        {
            UseGravity = true,
            GravityScale = 1.0f,
            StretchStiffness = 0.9f,
            BendStiffness = 0.5f,
            Iterations = 10,
            Substeps = 2,
            RandomAcceleration = 0f,
        };
        var s1 = new VelocityImpulseSolver();
        var s2 = new VelocityImpulseSolver();

        var aPos = (Vector3[])positions.Clone();
        var bPos = (Vector3[])positions.Clone();
        var aVel = new Vector3[aPos.Length];
        var bVel = new Vector3[bPos.Length];

        s1.Initialize(aPos, tris, p);
        s2.Initialize(bPos, tris, p);

        float dt = 0.008f;
        for (int k = 0; k < 200; k++)
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
}

