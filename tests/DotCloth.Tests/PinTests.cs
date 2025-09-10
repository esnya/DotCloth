using System.Numerics;
using DotCloth;
using DotCloth.Forces;
using DotCloth.MassSpring;
using Xunit;

namespace DotCloth.Tests;

public class PinTests
{
    [Fact]
    public void ForceCloth_PinnedParticle_RemainsFixed()
    {
        var positions = new[] { new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 0f) };
        var invMass = new[] { 1f, 1f };
        var cloth = new ForceCloth(positions, invMass, Array.Empty<IForce>(), new Vector3(0f, -9.81f, 0f), 0.99f);
        cloth.Pin(0, positions[0]);
        cloth.Step(0.016f);
        Assert.Equal(1f, cloth.Positions[0].Y, 5);
        Assert.True(cloth.Positions[1].Y < cloth.Positions[0].Y);
    }

    [Fact]
    public void MassSpringCloth_PinnedParticle_RemainsFixed()
    {
        var positions = new[] { new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 0f) };
        var invMass = new[] { 1f, 1f };
        var springs = new MassSpringCloth.Spring[] { new(0, 1, 1f, 10f) };
        var cloth = new MassSpringCloth(positions, invMass, springs, new Vector3(0f, -9.81f, 0f), 0.99f);
        cloth.Pin(0, positions[0]);
        cloth.Step(0.016f);
        Assert.Equal(1f, cloth.Positions[0].Y, 5);
        Assert.True(cloth.Positions[1].Y < cloth.Positions[0].Y);
    }

    [Fact]
    public void ForceCloth_Unpin_AllowsMotion()
    {
        var positions = new[] { new Vector3(0f, 1f, 0f) };
        var invMass = new[] { 1f };
        var cloth = new ForceCloth(positions, invMass, Array.Empty<IForce>(), new Vector3(0f, -9.81f, 0f), 0.99f);
        cloth.Pin(0, positions[0]);
        cloth.Step(0.016f);
        cloth.Unpin(0, 1f);
        cloth.Step(0.016f);
        Assert.True(cloth.Positions[0].Y < 1f);
    }
}
