---
description: "Use bundled Archivo and JetBrains Mono with matching browser and native typography, licensed sources and explicit script fallbacks."
---

# Product typography

Charts, HtmlForgeX reports and CanopyX grids share Archivo for ordinary text and JetBrains Mono for technical text. Archivo gives the chart and its host the same letterforms and measured label widths. The product sans stack is `ChartFontStacks.ProductSans`:

```css
Archivo, "Segoe UI Variable Text", "Segoe UI", system-ui, -apple-system, sans-serif
```

`ChartFontStacks.ProductMono` is `"JetBrains Mono", "Cascadia Mono", Consolas, monospace`.

The core package embeds upright variable TrueType faces for native PNG rendering and matching WOFF2 faces for browser output. No installed copy, network request or runtime font package is needed for the bundled repertoire. Native requests use real weight instances, including regular 400, medium 500, semibold 600, intermediate 650 and bold 700. Archivo retains weights 100–900 and JetBrains Mono 100–800; separate files for each weight would duplicate the same outlines. Archivo's width axis is fixed at its normal width of 100. Italic requests use the renderer's existing synthetic oblique policy; an italic face is not bundled.

`ChartForgeX.Typography.ChartFontAssets.StyleSheet` supplies both embedded browser faces. Add it once to a host page's stylesheet when that page needs the fonts outside chart SVGs. Product-font SVG output includes the required face for standalone viewing. The gallery and browser/native proof helpers use these product assets directly, without registering a separate fixture alias. Explicit custom fonts remain available through `WithFontFamily`, `FontSpec` and `FontRegistry`.

A host that emits the shared stylesheet once can omit the bundled product faces from each child SVG with `VisualSvgOptions.WithoutProductFonts()`. Captured custom fonts remain embedded. Preserve the chart's colour-variable mappings when supplying export options:

```csharp
var svgOptions = new VisualSvgOptions(colorVariables: chart.Options.SvgColorVariables)
    .WithoutProductFonts();
var svg = chart.ToSvg(svgOptions, "monthly-sales");
```

Use the default export for a standalone SVG that needs its own font faces. Built-in graph, stencil, sequence and motion-player hosts load the shared product faces for their controls and surrounding text as well as their visual content.

Numeric text uses tabular numerals where the face supports them. Hosts should use `font-variant-numeric: tabular-nums` for axis ticks, data labels and numeric table/grid cells so that changing a value does not shift its digit columns. The native bundled-font path applies the corresponding tabular substitutions.

## Chart roles in the shared type ramp

Graphite reads the chart-specific roles in the shared token source's `typography.chart` block. Compact means a logical viewport narrower than 480 pixels; sizes resolve before text measurement and scale with the export scale.

| Chart role | Wide / compact | Weight |
| --- | --- | ---: |
| Plot title | 17 / 15 px | 600 |
| Plot subtitle | 13 / 12 px | 400 |
| Panel title | 15 / 14 px | 600 |
| Axis title | 12 / 11 px | 500 |
| Tick | 12 / 11 px | 400 |
| Legend | 12.5 / 12 px | 400; values 500 |
| Data label | 12 / 11 px | 500 |
| Centre value | `clamp(20, 0.16 * radius, 40)` px | 700 |

Centre captions use 12 pixels with a 4-pixel gap. Scalar values retain their authored numeric presentation and tile values retain 22 pixels. Report headings and body text continue to use the general type ramp; a plot title does not inherit a report heading's size.

The chart text floor is 10 logical pixels. Text that cannot stay at that size is hidden; complete values and titles remain in semantic metadata. Explicit sizes above the floor retain precedence. A custom `VisualTypography` keeps its authored scale; changing a shared scale's font family retains its wide and compact roles. Shared host properties use the `--hfx-chart-type-*` prefix, including `--hfx-chart-type-title-size`, `--hfx-chart-type-title-compact-size` and `--hfx-chart-type-minimum-size`.

## Font sources and coverage

The [font source manifest](../../ChartForgeX/Typography/Fonts/font-sources.json) records the pinned upstream revisions, original and shipped SHA-256 hashes, file sizes and transformation details. Both families use the SIL Open Font License 1.1; the [Archivo license](../../ChartForgeX/Typography/Fonts/OFL-Archivo.txt) and [JetBrains Mono license](../../ChartForgeX/Typography/Fonts/OFL-JetBrainsMono.txt) travel with the package. The manifest is the reproducible source record for the bundled files.

The shipped fonts preserve every Unicode character in their pinned upstream cmap; they are not Unicode subsets. Archivo contains 653 mapped characters and JetBrains Mono 976. This covers their supplied Latin repertoire and symbols, with additional characters depending on the family; it does not establish complete Unicode, CJK, Arabic or Indic coverage. Missing characters continue through the documented stack and native font fallback. A script outside the bundled repertoire needs a suitable installed or explicitly registered font on the native host and the corresponding browser face. Match those fallback files too when repeatable metrics are required. Bundling Archivo does not make arbitrary fallback scripts or browser/native antialiasing pixel-identical.

See the [migration notes](migration.md#product-typography) for the visible change from earlier defaults.
