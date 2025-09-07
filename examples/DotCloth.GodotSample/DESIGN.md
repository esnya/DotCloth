# Godot Sample Scene Segmentation

## Purpose
Move cloth, collider, and collider motion definitions into the Godot scene so they can be configured visually and switched on/off at runtime. Parameter tweaks flow through dedicated UI scripts rather than being wired directly in `Main`.

## Boundaries
- Only files inside `examples/DotCloth.GodotSample` change.
- `Main` orchestrates simulation and consumes definitions.
- `ClothDefinition` marks mesh nodes providing initial geometry.
- `ColliderDefinition` and `ColliderMover` mark collider nodes and optionally animate their transforms.
- `SampleUi` mediates user control and forwards events to `Main`.

## Model/API
- `ClothDefinition : MeshInstance3D` exposes cloth mesh and transform. A 1‑unit template mesh is placed in the scene for the “Large” scenario; runtime vertex count adjustment is removed.
- `ColliderDefinition : MeshInstance3D` exports `Shape` (`Sphere` or `Capsule`) and dimensions.
- `ColliderMover : ColliderDefinition` adds exported motion parameters (`MotionAmplitude`, `MotionFrequency`, `MotionPhase`) and updates its own `GlobalPosition` each physics tick.
- `SampleUi : PanelContainer` connects sliders and option button to `Main` via exported node reference.

## Placement/Dependencies
- Scene nodes reference these scripts via `script` property.
- `Main` locates definitions with `GetNode<ClothDefinition>` and gathers `ColliderDefinition`/`ColliderMover` nodes each frame to update collision objects.

## Testing
Run standard `dotnet` format/build/test matrix.

## Migration
No external consumers; sample scene only. Rollback by reverting this directory.
