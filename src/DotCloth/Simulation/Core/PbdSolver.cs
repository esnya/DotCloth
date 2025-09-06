using System;
using System.Collections.Generic;
using System.Numerics;
using DotCloth.Simulation.Collision;
using DotCloth.Simulation.Parameters;

namespace DotCloth.Simulation.Core;

/// <summary>
/// Backward-compatible solver that selects implementation at compile time.
/// </summary>
public sealed class PbdSolver : IClothSimulator
{
#if DOTCLOTH_EXPERIMENTAL_XPBD
    private readonly XpbdSolver _impl = new();
#else
    private readonly VelocityImpulseSolver _impl = new();
#endif

    /// <inheritdoc />
    public void SetColliders(IEnumerable<ICollider> colliders) => _impl.SetColliders(colliders);

    /// <inheritdoc />
    public void Initialize(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, ClothParameters parameters) => _impl.Initialize(positions, triangles, parameters);

    /// <inheritdoc />
    public void Step(float deltaTime, Span<Vector3> positions, Span<Vector3> velocities) => _impl.Step(deltaTime, positions, velocities);

    /// <inheritdoc />
    public void UpdateParameters(ClothParameters parameters) => _impl.UpdateParameters(parameters);

    /// <inheritdoc />
    public void SetInverseMasses(ReadOnlySpan<float> inverseMasses) => _impl.SetInverseMasses(inverseMasses);

    /// <inheritdoc />
    public void ResetRestState(ReadOnlySpan<Vector3> positions) => _impl.ResetRestState(positions);

    /// <inheritdoc />
    public void PinVertices(ReadOnlySpan<int> indices) => _impl.PinVertices(indices);

    /// <inheritdoc />
    public void PinVertices(params int[] indices) => _impl.PinVertices(indices);

    /// <inheritdoc />
    public void UnpinVertices(ReadOnlySpan<int> indices) => _impl.UnpinVertices(indices);

    /// <inheritdoc />
    public void UnpinVertices(params int[] indices) => _impl.UnpinVertices(indices);

    /// <inheritdoc />
    public void ClearPins() => _impl.ClearPins();

    /// <inheritdoc />
    public void SetTetherAnchors(ReadOnlySpan<int> anchors) => _impl.SetTetherAnchors(anchors);
}
