Evaluation Metrics
==================

Purpose
- Provide common metrics to compare cloth simulations.

Metrics
- **Average Stretch Ratio**: mean of \(\frac{\|e\|}{\|e_0\|}\) across edges.
- **Angle Variance**: variance of dihedral angles between adjacent triangles.
- **Runtime**: wall-clock execution time for a fixed step count.

Usage
- Record metrics for two configurations with identical parameters.
- Compare values without assuming a specific solver as the baseline.
