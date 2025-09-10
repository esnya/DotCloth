using System.Numerics;
using System.Collections.Generic;
using DotCloth;
using DotCloth.Forces;
using DotCloth.Constraints;
using DotCloth.MassSpring;

IIntegrator integrator = args.Length > 0 && args[0].Equals("explicit", StringComparison.OrdinalIgnoreCase)
    ? (IIntegrator)ExplicitEulerIntegrator.Instance
    : SemiImplicitEulerIntegrator.Instance;
var model = args.Length > 1 ? args[1].ToLowerInvariant() : "springs";

ForceCloth Build(string m)
{
    var width = 3;
    var height = 3;
    var positions = new Vector3[width * height];
    var invMass = new float[positions.Length];
    var springs = new List<EdgeSpringForce.Spring>();
    var edges = new List<StrainLimiter.Edge>();
    var tris = new List<CoRotationalFemForce.Triangle>();
    var dihedrals = new List<DiscreteShellForce.Dihedral>();
    float spacing = 1f;
    int Idx(int x, int y) => y * width + x;
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            var idx = Idx(x, y);
            positions[idx] = new Vector3(x * spacing, 2f + y * spacing, 0f);
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
                tris.Add(new CoRotationalFemForce.Triangle(a, b, c, positions[a], positions[b], positions[c], 20f));
                tris.Add(new CoRotationalFemForce.Triangle(c, b, d, positions[c], positions[b], positions[d], 20f));
                dihedrals.Add(new DiscreteShellForce.Dihedral(a, b, c, d, 0f, 5f));
            }
        }
    }
    for (int x = 0; x < width; x++) invMass[x] = 0f;
    var forces = new List<IForce>();
    var constraints = new List<IConstraint>();
    switch (m)
    {
        case "springs":
            forces.Add(new EdgeSpringForce(springs.ToArray()));
            break;
        case "shells":
            forces.Add(new EdgeSpringForce(springs.ToArray()));
            forces.Add(new DiscreteShellForce(dihedrals.ToArray()));
            break;
        case "fem":
            forces.Add(new CoRotationalFemForce(tris.ToArray()));
            break;
        case "strain":
            forces.Add(new EdgeSpringForce(springs.ToArray()));
            constraints.Add(new StrainLimiter(edges.ToArray()));
            break;
        default:
            throw new ArgumentException($"Unknown model '{m}'");
    }
    return new ForceCloth(positions, invMass, forces.ToArray(), new Vector3(0f, -9.81f, 0f), 0.98f, constraints.ToArray(), integrator);
}

var cloth = Build(model);
for (int i = 0; i < 60; i++)
{
    cloth.Step(0.016f);
}
Console.WriteLine($"Integrator: {integrator.GetType().Name}, Model: {model}, Bottom Y: {cloth.Positions[^1].Y:F3}");
