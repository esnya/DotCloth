using System.Numerics;

namespace DotCloth.Forces;

/// <summary>Simple co-rotational FEM for triangle elements.</summary>
public sealed class CoRotationalFemForce : IForce
{
    private readonly Triangle[] _tris;

    /// <summary>Triangle element with precomputed rest state.</summary>
    public readonly struct Triangle
    {
        /// <summary>Initializes a triangle.</summary>
        public Triangle(int i0, int i1, int i2, Vector3 p0, Vector3 p1, Vector3 p2, float stiffness)
        {
            I0 = i0; I1 = i1; I2 = i2; Stiffness = stiffness;
            var e1 = p1 - p0;
            var e2 = p2 - p0;
            var det = e1.X * e2.Y - e1.Y * e2.X;
            InvRest = new Matrix2x2(e2.Y / det, -e2.X / det, -e1.Y / det, e1.X / det);
            Area = 0.5f * MathF.Abs(det);
        }

        /// <summary>Index of first vertex.</summary>
        public int I0 { get; }

        /// <summary>Index of second vertex.</summary>
        public int I1 { get; }

        /// <summary>Index of third vertex.</summary>
        public int I2 { get; }

        /// <summary>Linear stiffness coefficient.</summary>
        public float Stiffness { get; }

        /// <summary>Inverse rest shape matrix.</summary>
        public Matrix2x2 InvRest { get; }

        /// <summary>Rest area of the triangle.</summary>
        public float Area { get; }
    }

    /// <summary>2x2 matrix used for rest-state transforms.</summary>
    public readonly struct Matrix2x2
    {
        /// <summary>Initializes the matrix.</summary>
        public Matrix2x2(float m11, float m12, float m21, float m22)
        { M11 = m11; M12 = m12; M21 = m21; M22 = m22; }

        /// <summary>Row 1 column 1.</summary>
        public float M11 { get; }

        /// <summary>Row 1 column 2.</summary>
        public float M12 { get; }

        /// <summary>Row 2 column 1.</summary>
        public float M21 { get; }

        /// <summary>Row 2 column 2.</summary>
        public float M22 { get; }
    }

    /// <summary>Creates the force from triangle elements.</summary>
    public CoRotationalFemForce(Triangle[] tris)
    {
        _tris = (Triangle[])tris.Clone();
    }

    /// <inheritdoc />
    public void Accumulate(Vector3[] positions, Vector3[] forces)
    {
        foreach (var t in _tris)
        {
            var p0 = positions[t.I0];
            var p1 = positions[t.I1];
            var p2 = positions[t.I2];

            var ds1 = p1 - p0;
            var ds2 = p2 - p0;
            var f1 = ds1 * t.InvRest.M11 + ds2 * t.InvRest.M21;
            var f2 = ds1 * t.InvRest.M12 + ds2 * t.InvRest.M22;

            var r1 = Vector3.Normalize(f1);
            var temp = f2 - Vector3.Dot(f2, r1) * r1;
            var r2 = Vector3.Normalize(temp);
            var diff1 = f1 - r1;
            var diff2 = f2 - r2;

            var h1 = new Vector2(-t.InvRest.M11 - t.InvRest.M21, -t.InvRest.M12 - t.InvRest.M22);
            var h2 = new Vector2(t.InvRest.M11, t.InvRest.M12);
            var h3 = new Vector2(t.InvRest.M21, t.InvRest.M22);

            var scale = -t.Stiffness * t.Area;
            forces[t.I0] += scale * (diff1 * h1.X + diff2 * h1.Y);
            forces[t.I1] += scale * (diff1 * h2.X + diff2 * h2.Y);
            forces[t.I2] += scale * (diff1 * h3.X + diff2 * h3.Y);
        }
    }
}
