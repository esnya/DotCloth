using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class StabilityExtremeParamsTests
{
    [Fact]
    public void ExtremeParameters_DoNotExplodeOrNaN()
    {
        var positions = new[]
        {
            new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(0,-1,0), new Vector3(1,-1,0)
        };
        var tris = new[] { 0,1,2, 2,1,3 };
        var p = new ClothParameters
        {
            UseGravity = true,
            GravityScale = 10f,
            Damping = 0.99f,
            AirDrag = 1.0f,
            StretchStiffness = 1.0f,
            BendStiffness = 1.0f,
            TetherStiffness = 1.0f,
            Iterations = 30,
            Substeps = 4,
            ComplianceScale = 1e-6f,
        };
        var s = new PbdSolver();
        var pos = (Vector3[])positions.Clone();
        var vel = new Vector3[pos.Length];
        s.Initialize(pos, tris, p);
        s.PinVertices(0); // stabilize with a pin
        s.SetTetherAnchors(new [] { 0 });
        float dt = 0.005f;
        for (int k = 0; k < 500; k++)
        {
            s.Step(dt, pos, vel);
        }
        for (int i = 0; i < pos.Length; i++)
        {
            Assert.False(float.IsNaN(pos[i].X) || float.IsInfinity(pos[i].X));
            Assert.False(float.IsNaN(pos[i].Y) || float.IsInfinity(pos[i].Y));
            Assert.False(float.IsNaN(pos[i].Z) || float.IsInfinity(pos[i].Z));
        }
    }
}

