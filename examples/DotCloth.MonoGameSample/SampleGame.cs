using System;
using System.Numerics;
using System.Diagnostics;
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
    private readonly IScenario[] _scenarios = { new MinimalScenario(), new LargeScenario(), new XLargeScenario() };
    private readonly string[] _models = { "Springs", "Shells", "FEM", "Springs+Strain" };
    private int _scenarioIndex;
    private int _modelIndex;
    private ForceCloth _cloth = null!;
    private VertexPositionColor[] _verts = Array.Empty<VertexPositionColor>();
    private KeyboardState _prevKeys;
    private float _accum;
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
    }

    protected override void Initialize()
    {
        base.Initialize();
        _effect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };
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
        var modelName = _models[_modelIndex];
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
        _prevKeys = kb;

        const float dt = 1f / 60f;
        _accum += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _solverTicks = 0;
        while (_accum >= dt)
        {
            var t0 = Stopwatch.GetTimestamp();
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
        var view = Matrix.CreateLookAt(new XnaVec(30, 30, 30), XnaVec.Zero, XnaVec.UnitY);
        var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 100f);
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

        base.Draw(gameTime);
        UpdateWindowTitle(gameTime);
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
}
