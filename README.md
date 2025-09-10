# DotCloth

## Note
- This repository’s code and documentation annotations are 100% produced with Codex (OpenAI) assistance and maintained under human review.

High‑performance, UnityCloth‑compatible cloth simulation library targeting .NET 9.0. DotCloth offers patent‑neutral force modules (edge springs, discrete shells, co‑rotational FEM, strain limiting) with swappable Euler integrators.

⚠️ Performance is under active improvement; targeting roughly 5× higher throughput.

## Screenshot
- ![MonoGame sample screenshot](docs/images/sample-monogame.png)
- ![Godot sample screenshot](docs/images/sample-godot.png)
- ⚠️ These captures come from an experimental build with higher throughput; the mainline solver is currently slower.

## Goals
- UnityCloth‑compatible parameter structure and behavior alignment where practical.
- Cross‑platform .NET 9.0 library with strong static safety (nullable enabled, warnings as errors).
- Thread‑safe design suitable for integration into host apps/engines.
- Robust unit tests and CI for format/lint/typecheck/test.
- Documentation‑first: design and API docs under `docs/`.

## Getting Started
- Build: `dotnet build`
- Test: `dotnet test` (xUnit)

## Quick Example
```csharp
var positions = new[]
{
    new Vector3(0f, 2f, 0f),
    new Vector3(0f, 1f, 0f)
};
var invMass = new[] { 0f, 1f };
var springs = new[] { new EdgeSpringForce.Spring(0, 1, 1f, 100f) };
var forces = new IForce[] { new EdgeSpringForce(springs) };
var cloth = new ForceCloth(positions, invMass, forces, new Vector3(0f, -9.81f, 0f), 0.98f);
cloth.Step(0.016f);
```

## Repository Structure
- `docs/` — Design notes, algorithms, API, glossary.
- `src/` — Library code (`DotCloth`).
- `tests/` — Unit tests (`DotCloth.Tests`).
- `examples/` — Sample applications.
- `AGENTS.md` — Project agent rules (extends team defaults).

## Documentation
- Auto-generated docs (DocFX) + guides live under `docs/docfx`. CI builds and can publish to GitHub Pages.
- Legal notes are available under `docs/legal/LEGAL_NOTES.md`.

## Contributing
- Follow the rules in `AGENTS.md`.
- Keep changes cohesive, documented, and covered by tests.

## Console Sample
- Run: `dotnet run --project examples/DotCloth.ConsoleSample`
- Select integrator and model: `dotnet run --project examples/DotCloth.ConsoleSample -- explicit fem`

## License
- Apache License 2.0. See `LICENSE` and `NOTICE`.

## Legal
- Terminology such as “Position‑Based Dynamics (PBD)” and “Extended Position‑Based Dynamics (XPBD)” is used descriptively with references to public literature only; no third‑party code is included.
- “Unity” and “UnityCloth” are associated with products of Unity Technologies. Any mention herein is purely descriptive (e.g., conceptual alignment) and does not imply affiliation, sponsorship, or endorsement.
- See `docs/legal/LEGAL_NOTES.md` for details and risk considerations.
- Default algorithm performance (patent‑risk‑avoidance path): see `docs/legal/LEGAL_NOTES.md#default-algorithm-performance-measured`.
