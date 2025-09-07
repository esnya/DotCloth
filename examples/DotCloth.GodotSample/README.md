DotCloth Godot Sample
=====================

Overview
- Minimal Godot 4 C# project that integrates DotCloth and renders a dynamic cloth mesh.
- By default it uses the velocity‑level solver. The XPBD variant is experimental and requires compiling the library with `DOTCLOTH_EXPERIMENTAL_XPBD` and explicitly using `XpbdSolver`.
- Scene-driven: `main.tscn` defines camera, lighting, ground, scenario meshes, and UI panels; `Main.cs` reads the active mesh and updates its vertices.
- Cross‑platform: runs on Windows/macOS/Linux with Godot 4 .NET installed.

Requirements
- Godot 4.2+ with .NET (4.3 recommended).
- .NET SDK 8.0+ installed. The core library multi‑targets `net9.0;net8.0` for compatibility.

Run
- Open the folder in the Godot editor: `examples/DotCloth.GodotSample` and press Play.
- Or from CLI (if `godot` is on PATH):
  - Windows: `godot4.exe --path examples/DotCloth.GodotSample`
  - macOS/Linux: `godot4 --path examples/DotCloth.GodotSample`

Controls
- Orbit camera: Right-drag, Wheel zoom
- Pin: Left click near a vertex
- Unpin: Middle click near a pinned vertex; Reset pins: R
- Scenarios: 1–4 keys or dropdown (Minimal, Tube, Collision, Large)

Notes
- The sample is intentionally not added to the solution to keep CI green (no Godot SDK required on agents).
- If your Godot install uses a different .NET SDK, adjust `DotCloth.GodotSample.csproj` Sdk line as needed.
