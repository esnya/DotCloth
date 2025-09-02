Parameter Recommendations
=========================

- Iterations: 8–20 depending on stiffness; increase for stiffer cloth.
- Substeps: 1–2 for most use cases; use >1 for fast motion/collisions.
- ComplianceScale: 1e-6 to 1e-4. Lower = stiffer when stiffness is high.
- StretchStiffness: 0.7–1.0 for typical fabrics.
- BendStiffness: 0.2–0.7 for flexible to stiffer; distance-based bending is stable.
- TetherStiffness: 0.2–1.0; TetherLengthScale ~0.5–1.0 depending on desired tightening.
- Damping: 0.0–0.2; AirDrag small (0.0–0.1) to avoid overdamping.
- VertexMass: 0.01–0.05 kg typical; ensure stable under your time step.

Tips
- Start with fewer iterations and add substeps only if necessary.
- Prefer adjusting stiffness before ComplianceScale.
- Pin a few stable anchors for faster settling.
- Collisions: begin with small friction and thickness; increase gradually.
