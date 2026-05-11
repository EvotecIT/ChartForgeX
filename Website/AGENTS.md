# Agents Guide (ChartForgeX Website)

Last updated: 2026-05-09

## Purpose

This folder is the dedicated PowerForge.Web pilot site for ChartForgeX. Its
presence is intentional: `Website/site.json` and `Website/pipeline.json` mean
this repo owns a publishable site, not only hub-ingested website data.

## Ownership Rules

- Keep the central Evotec hub page at `https://evotec.xyz/projects/chartforgex/`.
- Publish the richer visual/demo experience from this folder to
  `https://chartforgex.evotec.xyz/`.
- Keep generated demo outputs source-first in the ChartForgeX build:
  `ChartForgeX.Examples/bin/Release/net8.0/output`.
- The website may copy those generated outputs under `/examples/generated/`
  when they exist, but the site pipeline should still build without them.
- Keep the browsable surfaces data-driven:
  - `data/showcase.json` feeds curated `/examples/` cases.
  - `data/gallery.json` feeds the full `/gallery/` catalog.
  - `build/Sync-GeneratedExamples.ps1` refreshes gallery data from generated
    SVG/PNG/HTML output and preserves existing metadata by artifact URL.
- Use shared PowerForge conventions for navigation, docs, search, metadata,
  and quality gates. Make ChartForgeX-specific visuals through theme tokens,
  gallery layouts, and examples.
- Keep the visible shell aligned with the Evotec family when publishing under
  `*.evotec.xyz`: header rhythm, panel language, theme behavior, and project
  navigation should feel related to the main site.
- Promoted examples should connect preview to reproduction: show the rendered
  HTML/SVG/PNG artifacts and point back to the source file or builder method
  that creates the same output.
- Site audit intentionally excludes copied generated example pages under
  `/examples/generated/` and `/examples/topology/`; those HTML files are product
  artifacts with demo-internal links, while the website shell audits the pages
  that browse and explain them.

## Build Commands

From `C:\Support\GitHub\ChartForgeX\Website`:

```powershell
.\build.ps1 -Dev
.\build.ps1 -Ci
.\build.ps1 -Dev -Serve -Port 8021
```

The wrapper prefers the sibling local `PSPublishModule` checkout. Direct CLI
commands are still valid when the executable is on `PATH`:

```powershell
powerforge-web pipeline --config .\pipeline.json --mode dev
powerforge-web pipeline --config .\pipeline.json --mode ci
```

Before reviewing the generated gallery, run the repo build from the repository
root so example assets exist:

```powershell
.\Build.ps1
```

## Files To Know

- `site.json`
- `pipeline.json`
- `build.ps1`
- `content/pages/index.md`
- `content/pages/examples.md`
- `content/pages/gallery.md`
- `data/showcase.json`
- `data/gallery.json`
- `build/Sync-GeneratedExamples.ps1`
- `content/docs/deployment.md`
- `themes/chartforgex/theme.manifest.json`
- `static/css/app.css`
- `static/js/site.js`
