using System.Numerics;

namespace DotCloth.Forces;

/// <summary>Force contribution applied to cloth particles.</summary>
public interface IForce
{
    /// <summary>Accumulates forces into <paramref name="forces"/> based on current <paramref name="positions"/>.</summary>
    void Accumulate(Vector3[] positions, Vector3[] forces);
}
