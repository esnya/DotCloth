using System.Numerics;

namespace DotCloth.Simulation.Parameters;

/// <summary>
/// UnityCloth-like parameter set for cloth simulation.
/// Values are validated by the simulator; ranges are documented here for guidance.
/// </summary>
public sealed class ClothParameters
{
    public bool UseGravity { get; set; } = true;

    /// <summary>Gravity scale multiplier when <see cref="UseGravity"/> is true. Typical: 1.0.</summary>
    public float GravityScale { get; set; } = 1.0f;

    /// <summary>Uniform damping in [0, 1). 0 = no damping.</summary>
    public float Damping { get; set; } = 0.0f;

    /// <summary>Air drag (approximate), usually small (e.g., 0..0.1).</summary>
    public float AirDrag { get; set; } = 0.0f;

    /// <summary>Stretch stiffness [0, 1]. Interpreted as XPBD compliance internally.</summary>
    public float StretchStiffness { get; set; } = 1.0f;

    /// <summary>Bend stiffness [0, 1]. Interpreted as XPBD compliance internally.</summary>
    public float BendStiffness { get; set; } = 0.5f;

    /// <summary>Tether stiffness [0, 1].</summary>
    public float TetherStiffness { get; set; } = 0.0f;

    /// <summary>
    /// Tether length scale relative to initial distance to anchor/rest.
    /// 1.0 keeps full length; smaller pulls cloth tighter.
    /// </summary>
    public float TetherLengthScale { get; set; } = 1.0f;

    /// <summary>Collision thickness (shell) in world units.</summary>
    public float CollisionThickness { get; set; } = 0.005f;

    /// <summary>Kinetic friction coefficient [0, 1].</summary>
    public float Friction { get; set; } = 0.0f;

    /// <summary>Uniform mass per vertex (kg). Inverse mass derived internally.</summary>
    public float VertexMass { get; set; } = 0.02f;

    /// <summary>Constant external acceleration (e.g., wind in m/s^2).</summary>
    public Vector3 ExternalAcceleration { get; set; } = Vector3.Zero;

    /// <summary>Random acceleration magnitude for jitter (diagnostics/off by default).</summary>
    public float RandomAcceleration { get; set; } = 0.0f;

    /// <summary>Seed for deterministic random acceleration.</summary>
    public int RandomSeed { get; set; } = 0;

    // Solver settings
    /// <summary>Constraint iterations per substep.</summary>
    public int Iterations { get; set; } = 8;

    /// <summary>Number of substeps per Step().</summary>
    public int Substeps { get; set; } = 1;

    /// <summary>
    /// Global scale to derive XPBD compliance from [0..1] stiffness.
    /// Smaller values increase effective rigidity. Typical: 1e-6 .. 1e-4.
    /// </summary>
    public float ComplianceScale { get; set; } = 1e-6f;
}
