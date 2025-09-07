using System;
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
        BendStiffness = 0.1f,
        Iterations = 8,
        ComplianceScale = 1e-6f,
    };

    // Rendering
    private ArrayMesh _mesh = default!;
    private MeshInstance3D _meshInst = default!;
    private MeshInstance3D _ground = default!;
    private Node3D _scenarios = default!;
    private WorldEnvironment _worldEnv = default!;
    private System.Collections.Generic.List<MeshInstance3D> _largeColliderVis = new();

    // Camera/light
    private Camera3D _cam = default!;
    private DirectionalLight3D _sun = default!;
    private float _yaw = 0.6f, _pitch = -0.8f, _dist = 3.0f;
    private bool _orbiting = false; private Vector2 _lastMouse;
    private Vector3 _target = Vector3.Zero;
    private SampleUi _ui = default!;
    private VBoxContainer _scenarioControls = default!;
    private Label _perfLabel = default!;
    private bool _updatingUI = false;
    private HashSet<int> _pinned = new();
    private readonly bool _xpbd = Type.GetType("DotCloth.Simulation.Core.XpbdSolver") is not null;

    [Export]
    public float PickRadius { get; set; } = 0.08f;

    private enum Scenario { Minimal, Tube, Collision, Large }
    private Scenario _scenario = Scenario.Minimal;
    // Collision scenario collider
    private ColliderDefinition? _collider;
    private Vector3 _colliderBasePos;

    public override void _Ready()
    {
        Name = "DotCloth.GodotSample";
        if (_xpbd)
            DisplayServer.WindowSetTitle("DotCloth.GodotSample (XPBD)");
        SetupScene();
        SetupUI();
        SetupScenario(_scenario);
    }

    public override void _PhysicsProcess(double delta)
    {
        _time += delta;
        // Dynamic collider motion (small oscillation) to observe contacts
        if (_scenario == Scenario.Collision && _collider != null)
        {
            float t = (float)_time;
            var basePos = _colliderBasePos;
            float x = basePos.X + 0.15f * MathF.Sin(0.9f * t);
            float z = basePos.Z + 0.15f * MathF.Cos(0.7f * t);
            float y = basePos.Y + 0.05f * MathF.Sin(1.2f * t);
            _collider.GlobalPosition = new Vector3(x, y, z);
            ApplyCollisionSetup();
        }
        else if (_scenario == Scenario.Large)
        {
            float t = (float)_time;
            var collidersDyn = new System.Collections.Generic.List<DotCloth.Simulation.Collision.ICollider>();
            collidersDyn.Add(new DotCloth.Simulation.Collision.PlaneCollider(new Vec3(0, 1, 0), -0.8f));
            for (int i = 0; i < _largeCenters.Count; i++)
            {
                var c = _largeCenters[i];
                float phase = i * 0.6f;
                var pos = new Vec3(
                    c.X + 0.10f * MathF.Sin(0.8f * t + phase),
                    c.Y,
                    c.Z + 0.10f * MathF.Cos(0.6f * t + phase)
                );
                collidersDyn.Add(new DotCloth.Simulation.Collision.SphereCollider(pos, _largeColliderRadius));
                if (i < _largeColliderVis.Count)
                    _largeColliderVis[i].Position = new Vector3(pos.X, pos.Y, pos.Z);
            }
            _solver.SetColliders(collidersDyn.ToArray());
        }

        // Step simulation (measure)
        _sw.Restart();
        _solver.Step((float)delta, _positions, _velocities);
        var simMs = (float)_sw.Elapsed.TotalMilliseconds;
        // Rebuild geometry each frame (simple & robust for sample)
        _sw.Restart();
        UpdateMesh();
        var meshMs = (float)_sw.Elapsed.TotalMilliseconds;

        // Update perf label at low frequency to reduce overhead
        _perfAccum += delta;
        if (_perfAccum >= 0.15 && _perfLabel != null)
        {
            _perfAccum = 0.0;
            // Smooth FPS estimate based on actual frame rate
            double instFps = Engine.GetFramesPerSecond();
            _fpsSmooth = _fpsSmooth <= 0 ? instFps : (_fpsSmooth * 0.9 + instFps * 0.1);
            var totalMs = simMs + meshMs;
            _perfLabel.Text = $"Perf: Total {totalMs:F2} ms | Sim {simMs:F2} | Mesh {meshMs:F2} | FPS {(float)_fpsSmooth:F1} | Verts {_positions.Length}";
        }
    }

    private void SetupScene()
    {
        _cam = GetNode<Camera3D>("Camera");
        _sun = GetNode<DirectionalLight3D>("Sun");
        _worldEnv = GetNode<WorldEnvironment>("WorldEnvironment");
        _ground = GetNode<MeshInstance3D>("Ground");
        _scenarios = GetNode<Node3D>("Scenarios");

        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightEnergy = 0.35f,
            AmbientLightSkyContribution = 0.7f,
        };
        env.Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() };
        _worldEnv.Environment = env;

        _ground.Mesh = new PlaneMesh { Size = new Vector2(8, 8), SubdivideWidth = 1, SubdivideDepth = 1 };
        _ground.Position = new Vector3(0, -0.8f, 0);
        _ground.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.16f, 0.18f, 0.22f),
            Roughness = 0.9f,
            Metallic = 0.0f,
        };

        _sun.RotationDegrees = new Vector3(45, 145, 0);
        UpdateCamera();
    }

    private void SetupUI()
    {
        _ui = GetNode<SampleUi>("UI/Panel");
        _scenarioControls = _ui.ScenarioControls;
        _perfLabel = GetNode<Label>("UI/Panel/VBox/PerfLabel");

        _ui.SetScenarioOptions(new[] { "Minimal", "Tube", "Collision", "Large" }, (int)_scenario);
        _ui.SetScenarioDescription(GetScenarioDescription(_scenario));
        _ui.SetParameterValues(_parms.Iterations, _parms.StretchStiffness, _parms.BendStiffness);
    }

    // Collider scenario controls
    private int _largeN = 16;
    private int _largeInstX = 2;
    private int _largeInstY = 2;
    private System.Diagnostics.Stopwatch _sw = new();
    private double _perfAccum = 0.0;
    private double _fpsSmooth = 0.0;
    private double _time = 0.0;
    private System.Collections.Generic.List<Vec3> _largeCenters = new();
    private float _largeColliderRadius = 0.3f;
    private Label? _largeInstInfo;

    private void BuildScenarioControls()
    {
        if (_scenarioControls == null) return;
        foreach (var c in _scenarioControls.GetChildren()) c.QueueFree();
        switch (_scenario)
        {
            case Scenario.Collision:
                {
                    _scenarioControls.AddChild(new Label { Text = "Collision Controls" });
                    // Collider type
                    var hbType = new HBoxContainer();
                    hbType.AddChild(new Label { Text = "Collider" });
                    var opt = new OptionButton();
                    opt.AddItem("Sphere", 0); opt.AddItem("Capsule", 1);
                    opt.Selected = (int)(_collider?.Shape ?? ColliderDefinition.ShapeKind.Sphere);
                    opt.ItemSelected += (long i) => { if (_collider != null) { _collider.Shape = (ColliderDefinition.ShapeKind)i; ApplyCollisionSetup(); } };
                    hbType.AddChild(opt);
                    _scenarioControls.AddChild(hbType);

                    if (_collider != null)
                    {
                        // Radius
                        var hbR = new HBoxContainer();
                        hbR.AddChild(new Label { Text = "Radius" });
                        var sR = new HSlider { MinValue = 0.2, MaxValue = 0.6, Step = 0.01, Value = _collider.Radius, CustomMinimumSize = new Vector2(220, 0) };
                        sR.ValueChanged += (double v) => { _collider.Radius = (float)v; ApplyCollisionSetup(); };
                        hbR.AddChild(sR);
                        _scenarioControls.AddChild(hbR);
                    }
                    break;
                }
            case Scenario.Large:
                {
                    _scenarioControls.AddChild(new Label { Text = "Large Controls" });
                    // Grid resolution (n)
                    var hbN = new HBoxContainer();
                    hbN.AddChild(new Label { Text = "Resolution n" });
                    var sN = new HSlider { MinValue = 10, MaxValue = 28, Step = 2, Value = _largeN, CustomMinimumSize = new Vector2(220, 0) };
                    sN.ValueChanged += (double v) => { _largeN = (int)v; };
                    hbN.AddChild(sN);
                    _scenarioControls.AddChild(hbN);
                    // Instances X
                    var hbIX = new HBoxContainer();
                    hbIX.AddChild(new Label { Text = "Instances X" });
                    var sIX = new HSlider { MinValue = 1, MaxValue = 5, Step = 1, Value = _largeInstX, CustomMinimumSize = new Vector2(220, 0) };
                    sIX.ValueChanged += (double v) => { _largeInstX = (int)v; UpdateLargeInstInfo(); };
                    hbIX.AddChild(sIX);
                    _scenarioControls.AddChild(hbIX);
                    // Instances Y
                    var hbIY = new HBoxContainer();
                    hbIY.AddChild(new Label { Text = "Instances Y" });
                    var sIY = new HSlider { MinValue = 1, MaxValue = 5, Step = 1, Value = _largeInstY, CustomMinimumSize = new Vector2(220, 0) };
                    sIY.ValueChanged += (double v) => { _largeInstY = (int)v; UpdateLargeInstInfo(); };
                    hbIY.AddChild(sIY);
                    _scenarioControls.AddChild(hbIY);
                    // Instances info
                    _largeInstInfo = new Label();
                    _scenarioControls.AddChild(_largeInstInfo);
                    UpdateLargeInstInfo();
                    // Apply button to rebuild
                    var apply = new Button { Text = "Apply Size" };
                    apply.Pressed += () => { SetupScenario(Scenario.Large); };
                    _scenarioControls.AddChild(apply);
                    break;
                }
            default:
                _scenarioControls.AddChild(new Label { Text = "No scenario-specific controls" });
                break;
        }
    }

    private void HideAllColliderVisuals()
    {
        foreach (var v in _largeColliderVis)
        {
            if (v != null)
            {
                v.Visible = false;
                v.QueueFree();
            }
        }
        _largeColliderVis.Clear();
    }

    private static string GetScenarioDescription(Scenario s)
    {
        return s switch
        {
            Scenario.Minimal => "Square cloth pinned on one edge; ground plane.",
            Scenario.Tube => "Cylindrical cloth pinned at the top ring.",
            Scenario.Collision => "Square cloth with ground plane and a collider (Sphere/Capsule).",
            Scenario.Large => "Multiple cloth instances with per-instance sphere colliders.",
            _ => ""
        };
    }

    private (int iter, float stretch, float bend) GetScenarioDefaults(Scenario s)
    {
        return s switch
        {
            Scenario.Minimal => (8, 0.9f, 0.1f),
            Scenario.Tube => (10, 0.9f, 0.45f),
            Scenario.Collision => (10, 0.9f, 0.5f),
            Scenario.Large => (10, 0.85f, 0.45f),
            _ => (10, 0.9f, 0.5f)
        };
    }

    private void UpdateGlobalControlsFromParams()
    {
        _updatingUI = true;
        _ui.SetParameterValues(_parms.Iterations, _parms.StretchStiffness, _parms.BendStiffness);
        _updatingUI = false;
    }

    public void SelectScenario(int index) => SetupScenario((Scenario)index);

    public void SetIterations(int iter)
    {
        if (_updatingUI) return;
        _parms.Iterations = iter;
        _solver.UpdateParameters(_parms);
    }

    public void SetStretch(float stretch)
    {
        if (_updatingUI) return;
        _parms.StretchStiffness = stretch;
        _solver.UpdateParameters(_parms);
    }

    public void SetBend(float bend)
    {
        if (_updatingUI) return;
        _parms.BendStiffness = bend;
        _solver.UpdateParameters(_parms);
    }

    private void UpdateLargeInstInfo()
    {
        if (_largeInstInfo == null) return;
        int total = _largeInstX * _largeInstY;
        _largeInstInfo.Text = $"Instances: {_largeInstX} × {_largeInstY} = {total}";
    }

    private void AutoPinEdge()
    {
        var min = new Vec3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vec3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < _positions.Length; i++)
        {
            var p = _positions[i];
            if (p.X < min.X) min.X = p.X; if (p.Y < min.Y) min.Y = p.Y; if (p.Z < min.Z) min.Z = p.Z;
            if (p.X > max.X) max.X = p.X; if (p.Y > max.Y) max.Y = p.Y; if (p.Z > max.Z) max.Z = p.Z;
        }
        var pins = new System.Collections.Generic.List<int>();
        float eps = 1e-3f;
        if (MathF.Abs(max.Y - min.Y) < 1e-4f)
        {
            for (int i = 0; i < _positions.Length; i++)
                if (max.Z - _positions[i].Z <= eps) pins.Add(i);
        }
        else
        {
            for (int i = 0; i < _positions.Length; i++)
                if (max.Y - _positions[i].Y <= eps) pins.Add(i);
        }
        PinVertices(pins);
    }

    private void SetupScenario(Scenario s)
    {
        _scenario = s;
        _pinned.Clear();
        HideAllColliderVisuals();
        foreach (var node in _scenarios.GetChildren())
            if (node is Node3D n) n.Visible = false;
        var scenNode = _scenarios.GetNode<Node3D>(s.ToString());
        scenNode.Visible = true;
        var clothDef = scenNode.GetNode<ClothDefinition>("Cloth");
        _meshInst = clothDef;
        var srcMesh = clothDef.Mesh;
        var srcArrays = srcMesh.SurfaceGetArrays(0);
        _mesh = new ArrayMesh();
        _mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, srcArrays);
        _meshInst.Mesh = _mesh;
        _meshInst.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.85f, 0.9f, 1.0f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
            Roughness = 0.65f,
            Metallic = 0.0f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };

        var gverts = (Array<Vector3>)srcArrays[(int)Mesh.ArrayType.Vertex];
        var gidx = (Array<int>)srcArrays[(int)Mesh.ArrayType.Index];
        _positions = new Vec3[gverts.Count];
        for (int i = 0; i < gverts.Count; i++)
        {
            var v = gverts[i];
            _positions[i] = new Vec3(v.X, v.Y, v.Z);
        }
        _triangles = gidx.ToArray();
        _velocities = new Vec3[_positions.Length];

        _solver = new PbdSolver();
        var def = GetScenarioDefaults(s);
        _parms.Iterations = def.iter; _parms.StretchStiffness = def.stretch; _parms.BendStiffness = def.bend;
        _solver.Initialize(_positions, _triangles, _parms);
        AutoPinEdge();
        _solver.SetColliders(new DotCloth.Simulation.Collision.ICollider[] { new DotCloth.Simulation.Collision.PlaneCollider(new Vec3(0, 1, 0), -0.8f) });

        if (s == Scenario.Collision)
        {
            _collider = scenNode.GetNode<ColliderDefinition>("Collider");
            _colliderBasePos = _collider.GlobalPosition;
            ApplyCollisionSetup();
        }

        UpdateMesh();
        AutoFrame();
        BuildScenarioControls();
        _ui.SetScenarioDescription(GetScenarioDescription(_scenario));
        UpdateGlobalControlsFromParams();
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


    private void ApplyCollisionSetup()
    {
        if (_scenario != Scenario.Collision || _collider == null) return;

        var colliders = new System.Collections.Generic.List<DotCloth.Simulation.Collision.ICollider>();
        colliders.Add(new DotCloth.Simulation.Collision.PlaneCollider(new Vec3(0, 1, 0), -0.8f));
        var pos = _collider.GlobalPosition;
        if (_collider.Shape == ColliderDefinition.ShapeKind.Sphere)
        {
            colliders.Add(new DotCloth.Simulation.Collision.SphereCollider(new Vec3(pos.X, pos.Y, pos.Z), _collider.Radius));
            _collider.Mesh = new SphereMesh { Radius = _collider.Radius, Height = _collider.Radius * 2f, RadialSegments = 32, Rings = 16 };
        }
        else
        {
            float h = _collider.Height;
            float h0 = pos.Y + h * 0.5f;
            float h1 = pos.Y - h * 0.5f;
            colliders.Add(new DotCloth.Simulation.Collision.CapsuleCollider(new Vec3(pos.X, h0, pos.Z), new Vec3(pos.X, h1, pos.Z), _collider.Radius));
            _collider.Mesh = new CapsuleMesh { Radius = _collider.Radius, Height = MathF.Max(0.001f, h), RadialSegments = 32, Rings = 16 };
        }
        _solver.SetColliders(colliders.ToArray());
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
                ClearAllPins();
            }
            if (k.Keycode == Key.Key1) SetupScenario(Scenario.Minimal);
            if (k.Keycode == Key.Key2) SetupScenario(Scenario.Tube);
            if (k.Keycode == Key.Key3) SetupScenario(Scenario.Collision);
            if (k.Keycode == Key.Key4) SetupScenario(Scenario.Large);
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
                if (bestIdx >= 0 && bestD <= PickRadius)
                {
                    PinVertex(bestIdx);
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
                    var d = DistancePointToRay(new Vector3(p.X, p.Y, p.Z), rayFrom, rayDir);
                    if (d < bestD) { bestD = d; bestIdx = i; }
                }
                if (bestIdx >= 0) { UnpinVertex(bestIdx); }
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

    // Pin/unpin helpers to keep local state and solver synchronized
    private void PinVertex(int index)
    {
        if (index < 0 || index >= _positions.Length) return;
        if (_pinned.Add(index)) _solver.PinVertices(index);
    }

    private void UnpinVertex(int index)
    {
        if (index < 0 || index >= _positions.Length) return;
        if (_pinned.Remove(index)) _solver.UnpinVertices(index);
    }

    private void PinVertices(System.Collections.Generic.IEnumerable<int> indices)
    {
        var buf = new System.Collections.Generic.List<int>();
        foreach (var i in indices)
        {
            if (i < 0 || i >= _positions.Length) continue;
            if (_pinned.Add(i)) buf.Add(i);
        }
        if (buf.Count > 0) _solver.PinVertices(buf.ToArray());
    }

    private void UnpinVertices(System.Collections.Generic.IEnumerable<int> indices)
    {
        var buf = new System.Collections.Generic.List<int>();
        foreach (var i in indices)
        {
            if (i < 0 || i >= _positions.Length) continue;
            if (_pinned.Remove(i)) buf.Add(i);
        }
        if (buf.Count > 0) _solver.UnpinVertices(buf.ToArray());
    }

    private void ClearAllPins()
    {
        _solver.ClearPins();
        _pinned.Clear();
    }
    private void AutoFrame()
    {
        if (_positions.Length == 0) return;
        var min = new Vec3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vec3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < _positions.Length; i++)
        {
            var p = _positions[i];
            if (p.X < min.X) min.X = p.X; if (p.Y < min.Y) min.Y = p.Y; if (p.Z < min.Z) min.Z = p.Z;
            if (p.X > max.X) max.X = p.X; if (p.Y > max.Y) max.Y = p.Y; if (p.Z > max.Z) max.Z = p.Z;
        }
        var center = (min + max) * 0.5f;
        var ext = (max - min) * 0.5f;
        float radius = MathF.Max(0.001f, MathF.Max(ext.X, MathF.Max(ext.Y, ext.Z)));

        _target = new Vector3(center.X, center.Y, center.Z);
        _cam.Fov = 65f;
        _cam.Near = 0.01f; _cam.Far = 100f;
        float fovRad = MathF.PI * _cam.Fov / 180f;
        _dist = radius / MathF.Tan(fovRad * 0.5f) * 1.4f;
        UpdateCamera();
        _sun.RotationDegrees = new Vector3(45, 145, 0);
    }
    private void UpdateCamera()
    {
        var eye = Orbit(_yaw, _pitch, _dist) + _target;
        _cam.Position = eye;
        _cam.LookAt(_target, Vector3.Up);
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
