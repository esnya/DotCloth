using System;
using System.Numerics;
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
        while (_accum >= dt)
        {
            _cloth.Step(dt);
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
