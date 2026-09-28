# Agent Guide: ChartForgeX hub content

## Purpose

ChartForgeX publishes its project page, curated demos, documentation, and complete gallery through the central Evotec website at https://evotec.xyz/. This folder is a content source for that hub. It does not define or deploy a separate ChartForgeX site.

## Ownership

- The project page is https://evotec.xyz/projects/chartforgex/.
- The curated demo tour is https://evotec.xyz/demos/chartforgex/.
- The complete generated gallery is https://evotec.xyz/demos/chartforgex/gallery/.
- WebsiteArtifacts/project-manifest.json declares the hub-ingested project docs, examples, and demo manifest.
- content/project-docs and content/examples hold human-authored hub content.
- static/examples/promoted-cases.json describes source-linked curated demos. Its schema is owned by the Evotec Website repository at https://evotec.xyz/schemas/project-demos.schema.json.
- data/gallery.json and static/examples/generated hold the complete generated gallery. Keep these artifacts source-linked and consistent with the example build.
- build/Sync-GeneratedExamples.ps1 updates gallery metadata and committed examples from ChartForgeX.Examples/bin/Release/net8.0/output, preserving existing metadata by artifact URL.

The Evotec Website repository owns routing, layout, search, SEO, the project-demo schema, deployment, and the source lock that selects a ChartForgeX commit. Update the ChartForgeX content before bumping that lock. Do not reintroduce a dedicated site.json, pipeline.json, theme shell, or links to chartforgex.evotec.xyz or the retired GitHub Pages site.

## Validation

From the ChartForgeX repository root, build example output without publishing:

```powershell
pwsh ./Build.ps1 -Configuration Release -SkipAot -SkipPack
```

Then refresh the checked-in gallery when example output changes:

```powershell
pwsh ./Website/build/Sync-GeneratedExamples.ps1
```

Run the repository's focused project-demo smoke tests and validate the resulting manifest against the canonical schema in the Website source. Check the rendered hub at desktop and compact widths when changing user-facing pages. A source PR or local build does not prove that evotec.xyz has deployed the update.

## Files to know

- WebsiteArtifacts/project-manifest.json
- Website/content/project-docs/
- Website/content/examples/
- Website/static/examples/promoted-cases.json
- Website/data/gallery.json
- Website/static/examples/generated/
- Website/build/Sync-GeneratedExamples.ps1
