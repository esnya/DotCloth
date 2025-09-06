using System.Numerics;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class RandomAccelerationTests
{
    [Fact]
    public void RandomAcceleration_IsDeterministicWithSeed()
    {
        var positionsA = new[] { new Vector3(0,0,0) };
        var positionsB = new[] { new Vector3(0,0,0) };
        var velocitiesA = new[] { Vector3.Zero };
        var velocitiesB = new[] { Vector3.Zero };
        var tris = Array.Empty<int>();
        var p = new ClothParameters { UseGravity = false, RandomAcceleration = 5f, RandomSeed = 42 };
        var s1 = new Solver();
        var s2 = new Solver();
        s1.Initialize(positionsA, tris, p);
        s2.Initialize(positionsB, tris, p);

        float dt = 0.01f;
        for (int i = 0; i < 10; i++)
        {
            s1.Step(dt, positionsA, velocitiesA);
            s2.Step(dt, positionsB, velocitiesB);
        }
        Assert.Equal(positionsA[0].X, positionsB[0].X, 6);
        Assert.Equal(positionsA[0].Y, positionsB[0].Y, 6);
        Assert.Equal(positionsA[0].Z, positionsB[0].Z, 6);
        Assert.Equal(velocitiesA[0].X, velocitiesB[0].X, 6);
        Assert.Equal(velocitiesA[0].Y, velocitiesB[0].Y, 6);
        Assert.Equal(velocitiesA[0].Z, velocitiesB[0].Z, 6);
    }
}

