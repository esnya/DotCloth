using System.Numerics;

namespace DotCloth.Forces;

/// <summary>Hooke springs along mesh edges.</summary>
public sealed class EdgeSpringForce : IForce
{
    /// <summary>Spring connecting two particle indices.</summary>
    public readonly struct Spring
    {
        /// <summary>Initializes a spring.</summary>
        /// <param name="a">First particle index.</param>
        /// <param name="b">Second particle index.</param>
        /// <param name="restLength">Rest length.</param>
        /// <param name="stiffness">Hooke coefficient.</param>
        public Spring(int a, int b, float restLength, float stiffness)
        {
            A = a;
            B = b;
            RestLength = restLength;
            Stiffness = stiffness;
        }

        /// <summary>Index of the first particle.</summary>
        public int A { get; }

        /// <summary>Index of the second particle.</summary>
        public int B { get; }

        /// <summary>Rest length of the spring.</summary>
        public float RestLength { get; }

        /// <summary>Hooke stiffness coefficient.</summary>
        public float Stiffness { get; }
    }

    private readonly Spring[] _springs;

    /// <summary>Creates the force from edge springs.</summary>
    /// <param name="springs">Springs to simulate.</param>
    public EdgeSpringForce(Spring[] springs)
    {
        _springs = (Spring[])springs.Clone();
    }

    /// <inheritdoc />
    public void Accumulate(Vector3[] positions, Vector3[] forces)
    {
        foreach (var s in _springs)
        {
            var a = positions[s.A];
            var b = positions[s.B];
            var delta = b - a;
            var dist = delta.Length();
            if (dist < 1e-6f)
            {
                continue;
            }
            var dir = delta / dist;
            var f = s.Stiffness * (dist - s.RestLength) * dir;
            forces[s.A] += f;
            forces[s.B] -= f;
        }
    }
}
