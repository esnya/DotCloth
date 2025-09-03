using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DotCloth.Simulation;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
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

    // Camera (simple orbit)
    private float _yaw = 0.6f;
    private float _pitch = 0.6f;
    private float _dist = 6f;

    // Cloth state
    private IClothSimulator _sim = new PbdSolver();
    private NVec[] _pos = Array.Empty<NVec>();
    private NVec[] _vel = Array.Empty<NVec>();
    private int[] _tri = Array.Empty<int>();

    // Input state
    private MouseState _prevMouse;

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

        // Build a simple quad cloth (NxN grid)
        const int n = 20;
        Geometry.MakeGrid(n, n, 0.1f, out _pos, out _tri);
        _vel = new NVec[_pos.Length];

        var p = new ClothParameters
        {
            VertexMass = 1.0f,
            Damping = 0.05f,
            AirDrag = 0.2f,
            StretchStiffness = 0.9f,
            BendStiffness = 0.1f,
            GravityScale = 1.0f,
            UseGravity = true,
            Substeps = 1,
            Iterations = 8
        };

        _sim.Initialize(_pos, _tri, p);

        // Pin top row
        var pins = new int[n];
        for (int i = 0; i < n; i++) pins[i] = (n - 1) * n + i;
        _sim.PinVertices(pins);

        // Graphics setup
        _effect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };
        BuildEdgeBuffers();
    }

    protected override void Update(GameTime gameTime)
    {
        var k = Keyboard.GetState();
        if (k.IsKeyDown(Keys.Escape)) Exit();

        // Orbit camera with right mouse drag, wheel zoom
        var m = Mouse.GetState();
        if (m.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Pressed)
        {
            var dx = m.X - _prevMouse.X;
            var dy = m.Y - _prevMouse.Y;
            _yaw += dx * 0.01f;
            _pitch = MathHelper.Clamp(_pitch + dy * 0.01f, -1.2f, 1.2f);
        }
        _dist = MathHelper.Clamp(_dist - m.ScrollWheelValue * 0.0005f, 2f, 20f);
        _prevMouse = m;

        // Step simulation at fixed dt
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        dt = MathF.Min(dt, 1f / 60f);
        _sim.Step(dt, _pos, _vel);

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

        base.Draw(gameTime);
    }

    private void BuildEdgeBuffers()
    {
        // Build unique undirected edges from triangles
        Geometry.BuildUniqueEdges(_tri, out var edges);

        // For each edge, store two vertices (line list)
        var verts = new VertexPositionColor[edges.Length * 2];
        for (int e = 0; e < edges.Length; e++)
        {
            var (i, j) = edges[e];
            verts[2 * e + 0] = new VertexPositionColor(ToXna(_pos[i]), Color.White);
            verts[2 * e + 1] = new VertexPositionColor(ToXna(_pos[j]), Color.White);
        }

        var indices = new short[verts.Length];
        for (short i = 0; i < indices.Length; i++) indices[i] = i;

        _vb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), verts.Length, BufferUsage.WriteOnly);
        _vb.SetData(verts);
        _ib = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _ib.SetData(indices);
        _indexCount = indices.Length;
    }

    private void UpdateEdgeVertices()
    {
        if (_vb is null) return;
        // Map current edges again into the dynamic vertex buffer
        Geometry.BuildUniqueEdges(_tri, out var edges);
        var verts = new VertexPositionColor[edges.Length * 2];
        for (int e = 0; e < edges.Length; e++)
        {
            var (i, j) = edges[e];
            verts[2 * e + 0] = new VertexPositionColor(ToXna(_pos[i]), Color.White);
            verts[2 * e + 1] = new VertexPositionColor(ToXna(_pos[j]), Color.White);
        }
        _vb.SetData(verts);
    }

    private static XnaVec ToXna(NVec v)
        => new(v.X, v.Y, v.Z);

    private static NVec Orbit(float yaw, float pitch)
    {
        var cy = MathF.Cos(yaw); var sy = MathF.Sin(yaw);
        var cp = MathF.Cos(pitch); var sp = MathF.Sin(pitch);
        // Spherical to Cartesian around origin
        return new NVec(cp * cy, sp, cp * sy);
    }
}
