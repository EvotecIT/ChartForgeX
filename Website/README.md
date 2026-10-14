# ChartForgeX website

This folder builds [chartforgex.com](https://chartforgex.com) with PowerForge.Web: the home page, gallery, Mermaid support, playground, PowerShell examples, docs, examples and API reference.

```powershell
pwsh Website/build.ps1 -Serve
```

- `site.json`, `pipeline.json` and `themes/chartforgex/` define the site.
- `content/project-docs/docs/` and `content/examples/` are shared with the hub through `WebsiteArtifacts/project-manifest.json`.
- `data/gallery.json` and `static/examples/generated/` hold the generated gallery; refresh them with `build/Sync-GeneratedExamples.ps1`.
- `tools/SiteVisuals` renders the hero, surface and Mermaid visuals with ChartForgeX.

See [AGENTS.md](AGENTS.md) for the full workflow and validation steps.
