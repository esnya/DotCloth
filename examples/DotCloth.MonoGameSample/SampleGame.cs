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

    // Input state
    private MouseState _prevMouse;

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
        if (k.IsKeyDown(Keys.D1)) LoadScenario(new MinimalScenario());
        if (k.IsKeyDown(Keys.D2)) LoadScenario(new CylinderScenario());
        if (k.IsKeyDown(Keys.D3)) LoadScenario(new CollidersScenario());
        if (k.IsKeyDown(Keys.D4)) LoadScenario(new LargeScenario());
        if (k.IsKeyDown(Keys.R)) _scenario.Reset();

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

        // Fixed-step update with accumulator
        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (float.IsNaN(elapsed) || elapsed < 0f) elapsed = 0f;
        elapsed = MathF.Min(elapsed, 0.25f);
        _accum = MathF.Min(_accum + elapsed, 0.25f);
        const float fixedDt = 1f / 60f;
        int steps = 0; const int maxSteps = 8;
        _scenario.UpdatePreStep(elapsed);
        _colliders.Clear();
        _scenario.GetColliders(_colliders);
        foreach (var c in _cloths) c.Sim.SetColliders(_colliders);
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

        base.Draw(gameTime);
        UpdateWindowTitle(gameTime);
    }

    private void BuildEdgeBuffers()
    {
        _edgesPerCloth.Clear();
        foreach (var c in _cloths)
        {
            Geometry.BuildUniqueEdges(c.Tri, out var edges);
            _edgesPerCloth.Add(edges);
        }
        // allocate initial buffers
        var totalVerts = TotalEdgeVertexCount();
        if (totalVerts == 0) totalVerts = 2;
        _vb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), totalVerts, BufferUsage.WriteOnly);
        var indices = new short[totalVerts];
        for (short i = 0; i < indices.Length; i++) indices[i] = i;
        _ib = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _ib.SetData(indices);
        _indexCount = indices.Length;
    }

    private void UpdateEdgeVertices()
    {
        if (_vb is null) return;
        int needed = TotalEdgeVertexCount();
        if (needed <= 0) return;
        if (_vb.VertexCount < needed)
        {
            _vb.Dispose();
            _vb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), needed, BufferUsage.WriteOnly);
            var idx = new short[needed]; for (short i = 0; i < idx.Length; i++) idx[i] = i;
            _ib?.Dispose();
            _ib = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, idx.Length, BufferUsage.WriteOnly);
            _ib.SetData(idx);
            _indexCount = idx.Length;
        }
        var verts = new VertexPositionColor[needed];
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
        _vb.SetData(verts);
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
        Window.Title = $"DotCloth MonoGame Sample — {_scenario.Name} | FPS={_emaFps:F1} | Lib={_emaSolverMs:F2}ms | App={_emaSampleMs:F2}ms | Total={_emaTotalMs:F2}ms";
    }

    private void LoadScenario(IScenario scenario)
    {
        _scenario = scenario;
        _scenario.Initialize();
        _cloths.Clear();
        foreach (var c in _scenario.Cloths) _cloths.Add(c);
        BuildEdgeBuffers();
    }
}

