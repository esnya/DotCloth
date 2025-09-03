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
        var set = new HashSet<(int,int)>();
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

    private static void AddEdge(HashSet<(int,int)> set, int a, int b)
    {
        if (a < b) set.Add((a, b)); else set.Add((b, a));
    }
}

