Legal Notes
===========

License
- The project is licensed under the Apache License, Version 2.0. See the `LICENSE` file.
- The `NOTICE` file provides attribution and terminology clarifications.

Terminology and Third‑Party Rights
- The terms “Position‑Based Dynamics (PBD)” and “Extended Position‑Based Dynamics (XPBD)” are used solely as descriptive labels for well‑known techniques in the literature. They do not imply endorsement, affiliation, or origin by any third party (including NVIDIA).
- No third‑party source code is included unless explicitly stated. All implementation is original based on publicly available publications and general numerical methods.
- Patents/Trademarks: Some jurisdictions may have patents or other rights covering techniques related to PBD/XPBD or variants. Contributors and users are responsible for assessing applicability in their jurisdictions and seeking legal advice as necessary.

Risk Mitigations (documentation & process)
- Use “XPBD” only as a descriptive term in docs, not as a product name or branding.
- Avoid copying code or non‑trivial snippets from third‑party SDKs/samples; cite publications when describing algorithms.
- Keep algorithmic descriptions generic (e.g., “compliance‑based constraint solver”) when reasonable.
- Record sources in design notes (papers, talks) and avoid ambiguous claims of equivalence.

Potential Implementation Safeguards (proposals)
- Add a contributor guideline prohibiting inclusion of third‑party code without license review and attribution.
- Add a pre‑merge checklist item: confirm no proprietary snippets or assets are introduced (especially shaders/kernels from vendor samples).
- If GPU paths are added later, ensure kernels/shaders are written from scratch and documented with references to public papers.

