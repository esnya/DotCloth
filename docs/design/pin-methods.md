Pin Methods Design Note
=======================

Purpose
-------
Provide explicit APIs to fix or release cloth particles without manual inverse mass edits.

Public API
----------
- `ForceCloth.Pin(int index, Vector3 position)`
- `ForceCloth.Unpin(int index, float invMass)`
- `MassSpringCloth.Pin(int index, Vector3 position)`
- `MassSpringCloth.Unpin(int index, float invMass)`

Boundaries
----------
- Pins zero the inverse mass and velocity; callers may move pinned particles by writing to `Positions`.
- No `MaxDistance` or soft tether behavior is added.

Dependencies
------------
No new dependencies; operates on existing arrays.

Testing
-------
Unit tests pin a particle, verify it remains fixed, then unpin and confirm it moves under gravity.

Migration
---------
Additive API; existing code continues to work without changes.
