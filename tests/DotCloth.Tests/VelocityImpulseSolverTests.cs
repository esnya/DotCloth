using System.Numerics;
using Xunit;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;

namespace DotCloth.Tests;

public class VelocityImpulseSolverBasicsTests
{
    [Fact]
    public void Step_WithGravity_AccumulatesDownwardVelocity()
    {
        var solver = new VelocityImpulseSolver();
        var positions = new Vector3[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY };
        var velocities = new Vector3[3];
        var parameters = new ClothParameters
        {
            UseGravity = true,
            GravityScale = 1.0f,
            Iterations = 1,
            Substeps = 1
        };

        solver.Initialize(positions, new int[] { 0, 1, 2 }, parameters);
        solver.Step(0.1f, positions, velocities);

        Assert.True(velocities[0].Y < 0, "Expected downward velocity from gravity");
        Assert.True(velocities[1].Y < 0, "Expected downward velocity from gravity");
    }

    [Fact]
    public void Damping_ReducesVelocityMagnitude()
    {
        var solver = new VelocityImpulseSolver();
        var positions = new Vector3[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY };
        var velocities = new Vector3[] { Vector3.UnitY, Vector3.UnitY, Vector3.Zero };
        var parameters = new ClothParameters
        {
            UseGravity = false,
            Damping = 0.5f,
            Iterations = 1,
            Substeps = 1
        };

        solver.Initialize(positions, new int[] { 0, 1, 2 }, parameters);
        var initialMag = velocities[0].Length();
        
        solver.Step(0.1f, positions, velocities);
        
        var finalMag = velocities[0].Length();
        Assert.True(finalMag < initialMag, "Expected damping to reduce velocity magnitude");
    }
}

public class VelocityImpulseSolverConstraintTests
{
    [Fact]
    public void StretchConstraint_ReducesEdgeViolation()
    {
        var solver = new VelocityImpulseSolver();
        // Start with rest positions (unit lengths)
        var restPositions = new Vector3[] 
        { 
            Vector3.Zero, 
            Vector3.UnitX,        // Edge 0-1 has rest length 1.0
            Vector3.UnitY, 
            new Vector3(1f, 1f, 0f) 
        };
        var velocities = new Vector3[4];
        var triangles = new int[] { 0, 1, 2, 2, 1, 3 }; // Two triangles forming a quad
        var parameters = new ClothParameters
        {
            UseGravity = false,
            StretchStiffness = 1.0f,
            Iterations = 20,
            Substeps = 1
        };

        solver.Initialize(restPositions, triangles, parameters);
        
        // Start with rest positions and apply stretching velocity
        var positions = restPositions.ToArray();
        velocities[1] = new Vector3(5f, 0f, 0f); // Stretch edge 0-1
        
        // Predict unconstrained distance after dt
        float dt = 0.01f;
        var unconstrainedP1 = positions[1] + velocities[1] * dt;
        var restLen = (restPositions[1] - restPositions[0]).Length(); // Should be 1.0
        var unconstrainedLen = (unconstrainedP1 - positions[0]).Length();
        
        solver.Step(dt, positions, velocities);
        var constrainedLen = (positions[1] - positions[0]).Length();
        
        Assert.True(MathF.Abs(constrainedLen - restLen) < MathF.Abs(unconstrainedLen - restLen), 
                   $"Expected constraint to reduce violation: constrained={constrainedLen:F4}, unconstrained={unconstrainedLen:F4}, rest={restLen:F4}");
    }

    [Fact]
    public void ZeroStiffness_BehavesLikeUnconstrained()
    {
        // This test checks that with zero stiffness, the behavior is essentially unconstrained
        // Start with rest positions to ensure no built-in violations
        var restPositions = new Vector3[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY };
        var initialVelocities = new Vector3[] { Vector3.UnitY, -Vector3.UnitY, Vector3.Zero };
        
        // Test with zero stiffness
        var solver = new VelocityImpulseSolver();
        var positions = restPositions.ToArray();
        var velocities = initialVelocities.ToArray();
        var parameters = new ClothParameters
        {
            UseGravity = false,
            StretchStiffness = 0f,
            BendStiffness = 0f,
            Damping = 0f,  // No damping to avoid velocity changes
            AirDrag = 0f,   // No air drag
            Iterations = 1,
            Substeps = 1
        };

        solver.Initialize(positions, new int[] { 0, 1, 2 }, parameters);
        solver.Step(0.01f, positions, velocities);  // Use small time step

        // With zero stiffness and no external forces, velocities should be nearly unchanged
        for (int i = 0; i < 3; i++)
        {
            Assert.True((velocities[i] - initialVelocities[i]).Length() < 1e-2f, 
                       $"Expected velocity {i} to be nearly unchanged with zero stiffness. Initial: {initialVelocities[i]}, Final: {velocities[i]}");
        }
    }

    [Fact]
    public void HigherStiffness_ReducesViolationMore()
    {
        var positions = new Vector3[] { Vector3.Zero, Vector3.UnitX * 2f, Vector3.UnitY };
        var velocities1 = new Vector3[3];
        var velocities2 = new Vector3[3];

        var lowStiffness = new ClothParameters
        {
            UseGravity = false,
            StretchStiffness = 0.1f,
            Iterations = 5,
            Substeps = 1
        };

        var highStiffness = new ClothParameters
        {
            UseGravity = false,
            StretchStiffness = 0.8f,
            Iterations = 5,
            Substeps = 1
        };

        var solver1 = new VelocityImpulseSolver();
        var solver2 = new VelocityImpulseSolver();
        var positions1 = positions.ToArray();
        var positions2 = positions.ToArray();

        solver1.Initialize(positions1, new int[] { 0, 1, 2 }, lowStiffness);
        solver2.Initialize(positions2, new int[] { 0, 1, 2 }, highStiffness);

        for (int i = 0; i < 5; i++)
        {
            solver1.Step(0.01f, positions1, velocities1);
            solver2.Step(0.01f, positions2, velocities2);
        }

        var restLength = 1f;
        var violation1 = Math.Abs((positions1[1] - positions1[0]).Length() - restLength);
        var violation2 = Math.Abs((positions2[1] - positions2[0]).Length() - restLength);

        Assert.True(violation2 < violation1, 
                   $"Expected higher stiffness to reduce violation more: low={violation1:F4}, high={violation2:F4}");
    }

    [Fact]
    public void Determinism_FixedInputs_YieldsSameResults()
    {
        var positions = new Vector3[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY };
        var triangles = new int[] { 0, 1, 2 };
        var parameters = new ClothParameters
        {
            UseGravity = true,
            StretchStiffness = 0.5f,
            BendStiffness = 0.3f,
            RandomSeed = 12345,
            Iterations = 5,
            Substeps = 2
        };

        var solver1 = new VelocityImpulseSolver();
        var solver2 = new VelocityImpulseSolver();
        var positions1 = positions.ToArray();
        var positions2 = positions.ToArray();
        var velocities1 = new Vector3[3];
        var velocities2 = new Vector3[3];

        solver1.Initialize(positions1, triangles, parameters);
        solver2.Initialize(positions2, triangles, parameters);

        for (int i = 0; i < 10; i++)
        {
            solver1.Step(0.01f, positions1, velocities1);
            solver2.Step(0.01f, positions2, velocities2);
        }

        for (int i = 0; i < 3; i++)
        {
            Assert.True((positions1[i] - positions2[i]).Length() < 1e-6f, 
                       $"Expected deterministic behavior for position {i}");
            Assert.True((velocities1[i] - velocities2[i]).Length() < 1e-6f, 
                       $"Expected deterministic behavior for velocity {i}");
        }
    }

    [Fact]
    public void MoreIterations_MonotonicallyReduceStretchViolation()
    {
        var positions = new Vector3[] { Vector3.Zero, Vector3.UnitX * 3f, Vector3.UnitY };
        var parameters = new ClothParameters
        {
            UseGravity = false,
            StretchStiffness = 0.5f,
            Substeps = 1
        };

        float prevViolation = float.MaxValue;
        for (int iterations = 1; iterations <= 5; iterations++)
        {
            var solver = new VelocityImpulseSolver();
            var testPositions = positions.ToArray();
            var velocities = new Vector3[3];
            parameters.Iterations = iterations;

            solver.Initialize(testPositions, new int[] { 0, 1, 2 }, parameters);
            solver.Step(0.1f, testPositions, velocities);

            var restLength = 1f;
            var violation = Math.Abs((testPositions[1] - testPositions[0]).Length() - restLength);

            Assert.True(violation <= prevViolation + 1e-6f, 
                       $"Expected monotonic improvement: iter{iterations}={violation:F6} should be <= {prevViolation:F6}");
            prevViolation = violation;
        }
    }
}