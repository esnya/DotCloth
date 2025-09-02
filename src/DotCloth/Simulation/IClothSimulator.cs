using System.Numerics;
using System.Collections.Generic;

namespace DotCloth.Simulation;

/// <summary>
/// Core cloth simulator contract.
/// Implementations should be deterministic for fixed inputs and time step.
/// </summary>
public interface IClothSimulator
{
    /// <summary>
    /// Initializes the simulator with initial positions and triangle topology.
    /// </summary>
    /// <param name="positions">Initial vertex positions.</param>
    /// <param name="triangles">Triangle indices (3*n).</param>
    /// <param name="parameters">Simulation parameters.</param>
    void Initialize(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, Parameters.ClothParameters parameters);

    /// <summary>
    /// Advances the simulation by <paramref name="deltaTime"/>.
    /// Positions and velocities are updated in-place.
    /// </summary>
    void Step(float deltaTime, Span<Vector3> positions, Span<Vector3> velocities);

    /// <summary>Updates parameters. May re-derive internal coefficients.</summary>
    void UpdateParameters(Parameters.ClothParameters parameters);

    /// <summary>Sets per-vertex inverse masses. Zero fixes a vertex (pinned).</summary>
    void SetInverseMasses(ReadOnlySpan<float> inverseMasses);

    /// <summary>Rebuilds rest-state dependent data (e.g., rest lengths) from positions.</summary>
    void ResetRestState(ReadOnlySpan<Vector3> positions);

    /// <summary>Sets collision resolvers for this simulator.</summary>
    void SetColliders(IEnumerable<Collision.ICollider> colliders);

    /// <summary>Pins given vertex indices by setting inverse mass to zero.</summary>
    void PinVertices(ReadOnlySpan<int> indices);

    /// <summary>Pins given vertex indices by setting inverse mass to zero.</summary>
    void PinVertices(params int[] indices);

    /// <summary>Define tether anchors (by vertex indices). Nearest anchor per vertex is used.</summary>
    void SetTetherAnchors(ReadOnlySpan<int> anchors);
}
