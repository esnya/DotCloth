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

Repository Structure
- `docs/` — Design notes, algorithms, API, glossary.
- `src/` — Library code (`DotCloth`).
- `tests/` — Unit tests (`DotCloth.Tests`).
- `AGENTS.md` — Project agent rules (extends team defaults).

Contributing
- Follow the rules in `AGENTS.md`.
- Keep changes cohesive, documented, and covered by tests.

License
- TBD by repository owner.

