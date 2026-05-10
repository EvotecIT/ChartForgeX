# ChartForgeX Website

This folder is the dedicated PowerForge.Web pilot for visual-heavy project
sites. It is intentionally different from a project repo that only has
`Website/content` or `WebsiteArtifacts` for hub ingestion: this folder contains
`site.json` and `pipeline.json`, so it owns a publishable site.

Target host:

```text
https://chartforgex.evotec.xyz/
```

Local build:

```powershell
.\build.ps1 -Dev
.\build.ps1 -Ci
```

Local preview:

```powershell
.\build.ps1 -Dev -Serve -Port 8021
```

The wrapper prefers the sibling `PSPublishModule` checkout when it is available
and falls back to `powerforge-web` from `PATH`.

The generated examples are copied opportunistically from:

```text
..\ChartForgeX.Examples\bin\Release\net8.0\output
..\artifacts\topology-demo
```

Those folders are produced by the repository build and are not the source of
truth for the site.

The browsable examples are data-driven:

- `data/showcase.json` controls the curated examples on `/examples/`.
- `data/gallery.json` controls the full catalog on `/gallery/`.
- `static/examples/generated/` contains committed seed artifacts and can be
  refreshed from generated output.

To fold newly generated SVG/PNG/HTML artifacts into the gallery data, run:

```powershell
.\build\Sync-GeneratedExamples.ps1
```

The sync script preserves existing gallery metadata by artifact URL and creates
reasonable placeholder entries for new SVG files. Promote the best new cases to
`data/showcase.json` when they deserve the richer code-and-preview treatment.

See `content/docs/deployment.md` for the publish shape and route ownership
rules.
