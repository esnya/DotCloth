# Examples

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

## Controls
 - `1`: Minimal ? single grid cloth with floor; top row pinned.
 - `2`: Cylinder ? closed tube (seamed) cloth; top ring pinned; floor.
 - `3`: Colliders ? grid cloth with moving sphere/capsule colliders; two-point pin; floor.
 - `4`: Large ? many cloth instances (grid), each with a moving sphere collider centered under the pinned row; floor.
 - `5`: X Large ? larger/more instances; moving colliders; floor.
 - `R`: Reset current scenario.
 - Mouse: RMB drag to orbit; wheel to zoom.

## HUD
 - Window title shows: `FPS` | `Solver` (ms per frame spent in DotCloth) | `App` (ms in sample) | `Total` (ms) | `Verts` (total vertices across all cloths).

## Implementation Notes
 - Y-up world, cloth heights initialized around y?1.5 (Minimal baseline) unless scenarios adjust.
 - Collider visualization renders wireframes (spheres/capsules) and a static floor grid (plane).
 - Fixed-step simulation (60 Hz) with accumulator for stable integration.

