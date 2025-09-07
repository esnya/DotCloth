using Godot;
using System;

namespace DotCloth.GodotSample;

/// <summary>
/// Procedurally generates a seamless open cylinder without caps.
/// </summary>
[Tool]
public partial class TubeMesh : PrimitiveMesh
{
    [Export] public int RadialSegments { get; set; } = 20;
    [Export] public int HeightSegments { get; set; } = 15;
    [Export] public float Radius { get; set; } = 0.5f;
    [Export] public float Height { get; set; } = 1.0f;

    public override Godot.Collections.Array _CreateMeshArray()
    {
        int rs = Math.Max(3, RadialSegments);
        int hs = Math.Max(1, HeightSegments);
        int rings = hs + 1;
        var verts = new Vector3[rs * rings];
        float halfH = Height * 0.5f;
        for (int y = 0; y < rings; y++)
        {
            float py = -halfH + Height * y / hs;
            for (int r = 0; r < rs; r++)
            {
                float ang = Mathf.Tau * r / rs;
                float x = Radius * Mathf.Cos(ang);
                float z = Radius * Mathf.Sin(ang);
                verts[y * rs + r] = new Vector3(x, py, z);
            }
        }
        var tris = new int[rs * hs * 6];
        int t = 0;
        for (int y = 0; y < hs; y++)
        {
            int y0 = y * rs;
            int y1 = (y + 1) * rs;
            for (int r = 0; r < rs; r++)
            {
                int r0 = r;
                int r1 = (r + 1) % rs;
                int i00 = y0 + r0;
                int i01 = y0 + r1;
                int i10 = y1 + r0;
                int i11 = y1 + r1;
                tris[t++] = i00; tris[t++] = i10; tris[t++] = i01;
                tris[t++] = i10; tris[t++] = i11; tris[t++] = i01;
            }
        }
        var arr = new Godot.Collections.Array();
        arr.Resize((int)Mesh.ArrayType.Max);
        arr[(int)Mesh.ArrayType.Vertex] = verts;
        arr[(int)Mesh.ArrayType.Index] = tris;
        return arr;
    }
}
