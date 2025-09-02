using System.Numerics;
using DotCloth.Simulation.Parameters;

namespace DotCloth.Simulation.Core;

/// <summary>
/// XPBD solver with stretch constraints and placeholders for future constraints.
/// </summary>
public sealed class PbdSolver : IClothSimulator
{
    private ClothParameters _p = new();
    private int _vertexCount;

    // Mass/inertia
    private float[] _invMass = Array.Empty<float>();

    // Edge stretch constraints (unique undirected)
    private struct Edge
    {
        public int I;
        public int J;
        public float RestLength;
        public float Compliance;
        public float Lambda; // XPBD running Lagrange multiplier
    }

    private Edge[] _edges = Array.Empty<Edge>();

    // Collision hooks (optional)
    private readonly List<ICollider> _colliders = new();
    public void SetColliders(IEnumerable<ICollider> colliders)
    {
        _colliders.Clear();
        _colliders.AddRange(colliders);
    }

    public void Initialize(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, ClothParameters parameters)
    {
        if (positions.Length == 0) throw new ArgumentException("positions empty", nameof(positions));
        if (triangles.Length % 3 != 0) throw new ArgumentException("triangles must be multiple of 3", nameof(triangles));
        _vertexCount = positions.Length;
        UpdateParameters(parameters);

        // Mass setup (uniform per parameters)
        _invMass = new float[_vertexCount];
        var inv = 1.0f / _p.VertexMass;
        for (int i = 0; i < _vertexCount; i++) _invMass[i] = inv;

        // Build unique edges from triangles and set rest lengths
        _edges = BuildEdges(positions, triangles, _p);
    }

    public void Step(float deltaTime, Span<Vector3> positions, Span<Vector3> velocities)
    {
        if (positions.Length != _vertexCount) throw new ArgumentException("positions length mismatch", nameof(positions));
        if (velocities.Length != _vertexCount) throw new ArgumentException("velocities length mismatch", nameof(velocities));
        if (deltaTime <= 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));

        int substeps = Math.Max(1, _p.Substeps);
        int iterations = Math.Max(1, _p.Iterations);
        float dt = deltaTime / substeps;

        var gravity = _p.UseGravity ? new Vector3(0, -9.80665f * _p.GravityScale, 0) : Vector3.Zero;
        var accel = gravity + _p.ExternalAcceleration;

        var drag = Math.Max(0f, _p.AirDrag);
        var damping = Math.Clamp(_p.Damping, 0f, 0.999f);

        var prev = new Vector3[_vertexCount];

        for (int s = 0; s < substeps; s++)
        {
            // Save previous positions for velocity update
            for (int i = 0; i < _vertexCount; i++) prev[i] = positions[i];

            // Integrate external acceleration (semi-implicit Euler) and predict positions
            for (int i = 0; i < _vertexCount; i++)
            {
                var v = velocities[i];
                // Acceleration
                v += accel * dt;
                // Drag (approx.)
                v -= v * drag * dt;
                velocities[i] = v;
                positions[i] += v * dt;
            }

            // XPBD iterations for stretch constraints
            for (int it = 0; it < iterations; it++)
            {
                for (int e = 0; e < _edges.Length; e++)
                {
                    ref var edge = ref _edges[e];
                    int i = edge.I;
                    int j = edge.J;
                    var xi = positions[i];
                    var xj = positions[j];
                    var d = xj - xi;
                    var len = d.Length();
                    if (len <= 1e-9f)
                    {
                        continue;
                    }
                    var n = d / len; // normalized direction
                    float C = len - edge.RestLength;

                    float wi = _invMass[i];
                    float wj = _invMass[j];
                    float wsum = wi + wj;
                    if (wsum <= 0f) continue;

                    float alpha = edge.Compliance;
                    float alphaTilde = alpha / (dt * dt);
                    float dlambda = (-C - alphaTilde * edge.Lambda) / (wsum + alphaTilde);
                    edge.Lambda += dlambda;

                    var corr = dlambda * n;
                    positions[i] -= wi * corr;
                    positions[j] += wj * corr;
                }
            }

            // Collisions (optional hooks)
            if (_colliders.Count > 0)
            {
                foreach (var c in _colliders)
                {
                    c.Resolve(positions, velocities, dt, _p.CollisionThickness, _p.Friction);
                }
            }

            // Update velocities from positions delta, then apply damping
            for (int i = 0; i < _vertexCount; i++)
            {
                var v = (positions[i] - prev[i]) / dt;
                v *= (1.0f - damping);
                velocities[i] = v;
            }
        }
    }

    public void UpdateParameters(ClothParameters parameters)
    {
        _p = parameters ?? throw new ArgumentNullException(nameof(parameters));
        _p.Damping = float.Clamp(_p.Damping, 0f, 0.999f);
        _p.StretchStiffness = float.Clamp(_p.StretchStiffness, 0f, 1f);
        _p.BendStiffness = float.Clamp(_p.BendStiffness, 0f, 1f);
        _p.TetherStiffness = float.Clamp(_p.TetherStiffness, 0f, 1f);
        _p.Friction = float.Clamp(_p.Friction, 0f, 1f);
        _p.CollisionThickness = Math.Max(0f, _p.CollisionThickness);
        _p.VertexMass = Math.Max(1e-6f, _p.VertexMass);
        _p.AirDrag = Math.Max(0f, _p.AirDrag);
        _p.RandomAcceleration = Math.Max(0f, _p.RandomAcceleration);
        _p.Iterations = Math.Max(1, _p.Iterations);
        _p.Substeps = Math.Max(1, _p.Substeps);
        _p.ComplianceScale = Math.Max(0f, _p.ComplianceScale);

        // Update compliance on existing edges if any
        if (_edges.Length > 0)
        {
            for (int e = 0; e < _edges.Length; e++)
            {
                _edges[e].Compliance = MapStiffnessToCompliance(_p.StretchStiffness, _p.ComplianceScale);
            }
        }
    }

    private static Edge[] BuildEdges(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, ClothParameters p)
    {
        var set = new HashSet<(int, int)>();
        void Add(int a, int b)
        {
            int i = Math.Min(a, b);
            int j = Math.Max(a, b);
            set.Add((i, j));
        }

        for (int t = 0; t < triangles.Length; t += 3)
        {
            int a = triangles[t];
            int b = triangles[t + 1];
            int c = triangles[t + 2];
            Add(a, b);
            Add(b, c);
            Add(c, a);
        }

        var edges = new Edge[set.Count];
        int k = 0;
        float compliance = MapStiffnessToCompliance(p.StretchStiffness, p.ComplianceScale);
        foreach (var (i, j) in set)
        {
            var rest = Vector3.Distance(positions[i], positions[j]);
            edges[k++] = new Edge { I = i, J = j, RestLength = rest, Compliance = compliance, Lambda = 0f };
        }
        return edges;
    }

    private static float MapStiffnessToCompliance(float stiffness01, float scale)
    {
        // Map [0..1] stiffness to XPBD compliance alpha >= 0. Lower alpha = stiffer.
        // Quadratic emphasis near 1 for stability.
        var s = float.Clamp(stiffness01, 0f, 1f);
        var softness = 1f - s;
        return softness * softness * Math.Max(1e-12f, scale);
    }
}
