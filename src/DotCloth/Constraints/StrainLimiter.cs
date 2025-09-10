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
    public void Project(Vector3[] positions)
    {
        foreach (var e in _edges)
        {
            var a = positions[e.A];
            var b = positions[e.B];
            var delta = b - a;
            var dist = delta.Length();
            var max = e.RestLength * e.MaxStretch;
            if (dist > max && dist > 1e-6f)
            {
                var diff = 0.5f * (dist - max);
                var dir = delta / dist;
                positions[e.A] += diff * dir;
                positions[e.B] -= diff * dir;
            }
        }
    }
}
