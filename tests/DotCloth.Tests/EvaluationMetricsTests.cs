using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class EvaluationMetricsTests
{
    [Fact]
    public void ShortRun_MetricsWithinBounds()
    {
        const int size = 8;
        const int steps = 30;
        const float dt = 1f / 60f;
        var (positions, triangles) = MakeGrid(size, 0.1f);
        var velocities = new Vector3[positions.Length];
        var parameters = new ClothParameters
        {
            UseGravity = true,
            GravityScale = 1f,
            Damping = 0.02f,
            AirDrag = 0.02f,
            StretchStiffness = 0.9f,
            BendStiffness = 0.1f,
            Iterations = 8,
            Substeps = 1,
            Friction = 0.2f,
            CollisionThickness = 0.005f,
        };
        var solver = new PbdSolver();
        solver.Initialize(positions, triangles, parameters);
        var pins = new int[size];
        for (int i = 0; i < size; i++) pins[i] = (size - 1) * size + i;
        solver.PinVertices(pins);

        var edges = UniqueEdges(triangles).ToArray();
        var restLen = new float[edges.Length];
        for (int e = 0; e < edges.Length; e++)
        {
            var (i, j) = edges[e];
            restLen[e] = Vector3.Distance(positions[i], positions[j]);
        }
        var restAngles = new float[triangles.Length];
        ComputeAngles(positions, triangles, restAngles);
        var curAngles = new float[restAngles.Length];

        for (int step = 0; step < steps; step++)
        {
            solver.Step(dt, positions, velocities);
        }
        float avgStretch = AverageStretch(positions, edges, restLen);
        ComputeAngles(positions, triangles, curAngles);
        float angleVar = AngleVariance(curAngles, restAngles);

#if DOTCLOTH_EXPERIMENTAL_XPBD
        Assert.InRange(avgStretch, 0.98f, 1.02f);
        Assert.True(angleVar < 0.0001f);
#else
        Assert.InRange(avgStretch, 0.95f, 1.05f);
        Assert.True(angleVar < 0.01f);
#endif
    }

    private static (Vector3[] pos, int[] tris) MakeGrid(int n, float spacing)
    {
        var pos = new Vector3[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                pos[y * n + x] = new Vector3(x * spacing, (n - 1 - y) * spacing, 0f);
            }
        }
        var tris = new int[(n - 1) * (n - 1) * 6];
        int t = 0;
        for (int y = 0; y < n - 1; y++)
        {
            for (int x = 0; x < n - 1; x++)
            {
                int i = y * n + x;
                int ir = i + 1;
                int id = i + n;
                int idr = i + n + 1;
                tris[t++] = i; tris[t++] = ir; tris[t++] = id;
                tris[t++] = id; tris[t++] = ir; tris[t++] = idr;
            }
        }
        return (pos, tris);
    }

    private static IEnumerable<(int i, int j)> UniqueEdges(ReadOnlySpan<int> tris)
    {
        var set = new HashSet<(int, int)>();
        for (int t = 0; t < tris.Length; t += 3)
        {
            int a = tris[t];
            int b = tris[t + 1];
            int c = tris[t + 2];
            void Add(int u, int v)
            {
                int i = Math.Min(u, v);
                int j = Math.Max(u, v);
                set.Add((i, j));
            }
            Add(a, b);
            Add(b, c);
            Add(c, a);
        }
        return set;
    }

    private static void ComputeAngles(Vector3[] pos, int[] tris, float[] dst)
    {
        for (int t = 0, ai = 0; t < tris.Length; t += 3)
        {
            int i0 = tris[t];
            int i1 = tris[t + 1];
            int i2 = tris[t + 2];
            float a = Vector3.Distance(pos[i1], pos[i2]);
            float b = Vector3.Distance(pos[i0], pos[i2]);
            float c = Vector3.Distance(pos[i0], pos[i1]);
            dst[ai++] = AngleFromLengths(b, c, a);
            dst[ai++] = AngleFromLengths(a, c, b);
            dst[ai++] = AngleFromLengths(a, b, c);
        }
    }

    private static float AngleFromLengths(float adj1, float adj2, float opp)
    {
        float cos = (adj1 * adj1 + adj2 * adj2 - opp * opp) / (2f * adj1 * adj2);
        cos = Math.Clamp(cos, -1f, 1f);
        return MathF.Acos(cos);
    }

    private static float AverageStretch(Vector3[] pos, (int i, int j)[] edges, float[] rest)
    {
        float sum = 0f;
        for (int e = 0; e < edges.Length; e++)
        {
            var (i, j) = edges[e];
            float len = Vector3.Distance(pos[i], pos[j]);
            sum += len / rest[e];
        }
        return edges.Length > 0 ? sum / edges.Length : 0f;
    }

    private static float AngleVariance(float[] cur, float[] rest)
    {
        float mean = 0f;
        int n = cur.Length;
        for (int i = 0; i < n; i++)
        {
            mean += cur[i] - rest[i];
        }
        mean /= n;
        float var = 0f;
        for (int i = 0; i < n; i++)
        {
            float d = (cur[i] - rest[i]) - mean;
            var += d * d;
        }
        return var / n;
    }
}
