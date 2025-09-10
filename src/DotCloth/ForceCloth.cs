using System.Numerics;
using System.Threading.Tasks;
using DotCloth.Forces;
using DotCloth.Constraints;
using DotCloth.MassSpring; // reuse integrators

namespace DotCloth;

/// <summary>Force-based cloth simulator with pluggable algorithms.</summary>
public sealed class ForceCloth
{
    private readonly float[] _invMass;
    private readonly Vector3[] _velocities;
    private readonly IForce[] _forces;
    private readonly IConstraint[] _constraints;
    private readonly IIntegrator _integrator;
    private readonly Vector3 _gravity;
    private readonly float _damping;

    /// <summary>Initializes the cloth simulator.</summary>
    /// <param name="initialPositions">Starting particle positions.</param>
    /// <param name="invMass">Inverse masses per particle (0 for pinned).</param>
    /// <param name="forces">Force models to apply.</param>
    /// <param name="gravity">Constant gravity vector.</param>
    /// <param name="damping">Velocity damping factor.</param>
    /// <param name="constraints">Optional position constraints.</param>
    /// <param name="integrator">Time integrator, defaults to semi-implicit Euler.</param>
    public ForceCloth(Vector3[] initialPositions, float[] invMass, IForce[] forces, Vector3 gravity, float damping, IConstraint[]? constraints = null, IIntegrator? integrator = null)
    {
        Positions = (Vector3[])initialPositions.Clone();
        _velocities = new Vector3[initialPositions.Length];
        _invMass = (float[])invMass.Clone();
        _forces = (IForce[])forces.Clone();
        _gravity = gravity;
        _damping = damping;
        _constraints = constraints ?? Array.Empty<IConstraint>();
        _integrator = integrator ?? SemiImplicitEulerIntegrator.Instance;
    }

    /// <summary>Current particle positions.</summary>
    public Vector3[] Positions { get; }

    /// <summary>Advances the simulation by <paramref name="dt"/> seconds.</summary>
    public void Step(float dt)
    {
        var forces = new Vector3[Positions.Length];
        foreach (var f in _forces)
        {
            f.Accumulate(Positions, forces);
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

            if (Positions[i].Y < 0f)
            {
                Positions[i].Y = 0f;
                if (_velocities[i].Y < 0f)
                {
                    _velocities[i].Y = 0f;
                }
            }
        });

        foreach (var c in _constraints)
        {
            c.Project(Positions);
        }
    }
}
