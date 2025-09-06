#if DOTCLOTH_EXPERIMENTAL_XPBD
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using DotCloth.Simulation.Parameters;

namespace DotCloth.Simulation.Core;

/// <summary>
/// XPBD solver with stretch constraints and placeholders for future constraints.
/// </summary>
public sealed class XpbdSolver : IClothSimulator
{
    private Config _cfg;
    private int _vertexCount;

    // Mass/inertia
    private float[] _invMass = Array.Empty<float>();
    private Vector3[] _prev = Array.Empty<Vector3>();

    // Edge stretch constraints (unique undirected)
    private struct Edge
    {
        public int I;
        public int J;
        public float RestLength;
        public float Compliance;
        public float Lambda; // XPBD running Lagrange multiplier
        public float Wi;
        public float Wj;
        public float WSum;
    }

    private Edge[] _edges = Array.Empty<Edge>();
    private int[][] _edgeBatches = Array.Empty<int[]>();
    private float[] _edgeAlphaTilde = Array.Empty<float>();

    // Bend constraints across opposite vertices of adjacent triangles
    private struct Bend
    {
        public int K;
        public int L;
        public float RestDistance;
        public float Compliance;
        public float Lambda;
        public float Wk;
        public float Wl;
        public float WSum;
    }
    private Bend[] _bends = Array.Empty<Bend>();
    private int[][] _bendBatches = Array.Empty<int[]>();
    private float[] _bendAlphaTilde = Array.Empty<float>();

    // Tether-to-rest constraints (per-vertex to rest position)
    private Vector3[] _rest = Array.Empty<Vector3>();
    private float[] _tetherLambda = Array.Empty<float>();
    private int[] _tetherAnchorIndex = Array.Empty<int>();
    private float[] _tetherAnchorRestLength = Array.Empty<float>();

    // Collision hooks (optional)
    private readonly List<Collision.ICollider> _colliders = new();
    /// <inheritdoc />
    public void SetColliders(IEnumerable<Collision.ICollider> colliders)
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

        // Mass setup (uniform per parameters)
        _invMass = new float[_vertexCount];
        var inv = 1.0f / _cfg.VertexMass;
        for (int i = 0; i < _vertexCount; i++) _invMass[i] = inv;
        _prev = new Vector3[_vertexCount];

        // Rest state
        _rest = new Vector3[_vertexCount];
        for (int i = 0; i < _vertexCount; i++) _rest[i] = positions[i];
        _tetherLambda = new float[_vertexCount];
        _tetherAnchorIndex = Enumerable.Repeat(-1, _vertexCount).ToArray();
        _tetherAnchorRestLength = new float[_vertexCount];

        // Build unique edges from triangles and set rest lengths
        ValidateTriangles(triangles, _vertexCount);
        (_edges, _bends, _edgeBatches, _bendBatches) = BuildTopology(positions, triangles, _cfg);
        SortBatchesByVertexIndex();
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void Step(float deltaTime, Span<Vector3> positions, Span<Vector3> velocities)
    {
        if (positions.Length != _vertexCount) throw new ArgumentException("positions length mismatch", nameof(positions));
        if (velocities.Length != _vertexCount) throw new ArgumentException("velocities length mismatch", nameof(velocities));
        if (deltaTime <= 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));

        int substeps = Math.Max(1, _cfg.Substeps);
        int iterations = Math.Max(1, _cfg.Iterations);
        float dt = deltaTime / substeps;
        float invDt2 = 1.0f / (dt * dt);

        // Precompute alphaTilde per constraint to avoid repeated multiplications
        if (_edgeAlphaTilde.Length != _edges.Length) _edgeAlphaTilde = new float[_edges.Length];
        for (int e = 0; e < _edges.Length; e++) _edgeAlphaTilde[e] = _edges[e].Compliance * invDt2;
        if (_bendAlphaTilde.Length != _bends.Length) _bendAlphaTilde = new float[_bends.Length];
        for (int b = 0; b < _bends.Length; b++) _bendAlphaTilde[b] = _bends[b].Compliance * invDt2;

        var gravity = _cfg.UseGravity ? new Vector3(0, -9.80665f * _cfg.GravityScale, 0) : Vector3.Zero;
        var accel = gravity + _cfg.ExternalAcceleration;
        var useRandom = _cfg.RandomAcceleration > 0f;
        var rng = useRandom ? new Rng((uint)_cfg.RandomSeed) : default;

        var drag = Math.Max(0f, _cfg.AirDrag);
        var damping = Math.Clamp(_cfg.Damping, 0f, 0.999f);

        for (int s = 0; s < substeps; s++)
        {
            // Save previous positions for velocity update
            for (int i = 0; i < _vertexCount; i++) _prev[i] = positions[i];

            // Integrate external acceleration (semi-implicit Euler) and predict positions
            for (int i = 0; i < _vertexCount; i++)
            {
                if (_invMass[i] == 0f)
                {
                    velocities[i] = Vector3.Zero;
                    continue;
                }
                var v = velocities[i];
                // Acceleration
                var a = accel;
                if (useRandom)
                {
                    var dir = rng.NextUnitVector();
                    a += dir * _cfg.RandomAcceleration;
                }
                v += a * dt;
                // Drag (approx.)
                v -= v * drag * dt;
                velocities[i] = v;
                positions[i] += v * dt;
            }

            // XPBD iterations for stretch, bending, and tether constraints
            for (int it = 0; it < iterations; it++)
            {
                // Stretch
                for (int b = 0; b < _edgeBatches.Length; b++)
                {
                    var batch = _edgeBatches[b];
                    for (int bi = 0; bi < batch.Length; bi++)
                    {
                        int e = batch[bi];
                        ref var edge = ref _edges[e];
                        int i = edge.I;
                        int j = edge.J;
                        var xi = positions[i];
                        var xj = positions[j];
                        var d = xj - xi;
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        var len = 1f / invLen;
                        var n = d * invLen;
                        float C = len - edge.RestLength;

                        float wi = edge.Wi;
                        float wj = edge.Wj;
                        float wsum = edge.WSum;
                        if (wsum <= 0f) continue;

                        float alphaTilde = _edgeAlphaTilde[e];
                        float dlambda = (-C - alphaTilde * edge.Lambda) / (wsum + alphaTilde);
                        edge.Lambda += dlambda;

                        var corr = dlambda * n;
                        positions[i] -= wi * corr;
                        positions[j] += wj * corr;
                    }
                }

                // Bending (distance across opposite vertices)
                if (_bends.Length > 0 && _cfg.BendStiffness > 0f)
                {
                    for (int bb = 0; bb < _bendBatches.Length; bb++)
                    {
                        var batch = _bendBatches[bb];
                        for (int bi = 0; bi < batch.Length; bi++)
                        {
                            int biIdx = batch[bi];
                            ref var bend = ref _bends[biIdx];
                            int k = bend.K;
                            int l = bend.L;
                            var xk = positions[k];
                            var xl = positions[l];
                            var d = xl - xk;
                            var lenSq = d.LengthSquared();
                            if (lenSq <= 1e-18f) continue;
                            var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                            var len = 1f / invLen;
                            var n = d * invLen;
                            float C = len - bend.RestDistance;
                            float wk = bend.Wk;
                            float wl = bend.Wl;
                            float wsum = bend.WSum;
                            if (wsum <= 0f) continue;

                            float alphaTilde = _bendAlphaTilde[biIdx];
                            float dlambda = (-C - alphaTilde * bend.Lambda) / (wsum + alphaTilde);
                            bend.Lambda += dlambda;
                            var corr = dlambda * n;
                            positions[k] -= wk * corr;
                            positions[l] += wl * corr;
                        }
                    }
                }

                // Tethers to rest positions or anchors
                if (_cfg.TetherStiffness > 0f)
                {
                    float alphaTilde = MapStiffnessToCompliance(_cfg.TetherStiffness, _cfg.ComplianceScale) * invDt2;
                    for (int i = 0; i < _vertexCount; i++)
                    {
                        float wi = _invMass[i];
                        if (wi <= 0f) continue;
                        var xi = positions[i];
                        Vector3 target;
                        float targetLen;
                        int a = _tetherAnchorIndex[i];
                        if (a >= 0)
                        {
                            target = positions[a];
                            targetLen = _tetherAnchorRestLength[i];
                        }
                        else
                        {
                            target = _rest[i];
                            targetLen = 0f; // pull to rest position exactly
                        }

                        var d = xi - target;
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f)
                        {
                            _tetherLambda[i] = 0f; // reset when satisfied
                            continue;
                        }
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        var len = 1f / invLen;
                        // Direction toward the target
                        var n = (target - xi) * invLen;
                        float C = len - targetLen;
                        float dlambda = (-C - alphaTilde * _tetherLambda[i]) / (wi + alphaTilde);
                        _tetherLambda[i] += dlambda;
                        var corr = dlambda * n;
                        positions[i] -= wi * corr;
                    }
                }
            }

            // Collisions (optional hooks)
            if (_colliders.Count > 0)
            {
                foreach (var c in _colliders)
                {
                    c.Resolve(_prev, positions, velocities, dt, _cfg.CollisionThickness, _cfg.Friction);
                }
            }

            // Update velocities from positions delta, then apply damping
            for (int i = 0; i < _vertexCount; i++)
            {
                if (_invMass[i] == 0f)
                {
                    velocities[i] = Vector3.Zero;
                    continue;
                }
                var v = (positions[i] - _prev[i]) / dt;
                v *= (1.0f - damping);
                velocities[i] = v;
            }
        }
    }

    /// <inheritdoc />
    public void UpdateParameters(ClothParameters parameters)
    {
        if (parameters is null) throw new ArgumentNullException(nameof(parameters));
        _cfg = Config.From(parameters);

        // Update compliance on existing edges if any
        if (_edges.Length > 0)
        {
            for (int e = 0; e < _edges.Length; e++)
            {
                _edges[e].Compliance = MapStiffnessToCompliance(_cfg.StretchStiffness, _cfg.ComplianceScale);
            }
        }
        if (_bends.Length > 0)
        {
            for (int b = 0; b < _bends.Length; b++)
            {
                _bends[b].Compliance = MapStiffnessToCompliance(_cfg.BendStiffness, _cfg.ComplianceScale);
            }
        }
    }

    private static (Edge[] edges, Bend[] bends, int[][] edgeBatches, int[][] bendBatches) BuildTopology(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, Config cfg)
    {
        var set = new HashSet<(int, int)>();
        // edge -> list of opposite vertices (tri third index)
        var opp = new Dictionary<(int,int), (int a, int b)>();
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

            // track opposite vertices per undirected edge
            AddOpp(a,b,c);
            AddOpp(b,c,a);
            AddOpp(c,a,b);
        }

        var edges = new Edge[set.Count];
        int k = 0;
        float compliance = MapStiffnessToCompliance(cfg.StretchStiffness, cfg.ComplianceScale);
        foreach (var (i, j) in set)
        {
            var rest = Vector3.Distance(positions[i], positions[j]);
            edges[k++] = new Edge { I = i, J = j, RestLength = rest, Compliance = compliance, Lambda = 0f };
        }

        // Build bends from opposite pairs across shared edges
        var bendsList = new List<Bend>();
        float bCompliance = MapStiffnessToCompliance(cfg.BendStiffness, cfg.ComplianceScale);
        foreach (var kv in opp)
        {
            var (i,j) = kv.Key;
            var pair = kv.Value;
            if (pair.a >= 0 && pair.b >= 0)
            {
                int kIdx = pair.a;
                int lIdx = pair.b;
                // distance-based bending across opposite vertices
                float rest = Vector3.Distance(positions[kIdx], positions[lIdx]);
                bendsList.Add(new Bend { K = kIdx, L = lIdx, RestDistance = rest, Compliance = bCompliance, Lambda = 0f });
            }
        }
        var bends = bendsList.ToArray();

        // Build batches (greedy coloring) to avoid shared vertices per batch
        int[][] edgeBatches = BuildBatchesForPairs(edges.Select(e => (e.I, e.J)), positions.Length);
        int[][] bendBatches = BuildBatchesForPairs(bends.Select(b => (b.K, b.L)), positions.Length);
        return (edges, bends, edgeBatches, bendBatches);

        void AddOpp(int a, int b, int c)
        {
            int i = Math.Min(a, b);
            int j = Math.Max(a, b);
            var key = (i, j);
            if (!opp.TryGetValue(key, out var val))
            {
                opp[key] = (c, -1);
            }
            else
            {
                if (val.b < 0)
                {
                    opp[key] = (val.a, c);
                }
            }
        }
    }

    private static int[][] BuildBatchesForPairs(IEnumerable<(int a, int b)> pairs, int vertexCount)
    {
        var pairList = pairs.ToList();
        var batches = new List<List<int>>();
        // Build incidence map to speed up checks (optional: for simplicity use set per batch)
        for (int idx = 0; idx < pairList.Count; idx++)
        {
            var (a, b) = pairList[idx];
            bool placed = false;
            for (int bi = 0; bi < batches.Count && !placed; bi++)
            {
                var used = new HashSet<int>();
                // Recompute used vertices of batch bi
                foreach (var ei in batches[bi])
                {
                    var p = pairList[ei];
                    used.Add(p.a);
                    used.Add(p.b);
                }
                if (!used.Contains(a) && !used.Contains(b))
                {
                    batches[bi].Add(idx);
                    placed = true;
                }
            }
            if (!placed)
            {
                batches.Add(new List<int> { idx });
            }
        }
        return batches.Select(l => l.ToArray()).ToArray();
    }

    private void SortBatchesByVertexIndex()
    {
        // Sort edges in each batch by min(vertex indices) to improve locality
        for (int bi = 0; bi < _edgeBatches.Length; bi++)
        {
            Array.Sort(_edgeBatches[bi], (x, y) =>
            {
                var ex = _edges[x];
                var ey = _edges[y];
                int kx = ex.I < ex.J ? ex.I : ex.J;
                int ky = ey.I < ey.J ? ey.I : ey.J;
                return kx.CompareTo(ky);
            });
        }
        for (int bi = 0; bi < _bendBatches.Length; bi++)
        {
            Array.Sort(_bendBatches[bi], (x, y) =>
            {
                var bx = _bends[x];
                var by = _bends[y];
                int kx = bx.K < bx.L ? bx.K : bx.L;
                int ky = by.K < by.L ? by.K : by.L;
                return kx.CompareTo(ky);
            });
        }
    }

    private static float MapStiffnessToCompliance(float stiffness01, float scale)
    {
        // Map [0..1] stiffness to XPBD compliance alpha >= 0. Lower alpha = stiffer.
        // Quadratic emphasis near 1 for stability.
        var s = float.Clamp(stiffness01, 0f, 1f);
        var softness = 1f - s;
        return softness * softness * Math.Max(1e-12f, scale);
    }

    private void RecomputeEdgeMasses()
    {
        for (int e = 0; e < _edges.Length; e++)
        {
            ref var edge = ref _edges[e];
            edge.Wi = _invMass[edge.I];
            edge.Wj = _invMass[edge.J];
            edge.WSum = edge.Wi + edge.Wj;
        }
    }

    private void RecomputeBendMasses()
    {
        for (int b = 0; b < _bends.Length; b++)
        {
            ref var bend = ref _bends[b];
            bend.Wk = _invMass[bend.K];
            bend.Wl = _invMass[bend.L];
            bend.WSum = bend.Wk + bend.Wl;
        }
    }

    private static void ValidateTriangles(ReadOnlySpan<int> tris, int vertexCount)
    {
        for (int t = 0; t < tris.Length; t++)
        {
            int idx = tris[t];
            if ((uint)idx >= (uint)vertexCount)
            {
                throw new ArgumentOutOfRangeException(nameof(tris), $"triangle index {idx} out of range [0,{vertexCount - 1}]");
            }
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
        public readonly float ComplianceScale;
        public readonly float TetherLengthScale;

        private Config(
            bool useGravity, float gravityScale, float damping, float airDrag,
            float stretch, float bend, float tether, float thickness, float friction,
            float vertexMass, Vector3 externalAccel, float randomAccel, int randomSeed, int iterations, int substeps, float complianceScale, float tetherLengthScale)
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
            ComplianceScale = complianceScale;
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
                Math.Max(0f, p.ComplianceScale),
                Math.Max(0f, p.TetherLengthScale)
            );
        }
    }

    private struct Rng
    {
        private uint _state;
        public Rng(uint seed) { _state = seed == 0 ? 1u : seed; }
        public uint NextU32()
        {
            // Xorshift32
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }
        public float NextFloat01()
        {
            return (NextU32() & 0xFFFFFF) / (float)0x1000000; // [0,1)
        }
        public Vector3 NextUnitVector()
        {
            // Marsaglia method
            float u = 2f * NextFloat01() - 1f;
            float v = 2f * NextFloat01() - 1f;
            float s = u * u + v * v;
            if (s >= 1f || s <= 1e-12f) return new Vector3(1, 0, 0);
            float f = MathF.Sqrt(1f - s);
            return new Vector3(2f * u * f, 2f * v * f, 1f - 2f * s);
        }
    }

    /// <inheritdoc />
    public void SetInverseMasses(ReadOnlySpan<float> inverseMasses)
    {
        if (inverseMasses.Length != _vertexCount) throw new ArgumentException("inverseMasses length mismatch", nameof(inverseMasses));
        for (int i = 0; i < _vertexCount; i++)
        {
            _invMass[i] = Math.Max(0f, inverseMasses[i]);
        }
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void ResetRestState(ReadOnlySpan<Vector3> positions)
    {
        if (positions.Length != _vertexCount) throw new ArgumentException("positions length mismatch", nameof(positions));
        // Recompute edge and bend rest values; keep topology
        for (int i = 0; i < _vertexCount; i++)
        {
            _rest[i] = positions[i];
            _tetherLambda[i] = 0f;
        }
        for (int e = 0; e < _edges.Length; e++)
        {
            var (i, j) = (_edges[e].I, _edges[e].J);
            _edges[e].RestLength = Vector3.Distance(positions[i], positions[j]);
            _edges[e].Lambda = 0f;
        }
        for (int b = 0; b < _bends.Length; b++)
        {
            var (k, l) = (_bends[b].K, _bends[b].L);
            _bends[b].RestDistance = Vector3.Distance(positions[k], positions[l]);
            _bends[b].Lambda = 0f;
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
    public void PinVertices(params int[] indices)
    {
        PinVertices((ReadOnlySpan<int>)indices);
    }

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
    public void UnpinVertices(params int[] indices)
    {
        UnpinVertices((ReadOnlySpan<int>)indices);
    }

    /// <inheritdoc />
    public void ClearPins()
    {
        float inv = 1.0f / _cfg.VertexMass;
        for (int i = 0; i < _vertexCount; i++)
        {
            _invMass[i] = inv;
        }
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void SetTetherAnchors(ReadOnlySpan<int> anchors)
    {
        // Build nearest anchor per vertex based on rest positions
        // Precondition: anchors indices in range
        foreach (var a in anchors)
        {
            if ((uint)a >= (uint)_vertexCount)
                throw new ArgumentOutOfRangeException(nameof(anchors));
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
            _tetherLambda[i] = 0f;
        }
    }
}

#endif
