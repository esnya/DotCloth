using System.Numerics;
using DotCloth.Collisions;
using Xunit;

namespace DotCloth.Tests;

public class ColliderTests
{
    [Fact]
    public void PlaneProjectsBelowPoint()
    {
        var collider = new PlaneCollider(Vector3.Zero, Vector3.UnitY);
        var pos = new Vector3(0f, -1f, 0f);
        var vel = new Vector3(0f, -1f, 0f);
        collider.Resolve(ref pos, ref vel);
        Assert.Equal(0f, pos.Y);
        Assert.True(vel.Y >= 0f);
    }

    [Fact]
    public void SphereProjectsInsidePoint()
    {
        var collider = new SphereCollider(Vector3.Zero, 1f);
        var pos = new Vector3(0.2f, 0f, 0f);
        var vel = new Vector3(-1f, 0f, 0f);
        collider.Resolve(ref pos, ref vel);
        Assert.Equal(1f, pos.Length(), 3);
        Assert.True(Vector3.Dot(vel, pos) >= 0f);
    }

    [Fact]
    public void CapsuleProjectsInsidePoint()
    {
        var collider = new CapsuleCollider(new Vector3(0f, -1f, 0f), new Vector3(0f, 1f, 0f), 0.5f);
        var pos = new Vector3(0.1f, 0f, 0f);
        var vel = new Vector3(-1f, 0f, 0f);
        collider.Resolve(ref pos, ref vel);
        Assert.Equal(0.5f, pos.X, 3);
        Assert.True(vel.X >= 0f);
    }
}
