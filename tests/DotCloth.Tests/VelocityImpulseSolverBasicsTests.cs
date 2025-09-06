using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class VelocityImpulseSolverBasicsTests
{
    [Fact]
    public void Step_WithGravity_AccumulatesDownwardVelocity()
    {
        var solver = new VelocityImpulseSolver();
        var p = new ClothParameters { UseGravity = true, GravityScale = 1.0f, Damping = 0.0f };
        var positions = new Vector3[] { new(0, 0, 0) };
        var velocities = new Vector3[] { Vector3.Zero };
        var triangles = Array.Empty<int>();
        solver.Initialize(positions, triangles, p);

        solver.Step(0.01f, positions, velocities);

        Assert.True(velocities[0].Y < 0f);
        Assert.True(positions[0].Y < 0f);
    }

    [Fact]
    public void Damping_ReducesVelocityMagnitude()
    {
        var solver = new VelocityImpulseSolver();
        var p = new ClothParameters { UseGravity = false, Damping = 0.5f };
        var positions = new Vector3[] { new(0, 0, 0) };
        var velocities = new Vector3[] { new(1, 0, 0) };
        var triangles = Array.Empty<int>();
        solver.Initialize(positions, triangles, p);

        solver.Step(0.01f, positions, velocities);

        Assert.True(velocities[0].X < 1f);
    }
}
