# Examples

## Console Sample
Run the sample with the default semi-implicit integrator:

```
dotnet run --project examples/DotCloth.ConsoleSample
```

Use explicit Euler instead:

```
dotnet run --project examples/DotCloth.ConsoleSample -- explicit
```

## MonoGame Sample
Launch the desktop sample:

```
dotnet run --project examples/DotCloth.MonoGameSample
```

Press `S` to cycle scenarios (including moving colliders) and `M` to cycle force models.
A ground plane collider keeps the cloth from falling below Y=0.

## Godot Sample
Open `examples/DotCloth.GodotSample` in Godot 4 and run the scene.
Use the on-screen panel to switch force models and toggle scenario nodes.
Plane, sphere, and capsule colliders are Godot nodes; a ground plane keeps the cloth above Y=0.
