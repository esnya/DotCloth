using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DotCloth;
using DotCloth.Forces;
using DotCloth.Constraints;
using DotCloth.MassSpring;
using DotCloth.MonoGameSample.Scenarios;
using XnaVec = Microsoft.Xna.Framework.Vector3;
using NVec = System.Numerics.Vector3;

namespace DotCloth.MonoGameSample;

public sealed class SampleGame : Game
{
    private const double EPSILON = 1e-6;
    private const double EMA_ALPHA = 0.2;

    private readonly GraphicsDeviceManager _graphics;
    private BasicEffect? _effect;
    private VertexBuffer? _vb;
    private IndexBuffer? _ib;
    private int _indexCount;
    private VertexBuffer? _floorVb;
    private IndexBuffer? _floorIb;
    private int _floorIndexCount;
    private readonly List<ColliderViz> _colliderViz = new();
    private readonly List<VertexPositionColor> _colliderLines = new();
    private VertexBuffer? _colliderVb;
    private IndexBuffer? _colliderIb;
    private int _colliderIndexCount;
    private readonly IScenario[] _scenarios =
    {
        new MinimalScenario(),
        new LargeScenario(),
        new XLargeScenario(),
        new CollidersScenario()
    };
    private readonly ForceModel[] _models =
    {
        ForceModel.Springs,
        ForceModel.Shells,
        ForceModel.Fem,
        ForceModel.SpringsWithStrain
    };
    private int _scenarioIndex;
    private int _modelIndex;
    private ForceCloth _cloth = null!;
    private VertexPositionColor[] _verts = Array.Empty<VertexPositionColor>();
    private KeyboardState _prevKeys;
    private MouseState _prevMouse;
    private float _accum;
    private long _solverTicks;
    private double _emaFps, _emaSolverMs, _emaTotalMs, _emaSampleMs;
    private float _yaw = 0.6f;
    private float _pitch = 0.6f;
    private float _dist = 20f;

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
        _effect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };
        BuildFloorGrid();
        _prevMouse = Mouse.GetState();
        LoadScenario();
    }

    private void LoadScenario()
    {
        var scenario = _scenarios[_scenarioIndex];
        _cloth = scenario.Create(_models[_modelIndex]);
        var size = scenario.GridSize;
        _verts = new VertexPositionColor[_cloth.Positions.Length];
        var indices = BuildIndices(size);
        _vb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), _verts.Length, BufferUsage.WriteOnly);
        _ib = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _ib.SetData(indices);
        _indexCount = indices.Length;
        UpdateWindowTitle(new GameTime(TimeSpan.Zero, TimeSpan.Zero));
    }

    private static double Ema(double prev, double value, double alpha) => prev == 0 ? value : (alpha * value + (1 - alpha) * prev);

    private void UpdateWindowTitle(GameTime time)
    {
        double totalMs = time.ElapsedGameTime.TotalMilliseconds;
        double solverMs = _solverTicks * 1000.0 / Stopwatch.Frequency;
        double sampleMs = Math.Max(0.0, totalMs - solverMs);
        double fps = totalMs > EPSILON ? 1000.0 / totalMs : 0.0;
        _emaFps = Ema(_emaFps, fps, EMA_ALPHA);
        _emaSolverMs = Ema(_emaSolverMs, solverMs, EMA_ALPHA);
        _emaSampleMs = Ema(_emaSampleMs, sampleMs, EMA_ALPHA);
        _emaTotalMs = Ema(_emaTotalMs, totalMs, EMA_ALPHA);
        int verts = _cloth.Positions.Length;
        var scenarioName = _scenarios[_scenarioIndex].Name;
        var modelName = _models[_modelIndex].ToString();
        Window.Title = $"DotCloth MonoGame Sample — {scenarioName} - {modelName} | FPS={_emaFps:F1} | Solver={_emaSolverMs:F2}ms | App={_emaSampleMs:F2}ms | Total={_emaTotalMs:F2}ms | Verts={verts}";
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.Escape))
        {
            Exit();
            return;
        }
        if (kb.IsKeyDown(Keys.S) && !_prevKeys.IsKeyDown(Keys.S))
        {
            _scenarioIndex = (_scenarioIndex + 1) % _scenarios.Length;
            LoadScenario();
        }
        if (kb.IsKeyDown(Keys.M) && !_prevKeys.IsKeyDown(Keys.M))
        {
            _modelIndex = (_modelIndex + 1) % _models.Length;
            LoadScenario();
        }
        var mouse = Mouse.GetState();
        if (mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Pressed)
        {
            var dx = mouse.X - _prevMouse.X;
            var dy = mouse.Y - _prevMouse.Y;
            _yaw += dx * 0.01f;
            _pitch = MathHelper.Clamp(_pitch + dy * 0.01f, -1.2f, 1.2f);
        }
        var wheel = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
        _dist = MathHelper.Clamp(_dist - wheel * 0.0025f, 5f, 100f);
        _prevMouse = mouse;
        _prevKeys = kb;

        const float dt = 1f / 60f;
        _accum += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _solverTicks = 0;
        while (_accum >= dt)
        {
            var t0 = Stopwatch.GetTimestamp();
            _scenarios[_scenarioIndex].Update(dt);
            _cloth.Step(dt);
            _solverTicks += Stopwatch.GetTimestamp() - t0;
            _accum -= dt;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        if (_effect == null || _vb == null || _ib == null)
        {
            return;
        }
        var eye = Orbit(_yaw, _pitch) * _dist;
        var view = Matrix.CreateLookAt(eye, XnaVec.Zero, XnaVec.UnitY);
        var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 200f);
        _effect.View = view;
        _effect.Projection = proj;

        var pos = _cloth.Positions;
        for (int i = 0; i < pos.Length; i++)
        {
            _verts[i].Position = new XnaVec(pos[i].X, pos[i].Y, pos[i].Z);
            _verts[i].Color = Color.White;
        }
        _vb.SetData(_verts);

        GraphicsDevice.SetVertexBuffer(_vb);
        GraphicsDevice.Indices = _ib;
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
        }
        if (_floorVb != null && _floorIb != null)
        {
            GraphicsDevice.SetVertexBuffer(_floorVb);
            GraphicsDevice.Indices = _floorIb;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _floorIndexCount / 2);
            }
        }
        DrawColliders();
        base.Draw(gameTime);
        UpdateWindowTitle(gameTime);
    }

    private void DrawColliders()
    {
        if (_effect == null) return;
        if (_scenarios[_scenarioIndex] is not IColliderScenario cs) return;
        _colliderViz.Clear();
        cs.CollectColliderVisuals(_colliderViz);
        if (_colliderViz.Count == 0) return;
        _colliderLines.Clear();
        foreach (var v in _colliderViz)
        {
            switch (v.Kind)
            {
                case ColliderKind.Sphere:
                    AddSphereLines(_colliderLines, ToXna(v.Center), v.Radius, Color.Orange);
                    break;
                case ColliderKind.Capsule:
                    AddCapsuleLines(_colliderLines, ToXna(v.P0), ToXna(v.P1), v.Radius, Color.OrangeRed);
                    break;
            }
        }
        EnsureColliderBuffers(_colliderLines.Count);
        _colliderVb!.SetData(_colliderLines.ToArray());
        GraphicsDevice.SetVertexBuffer(_colliderVb);
        GraphicsDevice.Indices = _colliderIb;
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _colliderIndexCount / 2);
        }
    }

    private static short[] BuildIndices(int size)
    {
        var indices = new short[(size - 1) * (size - 1) * 6];
        int k = 0;
        for (int y = 0; y < size - 1; y++)
        {
            for (int x = 0; x < size - 1; x++)
            {
                short i0 = (short)(y * size + x);
                short i1 = (short)(y * size + x + 1);
                short i2 = (short)((y + 1) * size + x);
                short i3 = (short)((y + 1) * size + x + 1);
                indices[k++] = i0; indices[k++] = i2; indices[k++] = i1;
                indices[k++] = i2; indices[k++] = i3; indices[k++] = i1;
            }
        }
        return indices;
    }

    private void BuildFloorGrid()
    {
        int half = 10; float step = 0.5f; var lines = new List<VertexPositionColor>();
        float y = 0f;
        for (int i = -half; i <= half; i++)
        {
            float x = i * step;
            lines.Add(new VertexPositionColor(new XnaVec(x, y, -half * step), Color.DarkGray));
            lines.Add(new VertexPositionColor(new XnaVec(x, y, half * step), Color.DarkGray));
            float z = i * step;
            lines.Add(new VertexPositionColor(new XnaVec(-half * step, y, z), Color.DarkGray));
            lines.Add(new VertexPositionColor(new XnaVec(half * step, y, z), Color.DarkGray));
        }
        var verts = lines.ToArray();
        var idx = new short[verts.Length];
        for (short i = 0; i < idx.Length; i++) idx[i] = i;
        _floorVb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), verts.Length, BufferUsage.WriteOnly);
        _floorVb.SetData(verts);
        _floorIb = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, idx.Length, BufferUsage.WriteOnly);
        _floorIb.SetData(idx);
        _floorIndexCount = idx.Length;
    }

    private void EnsureColliderBuffers(int vertexCount)
    {
        if (_colliderVb == null || _colliderVb.VertexCount < vertexCount)
        {
            _colliderVb?.Dispose();
            _colliderVb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), vertexCount, BufferUsage.WriteOnly);
            _colliderIb?.Dispose();
            var idx = new short[vertexCount];
            for (short i = 0; i < idx.Length; i++) idx[i] = i;
            _colliderIb = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, idx.Length, BufferUsage.WriteOnly);
            _colliderIb.SetData(idx);
            _colliderIndexCount = idx.Length;
        }
    }

    private static void AddSphereLines(List<VertexPositionColor> dst, XnaVec center, float radius, Color color)
    {
        int seg = 24;
        AddCircle(dst, center, radius, new XnaVec(1, 0, 0), new XnaVec(0, 1, 0), seg, color);
        AddCircle(dst, center, radius, new XnaVec(1, 0, 0), new XnaVec(0, 0, 1), seg, color);
        AddCircle(dst, center, radius, new XnaVec(0, 1, 0), new XnaVec(0, 0, 1), seg, color);
    }

    private static void AddCapsuleLines(List<VertexPositionColor> dst, XnaVec p0, XnaVec p1, float radius, Color color)
    {
        int seg = 16;
        var axis = XnaVec.Normalize(p1 - p0);
        var up = XnaVec.Dot(axis, XnaVec.Up) > 0.9f ? XnaVec.Right : XnaVec.Up;
        var t = XnaVec.Normalize(XnaVec.Cross(axis, up));
        var b = XnaVec.Normalize(XnaVec.Cross(axis, t));
        AddCircle(dst, p0, radius, t, b, seg, color);
        AddCircle(dst, p1, radius, t, b, seg, color);
        for (int i = 0; i < 4; i++)
        {
            float a = (float)(i * Math.PI * 0.5);
            var dir = MathF.Cos(a) * t + MathF.Sin(a) * b;
            dst.Add(new VertexPositionColor(p0 + dir * radius, color));
            dst.Add(new VertexPositionColor(p1 + dir * radius, color));
        }
    }

    private static void AddCircle(List<VertexPositionColor> dst, XnaVec center, float radius, XnaVec u, XnaVec v, int seg, Color color)
    {
        for (int i = 0; i < seg; i++)
        {
            float a0 = (float)(2 * Math.PI * i / seg);
            float a1 = (float)(2 * Math.PI * (i + 1) / seg);
            var p0 = center + MathF.Cos(a0) * u * radius + MathF.Sin(a0) * v * radius;
            var p1 = center + MathF.Cos(a1) * u * radius + MathF.Sin(a1) * v * radius;
            dst.Add(new VertexPositionColor(p0, color));
            dst.Add(new VertexPositionColor(p1, color));
        }
    }

    private static XnaVec Orbit(float yaw, float pitch)
    {
        var cy = MathF.Cos(yaw); var sy = MathF.Sin(yaw);
        var cp = MathF.Cos(pitch); var sp = MathF.Sin(pitch);
        return new XnaVec(cp * cy, sp, cp * sy);
    }

    private static XnaVec ToXna(NVec v) => new(v.X, v.Y, v.Z);
}
