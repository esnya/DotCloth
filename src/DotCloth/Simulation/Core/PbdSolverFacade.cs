using System.Numerics;
using System.Collections.Generic;

namespace DotCloth.Simulation.Core;

/// <summary>
/// Public PbdSolver façade that preserves the original class name and public API
/// while delegating to the new default VelocityImpulseSolver internally.
/// XPBD remains available as <c>XpbdSolver</c> when built with the experimental symbol DOTCLOTH_EXPERIMENTAL_XPBD.
/// </summary>
public sealed class PbdSolver : IClothSimulator
{
    private readonly IClothSimulator _impl;
    /// <summary>
    /// Creates a PBD-compatible solver façade that delegates to the default
    /// velocity-level sequential-impulses implementation.
    /// </summary>
    public PbdSolver()
    {
#if DOTCLOTH_EXPERIMENTAL_XPBD
        // Internal builds may prefer XPBD as the reference path.
        _impl = new XpbdSolver();
#else
        // External default remains the velocity-level solver.
        _impl = new VelocityImpulseSolver();
#endif
    }

    /// <inheritdoc />
    public void Initialize(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, Parameters.ClothParameters parameters)
        => _impl.Initialize(positions, triangles, parameters);

    /// <inheritdoc />
    public void Step(float deltaTime, Vector3[] positions, Vector3[] velocities)
        => _impl.Step(deltaTime, positions, velocities);

    /// <inheritdoc />
    public void UpdateParameters(Parameters.ClothParameters parameters)
        => _impl.UpdateParameters(parameters);

    /// <inheritdoc />
    public void SetInverseMasses(ReadOnlySpan<float> inverseMasses)
        => _impl.SetInverseMasses(inverseMasses);

    /// <inheritdoc />
    public void ResetRestState(ReadOnlySpan<Vector3> positions)
        => _impl.ResetRestState(positions);

    /// <inheritdoc />
    public void SetColliders(IEnumerable<Collision.ICollider> colliders)
        => _impl.SetColliders(colliders);

    /// <inheritdoc />
    public void PinVertices(ReadOnlySpan<int> indices)
        => _impl.PinVertices(indices);

    /// <inheritdoc />
    public void PinVertices(params int[] indices)
        => _impl.PinVertices(indices);

    /// <inheritdoc />
    public void UnpinVertices(ReadOnlySpan<int> indices)
        => _impl.UnpinVertices(indices);

    /// <inheritdoc />
    public void UnpinVertices(params int[] indices)
        => _impl.UnpinVertices(indices);

    /// <inheritdoc />
    public void ClearPins()
        => _impl.ClearPins();

    /// <inheritdoc />
    public void SetTetherAnchors(ReadOnlySpan<int> anchors)
        => _impl.SetTetherAnchors(anchors);
}
