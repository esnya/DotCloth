using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using Xunit;

namespace DotCloth.Tests;

public class DivergenceTests
{
    [Fact]
    public void DefaultSolver_NoForces_VelocitiesDoNotIncrease()
    {
        // No triangles → no constraints; no gravity/external forces.
        var positions = new[] { new Vector3(0, 0, 0), new Vector3(0.1f, 0, 0) };
        var velocities = new[] { new Vector3(0, 0.2f, 0), new Vector3(0, -0.2f, 0) };
        var tris = Array.Empty<int>();
        var p = new ClothParameters
        {
            UseGravity = false,
            Damping = 0.0f,
            AirDrag = 0.0f,
            Iterations = 8,
            Substeps = 1,
        };
        var solver = new PbdSolver();
        solver.Initialize(positions, tris, p);

        float initialMax = MathF.Max(velocities[0].Length(), velocities[1].Length());
        var pos = (Vector3[])positions.Clone();
        var vel = (Vector3[])velocities.Clone();

        float lastMax = initialMax;
        for (int i = 0; i < 300; i++)
        {
            solver.Step(1f / 120f, pos, vel);
            Assert.False(float.IsNaN(pos[0].X) || float.IsNaN(vel[0].X));
            float curMax = MathF.Max(vel[0].Length(), vel[1].Length());
            // Monotone non-increase (allow tiny numerical noise)
            Assert.True(curMax <= lastMax + 1e-6f);
            lastMax = curMax;
        }
        float afterMax = MathF.Max(vel[0].Length(), vel[1].Length());
        Assert.True(afterMax <= initialMax + 1e-6f);
    }

#if DOTCLOTH_EXPERIMENTAL_XPBD
    [Fact]
    public void XpbdSolver_NoForces_VelocitiesDoNotIncrease()
    {
        var positions = new[] { new Vector3(0, 0, 0), new Vector3(0.1f, 0, 0) };
        var velocities = new[] { new Vector3(0, 0.2f, 0), new Vector3(0, -0.2f, 0) };
        var tris = Array.Empty<int>();
        var p = new ClothParameters
        {
            UseGravity = false,
            Damping = 0.0f,
            AirDrag = 0.0f,
            Iterations = 8,
            Substeps = 1,
        };
        var solver = new XpbdSolver();
        solver.Initialize(positions, tris, p);

        float initialMax = MathF.Max(velocities[0].Length(), velocities[1].Length());
        var pos = (Vector3[])positions.Clone();
        var vel = (Vector3[])velocities.Clone();

        float lastMax = initialMax;
        for (int i = 0; i < 300; i++)
        {
            solver.Step(1f / 120f, pos, vel);
            Assert.False(float.IsNaN(pos[0].X) || float.IsNaN(vel[0].X));
            float curMax = MathF.Max(vel[0].Length(), vel[1].Length());
            Assert.True(curMax <= lastMax + 1e-6f);
            lastMax = curMax;
        }
        float afterMax = MathF.Max(vel[0].Length(), vel[1].Length());
        Assert.True(afterMax <= initialMax + 1e-6f);
    }
#endif
}
