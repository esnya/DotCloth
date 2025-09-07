# Godot Sample Scene Segmentation

## Purpose
Move cloth and collider definitions into the Godot scene so they can be configured visually and switched on/off at runtime. Parameter tweaks should flow through dedicated UI scripts rather than being wired directly in `Main`.

## Boundaries
- Only files inside `examples/DotCloth.GodotSample` change.
- `Main` orchestrates simulation and consumes definitions.
- `ClothDefinition` and `ColliderDefinition` mark scene nodes providing initial geometry and collider data.
- `SampleUi` mediates user control and forwards events to `Main`.

## Model/API
- `ClothDefinition : MeshInstance3D` exposes cloth mesh and transform.
- `ColliderDefinition : Node3D` exports `Shape` (`Sphere` or `Capsule`) and dimensions.
- `SampleUi : PanelContainer` connects sliders and option button to `Main` via exported node reference.

## Placement/Dependencies
- Scene nodes reference these scripts via `script` property.
- `Main` locates definitions with `GetNode<ClothDefinition>` and `GetNode<ColliderDefinition>`.

## Testing
Run standard `dotnet` format/build/test matrix.

## Migration
No external consumers; sample scene only. Rollback by reverting this directory.
