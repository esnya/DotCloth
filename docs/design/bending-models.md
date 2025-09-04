Bending Models (XPBD experimental)
==================================

Current
- Opposite-vertex distance (hinge distance). Default solver applies impulses; an XPBD compliance version is available experimentally.

Planned
- Dihedral-angle model. Default solver will provide an impulse-based version; an XPBD compliance version may be added experimentally with tests (angle reduction, rest-state match, energy behavior).

Notes
- Switch via a future parameter (e.g., `BendModel = Distance | Dihedral`). Default remains Distance to ensure stable behavior across topologies.
