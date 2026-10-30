---
description: "Embed script-free charts with native mark links, page themes, fluid tracks, shared scales and accessible data tables."
---

# Charts in server-rendered pages

ChartForgeX can emit linked SVG and an optional data table from an ASP.NET Core page or a Blazor component using static server rendering. The output needs no client framework or script. The page keeps its navigation, row labels, filters and layout; ChartForgeX supplies the chart geometry.

## Links and host hooks

Attach immutable `ChartMarkOptions` to a point or typed item. The same options work on bars, stacked segments, scatter and sparkline points, comparison ranges, calendar cells and schedule items.

```csharp
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

var point = new ChartPoint(1, 24).WithMarkOptions(new ChartMarkOptions(
    href: "/activity/2026-10-30",
    tooltip: "24 published updates",
    accessibleLabel: "30 October: 24 published updates; view this day",
    cssClass: "activity-day",
    dataAttributes: new Dictionary<string, string> { ["data-day"] = "2026-10-30" },
    current: true));

var chart = Chart.Create().AddBar("Updates", new[] { point });
var svg = chart.Prepare(new VisualRenderContext()).ToSvg(
    new VisualSvgOptions(idPrefix: "activity-daily"));
```

Use `linkTarget: VisualSvgLinkTarget.NewContext` for a publisher's page. New-context links receive `target="_blank"` and `rel="noopener noreferrer"`. Other authored links use the current context, independently of the render-wide default. Links receive a keyboard tab stop, `role="link"` and an accessible name. `Tooltip` becomes a plain-text SVG `<title>`; `AccessibleLabel` takes precedence over the generated name.

Give keyboard focus a visible treatment in the host stylesheet. SVG links use the page's CSS; ChartForgeX does not inject a focus stylesheet. For example:

```css
svg a:focus-visible {
  outline: 3px solid var(--accent, #2563eb);
  outline-offset: 3px;
}
```

Choose a focus colour that contrasts with both page themes, and check the links using Tab. Keep this focus treatment distinct from the chart's current or selected state.

`series.WithMarkOptions(options)` supplies series hooks and defaults for its marks. An item's non-null options replace those defaults. `series.WithRowOptions(options)` independently decorates the schedule label/summary row or matrix row. Put row navigation on that row, or keep the label link in HTML when using strips. The renderer avoids nested SVG links.

State-timeline items with authored mark options retain their interval boundaries. Unadorned adjacent intervals may still merge into one run; a series default then applies to the run without assigning it one contributor's observation ID. Plot-only strips omit the SVG label/summary row, including its navigation.

Class names are ASCII CSS identifiers separated by single spaces. Complete `data-*` names must use lowercase ASCII letters, digits, underscores and hyphens after `data-`, beginning with a letter. `data-cfx` and `data-cfx-*` are reserved. Invalid hooks throw rather than being rewritten. Values are escaped as text; valid class names, attribute names and values retain their spelling. `Current` and `Selected` identify the page's active item with `aria-current="true"` and the `data-cfx-current` or `data-cfx-selected` hook. Schedule rows use a subtle band across the label and track, a left accent and semibold row text. Individual marks use dashed current or solid selected outlines. These state treatments also appear in raster output. Links retain navigation semantics rather than becoming selection widgets.

## Shared scales and HTML row labels

A strip is a plot without axes, legend, surface or outer padding. `ChartStripScale` snapshots an existing `ChartAxis` with explicit increasing bounds, so every row and its axis use the same projection. This is useful for support windows, activity timelines and numeric tracks.

```csharp
var start = new DateTime(2026, 1, 1);
var end = new DateTime(2027, 1, 1);
var scale = new ChartStripScale(new ChartAxis {
    Minimum = start.ToOADate(), Maximum = end.ToOADate(),
    Scale = ChartScaleKind.Time, TickCount = 4,
    LabelFormatter = value => DateTime.FromOADate(value).ToString("MMM yyyy")
});
var row = Chart.Create().AddGanttTask("Version 4", start.AddMonths(2), start.AddMonths(9));
var track = row.PrepareStrip(
    new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 40), 0)), scale);
var axis = scale.PrepareAxis(
    new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 32), 0)));

var trackSvg = track.ToSvg(new VisualSvgOptions("version-4-track").WithFluidWidth());
var axisSvg = axis.ToSvg(new VisualSvgOptions("support-axis").WithFluidWidth());
```

Give all tracks the same logical width and the same HTML track column. `PrepareStrip` leaves the source chart and scale unchanged. It supports Cartesian and schedule families; the horizontal domain is shared, while the chart's vertical-axis settings still control Cartesian values. Explicit endpoints clip marks without expanding the domain. Data-dependent ordinal time spacing is rejected because different rows would otherwise place the same date at different positions.

Keep the labels in ordinary HTML:

```html
<div class="chart-row">
  <a href="/versions/4">Version 4 — long-term support</a>
  <div class="chart-track"><!-- generated trackSvg --></div>
</div>
<div class="chart-row chart-axis">
  <span aria-hidden="true"></span>
  <div class="chart-track"><!-- generated axisSvg --></div>
</div>
```

```css
.chart-row { display: grid; grid-template-columns: 13rem minmax(0, 1fr); gap: 1rem; align-items: center; }
.chart-track { min-width: 0; }
@media (max-width: 600px) {
  .chart-row { grid-template-columns: minmax(0, 1fr); gap: .35rem; }
  .chart-axis > span { display: none; }
}
```

Labels then wrap above their tracks on phones. The SVG has no label column to shrink or reserve.

## Fluid width and proportional responsive SVG

`WithFluidWidth()` fills the container horizontally at the prepared logical height. Text stays at its prepared CSS size, circles stay round, marker paths keep their size and strokes use `vector-effect="non-scaling-stroke"`. Rectangles, paths and horizontal positions follow the container width. No resize observer or script runs.

Fluid output does not select fewer ticks, wrap labels again or change the number of rows. Choose the tick count for the smallest intended container, and use HTML labels for longer text. It supports Cartesian, schedule and strip scenes, including rectangular and rounded-rectangle clips and horizontal fades. Polar geometry, images, retained effects, pattern tiles, arbitrary path clips and rotated non-text geometry fail with `NotSupportedException`; use ordinary SVG for those scenes. The existing `responsive: true` mode scales the whole image proportionally and remains available. Fluid width takes precedence if both are selected.

## Page colours and fonts

Name a custom property on the colour itself, with its channels as the fallback:

```csharp
var accent = ChartColor.FromHex("#2563EB").WithCssVariable("--accent");
var critical = ChartColor.FromHex("#2563EB").WithCssVariable("--critical");
chart.Series[0].Color = accent;
chart.Series[0].WithPointColor(0, critical);

var policy = new VisualSvgOptions("activity-daily")
    .WithFontVariables(familyVariable: "--body-font", sizeVariable: "--chart-font-size");
```

These colours keep separate variable names even when their fallbacks are identical. Alpha changes retain the variable and apply opacity to the browser-resolved colour. PNG uses the fallback channels. The older `SvgColorVariables` export map remains useful for existing theme-role mappings; a directly named variable takes precedence.

```css
:root { --accent: #2563eb; --critical: #c53636; --body-font: system-ui, sans-serif; --chart-font-size: 14px; }
@media (prefers-color-scheme: dark) {
  :root { --accent: #82adff; --critical: #ff9292; }
}
```

`WithInheritedFonts()` inherits both family and size from the page. `WithFontVariables()` can name either property and inherits the omitted one. Layout still uses the font metrics supplied during preparation. Choose matching preparation and page fonts when exact text fitting matters; changing CSS fonts does not remeasure or reflow the prepared chart. Native glyph contours, captured text images and raster exports retain their prepared fonts.

## Shared design roles

CanopyX and HtmlForgeX pages define the shared `--hfx-*` design roles (surfaces, text, series, ramps, severity and outcome colours, `--hfx-font-sans`). `WithSharedDesignRoles()` writes the chart's colours as those properties, with the chart theme's values as fallbacks, and sets text to `var(--hfx-font-sans, inherit)` while keeping its prepared sizes. The [design language](design-language.md) lists the roles.

```csharp
var chart = Chart.Create().AddBar("Updates", updates).AddBar("Fixes", fixes);
var svg = chart.Prepare(new VisualRenderContext()).ToSvg(
    new VisualSvgOptions("weekly-activity").WithSharedDesignRoles());
// fill="var(--hfx-color-series-1, #2A78D6)", stroke="var(--hfx-color-surface-line, #E2E4E7)", ...
```

That is all the page needs. Inside a CanopyX or HtmlForgeX page, the chart follows the page's theme, including its dark mode, without further configuration. A page that defines none of the properties shows the default Graphite colours and its own font. For a chart prepared with another theme or mode, pass them: `WithSharedDesignRoles(theme, VisualThemeMode.Dark)`. `chart.WithSvgColorVariables(SvgColorVariables.SharedRoles(theme, mode))` applies the same colours to `chart.ToSvg()`, and visual blocks accept them through `WithSvgColorVariables(...)`. PNG, email and Office output use the resolved theme colours.

Series keep their palette slot: series 3 writes `--hfx-color-series-3` even when its colour equals slot 1. Light and dark are separate token sets selected by `ThemeMode`; nothing is inverted automatically. A brand theme is token values only:

```csharp
var brand = VisualTheme.Graphite()
    .WithPalette(VisualThemeMode.Light, ChartPalettes.FromHex("#0b5cad", "#00897b", "#6a4fd8", "#a0522d", "#b03a8c", "#4f7a00"))
    .WithPalette(VisualThemeMode.Dark, ChartPalettes.FromHex("#4d8fe0", "#26a69a", "#8f7bf0", "#c06a42", "#cc5aa6", "#73a12a"));
chart.WithTheme(brand);
// Or load a complete light/dark token document: VisualTheme.FromPaletteJson(File.ReadAllText("brand.tokens.json"))
```

Validate a new categorical set in both modes before you ship it. The default order is the validated set from the design language. Status colours are kept out of the series cycle.

## Time overlays and open support windows

Existing `WithGanttToday(...)` and `WithGanttLaneNow(...)` supply an explicit current instant. `AddVerticalBand(start, end, ...)` highlights a selected interval behind schedule marks as well as Cartesian marks. Supply dates using the chart family's documented time convention; classic Gantt uses wall-clock OLE dates, while Gantt lanes use UTC instants.

```csharp
var support = ChartSeries.CreateGanttTask(new ChartSeriesId("version-4"), "Version 4", start, end);
support.ReplaceGanttItems(new[] { ChartGanttItem.OpenEnded(start) });
support.WithRowOptions(new ChartMarkOptions(href: "/versions/4", current: true));

var supportChart = Chart.Create().AddSeries(support)
    .WithXAxisBounds(start.ToOADate(), end.ToOADate())
    .WithGanttOpenEndStyle(ChartGanttOpenEndStyle.Fade)
    .WithGanttToday(start.AddMonths(6));
```

An open classic task requires an explicit X-axis maximum and fades or points toward that display endpoint. The endpoint is a rendering window, not an authored end date. A Gantt-lane item with `end: null` retains its existing current-time endpoint policy and can use the same fade style. Export never reads the clock; rendering the same prepared scene is deterministic.

## Data tables and SSR output

`HtmlChartRenderer` can place the chart's authored display data next to the SVG:

```csharp
using ChartForgeX.Html;

var fragment = new HtmlChartRenderer().RenderFragment(chart,
    new VisualSvgOptions("activity-daily"), ChartDataTableMode.Details);
// Use VisuallyHidden for a screen-reader table that becomes visible in print.
```

The table uses captions, column headers and row headers, with escaped labels and values. The default `None` mode preserves the ordinary chart fragment. Tables describe the authored display observations; explicit decimation or aggregation still determines which observations the chart retains.

The fallback covers numeric point series, sparklines, schedules, comparison ranges, calendar heatmaps and numeric/categorical matrix rows. Requesting a fallback for another typed family throws `NotSupportedException` rather than silently omitting its observations.

For an MVC/Razor page, insert the generated fragment with `@Html.Raw(Model.ChartHtml)`. For a static Blazor component, use `@((MarkupString)ChartHtml)`. Treat only ChartForgeX-generated markup as trusted: keep page content in normal encoded bindings and provide navigation URLs from the host's own routing rules.

## ID scopes, CSP and testing

Pass a unique, stable `idPrefix` for every occurrence on a page, including repeated equal charts. An automatic content namespace is deterministic, so two identical occurrences otherwise reuse definition IDs. Use a route-safe prefix such as `release-4-track`; keep source record identifiers in validated host data attributes.

Static SVG, native links, fluid tracks and data tables need no inline script. The static output includes SVG presentation/style attributes, and table fallbacks include scoped CSS. Account for those styles in the page's `style-src` policy; this API does not supply a CSP nonce. Interactive adapters have a separate script policy and are optional.

Test links and hooks against the [DOM contract](https://chartforgex.com/docs/dom-contract/), not child order or generated definition IDs. Check each link's URL, target, accessible label and tab stop; check host attributes verbatim; resize the HTML track column and measure text and marker dimensions. Inspect desktop and phone output in both page themes, and test the actual rendered page's keyboard navigation and data disclosure. Try the [linked columns, support schedule and shared strips](https://chartforgex.com/examples/generated/embedding/light.html), or open their companion source-and-output story.

## Optional enhancement

The page stays complete without script. To add hover and focus states, tooltips, keyboard navigation, legend toggles and linked views (for example with a CanopyX grid), export with `WithEnhanceHooks()`, give marks record `keys` and `fields` in `ChartMarkOptions`, and include the [enhance runtime](enhance.md) once per page.

## Updating existing applications

`ChartPoint` and `ChartColor` now contain optional managed metadata, so they are no longer unmanaged memory records. Code that treated point or colour arrays as raw memory must use the public coordinates or packed/RGBA colour conversions instead. The existing `SparklineData` and `ChartGanttItem` constructor signatures remain available to previously compiled consumers.
