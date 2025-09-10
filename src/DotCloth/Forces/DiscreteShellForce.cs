using System.Numerics;

namespace DotCloth.Forces;

/// <summary>Discrete shell bending based on dihedral angles.</summary>
public sealed class DiscreteShellForce : IForce
{
    /// <summary>Adjacent triangle pair sharing an edge.</summary>
    public readonly struct Dihedral
    {
        /// <summary>Initializes a dihedral angle definition.</summary>
        public Dihedral(int i0, int i1, int i2, int i3, float restAngle, float stiffness)
        {
            I0 = i0; I1 = i1; I2 = i2; I3 = i3;
            RestAngle = restAngle; Stiffness = stiffness;
        }

        /// <summary>Opposite vertex of the first triangle.</summary>
        public int I0 { get; }

        /// <summary>First shared edge vertex.</summary>
        public int I1 { get; }

        /// <summary>Second shared edge vertex.</summary>
        public int I2 { get; }

        /// <summary>Opposite vertex of the second triangle.</summary>
        public int I3 { get; }

        /// <summary>Rest dihedral angle in radians.</summary>
        public float RestAngle { get; }

        /// <summary>Bending stiffness.</summary>
        public float Stiffness { get; }
    }

    private readonly Dihedral[] _dihedrals;

    /// <summary>Creates the force from dihedral definitions.</summary>
    public DiscreteShellForce(Dihedral[] dihedrals)
    {
        _dihedrals = (Dihedral[])dihedrals.Clone();
    }

    /// <inheritdoc />
    public void Accumulate(Vector3[] positions, Vector3[] forces)
    {
        foreach (var d in _dihedrals)
        {
            var p0 = positions[d.I0];
            var p1 = positions[d.I1];
            var p2 = positions[d.I2];
            var p3 = positions[d.I3];

            var e = p2 - p1;
            var n1 = Vector3.Cross(p0 - p1, p2 - p1);
            var n2 = Vector3.Cross(p3 - p2, p1 - p2);
            var len1 = n1.Length();
            var len2 = n2.Length();
            if (len1 < 1e-6f || len2 < 1e-6f)
            {
                continue;
            }
            n1 /= len1;
            n2 /= len2;
            var angle = MathF.Atan2(Vector3.Dot(e, Vector3.Cross(n1, n2)), Vector3.Dot(n1, n2));
            var diff = angle - d.RestAngle;
            var f = d.Stiffness * diff;
            // Approximate gradient distribution
            var q0 = Vector3.Cross(e, n1) / len1;
            var q3 = Vector3.Cross(e, n2) / len2;
            forces[d.I0] += -f * q0;
            forces[d.I3] += f * q3;
            var shared = 0.5f * (f * q0 - f * q3);
            forces[d.I1] += shared;
            forces[d.I2] -= shared;
        }
    }
}
