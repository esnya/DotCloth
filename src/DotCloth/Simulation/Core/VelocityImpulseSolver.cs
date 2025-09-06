using System;
using System.Collections.Generic;
using System.Numerics;
using DotCloth.Simulation.Collision;
using DotCloth.Simulation.Parameters;

namespace DotCloth.Simulation.Core;

/// <summary>
/// Velocity-level cloth solver using sequential impulses.
/// </summary>
public sealed class VelocityImpulseSolver : IClothSimulator
{
    private Config _cfg;
    private int _vertexCount;

    private float[] _invMass = Array.Empty<float>();
    private Vector3[] _prev = Array.Empty<Vector3>();

    private struct Edge
    {
        public int I;
        public int J;
        public float Rest;
        public float Wi;
        public float Wj;
        public float WSum;
    }
    private Edge[] _edges = Array.Empty<Edge>();
    private int[][] _edgeBatches = Array.Empty<int[]>();

    private struct Bend
    {
        public int K;
        public int L;
        public float Rest;
        public float Wk;
        public float Wl;
        public float WSum;
    }
    private Bend[] _bends = Array.Empty<Bend>();
    private int[][] _bendBatches = Array.Empty<int[]>();

    private Vector3[] _rest = Array.Empty<Vector3>();
    private int[] _tetherAnchorIndex = Array.Empty<int>();
    private float[] _tetherAnchorRestLength = Array.Empty<float>();

    private readonly List<ICollider> _colliders = new();

    /// <inheritdoc />
    public void SetColliders(IEnumerable<ICollider> colliders)
    {
        _colliders.Clear();
        _colliders.AddRange(colliders);
    }

    /// <inheritdoc />
    public void Initialize(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, ClothParameters parameters)
    {
        if (positions.Length == 0) throw new ArgumentException("positions empty", nameof(positions));
        if (triangles.Length % 3 != 0) throw new ArgumentException("triangles must be multiple of 3", nameof(triangles));
        _vertexCount = positions.Length;
        UpdateParameters(parameters);

        _invMass = new float[_vertexCount];
        var inv = 1.0f / _cfg.VertexMass;
        for (int i = 0; i < _vertexCount; i++) _invMass[i] = inv;
        _prev = new Vector3[_vertexCount];

        _rest = new Vector3[_vertexCount];
        for (int i = 0; i < _vertexCount; i++) _rest[i] = positions[i];
        _tetherAnchorIndex = new int[_vertexCount];
        Array.Fill(_tetherAnchorIndex, -1);
        _tetherAnchorRestLength = new float[_vertexCount];

        ValidateTriangles(triangles, _vertexCount);
        (_edges, _bends, _edgeBatches, _bendBatches) = BuildTopology(positions, triangles, _cfg);
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void Step(float deltaTime, Span<Vector3> positions, Span<Vector3> velocities)
    {
        if (positions.Length != _vertexCount) throw new ArgumentException("positions length mismatch", nameof(positions));
        if (velocities.Length != _vertexCount) throw new ArgumentException("velocities length mismatch", nameof(velocities));
        if (deltaTime <= 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));

        int substeps = Math.Max(1, _cfg.Substeps);
        int iterations = Math.Max(1, _cfg.Iterations);
        float dt = deltaTime / substeps;

        var gravity = _cfg.UseGravity ? new Vector3(0, -9.80665f * _cfg.GravityScale, 0) : Vector3.Zero;
        var accel = gravity + _cfg.ExternalAcceleration;
        bool useRandom = _cfg.RandomAcceleration > 0f;
        var rng = useRandom ? new Rng((uint)_cfg.RandomSeed) : default;
        float drag = Math.Max(0f, _cfg.AirDrag);
        float damping = Math.Clamp(_cfg.Damping, 0f, 0.999f);

        for (int s = 0; s < substeps; s++)
        {
            for (int i = 0; i < _vertexCount; i++) _prev[i] = positions[i];

            for (int i = 0; i < _vertexCount; i++)
            {
                if (_invMass[i] == 0f)
                {
                    velocities[i] = Vector3.Zero;
                    continue;
                }
                var v = velocities[i];
                var a = accel;
                if (useRandom)
                {
                    var dir = rng.NextUnitVector();
                    a += dir * _cfg.RandomAcceleration;
                }
                v += a * dt;
                v -= v * drag * dt;
                velocities[i] = v;
            }

            // iterate constraints
            for (int it = 0; it < iterations; it++)
            {
                for (int i = 0; i < _vertexCount; i++)
                {
                    positions[i] = _prev[i] + velocities[i] * dt;
                }

                float betaStretch = MapStiffnessToBeta(_cfg.StretchStiffness, dt, iterations);
                float betaBend = MapStiffnessToBeta(_cfg.BendStiffness, dt, iterations) * 0.5f;
                float betaTether = MapStiffnessToBeta(_cfg.TetherStiffness, dt, iterations);

                for (int b = 0; b < _edgeBatches.Length; b++)
                {
                    var batch = _edgeBatches[b];
                    for (int bi = 0; bi < batch.Length; bi++)
                    {
                        ref var e = ref _edges[batch[bi]];
                        int i = e.I; int j = e.J;
                        var xi = positions[i];
                        var xj = positions[j];
                        var d = xj - xi;
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        var n = d * invLen;
                        float C = (1f / invLen) - e.Rest;
                        float rel = Vector3.Dot(velocities[j] - velocities[i], n);
                        float bTerm = -betaStretch * C / dt;
                        float w = e.WSum;
                        if (w <= 0f) continue;
                        float lambda = -(rel + bTerm) / w;
                        lambda = Math.Clamp(lambda, -10f, 10f);
                        var dv = lambda * n;
                        velocities[i] -= e.Wi * dv;
                        velocities[j] += e.Wj * dv;
                    }
                }

                for (int i = 0; i < _vertexCount; i++)
                {
                    positions[i] = _prev[i] + velocities[i] * dt;
                }

                if (_bends.Length > 0 && _cfg.BendStiffness > 0f)
                {
                    for (int bb = 0; bb < _bendBatches.Length; bb++)
                    {
                        var batch = _bendBatches[bb];
                        for (int bi = 0; bi < batch.Length; bi++)
                        {
                            ref var bend = ref _bends[batch[bi]];
                            int k = bend.K; int l = bend.L;
                            var xk = positions[k];
                            var xl = positions[l];
                            var d = xl - xk;
                            var lenSq = d.LengthSquared();
                            if (lenSq <= 1e-18f) continue;
                            var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                            var n = d * invLen;
                            float C = (1f / invLen) - bend.Rest;
                            float rel = Vector3.Dot(velocities[l] - velocities[k], n);
                            float bTerm = -betaBend * C / dt;
                            float w = bend.WSum;
                            if (w <= 0f) continue;
                            float lambda = -(rel + bTerm) / w;
                            lambda = Math.Clamp(lambda, -10f, 10f);
                            var dv = lambda * n;
                            velocities[k] -= bend.Wk * dv;
                            velocities[l] += bend.Wl * dv;
                        }
                    }
                }

                for (int i = 0; i < _vertexCount; i++)
                {
                    positions[i] = _prev[i] + velocities[i] * dt;
                }

                if (_cfg.TetherStiffness > 0f)
                {
                    for (int i = 0; i < _vertexCount; i++)
                    {
                        float wi = _invMass[i];
                        if (wi <= 0f) continue;
                        int a = _tetherAnchorIndex[i];
                        Vector3 target; float wj; Vector3 vj;
                        float restLen;
                        if (a >= 0)
                        {
                            target = positions[a];
                            wj = _invMass[a];
                            vj = velocities[a];
                            restLen = _tetherAnchorRestLength[i];
                        }
                        else
                        {
                            target = _rest[i];
                            wj = 0f;
                            vj = Vector3.Zero;
                            restLen = 0f;
                        }
                        var d = target - positions[i];
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        var n = d * invLen;
                        float C = (1f / invLen) - restLen;
                        float rel = Vector3.Dot(vj - velocities[i], n);
                        float bTerm = -betaTether * C / dt;
                        float w = wi + wj;
                        if (w <= 0f) continue;
                        float lambda = -(rel + bTerm) / w;
                        lambda = Math.Clamp(lambda, -10f, 10f);
                        var dv = lambda * n;
                        velocities[i] += wi * dv;
                        if (wj > 0f)
                            velocities[a] -= wj * dv;
                    }
                }
            }

            // final position update after iterations
            for (int i = 0; i < _vertexCount; i++)
            {
                positions[i] = _prev[i] + velocities[i] * dt;
                velocities[i] *= (1f - damping);
            }

            if (_colliders.Count > 0)
            {
                foreach (var c in _colliders)
                {
                    c.Resolve(_prev, positions, velocities, dt, _cfg.CollisionThickness, _cfg.Friction);
                }
            }

#if DOTCLOTH_ENABLE_VELOCITY_CLAMP
            ClampVelocities(dt, positions, velocities);
#endif
        }
    }

    /// <inheritdoc />
    public void UpdateParameters(ClothParameters parameters)
    {
        if (parameters is null) throw new ArgumentNullException(nameof(parameters));
        _cfg = Config.From(parameters);
        if (_edges.Length > 0) RecomputeEdgeMasses();
        if (_bends.Length > 0) RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void SetInverseMasses(ReadOnlySpan<float> inverseMasses)
    {
        if (inverseMasses.Length != _vertexCount) throw new ArgumentException("inverseMasses length mismatch", nameof(inverseMasses));
        for (int i = 0; i < _vertexCount; i++) _invMass[i] = Math.Max(0f, inverseMasses[i]);
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void ResetRestState(ReadOnlySpan<Vector3> positions)
    {
        if (positions.Length != _vertexCount) throw new ArgumentException("positions length mismatch", nameof(positions));
        for (int i = 0; i < _vertexCount; i++)
        {
            _rest[i] = positions[i];
        }
        for (int e = 0; e < _edges.Length; e++)
        {
            var (i, j) = (_edges[e].I, _edges[e].J);
            _edges[e].Rest = Vector3.Distance(positions[i], positions[j]);
        }
        for (int b = 0; b < _bends.Length; b++)
        {
            var (k, l) = (_bends[b].K, _bends[b].L);
            _bends[b].Rest = Vector3.Distance(positions[k], positions[l]);
        }
    }

    /// <inheritdoc />
    public void PinVertices(ReadOnlySpan<int> indices)
    {
        for (int n = 0; n < indices.Length; n++)
        {
            int i = indices[n];
            if ((uint)i >= (uint)_vertexCount) throw new ArgumentOutOfRangeException(nameof(indices));
            _invMass[i] = 0f;
        }
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void PinVertices(params int[] indices) => PinVertices((ReadOnlySpan<int>)indices);

    /// <inheritdoc />
    public void UnpinVertices(ReadOnlySpan<int> indices)
    {
        float inv = 1.0f / _cfg.VertexMass;
        for (int n = 0; n < indices.Length; n++)
        {
            int i = indices[n];
            if ((uint)i >= (uint)_vertexCount) throw new ArgumentOutOfRangeException(nameof(indices));
            _invMass[i] = inv;
        }
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void UnpinVertices(params int[] indices) => UnpinVertices((ReadOnlySpan<int>)indices);

    /// <inheritdoc />
    public void ClearPins()
    {
        float inv = 1.0f / _cfg.VertexMass;
        for (int i = 0; i < _vertexCount; i++) _invMass[i] = inv;
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void SetTetherAnchors(ReadOnlySpan<int> anchors)
    {
        foreach (var a in anchors)
        {
            if ((uint)a >= (uint)_vertexCount) throw new ArgumentOutOfRangeException(nameof(anchors));
        }
        if (anchors.Length == 0)
        {
            for (int i = 0; i < _vertexCount; i++) _tetherAnchorIndex[i] = -1;
            return;
        }
        for (int i = 0; i < _vertexCount; i++)
        {
            int best = -1;
            float bestD2 = float.PositiveInfinity;
            var xi0 = _rest[i];
            foreach (var a in anchors)
            {
                var d = xi0 - _rest[a];
                float d2 = d.LengthSquared();
                if (d2 < bestD2)
                {
                    bestD2 = d2;
                    best = a;
                }
            }
            _tetherAnchorIndex[i] = best;
            float restLen = best >= 0 ? MathF.Sqrt(bestD2) * _cfg.TetherLengthScale : 0f;
            _tetherAnchorRestLength[i] = restLen;
        }
    }

    private void ClampVelocities(float dt, Span<Vector3> positions, Span<Vector3> velocities)
    {
        float strainLimit = 0.08f;
        float kClamp = 0.5f;
        for (int e = 0; e < _edges.Length; e++)
        {
            var edge = _edges[e];
            int i = edge.I; int j = edge.J;
            var d = positions[j] - positions[i];
            float L = d.Length();
            if (L < 1e-7f) continue;
            float s = L / edge.Rest;
            if (s <= 1f + strainLimit) continue;
            var n = d / L;
            float rel = Vector3.Dot(velocities[j] - velocities[i], n);
            float targetRel = -kClamp * (s - (1f + strainLimit)) / dt;
            float corr = rel - targetRel;
            float w = edge.WSum;
            if (w <= 0f) continue;
            float lambda = corr / w;
            var dv = lambda * n;
            velocities[i] -= edge.Wi * dv;
            velocities[j] += edge.Wj * dv;
            positions[i] = _prev[i] + velocities[i] * dt;
            positions[j] = _prev[j] + velocities[j] * dt;
        }
    }

    private static float MapStiffnessToBeta(float s, float dt, int iterations)
    {
        float baseBeta = 0.05f + 0.45f * s;
        float iterScale = MathF.Min(1f, iterations / 4f);
        return MathF.Min(0.6f, baseBeta * iterScale);
    }

    private static void ValidateTriangles(ReadOnlySpan<int> triangles, int vertexCount)
    {
        for (int i = 0; i < triangles.Length; i++)
        {
            if ((uint)triangles[i] >= (uint)vertexCount)
                throw new ArgumentOutOfRangeException(nameof(triangles));
        }
    }

    private static (Edge[] edges, Bend[] bends, int[][] edgeBatches, int[][] bendBatches) BuildTopology(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, Config cfg)
    {
        var set = new HashSet<(int, int)>();
        var opp = new Dictionary<(int,int), (int a, int b)>();
        void AddEdge(int a, int b, int c)
        {
            var key = a < b ? (a, b) : (b, a);
            if (set.Add(key))
            {
                opp[key] = (c, -1);
            }
            else
            {
                var val = opp[key];
                opp[key] = (val.a, c);
            }
        }
        for (int t = 0; t < triangles.Length; t += 3)
        {
            int i = triangles[t];
            int j = triangles[t + 1];
            int k = triangles[t + 2];
            AddEdge(i, j, k);
            AddEdge(j, k, i);
            AddEdge(k, i, j);
        }
        var edges = new List<Edge>();
        var bends = new List<Bend>();
        foreach (var kvp in set)
        {
            var (i, j) = kvp;
            float rest = Vector3.Distance(positions[i], positions[j]);
            edges.Add(new Edge { I = i, J = j, Rest = rest });
        }
        foreach (var kvp in opp)
        {
            if (kvp.Value.b >= 0)
            {
                var e = kvp.Key;
                int a = kvp.Value.a;
                int b = kvp.Value.b;
                float rest = Vector3.Distance(positions[a], positions[b]);
                bends.Add(new Bend { K = a, L = b, Rest = rest });
            }
        }
        // trivial batching: single batch
        return (edges.ToArray(), bends.ToArray(), new[] { Enumerable.Range(0, edges.Count).ToArray() }, new[] { Enumerable.Range(0, bends.Count).ToArray() });
    }

    private void RecomputeEdgeMasses()
    {
        for (int i = 0; i < _edges.Length; i++)
        {
            ref var e = ref _edges[i];
            e.Wi = _invMass[e.I];
            e.Wj = _invMass[e.J];
            e.WSum = e.Wi + e.Wj;
        }
    }

    private void RecomputeBendMasses()
    {
        for (int i = 0; i < _bends.Length; i++)
        {
            ref var b = ref _bends[i];
            b.Wk = _invMass[b.K];
            b.Wl = _invMass[b.L];
            b.WSum = b.Wk + b.Wl;
        }
    }

    private readonly struct Config
    {
        public readonly bool UseGravity;
        public readonly float GravityScale;
        public readonly float Damping;
        public readonly float AirDrag;
        public readonly float StretchStiffness;
        public readonly float BendStiffness;
        public readonly float TetherStiffness;
        public readonly float CollisionThickness;
        public readonly float Friction;
        public readonly float VertexMass;
        public readonly Vector3 ExternalAcceleration;
        public readonly float RandomAcceleration;
        public readonly int RandomSeed;
        public readonly int Iterations;
        public readonly int Substeps;
        public readonly float TetherLengthScale;

        private Config(bool useGravity, float gravityScale, float damping, float airDrag,
            float stretch, float bend, float tether, float thickness, float friction,
            float vertexMass, Vector3 externalAccel, float randomAccel, int randomSeed,
            int iterations, int substeps, float tetherLengthScale)
        {
            UseGravity = useGravity;
            GravityScale = gravityScale;
            Damping = damping;
            AirDrag = airDrag;
            StretchStiffness = stretch;
            BendStiffness = bend;
            TetherStiffness = tether;
            CollisionThickness = thickness;
            Friction = friction;
            VertexMass = vertexMass;
            ExternalAcceleration = externalAccel;
            RandomAcceleration = randomAccel;
            RandomSeed = randomSeed;
            Iterations = iterations;
            Substeps = substeps;
            TetherLengthScale = tetherLengthScale;
        }

        public static Config From(ClothParameters p)
        {
            return new Config(
                p.UseGravity,
                Math.Max(0f, p.GravityScale),
                float.Clamp(p.Damping, 0f, 0.999f),
                Math.Max(0f, p.AirDrag),
                float.Clamp(p.StretchStiffness, 0f, 1f),
                float.Clamp(p.BendStiffness, 0f, 1f),
                float.Clamp(p.TetherStiffness, 0f, 1f),
                Math.Max(0f, p.CollisionThickness),
                float.Clamp(p.Friction, 0f, 1f),
                Math.Max(1e-6f, p.VertexMass),
                p.ExternalAcceleration,
                Math.Max(0f, p.RandomAcceleration),
                p.RandomSeed,
                Math.Max(1, p.Iterations),
                Math.Max(1, p.Substeps),
                Math.Max(0f, p.TetherLengthScale)
            );
        }
    }

    private struct Rng
    {
        private uint _state;
        public Rng(uint seed) => _state = seed == 0 ? 1u : seed;
        public Vector3 NextUnitVector()
        {
            _state = 1664525u * _state + 1013904223u;
            uint bits = _state;
            float x = (bits & 0x3FF) / 1024f * 2f - 1f;
            float y = ((bits >> 10) & 0x3FF) / 1024f * 2f - 1f;
            float z = ((bits >> 20) & 0x3FF) / 1024f * 2f - 1f;
            var v = new Vector3(x, y, z);
            float len = v.Length();
            return len > 1e-9f ? v / len : Vector3.UnitX;
        }
    }
}
