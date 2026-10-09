# ChartForgeX content for the Evotec website

ChartForgeX is presented on the [Evotec project page](https://evotec.xyz/projects/chartforgex/). The hub also hosts the [curated demo tour](https://evotec.xyz/demos/chartforgex/) and [complete generated gallery](https://evotec.xyz/demos/chartforgex/gallery/). ChartForgeX does not publish a separate website.

The content in this folder is consumed by the Evotec Website project:

- `content/project-docs/` contains project documentation.
- `content/examples/` contains source-linked examples.
- `static/examples/promoted-cases.json` supplies curated demos and points to `data/gallery.json`.
- `data/gallery.json` indexes the complete visual catalog; `static/examples/generated/` contains its SVG, PNG, and HTML artifacts.
- `static/examples/generated/catalog.html` is the single catalog, with matching light/dark examples, native thumbnails, SVG, PNG and C# source in the same directory.
- `build/Sync-GeneratedExamples.ps1` refreshes that catalog and its metadata from the prepared-scene manifest, plus the selected source-linked demo assets declared by `promoted-cases.json`.

`WebsiteArtifacts/project-manifest.json` declares the hub-ingested paths. The Evotec Website repository owns the page shell, routes, project-demo schema, source lock, deployment, and live availability. Its source lock must select a ChartForgeX commit containing the updated manifest before the gallery can appear on the hub.

To refresh generated content, run from the ChartForgeX repository root:

```powershell
pwsh ./Build.ps1 -Configuration Release -SkipAot -SkipPack
dotnet run --project ./ChartForgeX.Examples -c Release --no-build -- --v2-only --v2-curated
pwsh ./Website/build/Sync-GeneratedExamples.ps1
```

The example build and sync do not publish a package or deploy the site. Validate the project-demo manifest, generated artifact paths, and the hub-rendered pages before claiming the update is live.
