using System;
using System.Collections.Generic;
using System.Numerics;

namespace DotCloth.MonoGameSample;

internal static class Geometry
{
    public static void MakeGrid(int nx, int ny, float spacing, out Vector3[] positions, out int[] triangles)
    {
        positions = new Vector3[nx * ny];
        for (int y = 0; y < ny; y++)
            for (int x = 0; x < nx; x++)
            {
                int i = y * nx + x;
                positions[i] = new Vector3((x - (nx - 1) * 0.5f) * spacing,
                                           1.5f,
                                           (y - (ny - 1) * 0.5f) * spacing);
            }

        var tris = new List<int>(6 * (nx - 1) * (ny - 1));
        for (int y = 0; y < ny - 1; y++)
            for (int x = 0; x < nx - 1; x++)
            {
                int i0 = y * nx + x;
                int i1 = y * nx + (x + 1);
                int i2 = (y + 1) * nx + x;
                int i3 = (y + 1) * nx + (x + 1);
                // two triangles: (i0,i2,i1) and (i2,i3,i1)
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i2); tris.Add(i3); tris.Add(i1);
            }
        triangles = tris.ToArray();
    }

    public static void BuildUniqueEdges(ReadOnlySpan<int> triangles, out (int i, int j)[] edges)
    {
        var set = new HashSet<(int, int)>();
        for (int t = 0; t < triangles.Length; t += 3)
        {
            var a = triangles[t];
            var b = triangles[t + 1];
            var c = triangles[t + 2];
            AddEdge(set, a, b);
            AddEdge(set, b, c);
            AddEdge(set, c, a);
        }
        edges = new (int i, int j)[set.Count];
        int k = 0;
        foreach (var e in set) edges[k++] = e;
    }

    private static void AddEdge(HashSet<(int, int)> set, int a, int b)
    {
        if (a < b) set.Add((a, b)); else set.Add((b, a));
    }

    public static void MakeTube(int radial, int heightSeg, float radius, float height, out Vector3[] positions, out int[] triangles)
    {
        radial = Math.Max(3, radial);
        heightSeg = Math.Max(1, heightSeg);
        int rings = heightSeg + 1;
        positions = new Vector3[radial * rings];
        float dy = height / heightSeg;
        for (int y = 0; y < rings; y++)
        {
            float py = -0.5f * height + y * dy;
            for (int r = 0; r < radial; r++)
            {
                float a = (2f * MathF.PI * r) / radial;
                float x = radius * MathF.Cos(a);
                float z = radius * MathF.Sin(a);
                positions[y * radial + r] = new Vector3(x, py, z);
            }
        }
        var tris = new List<int>(radial * heightSeg * 6);
        for (int y = 0; y < heightSeg; y++)
        {
            int y0 = y; int y1 = y + 1;
            for (int r = 0; r < radial; r++)
            {
                int r0 = r;
                int r1 = (r + 1) % radial; // wrap seam
                int i00 = y0 * radial + r0;
                int i01 = y0 * radial + r1;
                int i10 = y1 * radial + r0;
                int i11 = y1 * radial + r1;
                // two triangles per quad
                tris.Add(i00); tris.Add(i10); tris.Add(i01);
                tris.Add(i10); tris.Add(i11); tris.Add(i01);
            }
        }
        triangles = tris.ToArray();
    }
}
