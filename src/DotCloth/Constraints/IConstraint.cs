using System.Numerics;

namespace DotCloth.Constraints;

/// <summary>Post-integration position constraint.</summary>
public interface IConstraint
{
    /// <summary>Adjusts <paramref name="positions"/> to satisfy the constraint.</summary>
    /// <param name="positions">Particle positions in-place.</param>
    void Project(Vector3[] positions);
}
