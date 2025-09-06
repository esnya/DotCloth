using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Collision;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class SimulationStabilityTests
{
    private static (Vector3[] positions, int[] triangles) MakeGrid(int nx, int ny, float spacing, float y, bool flipZ)
    {
        var positions = new Vector3[nx * ny];
        for (int gy = 0; gy < ny; gy++)
        for (int gx = 0; gx < nx; gx++)
        {
            float px = (gx - (nx - 1) * 0.5f) * spacing;
            float py = y;
            float pz = (gy - (ny - 1) * 0.5f) * spacing;
            if (flipZ) pz = -pz;
            positions[gy * nx + gx] = new Vector3(px, py, pz);
        }

        var triangles = new int[(nx - 1) * (ny - 1) * 6];
        int t = 0;
        for (int gy = 0; gy < ny - 1; gy++)
        for (int gx = 0; gx < nx - 1; gx++)
        {
            int i0 = gy * nx + gx;
            int i1 = gy * nx + (gx + 1);
            int i2 = (gy + 1) * nx + gx;
            int i3 = (gy + 1) * nx + (gx + 1);
            triangles[t++] = i0; triangles[t++] = i2; triangles[t++] = i1;
            triangles[t++] = i2; triangles[t++] = i3; triangles[t++] = i1;
        }
        return (positions, triangles);
    }

    private static (int i, int j, float rest)[] BuildEdges(ReadOnlySpan<Vector3> pos, ReadOnlySpan<int> tris)
    {
        var set = new HashSet<(int, int)>();
        var edges = new List<(int, int, float)>();
        for (int ti = 0; ti < tris.Length; ti += 3)
        {
            AddEdge(tris[ti], tris[ti + 1], pos, set, edges);
            AddEdge(tris[ti + 1], tris[ti + 2], pos, set, edges);
            AddEdge(tris[ti + 2], tris[ti], pos, set, edges);
        }
        return edges.ToArray();
    }

    private static void AddEdge(int a, int b, ReadOnlySpan<Vector3> pos,
        HashSet<(int, int)> set, List<(int, int, float)> edges)
    {
        int i = a < b ? a : b;
        int j = a < b ? b : a;
        if (set.Add((i, j)))
        {
            float rest = Vector3.Distance(pos[i], pos[j]);
            edges.Add((i, j, rest));
        }
    }

    private static float CenterY(ReadOnlySpan<Vector3> pos)
    {
        float sum = 0f;
        for (int i = 0; i < pos.Length; i++) sum += pos[i].Y;
        return sum / pos.Length;
    }

    [Fact]
    public void MonoGameMinimalParameters_ShouldBeStable()
    {
        const int n = 20;
        var (pos, tri) = MakeGrid(n, n, 0.1f, 1.5f, false);
        var vel = new Vector3[pos.Length];
        var parms = new ClothParameters
        {
            VertexMass = 1f,
            Damping = 0.05f,
            AirDrag = 0.2f,
            StretchStiffness = 0.9f,
            BendStiffness = 0.1f,
            GravityScale = 1f,
            UseGravity = true,
            Substeps = 1,
            Iterations = 8,
        };
        var solver = new VelocityImpulseSolver();
        solver.Initialize(pos, tri, parms);
        var pins = new int[n];
        for (int i = 0; i < n; i++) pins[i] = (n - 1) * n + i;
        solver.PinVertices(pins);
        solver.SetColliders(new ICollider[] { new PlaneCollider(new Vector3(0, 1, 0), 0f) });
        var edges = BuildEdges(pos, tri);
        float initialY = CenterY(pos);
        for (int step = 0; step < 200; step++) solver.Step(1f / 60f, pos, vel);

        AssertAllFinite(pos);
        Assert.True(MaxAbs(pos) < 10f);
        Assert.True(MinEdgeRatio(edges, pos) > 0.5f);
        Assert.True(CenterY(pos) <= initialY + 0.01f);
    }

    [Fact]
    public void GodotMinimalParameters_ShouldBeStable()
    {
        const int n = 32;
        var (pos, tri) = MakeGrid(n, n, 0.05f, 0f, true);
        var vel = new Vector3[pos.Length];
        var parms = new ClothParameters
        {
            UseGravity = true,
            StretchStiffness = 0.9f,
            BendStiffness = 0.5f,
            Iterations = 10,
        };
        var solver = new VelocityImpulseSolver();
        solver.Initialize(pos, tri, parms);
        solver.PinVertices(Enumerable.Range(0, n).ToArray());
        solver.SetColliders(new ICollider[] { new PlaneCollider(new Vector3(0, 1, 0), -0.8f) });
        var edges = BuildEdges(pos, tri);
        float initialY = CenterY(pos);
        for (int step = 0; step < 200; step++) solver.Step(1f / 60f, pos, vel);

        AssertAllFinite(pos);
        Assert.True(MaxAbs(pos) < 10f);
        Assert.True(MinEdgeRatio(edges, pos) > 0.5f);
        Assert.True(CenterY(pos) <= initialY + 0.01f);
    }

    private static void AssertAllFinite(ReadOnlySpan<Vector3> pos)
    {
        for (int i = 0; i < pos.Length; i++)
        {
            var p = pos[i];
            Assert.False(float.IsNaN(p.X) || float.IsInfinity(p.X));
            Assert.False(float.IsNaN(p.Y) || float.IsInfinity(p.Y));
            Assert.False(float.IsNaN(p.Z) || float.IsInfinity(p.Z));
        }
    }

    private static float MaxAbs(ReadOnlySpan<Vector3> pos)
    {
        float max = 0f;
        for (int i = 0; i < pos.Length; i++)
        {
            var p = pos[i];
            max = MathF.Max(max, MathF.Abs(p.X));
            max = MathF.Max(max, MathF.Abs(p.Y));
            max = MathF.Max(max, MathF.Abs(p.Z));
        }
        return max;
    }

    private static float MinEdgeRatio((int i, int j, float rest)[] edges, ReadOnlySpan<Vector3> pos)
    {
        float min = float.MaxValue;
        foreach (var (i, j, rest) in edges)
        {
            float len = Vector3.Distance(pos[i], pos[j]);
            float ratio = len / rest;
            if (ratio < min) min = ratio;
        }
        return min;
    }
}
