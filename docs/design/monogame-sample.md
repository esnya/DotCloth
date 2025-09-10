# MonoGame sample

## Purpose
Demonstrate ForceCloth with selectable scenarios and force models in an interactive desktop app.

## Scope and boundaries
- In: basic grid cloth rendered with MonoGame; scenarios switch grid size or animate colliders; force models toggle via a shared `ForceModel` enum and combine freely with any scenario; window title shows current selection and live performance metrics.
- Out: advanced rendering, mobile targets.

## Public API
None; sample uses internal helper factory and `IScenario.Update` to build and animate cloth instances.

## Test strategy
Manual: `dotnet run --project examples/DotCloth.MonoGameSample` and switch with `S` (scenario) and `M` (model).

## Migration
None.
