# DotCloth Documentation

Welcome to DotCloth — a high‑performance XPBD cloth simulation library targeting .NET 9.0, conceptually aligned with UnityCloth parameters.

- API Reference: auto‑generated from XML docs under `src/DotCloth`
- Guides: usage, algorithm notes, and parameter recommendations

Highlights
- XPBD constraints: stretch, distance‑based bending, tethers (rest/anchors)
- Deterministic stepping, span‑first API, collision hooks (plane/sphere/capsule)
- Batching for better locality; zero per‑step allocations in the hot path

Start Here
- Usage: `docs/usage/minimal.md`
- Parameters: `docs/usage/recommendations.md`
- API overview: `docs/api/overview.md`

