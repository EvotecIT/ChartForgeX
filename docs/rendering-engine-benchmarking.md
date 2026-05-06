# Rendering Engine Benchmarking

This branch keeps the rendering-engine work separate from active feature PRs such as topology. The first goal is to measure the current string-heavy baseline, the internal writer primitives, and a small SVG AST before migrating chart renderers.

Run the zero-dependency benchmark harness from the repository root:

```powershell
dotnet run --project ChartForgeX.Benchmarks -c Release -- 500
```

The harness reports mean milliseconds, allocated KB per operation, and output size for:

- synthetic raw `StringBuilder` SVG marker markup versus `SvgMarkupWriter`
- synthetic raw `StringBuilder` SVG marker markup versus `SvgElement` AST save
- synthetic `SvgDocument` parse, edit, and save for load/save cost
- synthetic raw path data versus `SvgPathDataBuilder`
- synthetic path data parse, edit, and save for `d` attribute manipulation cost
- synthetic transform parse, edit, and save for `transform` attribute manipulation cost
- synthetic inline style parse, edit, and save for `style` attribute manipulation cost
- synthetic viewBox parse, edit, and save for viewport manipulation cost
- synthetic points parse, edit, and save for polygon/polyline manipulation cost
- current SVG render baselines for mature chart families: line/area, grouped bars, and dotted maps

Migration checks should compare two dimensions:

- Runtime: keep mean time and allocation deltas visible for each migrated chart family.
- Maintenance: track reduced raw `<tag` append/interpolation sites, centralized escaping, optional attributes, and shared path formatting.
- Editability: use the AST where later query/edit/load/save behavior matters; use `SvgMarkupWriter` directly where a renderer is a hot one-pass stream.

`SvgDocument.Parse` is a semantic SVG load path, not a byte-preserving formatter. It keeps elements, attributes, comments inside the root, and text/CDATA content, then saves through the normalized writer. DTDs are rejected when loading arbitrary content. The AST supports structural edits such as clone, insert, remove, class changes, text replacement, `d` attribute path-data parsing, `transform` parsing, inline `style` parsing, typed `viewBox` edits, and polygon/polyline `points` edits so callers can migrate from string surgery in small steps.

Do not use topology as the first migration target while that PR is changing. Prefer stable existing renderers with SVG and PNG coverage already in the smoke suite.
