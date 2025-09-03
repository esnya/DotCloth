using System;
using System.Linq;
using Godot;
using DotCloth.Simulation.Core;
using DotCloth.Simulation.Parameters;
using Vec3 = System.Numerics.Vector3;
using Godot.Collections;

namespace DotCloth.GodotSample;

public partial class Main : Node3D
{
    // Simulation state
    private Vec3[] _positions = System.Array.Empty<Vec3>();
    private Vec3[] _velocities = System.Array.Empty<Vec3>();
    private int[] _triangles = System.Array.Empty<int>();
    private PbdSolver _solver = new();
    private ClothParameters _parms = new()
    {
        UseGravity = true,
        StretchStiffness = 0.9f,
        BendStiffness = 0.5f,
        Iterations = 10,
        ComplianceScale = 1e-6f,
    };

    // Rendering
    private ArrayMesh _mesh = default!;
    private MeshInstance3D _meshInst = default!;

    // Camera/light
    private Camera3D _cam = default!;
    private DirectionalLight3D _sun = default!;
    private CanvasLayer _ui = default!;
    private HashSet<int> _pinned = new();

    public override void _Ready()
    {
        Name = "DotCloth.GodotSample";
        SetupScene();
        SetupSimulation();
        BuildMesh();
        BuildUI();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Step simulation
        _solver.Step((float)delta, _positions, _velocities);
        // Rebuild geometry each frame (simple & robust for sample)
        UpdateMesh();
    }

    private void SetupScene()
    {
        // Camera
        _cam = new Camera3D
        {
            Position = new Vector3(0, 1.2f, 3.2f),
            RotationDegrees = new Vector3(-10, 0, 0),
            Current = true,
        };
        AddChild(_cam);

        // Light
        _sun = new DirectionalLight3D
        {
            LightColor = new Color(0.95f, 0.98f, 1f),
            LightEnergy = 1.1f,
        };
        _sun.RotationDegrees = new Vector3(-45, 30, 0);
        AddChild(_sun);

        // Mesh holder
        _mesh = new ArrayMesh();
        _meshInst = new MeshInstance3D { Mesh = _mesh };
        _meshInst.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.85f, 0.9f, 1.0f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
            Roughness = 0.65f,
            Metallic = 0.0f,
            CullMode = BaseMaterial3D.CullModeEnum.Back,
        };
        AddChild(_meshInst);
    }

    private void BuildUI()
    {
        _ui = new CanvasLayer();
        AddChild(_ui);
        var panel = new PanelContainer();
        panel.Name = "UI";
        panel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        panel.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        var vb = new VBoxContainer();
        vb.AddChild(new Label { Text = "DotCloth — Godot Sample" });
        vb.AddChild(new Label { Text = "Left-click: Pin | Right-click: Unpin | R: Reset Pins" });

        // Iterations
        var hbIter = new HBoxContainer();
        hbIter.AddChild(new Label { Text = "Iterations" });
        var sIter = new HSlider { MinValue = 1, MaxValue = 64, Step = 1, Value = _parms.Iterations, CustomMinimumSize = new Vector2(180, 0) };
        sIter.ValueChanged += (double v) => { _parms.Iterations = (int)v; _solver.UpdateParameters(_parms); };
        hbIter.AddChild(sIter);
        vb.AddChild(hbIter);

        // Stretch stiffness
        var hbStretch = new HBoxContainer();
        hbStretch.AddChild(new Label { Text = "Stretch" });
        var sStretch = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.01, Value = _parms.StretchStiffness, CustomMinimumSize = new Vector2(180, 0) };
        sStretch.ValueChanged += (double v) => { _parms.StretchStiffness = (float)v; _solver.UpdateParameters(_parms); };
        hbStretch.AddChild(sStretch);
        vb.AddChild(hbStretch);

        // Bend stiffness
        var hbBend = new HBoxContainer();
        hbBend.AddChild(new Label { Text = "Bend" });
        var sBend = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.01, Value = _parms.BendStiffness, CustomMinimumSize = new Vector2(180, 0) };
        sBend.ValueChanged += (double v) => { _parms.BendStiffness = (float)v; _solver.UpdateParameters(_parms); };
        hbBend.AddChild(sBend);
        vb.AddChild(hbBend);

        panel.AddChild(vb);
        _ui.AddChild(panel);
        panel.Position = new Vector2(10, 10);
        panel.Size = new Vector2(320, 120);
    }

    private void SetupSimulation()
    {
        // Cloth grid centered at origin
        (var pos, var tri) = MakeGrid(n: 32, spacing: 0.05f);
        _positions = pos;
        _velocities = new Vec3[_positions.Length];
        _triangles = tri;

        _solver = new PbdSolver();
        _solver.Initialize(_positions, _triangles, _parms);
        // Pin top edge (first row of the grid)
        _solver.PinVertices(Enumerable.Range(0, 32).ToArray());
        _solver.SetColliders(System.Array.Empty<DotCloth.Simulation.Collision.ICollider>());
    }

    private void BuildMesh()
    {
        _mesh.ClearSurfaces();
        RebuildGeometry();
    }

    private void UpdateMesh()
    {
        _mesh.ClearSurfaces();
        RebuildGeometry();
    }

    private void RebuildGeometry()
    {
        var normals = ComputeNormals(_positions, _triangles);
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int t = 0; t < _triangles.Length; t += 3)
        {
            int i0 = _triangles[t];
            int i1 = _triangles[t + 1];
            int i2 = _triangles[t + 2];
            // v0
            var n0 = normals[i0]; var p0 = _positions[i0];
            st.SetNormal(new Vector3(n0.X, n0.Y, n0.Z));
            st.AddVertex(new Vector3(p0.X, p0.Y, p0.Z));
            // v1
            var n1 = normals[i1]; var p1 = _positions[i1];
            st.SetNormal(new Vector3(n1.X, n1.Y, n1.Z));
            st.AddVertex(new Vector3(p1.X, p1.Y, p1.Z));
            // v2
            var n2 = normals[i2]; var p2 = _positions[i2];
            st.SetNormal(new Vector3(n2.X, n2.Y, n2.Z));
            st.AddVertex(new Vector3(p2.X, p2.Y, p2.Z));
        }
        st.Commit(_mesh);
    }

    private static (Vec3[] pos, int[] tri) MakeGrid(int n, float spacing)
    {
        var pos = new Vec3[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
            pos[y * n + x] = new Vec3((x - n / 2f) * spacing, 0, -(y - n / 2f) * spacing);
        var tri = new int[(n - 1) * (n - 1) * 6];
        int t = 0;
        for (int y = 0; y < n - 1; y++)
        for (int x = 0; x < n - 1; x++)
        {
            int i = y * n + x;
            int ir = i + 1;
            int id = i + n;
            int idr = i + n + 1;
            tri[t++] = i; tri[t++] = ir; tri[t++] = id;
            tri[t++] = id; tri[t++] = ir; tri[t++] = idr;
        }
        return (pos, tri);
    }

    private static Vec3[] ComputeNormals(ReadOnlySpan<Vec3> positions, ReadOnlySpan<int> triangles)
    {
        var normals = new Vec3[positions.Length];
        for (int ti = 0; ti < triangles.Length; ti += 3)
        {
            int i0 = triangles[ti];
            int i1 = triangles[ti + 1];
            int i2 = triangles[ti + 2];
            var p0 = positions[i0]; var p1 = positions[i1]; var p2 = positions[i2];
            var e1 = p1 - p0; var e2 = p2 - p0;
            var n = Vec3.Cross(e1, e2);
            normals[i0] += n; normals[i1] += n; normals[i2] += n;
        }
        for (int i = 0; i < normals.Length; i++)
        {
            var n = normals[i];
            float len = n.Length();
            normals[i] = len > 1e-20f ? n / len : new Vec3(0, 1, 0);
        }
        return normals;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo)
        {
            if (k.Keycode == Key.R)
            {
                _solver.ClearPins();
                _pinned.Clear();
            }
        }
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            var mp = mb.Position;
            var rayFrom = _cam.ProjectRayOrigin(mp);
            var rayDir = _cam.ProjectRayNormal(mp);
            int bestIdx = -1; float bestD = float.PositiveInfinity;
            // Ray-to-point distance test
            for (int i = 0; i < _positions.Length; i++)
            {
                var p = _positions[i];
                var wp = new Vector3(p.X, p.Y, p.Z);
                var d = DistancePointToRay(wp, rayFrom, rayDir);
                if (d < bestD)
                {
                    bestD = d; bestIdx = i;
                }
            }
            const float pickRadius = 0.08f; // world units
            if (bestIdx >= 0 && bestD <= pickRadius)
            {
                if (mb.ButtonIndex == MouseButton.Left)
                {
                    if (_pinned.Add(bestIdx)) _solver.PinVertices(bestIdx);
                }
                else if (mb.ButtonIndex == MouseButton.Right)
                {
                    if (_pinned.Remove(bestIdx)) _solver.UnpinVertices(bestIdx);
                }
            }
        }
    }

    private static float DistancePointToRay(Vector3 p, Vector3 ro, Vector3 rd)
    {
        var v = p - ro; var c = v.Dot(rd);
        var proj = ro + rd * c;
        return p.DistanceTo(proj);
    }
}
