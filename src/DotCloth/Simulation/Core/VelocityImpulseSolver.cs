using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using DotCloth.Simulation.Parameters;

namespace DotCloth.Simulation.Core;

/// <summary>
/// Velocity impulse-based cloth solver as an alternative to XPBD.
/// Resolves constraints by applying impulses to velocities rather than directly modifying positions.
/// </summary>
public sealed class VelocityImpulseSolver : IClothSimulator
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
        public float Stiffness;
        public float Wi;
        public float Wj;
        public float WSum;
    }

    private Edge[] _edges = Array.Empty<Edge>();
    private int[][] _edgeBatches = Array.Empty<int[]>();

    // Bend constraints across opposite vertices of adjacent triangles
    private struct Bend
    {
        public int K;
        public int L;
        public float RestDistance;
        public float Stiffness;
        public float Wk;
        public float Wl;
        public float WSum;
    }
    private Bend[] _bends = Array.Empty<Bend>();
    private int[][] _bendBatches = Array.Empty<int[]>();

    // Tether-to-rest constraints (per-vertex to rest position)
    private Vector3[] _rest = Array.Empty<Vector3>();
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

        var gravity = _cfg.UseGravity ? new Vector3(0, -9.80665f * _cfg.GravityScale, 0) : Vector3.Zero;
        var accel = gravity + _cfg.ExternalAcceleration;
        var useRandom = _cfg.RandomAcceleration > 0f;
        var rng = useRandom ? new Rng((uint)_cfg.RandomSeed) : default;

        var drag = Math.Max(0f, _cfg.AirDrag);
        var damping = Math.Clamp(_cfg.Damping, 0f, 0.999f);

        for (int s = 0; s < substeps; s++)
        {
            // Save previous positions for collision resolution
            for (int i = 0; i < _vertexCount; i++) _prev[i] = positions[i];

            // Apply external forces to velocities
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
            }

            // Velocity impulse constraint resolution
            for (int it = 0; it < iterations; it++)
            {
                // Stretch constraints via velocity impulses
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
                        var vi = velocities[i];
                        var vj = velocities[j];
                        
                        var d = xj - xi;
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        var len = 1f / invLen;
                        var n = d * invLen;
                        
                        // Constraint violation (current length - rest length)
                        float C = len - edge.RestLength;
                        
                        // Relative velocity along constraint direction
                        var relativeVel = Vector3.Dot(vj - vi, n);
                        
                        // Skip if stiffness is zero
                        if (edge.Stiffness <= 0f) continue;
                        
                        // For stretch constraints, we want relative velocity to reduce position error
                        // If edge is too long (C > 0), we want negative relative velocity (particles moving towards each other)
                        // If edge is too short (C < 0), we want positive relative velocity (particles moving apart)
                        float targetVel = -edge.Stiffness * C;
                        float velError = relativeVel - targetVel;
                        
                        // Lagrange multiplier (impulse magnitude)
                        float lambda = -velError / edge.WSum;
                        
                        // Apply impulses
                        var impulse = lambda * n;
                        if (_invMass[i] > 0f)
                            velocities[i] -= edge.Wi * impulse;
                        if (_invMass[j] > 0f)
                            velocities[j] += edge.Wj * impulse;
                    }
                }

                // Bending constraints via velocity impulses
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
                            var vk = velocities[k];
                            var vl = velocities[l];
                            
                            var d = xl - xk;
                            var lenSq = d.LengthSquared();
                            if (lenSq <= 1e-18f) continue;
                            
                            var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                            var len = 1f / invLen;
                            var n = d * invLen;
                            
                            float C = len - bend.RestDistance;
                            var relativeVel = Vector3.Dot(vl - vk, n);
                            
                            // Skip if stiffness is zero
                            if (bend.Stiffness <= 0f) continue;
                            
                            // For bend constraints, similar logic - target velocity to reduce position error
                            float targetVel = -bend.Stiffness * C;
                            float velError = relativeVel - targetVel;
                            
                            float lambda = -velError / bend.WSum;
                            
                            var impulse = lambda * n;
                            if (_invMass[k] > 0f)
                                velocities[k] -= bend.Wk * impulse;
                            if (_invMass[l] > 0f)
                                velocities[l] += bend.Wl * impulse;
                        }
                    }
                }

                // Tether constraints via velocity impulses
                if (_cfg.TetherStiffness > 0f)
                {
                    for (int i = 0; i < _vertexCount; i++)
                    {
                        float wi = _invMass[i];
                        if (wi <= 0f) continue;
                        
                        var xi = positions[i];
                        var vi = velocities[i];
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
                            targetLen = 0f;
                        }

                        var d = xi - target;
                        var lenSq = d.LengthSquared();
                        if (lenSq <= 1e-18f) continue;
                        
                        var invLen = MathF.ReciprocalSqrtEstimate(lenSq);
                        var len = 1f / invLen;
                        var n = d * invLen;
                        
                        float C = len - targetLen;
                        var relativeVel = Vector3.Dot(vi, n);
                        
                        // For tether constraints, we want particle velocity to reduce position error
                        float targetVel = -_cfg.TetherStiffness * C;
                        float velError = relativeVel - targetVel;
                        
                        float lambda = -velError / wi;
                        
                        var impulse = lambda * n;
                        velocities[i] -= wi * impulse;
                    }
                }
            }

            // Apply damping to velocities
            if (damping > 0f)
            {
                for (int i = 0; i < _vertexCount; i++)
                {
                    if (_invMass[i] > 0f)
                        velocities[i] *= (1f - damping);
                }
            }

            // Integrate positions
            for (int i = 0; i < _vertexCount; i++)
            {
                if (_invMass[i] > 0f)
                    positions[i] += velocities[i] * dt;
            }

            // Collision response
            ApplyCollisionConstraints(positions, velocities, _prev, dt);
        }
    }

    /// <inheritdoc />
    public void UpdateParameters(ClothParameters parameters)
    {
        _cfg = new Config(parameters);
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void SetInverseMasses(ReadOnlySpan<float> inverseMasses)
    {
        if (inverseMasses.Length != _vertexCount) throw new ArgumentException("length mismatch", nameof(inverseMasses));
        for (int i = 0; i < _vertexCount; i++) _invMass[i] = inverseMasses[i];
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void ResetRestState(ReadOnlySpan<Vector3> positions)
    {
        if (positions.Length != _vertexCount) throw new ArgumentException("length mismatch", nameof(positions));
        for (int i = 0; i < _vertexCount; i++) _rest[i] = positions[i];
        RecomputeRestLengths(positions);
    }

    /// <inheritdoc />
    public void PinVertices(ReadOnlySpan<int> indices)
    {
        foreach (int idx in indices)
        {
            if (idx >= 0 && idx < _vertexCount) _invMass[idx] = 0f;
        }
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void PinVertices(params int[] indices) => PinVertices(indices.AsSpan());

    /// <inheritdoc />
    public void UnpinVertices(ReadOnlySpan<int> indices)
    {
        float defaultInv = 1.0f / _cfg.VertexMass;
        foreach (int idx in indices)
        {
            if (idx >= 0 && idx < _vertexCount) _invMass[idx] = defaultInv;
        }
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void UnpinVertices(params int[] indices) => UnpinVertices(indices.AsSpan());

    /// <inheritdoc />
    public void ClearPins()
    {
        float defaultInv = 1.0f / _cfg.VertexMass;
        for (int i = 0; i < _vertexCount; i++) _invMass[i] = defaultInv;
        RecomputeEdgeMasses();
        RecomputeBendMasses();
    }

    /// <inheritdoc />
    public void SetTetherAnchors(ReadOnlySpan<int> anchors)
    {
        for (int i = 0; i < _vertexCount; i++) _tetherAnchorIndex[i] = -1;
        foreach (int anchor in anchors)
        {
            if (anchor < 0 || anchor >= _vertexCount) continue;
            for (int i = 0; i < _vertexCount; i++)
            {
                if (i == anchor) continue;
                var d = _rest[i] - _rest[anchor];
                float dist = d.Length();
                int currentAnchor = _tetherAnchorIndex[i];
                if (currentAnchor < 0 ||
                    dist < _tetherAnchorRestLength[i])
                {
                    _tetherAnchorIndex[i] = anchor;
                    _tetherAnchorRestLength[i] = dist * _cfg.TetherLengthScale;
                }
            }
        }
    }

    private void RecomputeEdgeMasses()
    {
        for (int e = 0; e < _edges.Length; e++)
        {
            ref var edge = ref _edges[e];
            edge.Wi = _invMass[edge.I];
            edge.Wj = _invMass[edge.J];
            edge.WSum = edge.Wi + edge.Wj;
            edge.Stiffness = _cfg.StretchStiffness;
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
            bend.Stiffness = _cfg.BendStiffness;
        }
    }

    private void RecomputeRestLengths(ReadOnlySpan<Vector3> positions)
    {
        for (int e = 0; e < _edges.Length; e++)
        {
            ref var edge = ref _edges[e];
            var d = positions[edge.J] - positions[edge.I];
            edge.RestLength = d.Length();
        }
        for (int b = 0; b < _bends.Length; b++)
        {
            ref var bend = ref _bends[b];
            var d = positions[bend.L] - positions[bend.K];
            bend.RestDistance = d.Length();
        }
    }

    private void ApplyCollisionConstraints(Span<Vector3> positions, Span<Vector3> velocities, ReadOnlySpan<Vector3> prevPositions, float dt)
    {
        if (_colliders.Count > 0)
        {
            foreach (var collider in _colliders)
            {
                collider.Resolve(prevPositions, positions, velocities, dt, _cfg.CollisionThickness, _cfg.Friction);
            }
        }
    }

    private void SortBatchesByVertexIndex()
    {
        for (int b = 0; b < _edgeBatches.Length; b++)
        {
            var batch = _edgeBatches[b];
            Array.Sort(batch, (a, b) => Math.Min(_edges[a].I, _edges[a].J).CompareTo(Math.Min(_edges[b].I, _edges[b].J)));
        }
        for (int b = 0; b < _bendBatches.Length; b++)
        {
            var batch = _bendBatches[b];
            Array.Sort(batch, (a, b) => Math.Min(_bends[a].K, _bends[a].L).CompareTo(Math.Min(_bends[b].K, _bends[b].L)));
        }
    }

    // These methods are borrowed/adapted from PbdSolver - they handle topology construction
    private static void ValidateTriangles(ReadOnlySpan<int> triangles, int vertexCount)
    {
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            if (a < 0 || a >= vertexCount) throw new ArgumentException($"triangle vertex {a} out of range", nameof(triangles));
            if (b < 0 || b >= vertexCount) throw new ArgumentException($"triangle vertex {b} out of range", nameof(triangles));
            if (c < 0 || c >= vertexCount) throw new ArgumentException($"triangle vertex {c} out of range", nameof(triangles));
        }
    }

    private static (Edge[], Bend[], int[][], int[][]) BuildTopology(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles, Config cfg)
    {
        var edgeSet = new HashSet<(int, int)>();
        var edgeToTriangles = new Dictionary<(int, int), List<int>>();

        // Build unique edges from triangles
        for (int t = 0; t < triangles.Length; t += 3)
        {
            int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
            var triangleEdges = new[] { (Math.Min(a, b), Math.Max(a, b)), (Math.Min(b, c), Math.Max(b, c)), (Math.Min(c, a), Math.Max(c, a)) };
            int triIdx = t / 3;
            foreach (var edge in triangleEdges)
            {
                edgeSet.Add(edge);
                if (!edgeToTriangles.ContainsKey(edge))
                    edgeToTriangles[edge] = new List<int>();
                edgeToTriangles[edge].Add(triIdx);
            }
        }

        var edgeList = edgeSet.ToArray();
        var edges = new Edge[edgeList.Length];
        for (int e = 0; e < edgeList.Length; e++)
        {
            var (i, j) = edgeList[e];
            var d = positions[j] - positions[i];
            edges[e] = new Edge
            {
                I = i,
                J = j,
                RestLength = d.Length(),
                Stiffness = cfg.StretchStiffness,
                Wi = 0f, // will be set later
                Wj = 0f,
                WSum = 0f
            };
        }

        // Build bending constraints from opposite vertices of adjacent triangles
        var bendSet = new HashSet<(int, int)>();
        foreach (var kvp in edgeToTriangles)
        {
            if (kvp.Value.Count != 2) continue; // Only interior edges
            var tri1 = kvp.Value[0] * 3;
            var tri2 = kvp.Value[1] * 3;
            var edge = kvp.Key;
            
            // Find opposite vertices
            var tri1Verts = new[] { triangles[tri1], triangles[tri1 + 1], triangles[tri1 + 2] };
            var tri2Verts = new[] { triangles[tri2], triangles[tri2 + 1], triangles[tri2 + 2] };
            
            int opposite1 = -1, opposite2 = -1;
            foreach (int v in tri1Verts)
                if (v != edge.Item1 && v != edge.Item2) { opposite1 = v; break; }
            foreach (int v in tri2Verts)
                if (v != edge.Item1 && v != edge.Item2) { opposite2 = v; break; }
            
            if (opposite1 >= 0 && opposite2 >= 0)
                bendSet.Add((Math.Min(opposite1, opposite2), Math.Max(opposite1, opposite2)));
        }

        var bendList = bendSet.ToArray();
        var bends = new Bend[bendList.Length];
        for (int b = 0; b < bendList.Length; b++)
        {
            var (k, l) = bendList[b];
            var d = positions[l] - positions[k];
            bends[b] = new Bend
            {
                K = k,
                L = l,
                RestDistance = d.Length(),
                Stiffness = cfg.BendStiffness,
                Wk = 0f, // will be set later
                Wl = 0f,
                WSum = 0f
            };
        }

        // Create batches for parallel processing (simplified - just single batch for now)
        var edgeBatches = edges.Length > 0 ? new int[][] { Enumerable.Range(0, edges.Length).ToArray() } : Array.Empty<int[]>();
        var bendBatches = bends.Length > 0 ? new int[][] { Enumerable.Range(0, bends.Length).ToArray() } : Array.Empty<int[]>();

        return (edges, bends, edgeBatches, bendBatches);
    }

    // Config struct to match PbdSolver pattern
    private readonly struct Config
    {
        public readonly float VertexMass;
        public readonly bool UseGravity;
        public readonly float GravityScale;
        public readonly float Damping;
        public readonly float AirDrag;
        public readonly float StretchStiffness;
        public readonly float BendStiffness;
        public readonly float TetherStiffness;
        public readonly float TetherLengthScale;
        public readonly float CollisionThickness;
        public readonly float Friction;
        public readonly Vector3 ExternalAcceleration;
        public readonly float RandomAcceleration;
        public readonly int RandomSeed;
        public readonly int Iterations;
        public readonly int Substeps;

        public Config(ClothParameters p)
        {
            VertexMass = Math.Max(1e-6f, p.VertexMass);
            UseGravity = p.UseGravity;
            GravityScale = p.GravityScale;
            Damping = Math.Clamp(p.Damping, 0f, 0.999f);
            AirDrag = Math.Max(0f, p.AirDrag);
            StretchStiffness = Math.Clamp(p.StretchStiffness, 0f, 1f);
            BendStiffness = Math.Clamp(p.BendStiffness, 0f, 1f);
            TetherStiffness = Math.Clamp(p.TetherStiffness, 0f, 1f);
            TetherLengthScale = Math.Max(0.01f, p.TetherLengthScale);
            CollisionThickness = Math.Max(0f, p.CollisionThickness);
            Friction = Math.Clamp(p.Friction, 0f, 1f);
            ExternalAcceleration = p.ExternalAcceleration;
            RandomAcceleration = Math.Max(0f, p.RandomAcceleration);
            RandomSeed = p.RandomSeed;
            Iterations = Math.Max(1, p.Iterations);
            Substeps = Math.Max(1, p.Substeps);
        }
    }

    // Simple RNG struct to match PbdSolver pattern
    private struct Rng
    {
        private uint _state;

        public Rng(uint seed) => _state = seed;

        public Vector3 NextUnitVector()
        {
            // Simple uniform distribution on unit sphere using rejection sampling
            Vector3 v;
            do
            {
                v = new Vector3(NextFloat() * 2f - 1f, NextFloat() * 2f - 1f, NextFloat() * 2f - 1f);
            } while (v.LengthSquared() > 1f || v.LengthSquared() < 1e-8f);
            return Vector3.Normalize(v);
        }

        private float NextFloat()
        {
            _state = _state * 1664525u + 1013904223u;
            return (_state >> 8) / (float)(1u << 24);
        }
    }
}