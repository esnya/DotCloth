using System.Diagnostics;
using System.Numerics;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;

static class Perf
{
    static (Vector3[] pos, int[] tris) MakeGrid(int n, float spacing)
    {
        var pos = new Vector3[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
            pos[y * n + x] = new Vector3(x * spacing, 0, -y * spacing);
        var tris = new int[(n - 1) * (n - 1) * 6];
        int t = 0;
        for (int y = 0; y < n - 1; y++)
        for (int x = 0; x < n - 1; x++)
        {
            int i = y * n + x;
            int iRight = i + 1;
            int iDown = i + n;
            int iDownRight = i + n + 1;
            tris[t++] = i; tris[t++] = iRight; tris[t++] = iDown;
            tris[t++] = iDown; tris[t++] = iRight; tris[t++] = iDownRight;
        }
        return (pos, tris);
    }

    static void RunCase(int n, int iterations, int substeps, int frames, float dt)
    {
        var (pos0, tris) = MakeGrid(n, 0.05f);
        var velocities = new Vector3[pos0.Length];
        var solver = new PbdSolver();
        var p = new ClothParameters
        {
            UseGravity = true,
            GravityScale = 1.0f,
            StretchStiffness = 0.9f,
            BendStiffness = 0.5f,
            TetherStiffness = 0.0f,
            Iterations = iterations,
            Substeps = substeps,
            ComplianceScale = 1e-6f,
        };
        solver.Initialize(pos0, tris, p);
        // Pin top row
        var pins = Enumerable.Range(0, n).Select(i => i).ToArray();
        solver.PinVertices(pins);

        // Warmup
        for (int i = 0; i < 60; i++) solver.Step(dt, pos0, velocities);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < frames; i++) solver.Step(dt, pos0, velocities);
        sw.Stop();
        double ms = sw.Elapsed.TotalMilliseconds;
        double msPerFrame = ms / frames;
        double fps = 1000.0 / msPerFrame;
        Console.WriteLine($"Grid {n}x{n} V={pos0.Length}, Iters={iterations}, Substeps={substeps}, Frames={frames}: {ms:F1} ms total, {msPerFrame:F3} ms/frame (~{fps:F1} FPS)");
    }

    static void RunMultiCase(int instances, int n, int iterations, int substeps, int frames, float dt)
    {
        var solvers = new PbdSolver[instances];
        var positions = new Vector3[instances][];
        var velocities = new Vector3[instances][];
        var (templatePos, tris) = MakeGrid(n, 0.05f);
        for (int idx = 0; idx < instances; idx++)
        {
            positions[idx] = (Vector3[])templatePos.Clone();
            velocities[idx] = new Vector3[templatePos.Length];
            var solver = new PbdSolver();
            var p = new ClothParameters
            {
                UseGravity = true,
                GravityScale = 1.0f,
                StretchStiffness = 0.9f,
                BendStiffness = 0.5f,
                TetherStiffness = 0.0f,
                Iterations = iterations,
                Substeps = substeps,
                ComplianceScale = 1e-6f,
            };
            solver.Initialize(positions[idx], tris, p);
            // Pin top row
            var pins = Enumerable.Range(0, n).Select(i => i).ToArray();
            solver.PinVertices(pins);
            solvers[idx] = solver;
        }

        // Warmup
        for (int i = 0; i < 60; i++)
        {
            for (int s = 0; s < instances; s++) solvers[s].Step(dt, positions[s], velocities[s]);
        }

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < frames; i++)
        {
            for (int s = 0; s < instances; s++) solvers[s].Step(dt, positions[s], velocities[s]);
        }
        sw.Stop();
        double ms = sw.Elapsed.TotalMilliseconds;
        double msPerFrame = ms / frames;
        double fps = 1000.0 / msPerFrame;
        int verts = templatePos.Length;
        Console.WriteLine($"Instances={instances} Grid {n}x{n} V={verts} each, Iters={iterations}, Substeps={substeps}, Frames={frames}: {ms:F1} ms total, {msPerFrame:F3} ms/frame (~{fps:F1} FPS)");
    }

    static void Main(string[] args)
    {
        int frames = 300; // ~5s @ 60 FPS
        float dt = 1f / 60f;
        Console.WriteLine("DotCloth perf smoke");
        RunCase(n: 32, iterations: 8, substeps: 1, frames, dt);
        RunCase(n: 48, iterations: 10, substeps: 1, frames, dt);
        RunCase(n: 64, iterations: 10, substeps: 1, frames, dt);
        // Heavier
        RunCase(n: 64, iterations: 12, substeps: 2, frames, dt);

        Console.WriteLine();
        Console.WriteLine("Multi-instance (avatar-scale) tests");
        int instances = 40;
        RunMultiCase(instances, n: 20, iterations: 8, substeps: 1, frames, dt);
        RunMultiCase(instances, n: 24, iterations: 8, substeps: 1, frames, dt);
        RunMultiCase(instances, n: 28, iterations: 10, substeps: 1, frames, dt);
        RunMultiCase(instances, n: 32, iterations: 10, substeps: 1, frames, dt);
    }
}
