Godot Sample – Mini Design
==========================

Purpose
- Provide a cross‑platform, editor‑friendly sample showcasing DotCloth integration in Godot 4 .NET.

Scope & Boundaries
- In: minimal Godot project that steps `PbdSolver` and updates a dynamic mesh (vertices + normals), pinning a row.
- Out: editor tooling, UI, picking/drag, platform‑specific code, CI build of Godot project.

Public Surface
- Folder: `examples/DotCloth.GodotSample` (not added to solution; opt‑in run from Godot).
- Main scene: `main.tscn` with `Main.cs` (`Node3D`) building the scene tree in code.
- Project file: `DotCloth.GodotSample.csproj` targeting `net8.0` with `Godot.NET.Sdk`.

Placement & Dependencies
- Depends on `src/DotCloth` via project reference.
- Library multi‑targets `net9.0;net8.0` to enable Godot consumption without changing default consumers.

Test Strategy
- No CI build for Godot sample; manual run via editor. Core remains covered by unit tests.

Migration/Compatibility
- No breaking API changes. Multi‑target added to `DotCloth` (net8.0) to support Godot.

