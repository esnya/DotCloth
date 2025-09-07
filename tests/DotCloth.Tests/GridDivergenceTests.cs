using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class GridDivergenceTests
{
    private static (Vector3[] pos, int[] tris) MakeGrid(int n, float spacing)
    {
        var pos = new Vector3[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                pos[y * n + x] = new Vector3(x * spacing, -y * spacing, 0);
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
    public void DefaultSolver_Grid_NoExplode_NoNaN()
    {
        var (pos0, tris) = MakeGrid(16, 0.05f);
        var vel = new Vector3[pos0.Length];
        var p = new ClothParameters
        {
            UseGravity = false,
            Damping = 0.01f,
            AirDrag = 0.01f,
            StretchStiffness = 0.8f,
            BendStiffness = 0.3f,
            Iterations = 8,
            Substeps = 1,
        };
        var solver = new PbdSolver();
        solver.Initialize(pos0, tris, p);
        solver.PinVertices(0, 15); // two corners

        var pos = (Vector3[])pos0.Clone();
        float maxSpeed = 0f;
        for (int i = 0; i < 240; i++)
        {
            solver.Step(1f / 120f, pos, vel);
            for (int k = 0; k < pos.Length; k++)
            {
                Assert.False(float.IsNaN(pos[k].X) || float.IsInfinity(pos[k].X));
                maxSpeed = MathF.Max(maxSpeed, vel[k].Length());
            }
        }
        // Velocity stays bounded by a modest envelope (empirical safety net)
        Assert.True(maxSpeed < 5.0f);
    }

#if DOTCLOTH_EXPERIMENTAL_XPBD
    [Fact]
    public void XpbdSolver_Grid_NoExplode_NoNaN()
    {
        var (pos0, tris) = MakeGrid(16, 0.05f);
        var vel = new Vector3[pos0.Length];
        var p = new ClothParameters
        {
            UseGravity = false,
            Damping = 0.0f,
            AirDrag = 0.0f,
            StretchStiffness = 0.8f,
            BendStiffness = 0.3f,
            Iterations = 8,
            Substeps = 1,
        };
        var solver = new XpbdSolver();
        solver.Initialize(pos0, tris, p);
        solver.PinVertices(0, 15);

        var pos = (Vector3[])pos0.Clone();
        float maxSpeed = 0f;
        for (int i = 0; i < 240; i++)
        {
            solver.Step(1f / 120f, pos, vel);
            for (int k = 0; k < pos.Length; k++)
            {
                Assert.False(float.IsNaN(pos[k].X) || float.IsInfinity(pos[k].X));
                maxSpeed = MathF.Max(maxSpeed, vel[k].Length());
            }
        }
        Assert.True(maxSpeed < 5.0f);
    }
#endif
}

