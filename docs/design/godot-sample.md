Godot Sample – Mini Design
==========================

Purpose
- Provide a cross-platform, editor-friendly sample showcasing DotCloth integration in Godot 4 .NET.
- Drive all simulation setup through Godot scene nodes so designers can inspect and toggle scenarios in the editor.

Scope & Boundaries
- In: minimal Godot project that steps `ForceCloth` through a dedicated node and allows runtime switching between force models and scenarios through a UI.
- Out: mesh deformation, editor tooling, picking/drag, platform-specific code, CI build of Godot project.

Public Surface
- Folder: `examples/DotCloth.GodotSample` (not added to solution; opt-in run from Godot).
- Main scene: `main.tscn` with multiple `ClothNode` instances representing scenarios.
- `ClothNode.cs` (`Node3D`) exposes `GridSize`, `Model`, and a metrics string while stepping `ForceCloth` and updating a `MultiMeshInstance3D`.
- Colliders are `Node3D` scripts (`PlaneColliderNode`, `SphereColliderNode`, `CapsuleColliderNode`) that implement `ICollider` and optionally animate themselves.
- `ClothPanel.cs` (`Control`) enumerates sibling `ClothNode` scenarios, toggles them on/off, and switches force models at runtime.
- Project file: `DotCloth.GodotSample.csproj` targeting `net8.0` with `Godot.NET.Sdk`. It links the shared `ForceModel` enum and `ClothFactory`.

Placement & Dependencies
- Depends on `src/DotCloth` via project reference.
- Reuses `ClothFactory` and shared `ForceModel` enum from the MonoGame sample via file links for consistency.

Test Strategy
- No CI build for Godot sample; manual run via editor. Core remains covered by unit tests.

Migration/Compatibility
- No breaking API changes. Library already multi-targets `net9.0;net8.0` for Godot.
