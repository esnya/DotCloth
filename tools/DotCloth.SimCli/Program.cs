using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;

namespace DotCloth.SimCli;

public static class Program
{
    private sealed record Options(int Size, float Stretch, float Bend, int Iterations, int Steps, float Dt, string Output, bool IncludeAll);

    public static void Main(string[] args)
    {
        var opts = ParseArgs(args);
        float spacing = 0.1f;
        var (pos, tris) = MakeGrid(opts.Size, spacing);
        var vel = new Vector3[pos.Length];
        var parms = new ClothParameters
        {
            UseGravity = true,
            GravityScale = 1f,
            Damping = 0.02f,
            AirDrag = 0.02f,
            StretchStiffness = opts.Stretch,
            BendStiffness = opts.Bend,
            Iterations = opts.Iterations,
            Substeps = 1,
            Friction = 0.2f,
            CollisionThickness = 0.005f,
        };
        var solver = new PbdSolver();
        solver.Initialize(pos, tris, parms);
        var pins = new int[opts.Size];
        for (int i = 0; i < opts.Size; i++) pins[i] = (opts.Size - 1) * opts.Size + i;
        solver.PinVertices(pins);

        var edges = UniqueEdges(tris).ToArray();
        var restLen = new float[edges.Length];
        for (int e = 0; e < edges.Length; e++)
        {
            var (i, j) = edges[e];
            restLen[e] = Vector3.Distance(pos[i], pos[j]);
        }
        var restAngles = new float[tris.Length];
        ComputeAngles(pos, tris, restAngles);
        var curAngles = new float[restAngles.Length];

        Directory.CreateDirectory(Path.GetDirectoryName(opts.Output)!);
        using var w = new StreamWriter(opts.Output);
        w.WriteLine("step,avgStretch,angleVar");
        for (int step = 0; step < opts.Steps; step++)
        {
            solver.Step(opts.Dt, pos, vel);
            if (opts.IncludeAll || step == opts.Steps - 1)
            {
                float avgStretch = AverageStretch(pos, edges, restLen);
                ComputeAngles(pos, tris, curAngles);
                float angleVar = AngleVariance(curAngles, restAngles);
                w.WriteLine($"{step + 1},{avgStretch.ToString(CultureInfo.InvariantCulture)},{angleVar.ToString(CultureInfo.InvariantCulture)}");
            }
        }
    }

    private static Options ParseArgs(string[] args)
    {
        int size = 20;
        float stretch = 0.9f;
        float bend = 0.1f;
        int iterations = 8;
        int steps = 600;
        float dt = 1f / 60f;
        string output = Path.Combine("sim-out", "output.csv");
        bool includeAll = false;
        foreach (var a in args)
        {
            if (a.StartsWith("--size=")) size = int.Parse(a[7..], CultureInfo.InvariantCulture);
            else if (a.StartsWith("--stretch=")) stretch = float.Parse(a[10..], CultureInfo.InvariantCulture);
            else if (a.StartsWith("--bend=")) bend = float.Parse(a[7..], CultureInfo.InvariantCulture);
            else if (a.StartsWith("--iterations=")) iterations = int.Parse(a[13..], CultureInfo.InvariantCulture);
            else if (a.StartsWith("--steps=")) steps = int.Parse(a[8..], CultureInfo.InvariantCulture);
            else if (a.StartsWith("--dt=")) dt = float.Parse(a[5..], CultureInfo.InvariantCulture);
            else if (a.StartsWith("--output=")) output = a[9..];
            else if (a == "--include-all") includeAll = true;
        }
        return new Options(size, stretch, bend, iterations, steps, dt, output, includeAll);
    }

    private static (Vector3[] pos, int[] tris) MakeGrid(int n, float spacing)
    {
        var pos = new Vector3[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                pos[y * n + x] = new Vector3(x * spacing, (n - 1 - y) * spacing, 0);
            }
        }
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
            Add(a, b); Add(b, c); Add(c, a);
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
        float cos = (adj1 * adj1 + adj2 * adj2 - opp * opp) / (2 * adj1 * adj2);
        cos = Math.Clamp(cos, -1f, 1f);
        return MathF.Acos(cos);
    }

    private static float AverageStretch(Vector3[] pos, (int i, int j)[] edges, float[] rest)
    {
        float sum = 0f;
        for (int e = 0; e < edges.Length; e++)
        {
            var (i, j) = edges[e];
            float L = Vector3.Distance(pos[i], pos[j]);
            sum += L / rest[e];
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

