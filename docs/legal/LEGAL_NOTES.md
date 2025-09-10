Legal Notes
===========

License
- The project is licensed under the Apache License, Version 2.0. See the `LICENSE` file.
- The `NOTICE` file provides attribution and terminology clarifications.

Terminology and Third-Party Rights
- The terms “Position-Based Dynamics (PBD)” and “Extended Position-Based Dynamics (XPBD)” are used solely as descriptive labels for well-known techniques in the literature. They do not imply endorsement, affiliation, or origin by any third party (including NVIDIA).
- No third-party source code is included unless explicitly stated. All implementation is original based on publicly available publications and general numerical methods.

Patent Position
- NVIDIA filings around XPBD and tether constraints (e.g., US20210175810A1, US20210175811A1) cover position projection and compliance accumulation. The redesign avoids these mechanisms.
- Common force-based and projective-dynamics methods (Baraff & Witkin 1998, Bridson 2002, Bouaziz et al. 2014, Li et al. 2020) appear unpatented according to USPTO/EPO searches as of 2024-09; contributors should verify in their jurisdictions.
- Additional unpatented sources: Grinspun et al. 2003 (discrete shells), Etzmuß et al. 2003 (co-rotational FEM), Provot 1995 (strain limiting).

Process Safeguards
- Use “XPBD” only as a descriptive term in docs.
- Avoid copying code or non-trivial snippets from third-party SDKs/samples; cite publications when describing algorithms.
- Record sources in design notes and avoid ambiguous claims of equivalence.
