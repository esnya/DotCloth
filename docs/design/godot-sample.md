Godot Sample – Mini Design
==========================

Purpose
- Provide a cross-platform, editor-friendly sample showcasing DotCloth integration in Godot 4 .NET.
- Encapsulate simulation logic in a reusable `ClothNode` and expose runtime controls via a small UI panel.

Scope & Boundaries
- In: minimal Godot project that steps `ForceCloth` through a dedicated node and allows runtime switching between force models and scenarios through a UI.
- Out: mesh deformation, editor tooling, picking/drag, platform-specific code, CI build of Godot project.

Public Surface
- Folder: `examples/DotCloth.GodotSample` (not added to solution; opt-in run from Godot).
- Main scene: `main.tscn` with root `Main.cs` (`Node3D`) wiring up cloth and UI.
- Child node: `ClothNode.cs` (`Node3D`) exposes `ScenarioIndex`, `ScenarioNames`, `Model`, and a metrics string while stepping `ForceCloth`.
- `ClothPanel.cs` (`Control`) hosts option lists to switch models and scenarios at runtime, and a performance label showing step time, FPS, and vertex count.
- Project file: `DotCloth.GodotSample.csproj` targeting `net8.0` with `Godot.NET.Sdk`.

Placement & Dependencies
- Depends on `src/DotCloth` via project reference.
- Reuses `ClothFactory`, scenarios, and `ForceModel` enum from the MonoGame sample via file links for consistency.

Test Strategy
- No CI build for Godot sample; manual run via editor. Core remains covered by unit tests.

Migration/Compatibility
- No breaking API changes. Library already multi-targets `net9.0;net8.0` for Godot.
