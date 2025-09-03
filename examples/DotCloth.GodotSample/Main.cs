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
    private MeshInstance3D _ground = default!;
    private WorldEnvironment _worldEnv = default!;

    // Camera/light
    private Camera3D _cam = default!;
    private DirectionalLight3D _sun = default!;
    private float _yaw = 0.4f, _pitch = 0.35f, _dist = 3.0f;
    private bool _orbiting = false; private Vector2 _lastMouse;
    private CanvasLayer _ui = default!;
    private HashSet<int> _pinned = new();

    private enum Scenario { Minimal, Tube, Collision, Tuning, Large }
    private Scenario _scenario = Scenario.Minimal;

    public override void _Ready()
    {
        Name = "DotCloth.GodotSample";
        SetupScene();
        SetupScenario(_scenario);
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
        // Camera (orbit)
        _cam = new Camera3D { Current = true };
        AddChild(_cam);
        UpdateCamera();

        // Light
        _sun = new DirectionalLight3D
        {
            LightColor = new Color(1.0f, 0.98f, 0.95f),
            LightEnergy = 2.0f,
        };
        _sun.RotationDegrees = new Vector3(-35, -35, 0);
        AddChild(_sun);

        // Ambient/environment
        _worldEnv = new WorldEnvironment();
        _worldEnv.Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.07f, 0.08f, 0.1f),
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightEnergy = 0.45f,
        };
        AddChild(_worldEnv);

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

        // Ground
        _ground = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(8, 8), SubdivideWidth = 1, SubdivideDepth = 1 },
            Position = new Vector3(0, -0.8f, 0),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.16f, 0.18f, 0.22f),
                Roughness = 0.9f,
                Metallic = 0.0f,
            }
        };
        AddChild(_ground);
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
        vb.AddChild(new Label { Text = "LMB: Pin | MMB: Unpin | RMB: Orbit | Wheel: Zoom | R: Reset Pins" });

        // Scenario selector
        var hbScenario = new HBoxContainer();
        hbScenario.AddChild(new Label { Text = "Scenario" });
        var scenarios = new OptionButton();
        scenarios.AddItem("Minimal", 0);
        scenarios.AddItem("Tube", 1);
        scenarios.AddItem("Collision", 2);
        scenarios.AddItem("Tuning", 3);
        scenarios.AddItem("Large", 4);
        scenarios.Selected = (int)_scenario;
        scenarios.ItemSelected += (long idx) => { SetupScenario((Scenario)idx); };
        hbScenario.AddChild(scenarios);
        vb.AddChild(hbScenario);

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
        panel.Size = new Vector2(360, 180);
    }

    private void SetupScenario(Scenario s)
    {
        _scenario = s;
        _pinned.Clear();
        switch (s)
        {
            case Scenario.Minimal:
            {
                (var pos, var tri) = MakeGrid(n: 32, spacing: 0.05f);
                _positions = pos;
                _velocities = new Vec3[_positions.Length];
                _triangles = tri;
                _solver = new PbdSolver();
                _parms.Iterations = 10;
                _solver.Initialize(_positions, _triangles, _parms);
                _solver.PinVertices(Enumerable.Range(0, 32).ToArray());
                _solver.SetColliders(new DotCloth.Simulation.Collision.ICollider[]{ new DotCloth.Simulation.Collision.PlaneCollider(new Vec3(0,1,0), -0.8f) });
                break;
            }
            case Scenario.Tube:
            {
                (var pos, var tri) = MakeCylinder(radial: 24, height: 24, radius: 0.6f, spacing: 0.05f);
                _positions = pos; _velocities = new Vec3[_positions.Length]; _triangles = tri;
                _solver = new PbdSolver();
                _parms.Iterations = 10;
                _solver.Initialize(_positions, _triangles, _parms);
                _solver.PinVertices(Enumerable.Range(0, 24).ToArray());
                _solver.SetColliders(new DotCloth.Simulation.Collision.ICollider[]{ new DotCloth.Simulation.Collision.PlaneCollider(new Vec3(0,1,0), -0.8f) });
                break;
            }
            case Scenario.Collision:
            {
                (var pos, var tri) = MakeGrid(n: 32, spacing: 0.05f);
                _positions = pos; _velocities = new Vec3[_positions.Length]; _triangles = tri;
                _solver = new PbdSolver();
                _parms.Iterations = 10;
                _solver.Initialize(_positions, _triangles, _parms);
                _solver.PinVertices(Enumerable.Range(0, 32).ToArray());
                var colliders = new DotCloth.Simulation.Collision.ICollider[]
                {
                    new DotCloth.Simulation.Collision.PlaneCollider(new Vec3(0,1,0), -0.8f),
                    new DotCloth.Simulation.Collision.SphereCollider(new Vec3(0,-0.3f,0), 0.4f),
                };
                _solver.SetColliders(colliders);
                break;
            }
            case Scenario.Large:
            {
                int n = 24; float spacing = 0.05f; int instX = 4, instY = 3; int instCount = instX * instY;
                var (basePos, baseTri) = MakeGrid(n, spacing);
                _positions = new Vec3[basePos.Length * instCount];
                _triangles = new int[baseTri.Length * instCount];
                _velocities = new Vec3[_positions.Length];
                int vertsPer = basePos.Length; int trisPer = baseTri.Length;
                float instGap = n * spacing * 1.3f;
                var pins = new System.Collections.Generic.List<int>(n * instCount);
                for (int iy = 0; iy < instY; iy++)
                for (int ix = 0; ix < instX; ix++)
                {
                    int inst = iy * instX + ix;
                    float ox = (ix - (instX - 1) * 0.5f) * instGap;
                    float oz = -((iy - (instY - 1) * 0.5f) * instGap);
                    for (int i = 0; i < vertsPer; i++)
                    {
                        var p = basePos[i];
                        _positions[inst * vertsPer + i] = new Vec3(p.X + ox, p.Y, p.Z + oz);
                    }
                    for (int i = 0; i < trisPer; i++)
                        _triangles[inst * trisPer + i] = baseTri[i] + inst * vertsPer;
                    for (int i = 0; i < n; i++) pins.Add(inst * vertsPer + i);
                }
                _solver = new PbdSolver();
                _parms.Iterations = 10;
                _solver.Initialize(_positions, _triangles, _parms);
                _solver.PinVertices(pins.ToArray());
                var colliders = new System.Collections.Generic.List<DotCloth.Simulation.Collision.ICollider> { new DotCloth.Simulation.Collision.PlaneCollider(new Vec3(0,1,0), -0.8f) };
                for (int iy = 0; iy < instY; iy++)
                for (int ix = 0; ix < instX; ix++)
                {
                    float ox = (ix - (instX - 1) * 0.5f) * instGap;
                    float oz = -((iy - (instY - 1) * 0.5f) * instGap);
                    colliders.Add(new DotCloth.Simulation.Collision.SphereCollider(new Vec3(ox, -0.3f, oz), 0.4f));
                }
                _solver.SetColliders(colliders.ToArray());
                break;
            }
            case Scenario.Tuning:
            {
                (var pos, var tri) = MakeGrid(n: 32, spacing: 0.05f);
                _positions = pos; _velocities = new Vec3[_positions.Length]; _triangles = tri;
                _solver = new PbdSolver();
                _parms.Iterations = 10;
                _solver.Initialize(_positions, _triangles, _parms);
                _solver.PinVertices(Enumerable.Range(0, 32).ToArray());
                _solver.SetColliders(new DotCloth.Simulation.Collision.ICollider[]{ new DotCloth.Simulation.Collision.PlaneCollider(new Vec3(0,1,0), -0.8f) });
                break;
            }
        }
        BuildMesh();
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

    private static (Vec3[] pos, int[] tri) MakeCylinder(int radial, int height, float radius, float spacing)
    {
        var pos = new Vec3[radial * height];
        for (int y = 0; y < height; y++)
        {
            float py = -y * spacing;
            for (int r = 0; r < radial; r++)
            {
                float theta = (float)(2 * Math.PI * r / radial);
                float x = radius * MathF.Cos(theta);
                float z = radius * MathF.Sin(theta);
                pos[y * radial + r] = new Vec3(x, py, z);
            }
        }
        var tri = new int[(height - 1) * radial * 6];
        int t = 0;
        for (int y = 0; y < height - 1; y++)
        for (int r = 0; r < radial; r++)
        {
            int r2 = (r + 1) % radial;
            int i = y * radial + r;
            int ir = y * radial + r2;
            int id = (y + 1) * radial + r;
            int idr = (y + 1) * radial + r2;
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
            if (k.Keycode == Key.Key1) SetupScenario(Scenario.Minimal);
            if (k.Keycode == Key.Key2) SetupScenario(Scenario.Tube);
            if (k.Keycode == Key.Key3) SetupScenario(Scenario.Collision);
            if (k.Keycode == Key.Key4) SetupScenario(Scenario.Tuning);
            if (k.Keycode == Key.Key5) SetupScenario(Scenario.Large);
        }
        if (@event is InputEventMouseButton mb)
        {
            if (mb.Pressed && (mb.ButtonIndex == MouseButton.Right)) { _orbiting = true; _lastMouse = mb.Position; }
            if (!mb.Pressed && (mb.ButtonIndex == MouseButton.Right)) { _orbiting = false; }
            if (mb.Pressed && (mb.ButtonIndex == MouseButton.WheelUp)) { _dist = Math.Max(0.5f, _dist * 0.9f); UpdateCamera(); }
            if (mb.Pressed && (mb.ButtonIndex == MouseButton.WheelDown)) { _dist = Math.Min(10f, _dist * 1.1f); UpdateCamera(); }
            if (mb.Pressed && (mb.ButtonIndex == MouseButton.Left))
            {
                var mp = mb.Position;
                var rayFrom = _cam.ProjectRayOrigin(mp);
                var rayDir = _cam.ProjectRayNormal(mp);
                int bestIdx = -1; float bestD = float.PositiveInfinity;
                for (int i = 0; i < _positions.Length; i++)
                {
                    var p = _positions[i];
                    var wp = new Vector3(p.X, p.Y, p.Z);
                    var d = DistancePointToRay(wp, rayFrom, rayDir);
                    if (d < bestD) { bestD = d; bestIdx = i; }
                }
                const float pickRadius = 0.08f;
                if (bestIdx >= 0 && bestD <= pickRadius)
                {
                    if (_pinned.Add(bestIdx)) _solver.PinVertices(bestIdx);
                }
            }
            if (mb.Pressed && (mb.ButtonIndex == MouseButton.Middle))
            {
                var mp = mb.Position;
                var rayFrom = _cam.ProjectRayOrigin(mp);
                var rayDir = _cam.ProjectRayNormal(mp);
                int bestIdx = -1; float bestD = float.PositiveInfinity;
                foreach (var i in _pinned)
                {
                    var p = _positions[i];
                    var d = DistancePointToRay(new Vector3(p.X,p.Y,p.Z), rayFrom, rayDir);
                    if (d < bestD) { bestD = d; bestIdx = i; }
                }
                if (bestIdx >= 0) { _pinned.Remove(bestIdx); _solver.UnpinVertices(bestIdx); }
            }
        }
        if (@event is InputEventMouseMotion mm && _orbiting)
        {
            var delta = mm.Position - _lastMouse;
            _lastMouse = mm.Position;
            _yaw += delta.X * 0.005f;
            _pitch = Math.Clamp(_pitch + delta.Y * 0.005f, -1.2f, 1.2f);
            UpdateCamera();
        }
    }

    private static float DistancePointToRay(Vector3 p, Vector3 ro, Vector3 rd)
    {
        var v = p - ro; var c = v.Dot(rd);
        var proj = ro + rd * c;
        return p.DistanceTo(proj);
    }
    private void UpdateCamera()
    {
        var eye = Orbit(_yaw, _pitch, _dist);
        _cam.Position = eye;
        _cam.LookAt(Vector3.Zero, Vector3.Up);
    }

    private static Vector3 Orbit(float yaw, float pitch, float dist)
    {
        var ey = new Vec3(
            MathF.Cos(pitch) * MathF.Cos(yaw),
            MathF.Sin(-pitch),
            MathF.Cos(pitch) * MathF.Sin(yaw)
        );
        var v3 = new Vector3(ey.X, ey.Y, ey.Z);
        return v3 * dist;
    }
}
