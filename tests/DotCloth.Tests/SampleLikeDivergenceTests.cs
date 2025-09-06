using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using DotCloth.Simulation.Collision;
using Xunit;

namespace DotCloth.Tests;

public class SampleLikeDivergenceTests
{
    private static (Vector3[] pos, int[] tris) MakeGrid(int n, float spacing)
    {
        var pos = new Vector3[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                pos[y * n + x] = new Vector3(x * spacing, (n - 1 - y) * spacing, 0);
        var tris = new int[(n - 1) * (n - 1) * 6];
        int t = 0;
        for (int y = 0; y < n - 1; y++)
        {
            for (int x = 0; x < n - 1; x++)
            {
                int i = y * n + x;
                int iRight = i + 1;
                int iDown = i + n;
                int iDownRight = i + n + 1;
                tris[t++] = i; tris[t++] = iRight; tris[t++] = iDown;
                tris[t++] = iDown; tris[t++] = iRight; tris[t++] = iDownRight;
            }
        }
        return (pos, tris);
    }

    [Fact]
    public void DefaultSolver_GravityAndPlane_NoExplosion_2Seconds()
    {
        int n = 20;
        var (pos0, tris) = MakeGrid(n, 0.1f);
        var vel = new Vector3[pos0.Length];
        var p = new ClothParameters
        {
            UseGravity = true,
            GravityScale = 1.0f,
            Damping = 0.05f,
            AirDrag = 0.05f,
            StretchStiffness = 0.8f,
            BendStiffness = 0.2f,
            Iterations = 10,
            Substeps = 2,
        };
        var sim = new PbdSolver();
        sim.Initialize(pos0, tris, p);
        // Pin top row
        var pins = new int[n];
        for (int i = 0; i < n; i++) pins[i] = (n - 1) * n + i;
        sim.PinVertices(pins);
        sim.SetColliders(new[] { new PlaneCollider(new Vector3(0, 1, 0), 0f) });

        var pos = (Vector3[])pos0.Clone();
        float dt = 1f / 60f;
        float maxSpeed = 0f;
        for (int i = 0; i < 120; i++) // 2 seconds with Substeps=2
        {
            sim.Step(dt, pos, vel);
            for (int k = 0; k < pos.Length; k++)
            {
                Assert.False(float.IsNaN(pos[k].X) || float.IsInfinity(pos[k].X));
                Assert.False(float.IsNaN(vel[k].X) || float.IsInfinity(vel[k].X));
                maxSpeed = MathF.Max(maxSpeed, vel[k].Length());
                // Position shouldn't escape a generous AABB quickly
                Assert.True(MathF.Abs(pos[k].X) < 50f && MathF.Abs(pos[k].Y) < 50f);
            }
        }
        Assert.True(maxSpeed < 20.0f);
    }

#if DOTCLOTH_EXPERIMENTAL_XPBD
    [Fact]
    public void XpbdSolver_GravityAndPlane_NoExplosion_2Seconds()
    {
        int n = 20;
        var (pos0, tris) = MakeGrid(n, 0.1f);
        var vel = new Vector3[pos0.Length];
        var p = new ClothParameters
        {
            UseGravity = true,
            GravityScale = 1.0f,
            Damping = 0.02f,
            AirDrag = 0.02f,
            StretchStiffness = 0.8f,
            BendStiffness = 0.2f,
            Iterations = 8,
            Substeps = 1,
        };
        var sim = new XpbdSolver();
        sim.Initialize(pos0, tris, p);
        var pins = new int[n];
        for (int i = 0; i < n; i++) pins[i] = (n - 1) * n + i;
        sim.PinVertices(pins);
        sim.SetColliders(new[] { new PlaneCollider(new Vector3(0, 1, 0), 0f) });

        var pos = (Vector3[])pos0.Clone();
        float dt = 1f / 60f;
        float maxSpeed = 0f;
        for (int i = 0; i < 120; i++)
        {
            sim.Step(dt, pos, vel);
            for (int k = 0; k < pos.Length; k++)
            {
                Assert.False(float.IsNaN(pos[k].X) || float.IsInfinity(pos[k].X));
                Assert.False(float.IsNaN(vel[k].X) || float.IsInfinity(vel[k].X));
                maxSpeed = MathF.Max(maxSpeed, vel[k].Length());
                Assert.True(MathF.Abs(pos[k].X) < 50f && MathF.Abs(pos[k].Y) < 50f);
            }
        }
        Assert.True(maxSpeed < 20.0f);
    }
#endif
}

