# MonoGame sample

## Purpose
Demonstrate ForceCloth with selectable scenarios and force models in an interactive desktop app.

## Scope and boundaries
- In: basic grid cloth rendered with MonoGame; scenarios switch grid size; models toggle force combinations.
- Out: collision geometry, advanced rendering, mobile targets.

## Public API
None; sample uses internal helper factory to build cloth instances.

## Test strategy
Manual: `dotnet run --project examples/DotCloth.MonoGameSample` and switch with `S` (scenario) and `M` (model).

## Migration
None.
