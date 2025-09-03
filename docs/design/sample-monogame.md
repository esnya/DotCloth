MonoGame Sample — Mini Design
=============================

Purpose
- Provide a MonoGame-based interactive sample that visualizes DotCloth in 3D and serves as a reference integration.

Scope and Boundaries
- In: minimal DesktopGL game loop, basic camera (orbit), floor grid, simple cloth mesh generator, stepping DotCloth each Update, line rendering of edges.
- Out (initial): content pipeline, textures/models, UI, advanced input, packaging.

Public API / Data Model (integration surface)
- Consumes `DotCloth` via `IClothSimulator` (`PbdSolver`), `ClothParameters`, and built-in colliders.
- Sample encapsulated in `examples/DotCloth.MonoGameSample` with program entry `Program` and `SampleGame : Game`.

Placement and Responsibilities
- `examples/DotCloth.MonoGameSample`: game host, camera, geometry helpers.
- No changes to core solver beyond multi-targeting to allow `net8.0` consumption where needed.

Dependencies
- Runtime: `MonoGame.Framework.DesktopGL` (well-known, cross-platform).
- Engine-agnostic math: `System.Numerics.Vector3` for interop with DotCloth.

Test Strategy
- CI builds sample (no GUI run). Core correctness remains covered by existing tests. Sample code avoids flaky timing.

Entry Point / Compatibility
- Sample entry point: `examples/DotCloth.MonoGameSample`.
- Library multi-targets `net9.0;net8.0` to simplify consumption from apps targeting .NET 8.
