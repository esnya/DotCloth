using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DotCloth.Simulation;
using DotCloth.Simulation.Collision;
using DotCloth.MonoGameSample.Scenarios;
using XnaVec = Microsoft.Xna.Framework.Vector3;
using NVec = System.Numerics.Vector3;

namespace DotCloth.MonoGameSample;

public sealed class SampleGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private BasicEffect? _effect;
    private VertexBuffer? _vb;
    private IndexBuffer? _ib;
    private int _indexCount;
    private VertexBuffer? _floorVb;
    private IndexBuffer? _floorIb;
    private int _floorIndexCount;

    // Camera (simple orbit)
    private float _yaw = 0.6f;
    private float _pitch = 0.6f;
    private float _dist = 6f;
    private float _accum; // fixed-step accumulator

    // Scenario state (multi-cloth)
    private IScenario _scenario = new MinimalScenario();
    private readonly List<ClothSim> _cloths = new();
    private readonly List<(int i,int j)[]> _edgesPerCloth = new();
    private readonly List<ICollider> _colliders = new();
    private readonly List<ColliderViz> _colliderViz = new();
    private readonly List<VertexPositionColor> _colliderLines = new();
    private VertexBuffer? _colliderVb;
    private IndexBuffer? _colliderIb;
    private int _colliderIndexCount;

    // Input state
    private MouseState _prevMouse;
    private KeyboardState _prevKeys;

    // Perf state
    private long _solverTicks;
    private double _emaFps, _emaSolverMs, _emaTotalMs, _emaSampleMs;

    public SampleGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            PreferMultiSampling = true
        };
        Window.Title = "DotCloth MonoGame Sample";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();

        // Scenario setup
        LoadScenario(new MinimalScenario());

        // Graphics setup
        _effect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };
        BuildEdgeBuffers();
        BuildFloorGrid();
    }

    protected override void Update(GameTime gameTime)
    {
        var k = Keyboard.GetState();
        if (k.IsKeyDown(Keys.Escape)) Exit();
        // Edge-triggered keys to avoid repeated reloads while held
        if (k.IsKeyDown(Keys.D1) && !_prevKeys.IsKeyDown(Keys.D1)) LoadScenario(new MinimalScenario());
        if (k.IsKeyDown(Keys.D2) && !_prevKeys.IsKeyDown(Keys.D2)) LoadScenario(new CylinderScenario());
        if (k.IsKeyDown(Keys.D3) && !_prevKeys.IsKeyDown(Keys.D3)) LoadScenario(new CollidersScenario());
        if (k.IsKeyDown(Keys.D4) && !_prevKeys.IsKeyDown(Keys.D4)) LoadScenario(new LargeScenario());
        if (k.IsKeyDown(Keys.D5) && !_prevKeys.IsKeyDown(Keys.D5)) LoadScenario(new XLargeScenario());
        if (k.IsKeyDown(Keys.R) && !_prevKeys.IsKeyDown(Keys.R))
        {
            _scenario.Reset();
            // Reload cloth references after reset and rebuild buffers
            _cloths.Clear();
            foreach (var c in _scenario.Cloths) _cloths.Add(c);
            BuildEdgeBuffers();
        }

        // Orbit camera with right mouse drag, wheel zoom
        var m = Mouse.GetState();
        if (m.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Pressed)
        {
            var dx = m.X - _prevMouse.X;
            var dy = m.Y - _prevMouse.Y;
            _yaw += dx * 0.01f;
            _pitch = MathHelper.Clamp(_pitch + dy * 0.01f, -1.2f, 1.2f);
        }
        var wheelDelta = m.ScrollWheelValue - _prevMouse.ScrollWheelValue;
        _dist = MathHelper.Clamp(_dist - wheelDelta * 0.0025f, 2f, 20f);
        _prevMouse = m;
        _prevKeys = k;

        // Fixed-step update with accumulator
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (float.IsNaN(elapsed) || elapsed < 0f) elapsed = 0f;
        elapsed = MathF.Min(elapsed, 0.25f);
        _accum = MathF.Min(_accum + elapsed, 0.25f);
        const float fixedDt = 1f / 60f;
        int steps = 0; const int maxSteps = 8;
        _scenario.UpdatePreStep(elapsed);
        for (int ci = 0; ci < _cloths.Count; ci++)
        {
            _colliders.Clear();
            _scenario.GetCollidersFor(ci, _colliders);
            _cloths[ci].Sim.SetColliders(_colliders);
        }
        _solverTicks = 0;
        long t0;
        while (_accum >= fixedDt && steps < maxSteps)
        {
            foreach (var c in _cloths)
            {
                t0 = Stopwatch.GetTimestamp();
                c.Sim.Step(fixedDt, c.Pos, c.Vel);
                _solverTicks += Stopwatch.GetTimestamp() - t0;
            }
            _accum -= fixedDt;
            steps++;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        if (_effect is null || _vb is null || _ib is null) return;

        // Camera matrices
        var eye = ToXna(Orbit(_yaw, _pitch) * _dist);
        var view = Matrix.CreateLookAt(eye, XnaVec.Zero, XnaVec.Up);
        var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f),
            GraphicsDevice.Viewport.AspectRatio, 0.05f, 100f);
        _effect.View = view;
        _effect.Projection = proj;
        _effect.World = Matrix.Identity;

        // Update dynamic vertex buffer with latest positions
        UpdateEdgeVertices();

        GraphicsDevice.SetVertexBuffer(_vb);
        GraphicsDevice.Indices = _ib;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _indexCount / 2);
        }

        // Draw floor grid (static)
        if (_floorVb is not null && _floorIb is not null)
        {
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.SetVertexBuffer(_floorVb);
                GraphicsDevice.Indices = _floorIb;
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _floorIndexCount / 2);
            }
        }

        // Draw colliders (dynamic)
        DrawColliders();

        base.Draw(gameTime);
        UpdateWindowTitle(gameTime);
    }

    private void BuildEdgeBuffers()
    {
        _edgesPerCloth.Clear();
        foreach (var c in _cloths)
        {
            Geometry.BuildUniqueEdges(c.Tri, out var edges);
            // Validate edges against current position array bounds
            var list = new List<(int i,int j)>(edges.Length);
            int max = c.Pos.Length;
            for (int ei = 0; ei < edges.Length; ei++)
            {
                var e = edges[ei];
                if ((uint)e.i < (uint)max && (uint)e.j < (uint)max) list.Add(e);
            }
            _edgesPerCloth.Add(list.ToArray());
        }
        // allocate initial buffers
        var totalVerts = TotalEdgeVertexCount();
        if (totalVerts == 0) totalVerts = 2;
        _vb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), totalVerts, BufferUsage.WriteOnly);
        var indices = new int[totalVerts];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        _ib = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
        _ib.SetData(indices);
        _indexCount = indices.Length;
    }

    private VertexPositionColor[] _edgeVertsScratch = Array.Empty<VertexPositionColor>();
    private void UpdateEdgeVertices()
    {
        if (_vb is null) return;
        int needed = TotalEdgeVertexCount();
        if (needed <= 0) return;
        if (_vb.VertexCount < needed)
        {
            _vb.Dispose();
            _vb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), needed, BufferUsage.WriteOnly);
            var idx = new int[needed]; for (int i = 0; i < idx.Length; i++) idx[i] = i;
            _ib?.Dispose();
            _ib = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, idx.Length, BufferUsage.WriteOnly);
            _ib.SetData(idx);
            _indexCount = idx.Length;
        }
        if (_edgeVertsScratch.Length < needed) _edgeVertsScratch = new VertexPositionColor[needed];
        var verts = _edgeVertsScratch;
        int k = 0;
        for (int ci = 0; ci < _cloths.Count; ci++)
        {
            var c = _cloths[ci];
            var edges = _edgesPerCloth[ci];
            for (int e = 0; e < edges.Length; e++)
            {
                var (i,j) = edges[e];
                verts[k++] = new VertexPositionColor(ToXna(c.Pos[i]), Color.White);
                verts[k++] = new VertexPositionColor(ToXna(c.Pos[j]), Color.White);
            }
        }
        _vb.SetData(verts, 0, needed);
    }

    private int TotalEdgeVertexCount()
    {
        int total = 0;
        foreach (var edges in _edgesPerCloth) total += edges.Length * 2;
        return total;
    }

    private void BuildFloorGrid()
    {
        // Simple XZ grid around origin
        int half = 10; float step = 0.5f;
        var lines = new List<VertexPositionColor>();
        var y = 0f;
        for (int i = -half; i <= half; i++)
        {
            float x = i * step;
            lines.Add(new VertexPositionColor(new XnaVec(x, y, -half * step), Color.DarkGray));
            lines.Add(new VertexPositionColor(new XnaVec(x, y,  half * step), Color.DarkGray));
            float z = i * step;
            lines.Add(new VertexPositionColor(new XnaVec(-half * step, y, z), Color.DarkGray));
            lines.Add(new VertexPositionColor(new XnaVec( half * step, y, z), Color.DarkGray));
        }
        var verts = lines.ToArray();
        var idx = new short[verts.Length]; for (short i = 0; i < idx.Length; i++) idx[i] = i;
        _floorVb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), verts.Length, BufferUsage.WriteOnly);
        _floorVb.SetData(verts);
        _floorIb = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, idx.Length, BufferUsage.WriteOnly);
        _floorIb.SetData(idx);
        _floorIndexCount = idx.Length;
    }

    private static XnaVec ToXna(NVec v) => new(v.X, v.Y, v.Z);
    private static NVec Orbit(float yaw, float pitch)
    {
        var cy = MathF.Cos(yaw); var sy = MathF.Sin(yaw);
        var cp = MathF.Cos(pitch); var sp = MathF.Sin(pitch);
        return new NVec(cp * cy, sp, cp * sy);
    }

    private static double Ema(double prev, double value, double alpha) => prev == 0 ? value : (alpha * value + (1 - alpha) * prev);
    private void UpdateWindowTitle(GameTime time)
    {
        double totalMs = time.ElapsedGameTime.TotalMilliseconds;
        double solverMs = _solverTicks * 1000.0 / Stopwatch.Frequency;
        double sampleMs = Math.Max(0.0, totalMs - solverMs);
        double fps = totalMs > 1e-6 ? 1000.0 / totalMs : 0.0;
        const double a = 0.2;
        _emaFps = Ema(_emaFps, fps, a);
        _emaSolverMs = Ema(_emaSolverMs, solverMs, a);
        _emaSampleMs = Ema(_emaSampleMs, sampleMs, a);
        _emaTotalMs = Ema(_emaTotalMs, totalMs, a);
        int totalVerts = 0; foreach (var c in _cloths) totalVerts += c.Pos.Length;
        Window.Title = $"DotCloth MonoGame Sample — {_scenario.Name} | FPS={_emaFps:F1} | Solver={_emaSolverMs:F2}ms | App={_emaSampleMs:F2}ms | Total={_emaTotalMs:F2}ms | Verts={totalVerts}";
    }

    private void LoadScenario(IScenario scenario)
    {
        _scenario = scenario;
        _scenario.Initialize();
        _cloths.Clear();
        foreach (var c in _scenario.Cloths) _cloths.Add(c);
        BuildEdgeBuffers();
    }

    private void DrawColliders()
    {
        if (_effect is null) return;
        var lines = _colliderLines;
        for (int ci = 0; ci < _cloths.Count; ci++)
        {
            _colliderViz.Clear();
            _scenario.GetColliderVisualsFor(ci, _colliderViz);
            foreach (var v in _colliderViz)
            {
                switch (v.Kind)
                {
                    case ColliderKind.Plane:
                        // Already visualized as grid
                        break;
                    case ColliderKind.Sphere:
                        AddSphereLines(lines, ToXna(v.Center), v.Radius, Color.Orange);
                        break;
                    case ColliderKind.Capsule:
                        AddCapsuleLines(lines, ToXna(v.P0), ToXna(v.P1), v.Radius, Color.OrangeRed);
                        break;
                }
            }
        }
        if (lines.Count == 0) return;
        EnsureColliderBuffers(lines.Count);
        _colliderVb!.SetData(lines.ToArray());
        GraphicsDevice.SetVertexBuffer(_colliderVb);
        GraphicsDevice.Indices = _colliderIb;
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _colliderIndexCount / 2);
        }
        lines.Clear();
    }

    private void EnsureColliderBuffers(int vertexCount)
    {
        if (_colliderVb is null || _colliderVb.VertexCount < vertexCount)
        {
            _colliderVb?.Dispose();
            _colliderVb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), vertexCount, BufferUsage.WriteOnly);
            var idx = new int[vertexCount]; for (int i = 0; i < idx.Length; i++) idx[i] = i;
            _colliderIb?.Dispose();
            _colliderIb = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, idx.Length, BufferUsage.WriteOnly);
            _colliderIb.SetData(idx);
            _colliderIndexCount = idx.Length;
        }
    }

    private static void AddSphereLines(List<VertexPositionColor> dst, XnaVec center, float radius, Color color)
    {
        int seg = 24;
        // three great circles
        AddCircle(dst, center, radius, new XnaVec(1,0,0), new XnaVec(0,1,0), seg, color);
        AddCircle(dst, center, radius, new XnaVec(1,0,0), new XnaVec(0,0,1), seg, color);
        AddCircle(dst, center, radius, new XnaVec(0,1,0), new XnaVec(0,0,1), seg, color);
    }

    private static void AddCapsuleLines(List<VertexPositionColor> dst, XnaVec p0, XnaVec p1, float radius, Color color)
    {
        int seg = 16;
        // body circles at ends
        var axis = Vector3.Normalize(p1 - p0);
        var up = Vector3.Dot(axis, Vector3.Up) > 0.9f ? Vector3.Right : Vector3.Up;
        var t = Vector3.Normalize(Vector3.Cross(axis, up));
        var b = Vector3.Normalize(Vector3.Cross(axis, t));
        AddCircle(dst, p0, radius, t, b, seg, color);
        AddCircle(dst, p1, radius, t, b, seg, color);
        // longitudinal lines
        for (int i = 0; i < 4; i++)
        {
            float a = (float)(i * Math.PI * 0.5);
            var dir = (float)Math.Cos(a) * t + (float)Math.Sin(a) * b;
            dst.Add(new VertexPositionColor(p0 + dir * radius, color));
            dst.Add(new VertexPositionColor(p1 + dir * radius, color));
        }
    }

    private static void AddCircle(List<VertexPositionColor> dst, XnaVec center, float radius, XnaVec u, XnaVec v, int seg, Color color)
    {
        for (int i = 0; i < seg; i++)
        {
            float a0 = (float)(2 * Math.PI * i / seg);
            float a1 = (float)(2 * Math.PI * (i+1) / seg);
            var p0 = center + (float)Math.Cos(a0) * u * radius + (float)Math.Sin(a0) * v * radius;
            var p1 = center + (float)Math.Cos(a1) * u * radius + (float)Math.Sin(a1) * v * radius;
            dst.Add(new VertexPositionColor(p0, color));
            dst.Add(new VertexPositionColor(p1, color));
        }
    }
}
