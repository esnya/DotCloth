# DotCloth Documentation

Welcome to DotCloth — a high‑performance cloth simulation library targeting .NET 9.0 and conceptually aligned with UnityCloth parameters. The default solver is a velocity‑level sequential‑impulses method. An XPBD variant exists for research and is experimental behind the `DOTCLOTH_EXPERIMENTAL_XPBD` compile symbol.

- API Reference: auto‑generated from XML docs under `src/DotCloth`
- Guides: usage, algorithm notes, and parameter recommendations

Highlights
- Constraints: stretch, distance‑based bending, tethers (rest/anchors). An XPBD variant is available experimentally via compile flag.
- Deterministic stepping, span‑first API, collision hooks (plane/sphere/capsule)
- Batching for better locality; zero per‑step allocations in the hot path

Start Here
- Usage: `docs/usage/minimal.md`
- Parameters: `docs/usage/recommendations.md`
- API overview: `docs/api/overview.md`
