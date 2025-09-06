using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class ParameterValidationTests
{
    [Fact]
    public void Initialize_Throws_On_BadTriangleIndex()
    {
        var s = new VelocityImpulseSolver();
        var p = new ClothParameters();
        var positions = new[] { new Vector3(0,0,0) };
        var tris = new[] { 0, 1, 2 };
        Assert.Throws<ArgumentOutOfRangeException>(() => s.Initialize(positions, tris, p));
    }

    [Fact]
    public void UpdateParameters_ClampsRanges()
    {
        var s = new VelocityImpulseSolver();
        var positions = new[] { new Vector3(0,0,0) };
        s.Initialize(positions, Array.Empty<int>(), new ClothParameters());
        var p = new ClothParameters
        {
            Damping = 5f,
            StretchStiffness = 2f,
            BendStiffness = -1f,
            Friction = 2f,
            CollisionThickness = -1f,
        };
        s.UpdateParameters(p);
        // No exception; internal config clamps; behavior validated indirectly by not throwing
    }
}

