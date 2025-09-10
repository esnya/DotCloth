using System.Collections.Generic;
using System.Numerics;
using DotCloth;
using DotCloth.Forces;
using DotCloth.Constraints;
using DotCloth.MassSpring;
using DotCloth.Collisions;

namespace DotCloth.MonoGameSample.Scenarios;

internal static class ClothFactory
{
    public static ForceCloth Create(int size, ForceModel model, ICollider[]? extraColliders = null)
    {
        var width = size;
        var height = size;
        var positions = new Vector3[width * height];
        var invMass = new float[positions.Length];
        var springs = new List<EdgeSpringForce.Spring>();
        var edges = new List<StrainLimiter.Edge>();
        var tris = new List<CoRotationalFemForce.Triangle>();
        var dihedrals = new List<DiscreteShellForce.Dihedral>();
        const float spacing = 0.5f;

        int Idx(int x, int y) => y * width + x;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var idx = Idx(x, y);
                positions[idx] = new Vector3(x * spacing, 5f + y * spacing, 0f);
                invMass[idx] = 1f;
                if (x > 0)
                {
                    springs.Add(new EdgeSpringForce.Spring(Idx(x - 1, y), idx, spacing, 100f));
                    edges.Add(new StrainLimiter.Edge(Idx(x - 1, y), idx, spacing, 1.1f));
                }
                if (y > 0)
                {
                    springs.Add(new EdgeSpringForce.Spring(Idx(x, y - 1), idx, spacing, 100f));
                    edges.Add(new StrainLimiter.Edge(Idx(x, y - 1), idx, spacing, 1.1f));
                }
                if (x > 0 && y > 0)
                {
                    var a = Idx(x - 1, y - 1);
                    var b = Idx(x, y - 1);
                    var c = Idx(x - 1, y);
                    var d = idx;
                    tris.Add(new CoRotationalFemForce.Triangle(a, b, c, positions[a], positions[b], positions[c], 50f));
                    tris.Add(new CoRotationalFemForce.Triangle(c, b, d, positions[c], positions[b], positions[d], 50f));
                    dihedrals.Add(new DiscreteShellForce.Dihedral(a, b, c, d, 0f, 10f));
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            invMass[x] = 0f;
        }

        var forces = new List<IForce>();
        var constraints = new List<IConstraint>();

        switch (model)
        {
            case ForceModel.Springs:
                forces.Add(new EdgeSpringForce(springs.ToArray()));
                break;
            case ForceModel.Shells:
                forces.Add(new EdgeSpringForce(springs.ToArray()));
                forces.Add(new DiscreteShellForce(dihedrals.ToArray()));
                break;
            case ForceModel.Fem:
                forces.Add(new CoRotationalFemForce(tris.ToArray()));
                break;
            case ForceModel.SpringsWithStrain:
                forces.Add(new EdgeSpringForce(springs.ToArray()));
                constraints.Add(new StrainLimiter(edges.ToArray()));
                break;
        }

        var colliders = new List<ICollider>();
        if (extraColliders != null)
        {
            colliders.AddRange(extraColliders);
        }

        return new ForceCloth(
            positions,
            invMass,
            forces.ToArray(),
            new Vector3(0f, -9.81f, 0f),
            0.99f,
            constraints: constraints.ToArray(),
            integrator: SemiImplicitEulerIntegrator.Instance,
            colliders: colliders.ToArray());
    }
}
