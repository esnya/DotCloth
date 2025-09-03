# DotCloth Release Guide (v0.x)

This guide targets project members (maintainers). Language: English. Code: English.

## Overview
- Default branch: `main`.
- Versions: Semantic Versioning (0.x pre-release). Conventional Commits + gitmoji for history.
- CI: GitHub Actions builds/tests on PRs and pushes; tagged releases build, pack, and publish to NuGet.

## Prerequisites
- GitHub repository secrets:
  - `NUGET_API_KEY`: NuGet.org API key for publishing.
- Branch protection on `main`: require CI success, at least 1 review.

## Pre‑release Checklist (0.1.0 example)
1) Freeze scope on `main` (only release‑critical fixes).
2) Ensure green CI on `main`:
   - `dotnet restore`
   - `dotnet build -c Release`
   - `dotnet test -c Release`
   - `dotnet build examples/DotCloth.MonoGameSample -c Release` (build only; no GUI)
3) Bump version in centralized `Directory.Build.props` (SSOT):
   - Edit `<Version>0.1.0</Version>`
   - Commit: `chore(release): 🔖 bump to 0.1.0`
   - Alternatively, use the `Release (manual)` workflow which performs this step for you
4) Changelog / Release notes:
   - Option A: Update `CHANGELOG.md` (added/changed/fixed/ci/docs)
   - Option B: Prepare GitHub Release notes (see template below)
5) Docs polish:
   - Root `README.md`: point to examples; mention multi‑target `net9.0;net8.0`
   - `examples/README.md`: controls, scenarios, HUD (already present)
   - Design note: `docs/design/sample-monogame.md` (keep in sync)

## Tagging and Publishing

### Option A — Manual GitHub Action (recommended)
1) Trigger `Release (manual)` workflow from the Actions tab.
   - Input `version` (e.g., `0.1.0`).
   - The workflow will:
     - Update central version in `Directory.Build.props`.
     - Commit the bump, create tag `v<version>`, and push to `main` with tags.
     - Restore, build, test.
     - Pack and publish to NuGet (if `NUGET_API_KEY` is configured).
2) Create a GitHub Release from tag `v0.1.0`:
   - Title: `DotCloth 0.1.0`
   - Notes (template):
     - Features: MonoGame sample with Minimal/Cylinder/Colliders/Large/X‑Large; collider visualization; perf HUD.
     - Fixes: fixed‑step accumulator; Vector3 disambiguation; 32‑bit index buffers; bounds validation.
     - CI: build + test; sample build headless.
     - Docs: examples README; design note.

### Option B — Tag‑driven release (alternative)
1) Create an annotated tag from `main`:
   - `git tag -a v0.1.0 -m "DotCloth 0.1.0"`
   - `git push origin main --tags`
2) CI (`Release`) workflow on `v*` tags will:
   - Restore, build, test.
   - Pack library to `artifacts/` with version from tag.
   - Push `.nupkg` to NuGet using `NUGET_API_KEY`.
3) Create a GitHub Release from tag `v0.1.0` using the same notes template.

## Local Validation (optional)
- Pack: `dotnet pack src/DotCloth -c Release -o artifacts /p:Version=0.1.0`
- Inspect: `unzip -l artifacts/DotCloth.0.1.0.nupkg`
- Consume: create a scratch app and `dotnet add package DotCloth --source artifacts`

## Post‑release
- Verify package on NuGet (metadata, tags, README link).
- Update any version badges or docs as needed.
- Track issues for 0.1.x and plan 0.2.0 scope.

## Breaking Changes (future)
- For `0.x`, avoid breaking changes where possible; if necessary:
  - Mark PR with `Migration` section (purpose → impact → rollback).
  - Document mapping and mitigations in README/ADR.
