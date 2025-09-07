# Godot Sample Scene Segmentation

## Purpose
Move cloth, collider, and collider motion definitions into the Godot scene so they can be configured visually and switched on/off at runtime. Parameter tweaks flow through dedicated UI scripts rather than being wired directly in `Main`.

## Boundaries
- Only files inside `examples/DotCloth.GodotSample` change.
- `Main` orchestrates simulation and consumes definitions.
- `ClothDefinition` marks mesh nodes providing initial geometry.
- `ColliderDefinition` and `ColliderMover` mark collider nodes and optionally animate their transforms.
- `TubeMesh` procedurally builds a seamless open cylinder used by the Tube scenario.
- `SampleUi` mediates user control and forwards events to `Main`.

## Model/API
 - `ClothDefinition : MeshInstance3D` exposes cloth mesh and transform and retains the scene’s original mesh so scenarios can be toggled without losing geometry. A 1‑unit template mesh is placed in the scene for the “Large” scenario; runtime vertex count adjustment is removed.
- `ColliderDefinition : MeshInstance3D` exports `Shape` (`Sphere` or `Capsule`) and dimensions. Collider nodes are positioned so their local origin matches pinned vertices.
- `ColliderMover : ColliderDefinition` adds exported motion parameters (`MotionAmplitude`, `MotionFrequency`, `MotionPhase`) and updates its own `GlobalPosition` each physics tick.
- `TubeMesh : PrimitiveMesh` generates a cap-less tube with welded seam so the cloth can wrap fully around.
- `SampleUi : PanelContainer` connects sliders and option button to `Main` via exported node reference and exposes Large scenario instance count.

## Placement/Dependencies
- Scene nodes reference these scripts via `script` property.
- `Main` locates definitions with `GetNode<ClothDefinition>` and gathers `ColliderDefinition`/`ColliderMover` nodes each frame to update collision objects. For the Large scenario it instantiates a grid of cloth copies and synthesizes per-instance moving sphere colliders.

## Testing
Run standard `dotnet` format/build/test matrix.

## Migration
No external consumers; sample scene only. Rollback by reverting this directory.
