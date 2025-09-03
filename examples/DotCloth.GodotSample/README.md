DotCloth Godot Sample
=====================

Overview
- Minimal Godot 4 C# project that integrates the DotCloth XPBD solver and renders a dynamic cloth mesh.
- Pure code-first: the scene (`main.tscn`) attaches `Main.cs` which creates camera, light, mesh, and steps the solver.
- Cross‑platform: runs on Windows/macOS/Linux with Godot 4 .NET installed.

Requirements
- Godot 4.2+ with .NET (4.3 recommended).
- .NET SDK 8.0+ installed. The core library multi‑targets `net9.0;net8.0` for compatibility.

Run
- Open the folder in the Godot editor: `examples/DotCloth.GodotSample` and press Play.
- Or from CLI (if `godot` is on PATH):
  - Windows: `godot4.exe --path examples/DotCloth.GodotSample`
  - macOS/Linux: `godot4 --path examples/DotCloth.GodotSample`

Notes
- The sample is intentionally not added to the solution to keep CI green (no Godot SDK required on agents).
- If your Godot install uses a different .NET SDK, adjust `DotCloth.GodotSample.csproj` Sdk line as needed.

