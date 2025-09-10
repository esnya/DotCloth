using System;
using System.Numerics;
using DotCloth;
using DotCloth.Collisions;
using System.Collections.Generic;

namespace DotCloth.MonoGameSample.Scenarios;

public sealed class CollidersScenario : IColliderScenario
{
    public string Name => "Colliders";
    public int GridSize => 24;

    private readonly MovingSphereCollider _sphere = new(new Vector3(0f, 2f, 0f), 1f);
    private readonly MovingCapsuleCollider _capsule = new(new Vector3(-3f, 1f, -1f), new Vector3(3f, 1f, 1f), 0.75f);
    private float _time;

    public ForceCloth Create(ForceModel model)
    {
        _time = 0f;
        var extras = new ICollider[] { new PlaneCollider(Vector3.Zero, Vector3.UnitY), _sphere, _capsule };
        return ClothFactory.Create(GridSize, model, extras);
    }

    public void Update(float dt)
    {
        _time += dt;
        _sphere.Center = new Vector3(MathF.Sin(_time) * 2f, 2f + 0.5f * MathF.Cos(_time * 0.7f), 0f);
        var sweep = 0.5f * MathF.Sin(_time * 0.5f);
        _capsule.P0 = new Vector3(-3f + sweep, 1f, -1f);
        _capsule.P1 = new Vector3(3f + sweep, 1f, 1f);
    }

    public void CollectColliderVisuals(List<ColliderViz> dst)
    {
        dst.Add(new ColliderViz { Kind = ColliderKind.Sphere, Center = _sphere.Center, Radius = _sphere.Radius });
        dst.Add(new ColliderViz { Kind = ColliderKind.Capsule, P0 = _capsule.P0, P1 = _capsule.P1, Radius = _capsule.Radius });
    }

    private sealed class MovingSphereCollider : ICollider
    {
        public Vector3 Center;
        public float Radius;
        public MovingSphereCollider(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public void Resolve(ref Vector3 position, ref Vector3 velocity)
        {
            var diff = position - Center;
            var distSq = diff.LengthSquared();
            var rSq = Radius * Radius;
            if (distSq >= rSq)
            {
                return;
            }

            var dist = MathF.Sqrt(distSq);
            var normal = dist > 0f ? diff / dist : Vector3.UnitY;
            position = Center + normal * Radius;
            var vn = Vector3.Dot(velocity, normal);
            if (vn < 0f)
            {
                velocity -= vn * normal;
            }
        }
    }

    private sealed class MovingCapsuleCollider : ICollider
    {
        public Vector3 P0;
        public Vector3 P1;
        public float Radius;
        public MovingCapsuleCollider(Vector3 p0, Vector3 p1, float radius)
        {
            P0 = p0;
            P1 = p1;
            Radius = radius;
        }

        public void Resolve(ref Vector3 position, ref Vector3 velocity)
        {
            var ab = P1 - P0;
            var ap = position - P0;
            float t = 0f;
            var abLenSq = ab.LengthSquared();
            if (abLenSq > 0f)
            {
                t = Vector3.Dot(ap, ab) / abLenSq;
                if (t < 0f) t = 0f;
                else if (t > 1f) t = 1f;
            }

            var closest = P0 + t * ab;
            var diff = position - closest;
            var distSq = diff.LengthSquared();
            var rSq = Radius * Radius;
            if (distSq >= rSq)
            {
                return;
            }

            var dist = MathF.Sqrt(distSq);
            Vector3 normal;
            if (dist > 0f)
            {
                normal = diff / dist;
            }
            else if (abLenSq > 0f)
            {
                normal = Vector3.Normalize(ab);
            }
            else
            {
                normal = Vector3.UnitY;
            }

            position = closest + normal * Radius;
            var vn = Vector3.Dot(velocity, normal);
            if (vn < 0f)
            {
                velocity -= vn * normal;
            }
        }
    }
}
