using System.Numerics;
using DotCloth;
using DotCloth.Forces;
using DotCloth.Collisions;
using DotCloth.Constraints;
using DotCloth.MassSpring;
using Xunit;

namespace DotCloth.Tests;

public class ForceClothTests
{
    private static readonly IIntegrator[] integrators = new IIntegrator[] { SemiImplicitEulerIntegrator.Instance, ExplicitEulerIntegrator.Instance };

    public static IEnumerable<object[]> Cases
    {
        get
        {
            foreach (var integ in integrators)
            {
                yield return new object[] { integ, (Func<IIntegrator, ForceCloth>)CreateSpringCloth };
                yield return new object[] { integ, (Func<IIntegrator, ForceCloth>)CreateShellCloth };
                yield return new object[] { integ, (Func<IIntegrator, ForceCloth>)CreateFemCloth };
                yield return new object[] { integ, (Func<IIntegrator, ForceCloth>)CreateStrainCloth };
            }
        }
    }

    private static ForceCloth CreateSpringCloth(IIntegrator integrator)
    {
        var positions = new[] { new Vector3(0f, 2f, 0f), new Vector3(0f, 1f, 0f) };
        var invMass = new[] { 1f, 1f };
        var springs = new EdgeSpringForce.Spring[] { new(0, 1, 1f, 100f) };
        var forces = new IForce[] { new EdgeSpringForce(springs) };
        var colliders = new ICollider[] { new PlaneCollider(Vector3.Zero, Vector3.UnitY) };
        var cloth = new ForceCloth(positions, invMass, forces, new Vector3(0f, -9.81f, 0f), 0.98f, integrator: integrator, colliders: colliders);
        cloth.Pin(0, positions[0]);
        return cloth;
    }

    private static ForceCloth CreateShellCloth(IIntegrator integrator)
    {
        var positions = new[]
        {
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f)
        };
        var invMass = new[] { 1f, 1f, 1f, 1f };
        var springs = new EdgeSpringForce.Spring[]
        {
            new(0,1,1f,50f), new(1,3,1f,50f), new(3,2,1f,50f), new(2,0,1f,50f),
            new(0,3,MathF.Sqrt(2f),50f), new(1,2,MathF.Sqrt(2f),50f)
        };
        var dihedrals = new DiscreteShellForce.Dihedral[] { new(0, 1, 3, 2, 0f, 10f) };
        var forces = new IForce[] { new EdgeSpringForce(springs), new DiscreteShellForce(dihedrals) };
        var colliders = new ICollider[] { new PlaneCollider(Vector3.Zero, Vector3.UnitY) };
        var cloth = new ForceCloth(positions, invMass, forces, Vector3.Zero, 0.99f, integrator: integrator, colliders: colliders);
        cloth.Pin(0, positions[0]);
        return cloth;
    }

    private static ForceCloth CreateFemCloth(IIntegrator integrator)
    {
        var positions = new[]
        {
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 0f, 0f)
        };
        var invMass = new[] { 1f, 1f, 1f };
        var tris = new CoRotationalFemForce.Triangle[] { new(0, 1, 2, positions[0], positions[1], positions[2], 10f) };
        var forces = new IForce[] { new CoRotationalFemForce(tris) };
        var colliders = new ICollider[] { new PlaneCollider(Vector3.Zero, Vector3.UnitY) };
        var cloth = new ForceCloth(positions, invMass, forces, Vector3.Zero, 0.99f, integrator: integrator, colliders: colliders);
        cloth.Pin(0, positions[0]);
        return cloth;
    }

    private static ForceCloth CreateStrainCloth(IIntegrator integrator)
    {
        var positions = new[] { new Vector3(0f, 2f, 0f), new Vector3(0f, 1f, 0f) };
        var invMass = new[] { 1f, 1f };
        var springs = new EdgeSpringForce.Spring[] { new(0, 1, 1f, 500f) };
        var forces = new IForce[] { new EdgeSpringForce(springs) };
        var edges = new StrainLimiter.Edge[] { new(0, 1, 1f, 1.1f) };
        var limiter = new StrainLimiter(edges);
        var colliders = new ICollider[] { new PlaneCollider(Vector3.Zero, Vector3.UnitY) };
        var cloth = new ForceCloth(positions, invMass, forces, Vector3.Zero, 0.99f, new IConstraint[] { limiter }, integrator, colliders);
        cloth.Pin(0, positions[0]);
        return cloth;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void PreservesRestShape(IIntegrator integrator, Func<IIntegrator, ForceCloth> factory)
    {
        var cloth = factory(integrator);
        for (int i = 0; i < 10; i++)
        {
            cloth.Step(0.016f);
        }
        foreach (var p in cloth.Positions)
        {
            Assert.False(float.IsNaN(p.X));
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void DoesNotDiverge(IIntegrator integrator, Func<IIntegrator, ForceCloth> factory)
    {
        var cloth = factory(integrator);
        for (int i = 0; i < 500; i++)
        {
            cloth.Step(0.008f);
        }
        foreach (var p in cloth.Positions)
        {
            Assert.True(float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z));
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ConvergesToRest(IIntegrator integrator, Func<IIntegrator, ForceCloth> factory)
    {
        var cloth = factory(integrator);
        for (int i = 0; i < 200; i++)
        {
            cloth.Step(0.016f);
        }
        foreach (var v in cloth.Positions)
        {
            Assert.InRange(v.Length(), 0f, 10f);
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void DoesNotOverContract(IIntegrator integrator, Func<IIntegrator, ForceCloth> factory)
    {
        var cloth = factory(integrator);
        for (int i = 0; i < 50; i++)
        {
            cloth.Step(0.016f);
        }
        for (int i = 1; i < cloth.Positions.Length; i++)
        {
            Assert.True(cloth.Positions[i].Y >= -0.01f);
        }
    }
}
