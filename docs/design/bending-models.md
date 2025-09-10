Bending Models
==============

Current
- Opposite-vertex distance (hinge distance). Default solver applies impulses.

Planned
- Dihedral-angle model with impulse-based solving and tests (angle reduction, rest-state match, energy behavior).

Notes
- Switch via a future parameter (e.g., `BendModel = Distance | Dihedral`). Default remains Distance to ensure stable behavior across topologies.
