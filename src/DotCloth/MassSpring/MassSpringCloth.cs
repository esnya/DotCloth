using System.Numerics;
using System.Threading.Tasks;

namespace DotCloth.MassSpring;

/// <summary>
/// Patent neutral cloth simulator based on mass–spring forces with pluggable Euler integration.
/// Avoids XPBD/PBD style constraint projection.
/// </summary>
public sealed class MassSpringCloth
{
    /// <summary>Hooke spring connecting two particles.</summary>
    public readonly struct Spring
    {
        /// <summary>Initializes a spring between particle indices.</summary>
        /// <param name="a">Index of the first particle.</param>
        /// <param name="b">Index of the second particle.</param>
        /// <param name="restLength">Rest length of the spring.</param>
        /// <param name="stiffness">Hooke stiffness coefficient.</param>
        public Spring(int a, int b, float restLength, float stiffness)
        {
            A = a;
            B = b;
            RestLength = restLength;
            Stiffness = stiffness;
        }

        /// <summary>Index of the first particle.</summary>
        public int A { get; }

        /// <summary>Index of the second particle.</summary>
        public int B { get; }

        /// <summary>Rest length.</summary>
        public float RestLength { get; }

        /// <summary>Hooke stiffness.</summary>
        public float Stiffness { get; }
    }

    private readonly Spring[] _springs;
    private readonly float[] _invMass;
    private readonly Vector3 _gravity;
    private readonly float _damping;
    private readonly Vector3[] _velocities;
    private readonly IIntegrator _integrator;

    /// <summary>Creates a cloth with the provided particle data.</summary>
    public MassSpringCloth(Vector3[] initialPositions, float[] invMass, Spring[] springs, Vector3 gravity, float damping, IIntegrator? integrator = null)
    {
        Positions = (Vector3[])initialPositions.Clone();
        _velocities = new Vector3[initialPositions.Length];
        _invMass = (float[])invMass.Clone();
        _springs = (Spring[])springs.Clone();
        _gravity = gravity;
        _damping = damping;
        _integrator = integrator ?? SemiImplicitEulerIntegrator.Instance;
    }

    /// <summary>Current particle positions.</summary>
    public Vector3[] Positions { get; }

    /// <summary>Pins the particle at <paramref name="index"/> to <paramref name="position"/>.</summary>
    public void Pin(int index, Vector3 position)
    {
        _invMass[index] = 0f;
        _velocities[index] = Vector3.Zero;
        Positions[index] = position;
    }

    /// <summary>Restores movement of the particle at <paramref name="index"/>.</summary>
    /// <param name="index">Particle to unpin.</param>
    /// <param name="invMass">Inverse mass to apply after unpinning.</param>
    public void Unpin(int index, float invMass)
    {
        _invMass[index] = invMass;
    }

    /// <summary>
    /// Advances the simulation by <paramref name="dt"/> seconds.
    /// </summary>
    public void Step(float dt)
    {
        var forces = new Vector3[Positions.Length];

        // Edge spring forces (sequential for determinism; parallelization can be added later)
        foreach (var s in _springs)
        {
            var a = Positions[s.A];
            var b = Positions[s.B];
            var delta = b - a;
            var dist = delta.Length();
            if (dist < 1e-6f)
            {
                continue;
            }

            var dir = delta / dist;
            var force = s.Stiffness * (dist - s.RestLength) * dir;
            forces[s.A] += force;
            forces[s.B] -= force;
        }

        Parallel.For(0, Positions.Length, i =>
        {
            if (_invMass[i] == 0f)
            {
                _velocities[i] = Vector3.Zero;
                return;
            }

            var accel = forces[i] * _invMass[i] + _gravity;
            _integrator.Integrate(ref Positions[i], ref _velocities[i], accel, _damping, dt);

            // Floor plane collision at y=0
            if (Positions[i].Y < 0f)
            {
                Positions[i].Y = 0f;
                if (_velocities[i].Y < 0f)
                {
                    _velocities[i].Y = 0f;
                }
            }
        });
    }
}
