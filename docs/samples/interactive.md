Interactive Sample Plan
=======================

Targets
- Provide a graphical sample that steps a cloth and renders it, with room for future interaction (mouse picking, wind, pin toggling).

Option A — Silk.NET (current sample)
- Pros: Lightweight, pure .NET; easy to script and run in CI; minimal assets; straightforward for coding agents.
- Cons: You implement your own input/UI/event loop; more boilerplate for camera, picking, gizmos.
- Status: `examples/DotCloth.SilkSample` shows a wireframe cloth updating each frame.

Option B — Godot .NET (added)
- Pros: Built-in scene graph, input, UI, editor tooling, quick interactive prototyping.
- Cons: Requires editor/assets; less friendly to headless CI; setup overhead for coding agents.
- Status: `examples/DotCloth.GodotSample` provides a minimal Godot 4 .NET project that steps the solver and renders a dynamic mesh. It is not in the solution to keep CI green.

Future Interaction Ideas
- Mouse picking to toggle pins on vertices, drag constraints, wind toggles.
- Parameter tweaking UI (iterations, stiffness, tethers) with live feedback.
- Performance overlay (ms/frame, iterations, substeps).

Parallelization Note (Future Plan)
- Internally, constraints are batched (no shared vertices per batch), enabling safe batch-parallel solving.
- Parallel stepping remains opt-in and will be guarded by a flag with deterministic ordering preserved; defaults remain single-threaded for simplicity.
