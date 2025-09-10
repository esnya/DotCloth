using System.Numerics;

namespace DotCloth.Constraints;

/// <summary>Post-integration position constraint.</summary>
public interface IConstraint
{
    /// <summary>Adjusts <paramref name="positions"/> to satisfy the constraint.</summary>
    /// <param name="positions">Particle positions in-place.</param>
    /// <param name="invMass">Inverse masses per particle (0 for pinned).</param>
    void Project(Vector3[] positions, float[] invMass);
}
