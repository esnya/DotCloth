Codex Cloud Setup and Maintenance Scripts
=========================================

Purpose
- Automate .NET SDK installation on Codex Cloud workers.
- Trim package caches after image caching to keep layers small.

Scope and Boundaries
- Setup installs .NET SDK 9.0 and 8.0 using the official installer and wires `DOTNET_ROOT` into `PATH`.
- Maintenance updates the existing .NET SDK installations.
- Post-cache maintenance removes apt package lists and cache.
- Scripts live under `.codex/cloud` and have no runtime dependency on library code.

Dependencies
- curl
- bash
- apt-get

Test Strategy
- Run setup and verify `dotnet --info` outputs installed SDKs.
- Maintenance is idempotent and safe to run after caching.

Migration / Compatibility
- None.
