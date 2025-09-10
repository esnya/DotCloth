using System.Numerics;

namespace DotCloth.Constraints;

/// <summary>Clamps edge stretch to prevent over-contraction.</summary>
public sealed class StrainLimiter : IConstraint
{
    /// <summary>Edge record with rest length and stretch limit.</summary>
    public readonly struct Edge
    {
        /// <summary>Initializes the edge.</summary>
        public Edge(int a, int b, float restLength, float maxStretch)
        {
            A = a; B = b; RestLength = restLength; MaxStretch = maxStretch;
        }

        /// <summary>First particle index.</summary>
        public int A { get; }

        /// <summary>Second particle index.</summary>
        public int B { get; }

        /// <summary>Rest edge length.</summary>
        public float RestLength { get; }

        /// <summary>Maximum stretch ratio.</summary>
        public float MaxStretch { get; }
    }

    private readonly Edge[] _edges;

    /// <summary>Creates the limiter for a set of edges.</summary>
    public StrainLimiter(Edge[] edges)
    {
        _edges = (Edge[])edges.Clone();
    }

    /// <inheritdoc />
    public void Project(Vector3[] positions, float[] invMass)
    {
        foreach (var e in _edges)
        {
            var a = e.A;
            var b = e.B;
            var w1 = invMass[a];
            var w2 = invMass[b];
            var wSum = w1 + w2;
            if (wSum == 0f)
            {
                continue;
            }

            var delta = positions[b] - positions[a];
            var dist = delta.Length();
            var max = e.RestLength * e.MaxStretch;
            if (dist > max && dist > 1e-6f)
            {
                var dir = delta / dist;
                var diff = (dist - max) / wSum;
                positions[a] += w1 * diff * dir;
                positions[b] -= w2 * diff * dir;
            }
        }
    }
}
