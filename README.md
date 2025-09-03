Note
- This repository’s code and documentation annotations are 100% produced with Codex (OpenAI) assistance and maintained under human review.

DotCloth
========

High‑performance, UnityCloth‑compatible eXtended PBD (XPBD) cloth simulation library targeting .NET 9.0. DotCloth aims to mirror UnityCloth’s parameter model while adopting recent PBD/XPBD research for performance, robustness, and ease of integration.

Goals
- UnityCloth‑compatible parameter structure and behavior alignment where practical.
- Cross‑platform .NET 9.0 library with strong static safety (nullable enabled, warnings as errors).
- Thread‑safe design suitable for integration into host apps/engines.
- Robust unit tests and CI for format/lint/typecheck/test.
- Documentation‑first: design and API docs under `docs/`.

Getting Started
- Build: `dotnet build`
- Test: `dotnet test` (xUnit)

Quick Example
```
var positions = new[] { new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(0,-1,0), new Vector3(1,-1,0) };
var triangles = new[] { 0,1,2, 2,1,3 };
var velocities = new Vector3[positions.Length];
var p = new ClothParameters { UseGravity = true, StretchStiffness = 0.9f, BendStiffness = 0.5f, Iterations = 10 };
var solver = new PbdSolver();
solver.Initialize(positions, triangles, p);
solver.PinVertices(0); // anchor one corner
solver.SetColliders(new [] { new PlaneCollider(new Vector3(0,1,0), 0f) });
solver.Step(0.016f, positions, velocities);
```

Repository Structure
- `docs/` — Design notes, algorithms, API, glossary.
- `src/` — Library code (`DotCloth`).
- `tests/` — Unit tests (`DotCloth.Tests`).
- `AGENTS.md` — Project agent rules (extends team defaults).

Documentation
- Auto-generated docs (DocFX) + guides live under `docs/docfx`. CI builds and can publish to GitHub Pages.
- Legal notes are available under `docs/legal/LEGAL_NOTES.md`.

Contributing
- Follow the rules in `AGENTS.md`.
- Keep changes cohesive, documented, and covered by tests.

License
- Apache License 2.0. See `LICENSE` and `NOTICE`.

Legal
- Terminology such as “Position‑Based Dynamics (PBD)” and “Extended Position‑Based Dynamics (XPBD)” is used descriptively with references to public literature only; no third‑party code is included.
- See `docs/legal/LEGAL_NOTES.md` for details and risk considerations.
