Examples
========

- MonoGame sample (DesktopGL):
  - Build: `dotnet build examples/DotCloth.MonoGameSample -c Release`
  - Run locally (GUI required): `dotnet run --project examples/DotCloth.MonoGameSample -c Release`
  - Notes: Requires MonoGame DesktopGL runtime dependencies (OpenGL/SDL2). CI only builds; it does not launch a window.

 Controls
 - `1`: Minimal — single grid cloth with floor; top row pinned.
 - `2`: Cylinder — closed tube (seamed) cloth; top ring pinned; floor.
 - `3`: Colliders — grid cloth with moving sphere/capsule colliders; two-point pin; floor.
 - `4`: Large — many cloth instances (grid), each with a moving sphere collider centered under the pinned row; floor.
 - `5`: X Large — larger/more instances; moving colliders; floor.
 - `R`: Reset current scenario.
 - Mouse: RMB drag to orbit; wheel to zoom.

 HUD
 - Window title shows: `FPS` | `Solver` (ms per frame spent in DotCloth) | `App` (ms in sample) | `Total` (ms) | `Verts` (total vertices across all cloths).

 Implementation Notes
 - Y-up world, cloth heights initialized around y≈1.5 (Minimal baseline) unless scenarios adjust.
 - Collider visualization renders wireframes (spheres/capsules) and a static floor grid (plane).
 - Fixed-step simulation (60 Hz) with accumulator for stable integration.
