using System.Numerics;

namespace DotCloth.MassSpring;

/// <summary>Time integrator for particle positions and velocities.</summary>
public interface IIntegrator
{
    /// <summary>Integrates position and velocity over a time step.</summary>
    void Integrate(ref Vector3 position, ref Vector3 velocity, Vector3 acceleration, float damping, float dt);
}

/// <summary>Semi-implicit (symplectic) Euler integration.</summary>
public sealed class SemiImplicitEulerIntegrator : IIntegrator
{
    /// <summary>Singleton instance.</summary>
    public static readonly SemiImplicitEulerIntegrator Instance = new();
    private SemiImplicitEulerIntegrator() { }
    /// <inheritdoc />
    public void Integrate(ref Vector3 position, ref Vector3 velocity, Vector3 acceleration, float damping, float dt)
    {
        velocity = (velocity + acceleration * dt) * damping;
        position += velocity * dt;
    }
}

/// <summary>Explicit Euler integration.</summary>
public sealed class ExplicitEulerIntegrator : IIntegrator
{
    /// <summary>Singleton instance.</summary>
    public static readonly ExplicitEulerIntegrator Instance = new();
    private ExplicitEulerIntegrator() { }
    /// <inheritdoc />
    public void Integrate(ref Vector3 position, ref Vector3 velocity, Vector3 acceleration, float damping, float dt)
    {
        position += velocity * dt;
        velocity = (velocity + acceleration * dt) * damping;
    }
}
