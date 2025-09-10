using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using DotCloth;
using DotCloth.Forces;
using DotCloth.Constraints;
using DotCloth.MassSpring;
using System.Threading;

static ForceCloth BuildCloth(int size, string model)
{
    var width = size;
    var height = size;
    var positions = new Vector3[width * height];
    var invMass = new float[positions.Length];
    var springs = new List<EdgeSpringForce.Spring>();
    var edges = new List<StrainLimiter.Edge>();
    var tris = new List<CoRotationalFemForce.Triangle>();
    var dihedrals = new List<DiscreteShellForce.Dihedral>();
    const float spacing = 1f;

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
        case "Springs":
            forces.Add(new EdgeSpringForce(springs.ToArray()));
            break;
        case "Shells":
            forces.Add(new EdgeSpringForce(springs.ToArray()));
            forces.Add(new DiscreteShellForce(dihedrals.ToArray()));
            break;
        case "FEM":
            forces.Add(new CoRotationalFemForce(tris.ToArray()));
            break;
        case "Springs+Strain":
            forces.Add(new EdgeSpringForce(springs.ToArray()));
            constraints.Add(new StrainLimiter(edges.ToArray()));
            break;
    }

    return new ForceCloth(
        positions,
        invMass,
        forces.ToArray(),
        new Vector3(0f, -9.81f, 0f),
        0.99f,
        constraints.ToArray(),
        SemiImplicitEulerIntegrator.Instance);
}

// args: --maxSize <N> --maxInstances <M>
var maxSize = 20;
var maxInstances = 8;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--maxSize" && int.TryParse(args[i + 1], out var ms))
    {
        maxSize = ms;
    }
    if (args[i] == "--maxInstances" && int.TryParse(args[i + 1], out var mi))
    {
        maxInstances = mi;
    }
}

var runtime = RuntimeInformation.FrameworkDescription;
var os = RuntimeInformation.OSDescription;
var cores = Environment.ProcessorCount;
ThreadPool.GetMaxThreads(out var workerThreads, out _);
Console.WriteLine($".NET Runtime: {runtime}");
Console.WriteLine($"OS: {os}, logical cores: {cores}, threadpool max threads: {workerThreads}");

string[] models = { "Springs", "Shells", "FEM", "Springs+Strain" };
var sizes = new List<int>();
for (int s = 10; s <= maxSize; s += 10)
{
    sizes.Add(s);
}
var instanceCounts = new List<int>();
for (int inst = 1; inst <= maxInstances; inst *= 2)
{
    instanceCounts.Add(inst);
}
const int steps = 200;
const float dt = 0.016f;

foreach (var model in models)
{
    var done = false;
    foreach (var size in sizes)
    {
        foreach (var instances in instanceCounts)
        {
            var cloths = new ForceCloth[instances];
            for (int i = 0; i < instances; i++)
            {
                cloths[i] = BuildCloth(size, model);
            }

            var sw = Stopwatch.StartNew();
            for (int step = 0; step < steps; step++)
            {
                for (int i = 0; i < instances; i++)
                {
                    cloths[i].Step(dt);
                }
            }
            sw.Stop();

            var totalMs = sw.Elapsed.TotalMilliseconds;
            var frameMs = totalMs / steps;
            var fps = 1000.0 / frameMs;

            Console.WriteLine($"{model}, {size}x{size}, inst {instances}, {steps} steps: {totalMs:F1} ms total, {frameMs:F2} ms/frame, {fps:F1} FPS");

            if (fps < 60)
            {
                done = true;
                break;
            }
        }
        if (done)
        {
            break;
        }
    }
}
