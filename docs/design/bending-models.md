Bending Models
==============

Current
- Opposite-vertex distance (hinge distance) XPBD: robust and simple; acts as a spring across the hinge.

Planned
- Dihedral-angle XPBD: preserves folding angle with better shape fidelity. Requires correct angle gradient and area weighting for stability. To be added with unit tests (angle reduction, rest-state match, energy behavior).

Notes
- Switch via a future parameter (e.g., `BendModel = Distance | Dihedral`). Default remains Distance to ensure stable behavior across topologies.

