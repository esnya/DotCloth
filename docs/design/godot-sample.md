Godot Sample – Mini Design
==========================

Purpose
- Provide a cross-platform, editor-friendly sample showcasing DotCloth integration in Godot 4 .NET.

Scope & Boundaries
- In: minimal Godot project that steps `ForceCloth` and allows runtime switching between force models.
- Out: mesh deformation, editor tooling, UI, picking/drag, platform-specific code, CI build of Godot project.

Public Surface
- Folder: `examples/DotCloth.GodotSample` (not added to solution; opt-in run from Godot).
- Main scene: `main.tscn` with `Main.cs` (`Node3D`). Model switches via number keys (1-4).
- Project file: `DotCloth.GodotSample.csproj` targeting `net8.0` with `Godot.NET.Sdk`.

Placement & Dependencies
- Depends on `src/DotCloth` via project reference.
- Reuses `ClothFactory` from MonoGame sample via file link for consistency.

Test Strategy
- No CI build for Godot sample; manual run via editor. Core remains covered by unit tests.

Migration/Compatibility
- No breaking API changes. Library already multi-targets `net9.0;net8.0` for Godot.
