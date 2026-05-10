# Topology Icon Assets

This folder contains generated, provenance-tracked topology icon packs that are intentionally separate from the ChartForgeX core runtime.

Generated vendor packs should include:

- a portable `manifest.json` file produced from `TopologyIconPackJson`
- editable/renderable `svg/*.svg` artwork files referenced by manifest `artwork.svgPath`
- optional `previews/*.png` thumbnails for picker UIs and documentation
- source metadata in every manifest: source URL, source revision, license, and per-icon source path
- an import report when a whole external collection is ingested

The conversion tooling lives under `ChartForgeX.Tools.IconImport` and `tools/topology-icons`. It may use tooling-only dependencies for SVG-to-PNG previews, but those packages are not runtime dependencies of `ChartForgeX`.

See `docs/topology-icon-packs.md` for the reusable pack layout, importer command, and review checklist for future vendor/community packs.
