# DotCloth Documentation

Welcome to DotCloth — a high‑performance cloth simulation library targeting .NET 9.0 and conceptually aligned with UnityCloth parameters. The current implementation is a patent‑neutral mass‑spring solver with pluggable integrators.

- API Reference: auto‑generated from XML docs under `src/DotCloth`
- Guides: usage, algorithm notes, and parameter recommendations

Highlights
- Constraints: stretch via springs (rest anchors), optional collision hooks (plane/sphere/capsule)
- Deterministic stepping, span‑first API
- Batching for better locality; zero per‑step allocations in the hot path

Start Here
- Usage: `docs/usage/minimal.md`
- Parameters: `docs/usage/recommendations.md`
- API overview: `docs/api/overview.md`
