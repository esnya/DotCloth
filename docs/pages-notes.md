GitHub Pages Notes (temporary)
=============================

This project uses DocFX to generate documentation and a standard GitHub Pages workflow to publish it.

How it works
- Workflow: `.github/workflows/docs.yml` builds metadata (`docfx metadata`) and site (`docfx build`) under `docs/docfx/_site`.
- Artifact is uploaded and deployed via `actions/deploy-pages` to the Pages environment.
- A `.nojekyll` file is placed to prevent Jekyll processing.

Enable Pages
- Repository Settings → Pages → Source: “GitHub Actions”.
- Trigger the workflow by pushing to `main` (changes under `docs/**` or `src/**`) or run it manually (`workflow_dispatch`).

Local preview (optional)
- Install DocFX locally: `dotnet tool update -g docfx`.
- From `docs/docfx`: `docfx metadata` then `docfx build` then `docfx serve _site`.

Note
- This is an internal memo. Public docs live under `docs/docfx` and are built by CI.

