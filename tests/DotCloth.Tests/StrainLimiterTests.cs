using System.Numerics;
using DotCloth.Constraints;
using Xunit;

namespace DotCloth.Tests;

public class StrainLimiterTests
{
    [Fact]
    public void PinnedVerticesRemainFixed()
    {
        var positions = new[] { new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f) };
        var invMass = new[] { 0f, 1f };
        var edges = new[] { new StrainLimiter.Edge(0, 1, 2f, 1f) };
        var limiter = new StrainLimiter(edges);
        limiter.Project(positions, invMass);
        Assert.Equal(new Vector3(0f, 0f, 0f), positions[0]);
        Assert.InRange(positions[1].X, 1.99f, 2.01f);
    }
}
