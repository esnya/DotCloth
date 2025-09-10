using System.Numerics;

namespace DotCloth.Collisions;

/// <summary>Resolves particle collisions against a shape.</summary>
public interface ICollider
{
    /// <summary>Projects <paramref name="position"/> out of the collider and removes opposing velocity.</summary>
    /// <param name="position">Particle position to adjust.</param>
    /// <param name="velocity">Particle velocity to adjust.</param>
    void Resolve(ref Vector3 position, ref Vector3 velocity);
}
