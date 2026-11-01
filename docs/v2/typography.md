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

Numeric text uses tabular numerals where the face supports them. Hosts should use `font-variant-numeric: tabular-nums` for axis ticks, data labels and numeric table/grid cells so that changing a value does not shift its digit columns. The native bundled-font path applies the corresponding tabular substitutions.

## Chart roles in the shared type ramp

Chart typography maps each role by its purpose within a plot. A chart title is a plot heading, so it uses the shared `subtitle` step rather than a report-level title. The default token reference is 16 pixels per rem:

| Chart role | Shared token role | Size in pixels |
| --- | --- | ---: |
| Plot title | `subtitle` | 16 |
| Plot subtitle | `small` | 13 |
| Axis ticks, legend entries and data labels | `caption` | 12 |
| Center value | `title` | 20 |
| Tile label | `body` | 14 |
| Tile detail | `small` | 13 |

Scalar values retain their distinct 34-pixel numeric presentation and tile values retain 22 pixels. These are numeric display roles, not report headings. This mapping keeps the shared ramp while avoiding an incidental increase of chart titles and subtitles to 20 and 16 pixels. Explicit authored sizes retain precedence.

## Font sources and coverage

The [font source manifest](../../ChartForgeX/Typography/Fonts/font-sources.json) records the pinned upstream revisions, original and shipped SHA-256 hashes, file sizes and transformation details. Both families use the SIL Open Font License 1.1; the [Archivo license](../../ChartForgeX/Typography/Fonts/OFL-Archivo.txt) and [JetBrains Mono license](../../ChartForgeX/Typography/Fonts/OFL-JetBrainsMono.txt) travel with the package. The manifest is the reproducible source record for the bundled files.

The shipped fonts preserve every Unicode character in their pinned upstream cmap; they are not Unicode subsets. Archivo contains 653 mapped characters and JetBrains Mono 976. This covers their supplied Latin repertoire and symbols, with additional characters depending on the family; it does not establish complete Unicode, CJK, Arabic or Indic coverage. Missing characters continue through the documented stack and native font fallback. A script outside the bundled repertoire needs a suitable installed or explicitly registered font on the native host and the corresponding browser face. Match those fallback files too when repeatable metrics are required. Bundling Archivo does not make arbitrary fallback scripts or browser/native antialiasing pixel-identical.

See the [migration notes](migration.md#product-typography) for the visible change from earlier defaults.
