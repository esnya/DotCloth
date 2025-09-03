Examples (Optional)
===================

The solution excludes examples from the default build to keep cross-platform CI green. Build/run locally as needed.

- MonoGame sample (DesktopGL):
  - Build: `dotnet build examples/DotCloth.MonoGameSample -c Release`
  - Run (GUI required): `dotnet run --project examples/DotCloth.MonoGameSample -c Release`
  - Notes: Requires MonoGame DesktopGL runtime deps (OpenGL/SDL2). CI only builds; it does not launch a window.

- Silk.NET sample (Windows/macOS/Linux with GLFW):
  - `dotnet run --project examples/DotCloth.SilkSample -c Release`
  - Optional input defines (enabled in csproj): `SILK_INPUT`
  - Optional overlay (Windows only): `OVERLAY_TEXT` (uses System.Drawing)

- Godot sample (Godot 4 .NET, Windows/macOS/Linux):
  - Open `examples/DotCloth.GodotSample` in the Godot editor and press Play.
  - The core library multi-targets `net9.0;net8.0` so Godot can consume `net8.0`.
  - The Godot project is not part of the solution to avoid CI/tooling friction.

Controls (common where available)
- `1`: Minimal — single grid; top row pinned; floor.
- `2`: Cylinder — seamed tube; top ring pinned; floor.
- `3`: Colliders — grid with moving sphere/capsule; pin; floor.
- `4`: Large — multi-instance grid; moving colliders.
- `5`: X Large — larger/more instances.
- `R`: Reset scenario; Mouse: RMB orbit, wheel zoom.

HUD (MonoGame/Silk)
- Window title shows: `FPS` | `Solver` ms | `App` ms | `Total` ms | `Verts`.

Implementation Notes
- Y-up world; fixed-step simulation (60 Hz) with accumulator.
- Collider visualization uses simple meshes/wireframes.
