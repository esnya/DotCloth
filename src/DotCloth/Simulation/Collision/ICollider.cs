using System.Numerics;

namespace DotCloth.Simulation.Collision;

/// <summary>
/// Collision resolver that can modify particle positions and velocities in-place.
/// Implementations should be deterministic for fixed inputs. Receives previous
/// positions to enable simple swept push-out when desired.
/// </summary>
public interface ICollider
{
    /// <summary>
    /// Resolves collisions in-place. Implementations may modify positions and velocities.
    /// </summary>
    void Resolve(ReadOnlySpan<Vector3> prevPositions, Span<Vector3> positions, Span<Vector3> velocities, float deltaTime, float thickness, float friction);
}
