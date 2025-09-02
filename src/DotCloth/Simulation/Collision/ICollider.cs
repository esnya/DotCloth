using System.Numerics;

namespace DotCloth.Simulation.Core;

public interface ICollider
{
    /// <summary>
    /// Resolves collisions in-place. Implementations may modify positions and velocities.
    /// </summary>
    void Resolve(Span<Vector3> positions, Span<Vector3> velocities, float deltaTime, float thickness, float friction);
}

