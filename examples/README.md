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

Press `S` to cycle scenarios and `M` to cycle force models.
A ground plane collider keeps the cloth from falling below Y=0.

## Godot Sample
Open `examples/DotCloth.GodotSample` in Godot 4 and run the scene.
Press number keys to switch force models.
A ground plane collider keeps the cloth from falling below Y=0.
