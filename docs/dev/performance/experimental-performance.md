# Experimental Performance Results

These benchmarks are experimental and subject to change.
Use them only for development tuning and not for production baselines.

## Benchmark Setup
- The `perf/DotCloth.Perf` project provides a lightweight CPU benchmark (single‑threaded) that steps representative cloth grids and multi‑instance sets.
- Run on .NET 8 and 9 to compare:
  - `.NET 9`: `dotnet run --project perf/DotCloth.Perf -c Release --framework net9.0`
  - `.NET 8`: `dotnet run --project perf/DotCloth.Perf -c Release --framework net8.0`
- Capture environment for context (example template):
  - OS: Windows 11 23H2 / Ubuntu 24.04 / macOS 14.x
  - CPU: 12C/20T, Turbo 4.8 GHz (e.g., Intel i7‑12700H)
  - RAM: 32 GB
  - .NET SDK: output of `dotnet --info` (include RID)
  - Power plan: Balanced / High Performance (laptops may throttle)
- Sample output lines (interpreting results):
  - `Grid 64x64 ... 12 iters, 2 substeps: 16.5 ms/frame (~60.6 FPS)` → larger grids/iters increase cost roughly linearly.
  - `Instances=40 Grid 20x20 ... 8 iters: 4.2 ms/frame` → use this to size per‑frame budgets for many avatars.
- Guidance for sizing
  - Budget per frame (CPU): 1–4 ms typical for gameplay on desktop; target fewer iters/substeps and smaller grids for mobile.
  - Increase stability with substeps only when necessary; prefer tuning iterations and stiffness first.
  - Multi‑instance runs scale with vertex count; pin counts and colliders also affect cost.

Measured Results (this environment)
- Host: Linux (WSL2 kernel) ubuntu 24.04 (RID: ubuntu.24.04‑x64)
- CPU: AMD Ryzen 9 9950X3D 16‑Core Processor
- RAM: DDR5‑6800 32 GB ×2 (visible to WSL2 ~31.6 GB)
- .NET SDK: 9.0.109; Host runtime: 9.0.8
- Commands:
  - `.NET 9`: `dotnet run --project perf/DotCloth.Perf -c Release --framework net9.0`
  - `.NET 8`: `dotnet run --project perf/DotCloth.Perf -c Release --framework net8.0`
- Note: Perf excludes rendering — it measures solver/CPU only.

Tables

Single instance (frames=300, dt=1/60)

| Grid | Vertices | Iterations | Substeps | ms/frame (net9/net8) | FPS (net9/net8) |
|---:|---:|---:|---:|---:|---:|
| 32x32 | 1024 | 8  | 1 | 0.199 / 0.191 | 5021.2 / 5245.7 |
| 48x48 | 2304 | 10 | 1 | 0.533 / 0.525 | 1877.0 / 1904.6 |
| 64x64 | 4096 | 10 | 1 | 0.953 / 0.931 | 1049.6 / 1074.0 |
| 64x64 | 4096 | 12 | 2 | 2.264 / 2.208 | 441.7  / 452.8 |

Multi‑instance (40 instances, frames=300, dt=1/60)

| Grid | Vertices/inst | Iterations | Substeps | ms/frame (net9/net8) | FPS (net9/net8) |
|---:|---:|---:|---:|---:|---:|
| 20x20 | 400  | 8  | 1 | 2.762 / 2.642 | 362.0 / 378.4 |
| 24x24 | 576  | 8  | 1 | 4.104 / 3.991 | 243.7 / 250.6 |
| 28x28 | 784  | 10 | 1 | 7.071 / 6.892 | 141.4 / 145.1 |
| 32x32 | 1024 | 10 | 1 | 9.314 / 9.183 | 107.4 / 108.9 |

Notes:
- .NET 8 results: run the same perf commands with `--framework net8.0` and replace the placeholders (`—`) with measured values.
