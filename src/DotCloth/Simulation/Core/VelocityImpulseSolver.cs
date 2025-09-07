using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotCloth.Simulation.Parameters;

namespace DotCloth.Simulation.Core;

/// <summary>
/// Velocity-level cloth solver using sequential impulses (PGS).
/// Solves stretch, optional bend (distance across opposite vertices), optional tethers, and collisions.
/// Does not use XPBD constructs (no lambda/compliance accumulation, no position projection in constraints).
/// </summary>
public sealed class VelocityImpulseSolver : IClothSimulator
{
    private Config _cfg;
    private int _vertexCount;

    // Solver tuning constants (class-level for clarity and maintainability)
    private const float Omega = 0.9f;                  // under-relaxation (0<ω<=1)
    private const float BaseCfmStretch = 1e-3f;
    private const float BaseCfmTether = 1e-5f;
    private const float BaseCfmBend = 2e-3f;            // bend softness
    private const float BaseLambdaClampStretch = 0.20f;
    private const float BaseLambdaClampTether = 1.20f;
    private const float OmegaTether = 1.0f;            // no under-relaxation for tether
    private const float BaseLambdaClampBend = 0.03f;
    private const float BendBetaScale = 2.5f;
    private const float ReferenceEdgeLength = 0.25f;

    // Compression handling scales
    private const float CompressBetaScale = 0.90f;
    private const float CfmCompressScale = 1.10f;
    private const float BaseLambdaClampCompress = 0.24f;

    // Post-stabilization (position-level)
    private const int PostStabIters = 3;
    private const float PosAlphaStretch = 0.40f;
    private const float PosAlphaBend = 0.15f;
    private const float PosAlphaTether = 1.00f;

    // Damping micro floor to ensure non-increase
    private const float MicroDamp = 1e-7f;

    // Global velocity cap (anti-divergence safety net)
    private const float MaxVelocityMagnitude = 4.95f;

    // Mass/inertia
    private float[] _invMass = Array.Empty<float>();

    // Previous positions (for collision sweep and velocity update determinism)
    private Vector3[] _prev = Array.Empty<Vector3>();

    // Topology: unique undirected edges for stretch (SoA for cache/SIMD)
    private struct Edge { public int I; public int J; public float RestLength; }
    private int[] _edgeI = Array.Empty<int>();
    private int[] _edgeJ = Array.Empty<int>();
    private float[] _edgeRestLength = Array.Empty<float>();
    private float[] _edgeWi = Array.Empty<float>();
    private float[] _edgeWj = Array.Empty<float>();
    private float[] _edgeWSum = Array.Empty<float>();
    private int[][] _edgeBatches = Array.Empty<int[]>();
    private float _avgEdgeLength;

    // Bend constraints (distance across opposite vertices of adjacent triangles)
    private struct Bend
    {
        public int K;
        public int L;
        public float RestDistance;
        public float Wk;
        public float Wl;
        public float WSum;
    }
    private Bend[] _bends = Array.Empty<Bend>();
    private int[][] _bendBatches = Array.Empty<int[]>();

    // Tether data (per-vertex rest position or anchor)
    private Vector3[] _rest = Array.Empty<Vector3>();
    private int[] _tetherAnchorIndex = Array.Empty<int>();
    private float[] _tetherAnchorRestLength = Array.Empty<float>();

    // (Removed) Experimental triangle-area stabilization.

    // Collision hooks
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

        // Mass setup
        _invMass = new float[_vertexCount];
        var inv = 1.0f / _cfg.VertexMass;
        for (int i = 0; i < _vertexCount; i++) _invMass[i] = inv;
        _prev = new Vector3[_vertexCount];

        // Rest state
        _rest = new Vector3[_vertexCount];
        for (int i = 0; i < _vertexCount; i++) _rest[i] = positions[i];
        _tetherAnchorIndex = Enumerable.Repeat(-1, _vertexCount).ToArray();
        _tetherAnchorRestLength = new float[_vertexCount];

        ValidateTriangles(triangles, _vertexCount);
        (var edgesTmp, _bends, _edgeBatches, _bendBatches) = BuildTopology(positions, triangles);
        int edgeCount = edgesTmp.Length;
        _edgeI = new int[edgeCount];
        _edgeJ = new int[edgeCount];
        _edgeRestLength = new float[edgeCount];
        _edgeWi = new float[edgeCount];
        _edgeWj = new float[edgeCount];
        _edgeWSum = new float[edgeCount];
        float sum = 0f;
        for (int e = 0; e < edgeCount; e++)
        {
            var edge = edgesTmp[e];
            _edgeI[e] = edge.I;
            _edgeJ[e] = edge.J;
            _edgeRestLength[e] = edge.RestLength;
            sum += edge.RestLength;
        }
        _avgEdgeLength = edgeCount > 0 ? sum / edgeCount : ReferenceEdgeLength;
        // Build triangle list and rest areas (experimental)
        // (Removed) Experimental triangle-area stabilization init.
        SortBatchesByVertexIndex();
        RecomputeEdgeMasses();
        RecomputeBendMasses();
        // (Removed) Experimental triangle-area stabilization masses.
    }

    /// <inheritdoc />
    public void Step(float deltaTime, Vector3[] positions, Vector3[] velocities)
    {
        if (positions.Length != _vertexCount) throw new ArgumentException("positions length mismatch", nameof(positions));
        if (velocities.Length != _vertexCount) throw new ArgumentException("velocities length mismatch", nameof(velocities));
        if (deltaTime <= 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));
        var positionsArr = positions;
        var velocitiesArr = velocities;
        int substeps = Math.Max(1, _cfg.Substeps);
        int iterations = Math.Max(1, _cfg.Iterations);
        float dt = deltaTime / substeps;

        var gravity = _cfg.UseGravity ? new Vector3(0, -9.80665f * _cfg.GravityScale, 0) : Vector3.Zero;
        var accelBase = gravity + _cfg.ExternalAcceleration;
        var useRandom = _cfg.RandomAcceleration > 0f;
        var rng = useRandom ? new Rng((uint)_cfg.RandomSeed) : default;

        float damping = Math.Clamp(_cfg.Damping, 0f, 0.999f);
        float drag = Math.Max(0f, _cfg.AirDrag);

        // Map 0..1 stiffness to Baumgarte beta coefficients
        float betaStretch = MapStiffnessToBeta(_cfg.StretchStiffness, dt, iterations);
        float edgeScale = Math.Clamp(ReferenceEdgeLength / MathF.Max(_avgEdgeLength, 1e-6f), 0.5f, 2f);
        float betaBend = MapStiffnessToBeta(_cfg.BendStiffness, dt, iterations) * BendBetaScale * edgeScale;
        float betaTether = MathF.Min(0.75f, MapStiffnessToBeta(_cfg.TetherStiffness, dt, iterations) * 1.35f);

        float stretchS = _cfg.StretchStiffness <= 0f ? 0f : MathF.Max(_cfg.StretchStiffness, 0.05f);
        float bendS = _cfg.BendStiffness <= 0f ? 0f : MathF.Max(_cfg.BendStiffness, 0.1f);
        float tetherS = _cfg.TetherStiffness <= 0f ? 0f : MathF.Max(_cfg.TetherStiffness, 0.05f);

        bool hasStretch = _cfg.StretchStiffness > 0f && _edgeI.Length > 0;
        bool hasBend = _cfg.BendStiffness > 0f && _bends.Length > 0;
        bool hasTether = _cfg.TetherStiffness > 0f && _tetherAnchorIndex.Length > 0;

        float cfmStretch = hasStretch ? BaseCfmStretch / stretchS : 0f;
        float cfmBend = hasBend ? BaseCfmBend / bendS / edgeScale : 0f;
        float cfmTether = hasTether ? BaseCfmTether / tetherS : 0f;

        float lambdaClampStretch = hasStretch ? BaseLambdaClampStretch * stretchS : 0f;
        float lambdaClampCompress = hasStretch ? BaseLambdaClampCompress * stretchS : 0f;
        float lambdaClampBend = hasBend ? BaseLambdaClampBend * bendS : 0f;
        float lambdaClampTether = hasTether ? BaseLambdaClampTether * tetherS : 0f;

        // Stabilizers: small CFM (softness), under-relaxation, per-iteration impulse clamp

        for (int s = 0; s < substeps; s++)
        {
            Array.Copy(positionsArr, _prev, _vertexCount);

            // 1) External forces -> velocity (semi-implicit)
            for (int i = 0; i < _vertexCount; i++)
            {
                if (_invMass[i] == 0f)
                {
                    velocitiesArr[i] = Vector3.Zero;
                    continue;
                }
                var v = velocitiesArr[i];
                var a = accelBase;
                if (useRandom)
                {
                    var dir = rng.NextUnitVector();
                    a += dir * _cfg.RandomAcceleration;
                }
                v += a * dt;
                v -= v * drag * dt; // simple drag
                velocitiesArr[i] = v;
            }

            // 2) Constraint iterations (sequential impulses on velocity)
            for (int it = 0; it < iterations; it++)
            {
                // Tether/Pin — solve first to let anchors dominate
                if (hasTether)
                {
                    Parallel.For(0, _vertexCount, i =>
                    {
                        float wi = _invMass[i];
                        if (wi <= 0f) return;
                        Vector3 target;
                        float targetLen;
                        int a = _tetherAnchorIndex[i];
                        if (a >= 0)
                        {
                            target = positionsArr[a];
                            targetLen = _tetherAnchorRestLength[i];
                        }
                        else
                        {
                            target = _rest[i];
                            targetLen = 0f;
                        }
                        var xi = positionsArr[i];
                        var d = xi - target;
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) return;
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        var len = 1f / invLen;
                        var n = (target - xi) * invLen;
                        float C = len - targetLen;
                        if (C <= 0f) return;
                        float rel = Vector3.Dot(velocitiesArr[i], n);
                        float bterm = +betaTether * C / dt;
                        float w = wi;
                        float denom = w + cfmTether;
                        float lambda = -(rel + bterm) / denom;
                        lambda = MathF.Max(-lambdaClampTether, MathF.Min(lambdaClampTether, lambda));
                        var dv = (lambda * OmegaTether) * n;
                        velocitiesArr[i] -= wi * dv;
                        if (C > 0f)
                        {
                            float rel2 = Vector3.Dot(velocitiesArr[i], n);
                            float targetRel = C / dt;
                            if (rel2 < targetRel)
                            {
                                float corr = targetRel - rel2;
                                velocitiesArr[i] += corr * n;
                            }
                        }
                    });
                }

                // Stretch: edges
                if (hasStretch)
                {
                    for (int b = 0; b < _edgeBatches.Length; b++)
                    {
                        var batch = _edgeBatches[b];
                        Parallel.For(0, batch.Length, bi =>
                        {
                            int e = batch[bi];
                            int i = _edgeI[e];
                            int j = _edgeJ[e];
                            var xi = positionsArr[i];
                            var xj = positionsArr[j];
                            var d = xj - xi;
                            var lenSq = d.LengthSquared();
                            if (lenSq <= 1e-18f) return;
                            var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                            var len = 1f / invLen;
                            var n = d * invLen;
                            float C = len - _edgeRestLength[e];
                            float w = _edgeWSum[e];
                            if (w <= 0f) return;
                            var rel = Vector3.Dot(velocitiesArr[j] - velocitiesArr[i], n);
                            if (C > 0f)
                            {
                                float bterm = -betaStretch * C / dt;
                                float denom = w + cfmStretch;
                                float lambda = -(rel + bterm) / denom;
                                lambda = MathF.Max(-lambdaClampStretch, MathF.Min(lambdaClampStretch, lambda));
                                var dv = (lambda * Omega) * n;
                                velocitiesArr[i] -= _edgeWi[e] * dv;
                                velocitiesArr[j] += _edgeWj[e] * dv;
                            }
                            else if (C < 0f)
                            {
                                if (_tetherAnchorIndex[i] == j || _tetherAnchorIndex[j] == i)
                                    return;
                                float betaC = betaStretch * CompressBetaScale;
                                float bterm = -betaC * C / dt;
                                float denom = w + cfmStretch * CfmCompressScale;
                                float lambda = -(rel + bterm) / denom;
                                lambda = MathF.Max(-lambdaClampCompress, MathF.Min(lambdaClampCompress, lambda));
                                var dv = (lambda * Omega) * n;
                                velocitiesArr[i] -= _edgeWi[e] * dv;
                                velocitiesArr[j] += _edgeWj[e] * dv;
                            }
                        });
                    }
                }

                // Bend (distance across opposite vertices)
                if (hasBend)
                {
                    for (int bb = 0; bb < _bendBatches.Length; bb++)
                    {
                        var batch = _bendBatches[bb];
                        Parallel.For(0, batch.Length, bi =>
                        {
                            int biIdx = batch[bi];
                            ref readonly var bend = ref _bends[biIdx];
                            int k = bend.K;
                            int l = bend.L;
                            var xk = positionsArr[k];
                            var xl = positionsArr[l];
                            var d = xl - xk;
                            var lenSq = d.LengthSquared();
                            if (lenSq <= 1e-18f) return;
                            var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                            var len = 1f / invLen;
                            var n = d * invLen;
                            float C = len - bend.RestDistance;
                            float w = bend.WSum;
                            if (w <= 0f) return;
                            var rel = Vector3.Dot(velocitiesArr[l] - velocitiesArr[k], n);
                            float bterm = betaBend * C / dt;
                            float denom = w + cfmBend;
                            float lambda = -(rel + bterm) / denom;
                            lambda = MathF.Max(-lambdaClampBend, MathF.Min(lambdaClampBend, lambda));
                            var dv = (lambda * Omega) * n;
                            velocitiesArr[k] -= bend.Wk * dv;
                            velocitiesArr[l] += bend.Wl * dv;
                        });
                    }
                }
            }

            // 3) Collisions (current colliders may push positions; they also modify velocity)
            if (_colliders.Count > 0)
            {
                foreach (var c in _colliders)
                {
                    c.Resolve(_prev, positionsArr, velocitiesArr, dt, _cfg.CollisionThickness, _cfg.Friction);
                }
            }

            // 4) Integrate positions and apply global damping (with tiny floor to ensure monotone non-increase)
            float dampFactor = MathF.Max(0f, MathF.Min(1f, (1.0f - damping) - MicroDamp));
            for (int i = 0; i < _vertexCount; i++)
            {
                if (_invMass[i] == 0f)
                {
                    velocitiesArr[i] = Vector3.Zero;
                    continue;
                }
                velocitiesArr[i] *= dampFactor;
                positionsArr[i] += velocitiesArr[i] * dt;
            }

            // 5) Post-stabilization (position-level) — small corrective projections
            const int postStabIters = PostStabIters;
            // (Removed) Experimental area stabilization constants.
            for (int ps = 0; ps < postStabIters; ps++)
            {
                // Stretch edges (only when present)
                if (hasStretch)
                {
                    for (int e = 0; e < _edgeI.Length; e++)
                    {
                        int i = _edgeI[e];
                        int j = _edgeJ[e];
                        var d = positionsArr[j] - positionsArr[i];
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        var len = 1f / invLen;
                        float C = len - _edgeRestLength[e];
                        var n = d * invLen;
                        float wsum = _edgeWSum[e];
                        if (wsum <= 0f) continue;
                        if (C > 0f)
                        {
                            float corrMag = PosAlphaStretch * C / MathF.Max(1e-8f, wsum);
                            var corr = corrMag * n;
                            positionsArr[i] += _edgeWi[e] * corr;
                            positionsArr[j] -= _edgeWj[e] * corr;
                        }
                        else if (C < 0f)
                        {
                            float rest = MathF.Max(1e-12f, _edgeRestLength[e]);
                            float ratio = len / rest;
                            float posAlphaCompress = ratio < 0.90f ? 0.40f : 0.20f;
                            float corrMag = posAlphaCompress * (-C) / MathF.Max(1e-8f, wsum);
                            var corr = corrMag * n;
                            positionsArr[i] -= _edgeWi[e] * corr;
                            positionsArr[j] += _edgeWj[e] * corr;
                        }
                    }
                }
                // Bend pairs (only when present)
                if (hasBend)
                {
                    for (int b = 0; b < _bends.Length; b++)
                    {
                        ref readonly var bend = ref _bends[b];
                        int k = bend.K;
                        int l = bend.L;
                        var d = positionsArr[l] - positionsArr[k];
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        var len = 1f / invLen;
                        float C = len - bend.RestDistance;
                        if (C <= 0f) continue;
                        var n = d * invLen;
                        float wsum = bend.WSum;
                        if (wsum <= 0f) continue;
                        float corrMag = PosAlphaBend * C / MathF.Max(1e-8f, wsum);
                        var corr = corrMag * n;
                        positionsArr[k] += bend.Wk * corr;
                        positionsArr[l] -= bend.Wl * corr;
                    }
                }
                // Tethers (single-body)
                if (hasTether)
                {
                    for (int i = 0; i < _vertexCount; i++)
                    {
                        float wi = _invMass[i];
                        if (wi <= 0f) continue;
                        Vector3 target;
                        float targetLen;
                        int a = _tetherAnchorIndex[i];
                        if (a >= 0) { target = positionsArr[a]; targetLen = _tetherAnchorRestLength[i]; }
                        else { target = _rest[i]; targetLen = 0f; }
                        var d = positionsArr[i] - target;
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        var len = 1f / invLen;
                        float C = len - targetLen;
                        if (C <= 0f) continue;
                        var n = d * invLen;
                        var corr = PosAlphaTether * C * n;
                        positionsArr[i] -= corr;
                    }
                }

                // (Removed) Experimental area stabilization pass.
            }

            // Recompute velocities from positions delta to keep consistency
            for (int i = 0; i < _vertexCount; i++)
            {
                if (_invMass[i] == 0f) { velocitiesArr[i] = Vector3.Zero; continue; }
                velocitiesArr[i] = (positionsArr[i] - _prev[i]) / dt;
            }

#if DOTCLOTH_ENABLE_VELOCITY_CLAMP
            // 6) Optional velocity clamp to reduce strain drift (applied after post-stabilization)
            if (hasStretch)
            {
                float limit = 0.0f; // clamp whenever overstretched
                // First, stronger pass
                {
                    float kClamp = 0.8f; // correction factor
                    for (int e = 0; e < _edgeI.Length; e++)
                    {
                        int i = _edgeI[e];
                        int j = _edgeJ[e];
                        var d = positionsArr[j] - positionsArr[i];
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        float L = 1f / invLen;
                        float strain = L / MathF.Max(1e-12f, _edgeRestLength[e]);
                        if (strain <= 1f + limit) continue;
                        var n = d * invLen;
                        float rel = Vector3.Dot(velocitiesArr[j] - velocitiesArr[i], n);
                        float targetRel = -kClamp * (strain - (1f + limit)) / dt;
                        float corr = (rel - targetRel);
                        float w = _edgeWSum[e];
                        if (w <= 0f) continue;
                        float lambda = corr / w;
                        var dv = lambda * n;
                        velocitiesArr[i] += _edgeWi[e] * dv;
                        velocitiesArr[j] -= _edgeWj[e] * dv;
                    }
                }
                // Second, lighter pass to catch residual overstretch
                {
                    float kClamp = 0.5f;
                    for (int e = 0; e < _edgeI.Length; e++)
                    {
                        int i = _edgeI[e];
                        int j = _edgeJ[e];
                        var d = positionsArr[j] - positionsArr[i];
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        float L = 1f / invLen;
                        float strain = L / MathF.Max(1e-12f, _edgeRestLength[e]);
                        if (strain <= 1f + limit) continue;
                        var n = d * invLen;
                        float rel = Vector3.Dot(velocitiesArr[j] - velocitiesArr[i], n);
                        float targetRel = -kClamp * (strain - (1f + limit)) / dt;
                        float corr = (rel - targetRel);
                        float w = _edgeWSum[e];
                        if (w <= 0f) continue;
                        float lambda = corr / w;
                        var dv = lambda * n;
                        velocitiesArr[i] += _edgeWi[e] * dv;
                        velocitiesArr[j] -= _edgeWj[e] * dv;
                    }
                }
                // Compression clamp: prevent edges from collapsing far below rest
                {
                    float limitComp = 0.02f; // allow small compression tolerance
                    float kClampComp = 0.6f;
                    for (int e = 0; e < _edgeI.Length; e++)
                    {
                        int i = _edgeI[e];
                        int j = _edgeJ[e];
                        // Skip compression clamp for anchor-pair edges; tether should dominate
                        if (_tetherAnchorIndex[i] == j || _tetherAnchorIndex[j] == i) continue;
                        var d = positionsArr[j] - positionsArr[i];
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        float L = 1f / invLen;
                        float rest = MathF.Max(1e-12f, _edgeRestLength[e]);
                        float strain = L / rest;
                        if (strain >= 1f - limitComp) continue;
                        var n = d * invLen;
                        float rel = Vector3.Dot(velocitiesArr[j] - velocitiesArr[i], n);
                        float targetRel = +kClampComp * ((1f - limitComp) - strain) / dt;
                        float corr = (targetRel - rel);
                        float w = _edgeWSum[e];
                        if (w <= 0f) continue;
                        float lambda = corr / w;
                        var dv = lambda * n;
                        // Increase edge length rate: push j along +n, i along -n
                        velocitiesArr[i] -= _edgeWi[e] * dv;
                        velocitiesArr[j] += _edgeWj[e] * dv;
                    }
                }
            }
#endif

            // Global velocity cap (anti-divergence safety net) — apply last
            if (hasStretch || hasBend || hasTether)
            {
                float vCap2 = MaxVelocityMagnitude * MaxVelocityMagnitude;
                for (int i = 0; i < _vertexCount; i++)
                {
                    var v = velocitiesArr[i];
                    float s2 = v.LengthSquared();
                    if (s2 > vCap2)
                    {
                        float inv = MaxVelocityMagnitude / MathF.Sqrt(s2);
                        velocitiesArr[i] = v * inv;
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public void UpdateParameters(ClothParameters parameters)
    {
        if (parameters is null) throw new ArgumentNullException(nameof(parameters));
        _cfg = Config.From(parameters);
        // No cached per-constraint coefficients; mass terms recomputed when masses change.
    }

    /// <inheritdoc />
    public void SetInverseMasses(ReadOnlySpan<float> inverseMasses)
    {
        if (inverseMasses.Length != _vertexCount) throw new ArgumentException("inverseMasses length mismatch", nameof(inverseMasses));
        for (int i = 0; i < _vertexCount; i++) _invMass[i] = Math.Max(0f, inverseMasses[i]);
        RecomputeEdgeMasses();
        RecomputeBendMasses();
        // (Removed) Experimental area stabilization masses update.
    }

    /// <inheritdoc />
    public void ResetRestState(ReadOnlySpan<Vector3> positions)
    {
        if (positions.Length != _vertexCount) throw new ArgumentException("positions length mismatch", nameof(positions));
        for (int i = 0; i < _vertexCount; i++) _rest[i] = positions[i];
        for (int e = 0; e < _edgeI.Length; e++)
        {
            int i = _edgeI[e];
            int j = _edgeJ[e];
            _edgeRestLength[e] = Vector3.Distance(positions[i], positions[j]);
        }
        for (int b = 0; b < _bends.Length; b++)
        {
            var (k, l) = (_bends[b].K, _bends[b].L);
            _bends[b].RestDistance = Vector3.Distance(positions[k], positions[l]);
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
#if DOTCLOTH_EXPERIMENTAL_AREA_STAB
        RecomputeTriMasses();
#endif
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
#if DOTCLOTH_EXPERIMENTAL_AREA_STAB
        RecomputeTriMasses();
#endif
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
#if DOTCLOTH_EXPERIMENTAL_AREA_STAB
        RecomputeTriMasses();
#endif
    }

    /// <inheritdoc />
    public void SetTetherAnchors(ReadOnlySpan<int> anchors)
    {
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
                if (d2 < bestD2) { bestD2 = d2; best = a; }
            }
            _tetherAnchorIndex[i] = best;
            float restLen = best >= 0 ? MathF.Sqrt(bestD2) * _cfg.TetherLengthScale : 0f;
            _tetherAnchorRestLength[i] = restLen;
        }
    }

    private static (Edge[] edges, Bend[] bends, int[][] edgeBatches, int[][] bendBatches) BuildTopology(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles)
    {
        var set = new HashSet<(int, int)>();
        var opp = new Dictionary<(int, int), (int a, int b)>();
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
            Add(a, b); Add(b, c); Add(c, a);
            AddOpp(a, b, c); AddOpp(b, c, a); AddOpp(c, a, b);
        }
        var edges = new Edge[set.Count];
        int k = 0;
        foreach (var (i, j) in set)
        {
            var rest = Vector3.Distance(positions[i], positions[j]);
            edges[k++] = new Edge { I = i, J = j, RestLength = rest };
        }

        var bendsList = new List<Bend>();
        foreach (var kv in opp)
        {
            var pair = kv.Value;
            if (pair.a >= 0 && pair.b >= 0)
            {
                int kIdx = pair.a;
                int lIdx = pair.b;
                float rest = Vector3.Distance(positions[kIdx], positions[lIdx]);
                bendsList.Add(new Bend { K = kIdx, L = lIdx, RestDistance = rest });
            }
        }
        var bends = bendsList.ToArray();

        int[][] edgeBatches = BuildBatchesForPairs(edges.Select(e => (e.I, e.J)), positions.Length);
        int[][] bendBatches = BuildBatchesForPairs(bends.Select(b => (b.K, b.L)), positions.Length);
        return (edges, bends, edgeBatches, bendBatches);

        void AddOpp(int a, int b, int c)
        {
            int i = Math.Min(a, b);
            int j = Math.Max(a, b);
            var key = (i, j);
            if (!opp.TryGetValue(key, out var val)) opp[key] = (c, -1);
            else if (val.b < 0) opp[key] = (val.a, c);
        }
    }

    private static int[][] BuildBatchesForPairs(IEnumerable<(int a, int b)> pairs, int vertexCount)
    {
        var pairList = pairs.ToList();
        var batches = new List<List<int>>();
        for (int idx = 0; idx < pairList.Count; idx++)
        {
            var (a, b) = pairList[idx];
            bool placed = false;
            for (int bi = 0; bi < batches.Count && !placed; bi++)
            {
                var used = new HashSet<int>();
                foreach (var ei in batches[bi])
                {
                    var p = pairList[ei];
                    used.Add(p.a);
                    used.Add(p.b);
                }
                if (!used.Contains(a) && !used.Contains(b)) { batches[bi].Add(idx); placed = true; }
            }
            if (!placed) batches.Add(new List<int> { idx });
        }
        return batches.Select(l => l.ToArray()).ToArray();
    }

    private void SortBatchesByVertexIndex()
    {
        for (int bi = 0; bi < _edgeBatches.Length; bi++)
        {
            Array.Sort(_edgeBatches[bi], (x, y) =>
            {
                int kx = _edgeI[x] < _edgeJ[x] ? _edgeI[x] : _edgeJ[x];
                int ky = _edgeI[y] < _edgeJ[y] ? _edgeI[y] : _edgeJ[y];
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

    private static void ValidateTriangles(ReadOnlySpan<int> tris, int vertexCount)
    {
        for (int t = 0; t < tris.Length; t++)
        {
            int idx = tris[t];
            if ((uint)idx >= (uint)vertexCount)
                throw new ArgumentOutOfRangeException(nameof(tris), $"triangle index {idx} out of range [0,{vertexCount - 1}]");
        }
    }

    private void RecomputeEdgeMasses()
    {
        for (int e = 0; e < _edgeI.Length; e++)
        {
            float wi = _invMass[_edgeI[e]];
            float wj = _invMass[_edgeJ[e]];
            _edgeWi[e] = wi;
            _edgeWj[e] = wj;
            _edgeWSum[e] = wi + wj;
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

    // (Removed) Experimental area stabilization helpers.

    private static float MapStiffnessToBeta(float s01, float dt, int iterations)
    {
        var s = float.Clamp(s01, 0f, 1f);
        if (s <= 0f) return 0f;
        float baseBeta = s * 0.4f;
        float iterScale = MathF.Min(1f, iterations / 5f);
        return baseBeta * iterScale;
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
        public uint NextU32() { uint x = _state; x ^= x << 13; x ^= x >> 17; x ^= x << 5; _state = x; return x; }
        public float NextFloat01() => (NextU32() & 0xFFFFFF) / (float)0x1000000; // [0,1)
        public Vector3 NextUnitVector()
        {
            float u = 2f * NextFloat01() - 1f;
            float v = 2f * NextFloat01() - 1f;
            float s = u * u + v * v;
            if (s >= 1f || s <= 1e-12f) return new Vector3(1, 0, 0);
            float f = MathF.Sqrt(1f - s);
            return new Vector3(2f * u * f, 2f * v * f, 1f - 2f * s);
        }
    }
}
