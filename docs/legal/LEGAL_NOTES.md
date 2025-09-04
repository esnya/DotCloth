Legal Notes
===========

License
- The project is licensed under the Apache License, Version 2.0. See the `LICENSE` file.
- The `NOTICE` file provides attribution and terminology clarifications.

Terminology and Third‑Party Rights
- The terms “Position‑Based Dynamics (PBD)” and “Extended Position‑Based Dynamics (XPBD)” are used solely as descriptive labels for well‑known techniques in the literature. They do not imply endorsement, affiliation, or origin by any third party (including NVIDIA).
- No third‑party source code is included unless explicitly stated. All implementation is original based on publicly available publications and general numerical methods.
- Patents/Trademarks: Some jurisdictions may have patents or other rights covering techniques related to PBD/XPBD or variants. Contributors and users are responsible for assessing applicability in their jurisdictions and seeking legal advice as necessary.

Default Algorithm Selection
- The default runtime path uses a velocity‑level sequential‑impulses solver (no position projection, no XPBD lambda accumulation/compliance).
- An experimental XPBD implementation can be included only by compiling with the `DOTCLOTH_EXPERIMENTAL_XPBD` symbol and is intended for research/verification builds.

Default Algorithm Performance (measured)
- Scope: CPU time of the default velocity‑level sequential‑impulses solver (no rendering). Method and scenarios mirror README’s Performance section for comparability.
- How to run:
  - .NET 9: `dotnet run --project perf/DotCloth.Perf -c Release --framework net9.0`
  - .NET 8: `dotnet run --project perf/DotCloth.Perf -c Release --framework net8.0`
- Environment (this run):
  - OS: Ubuntu 24.04 (WSL2 kernel); RID: `ubuntu.24.04-x64`
  - CPU: AMD Ryzen 9 9950X3D 16‑Core Processor
  - RAM: DDR5‑6800 64 GB (WSL2 visible ~31.6 GB)
  - .NET SDK: 9.0.109; Host runtime: 9.0.8 (also tested on .NET 8.0.19)
- Notes:
  - Frames = 300, `dt = 1/60`.
  - Tables report solver time only; higher grids/iterations scale roughly linearly.
  - Multi‑instance cost scales with total vertices; pins and active colliders also affect cost.

Single instance

| Grid | Vertices | Iterations | Substeps | ms/frame (net9/net8) | FPS (net9/net8) |
|---:|---:|---:|---:|---:|---:|
| 32x32 | 1024 | 8  | 1 | 0.199 / 0.191 | 5021.2 / 5245.7 |
| 48x48 | 2304 | 10 | 1 | 0.533 / 0.525 | 1877.0 / 1904.6 |
| 64x64 | 4096 | 10 | 1 | 0.953 / 0.931 | 1049.6 / 1074.0 |
| 64x64 | 4096 | 12 | 2 | 2.264 / 2.208 | 441.7  / 452.8 |

Multi‑instance (40 instances)

| Grid | Vertices/inst | Iterations | Substeps | ms/frame (net9/net8) | FPS (net9/net8) |
|---:|---:|---:|---:|---:|---:|
| 20x20 | 400  | 8  | 1 | 2.762 / 2.642 | 362.0 / 378.4 |
| 24x24 | 576  | 8  | 1 | 4.104 / 3.991 | 243.7 / 250.6 |
| 28x28 | 784  | 10 | 1 | 7.071 / 6.892 | 141.4 / 145.1 |
| 32x32 | 1024 | 10 | 1 | 9.314 / 9.183 | 107.4 / 108.9 |

Interpretation and guidance
- Single instance tables help size per‑avatar cloth budgets; 1–4 ms/frame CPU is typical on desktop gameplay.
- Prefer tuning iterations and stiffness before adding substeps; substeps improve stability but increase cost.
- Default algorithm preserves determinism and safety checks; optimization patterns are documented in `AGENTS.md`.

Risk Mitigations (documentation & process)
- Use “XPBD” only as a descriptive term in docs, not as a product name or branding.
- Avoid copying code or non‑trivial snippets from third‑party SDKs/samples; cite publications when describing algorithms.
- Keep algorithmic descriptions generic (e.g., “compliance‑based constraint solver”) when reasonable.
- Record sources in design notes (papers, talks) and avoid ambiguous claims of equivalence.

Potential Implementation Safeguards (proposals)
- Add a contributor guideline prohibiting inclusion of third‑party code without license review and attribution.
- Add a pre‑merge checklist item: confirm no proprietary snippets or assets are introduced (especially shaders/kernels from vendor samples).
- If GPU paths are added later, ensure kernels/shaders are written from scratch and documented with references to public papers.
