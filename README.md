# ChartForgeX

Dependency-free chart rendering for .NET reports, dashboards, documentation, and static HTML output.

The goal is not to clone ScottPlot. The goal is to provide a beautiful, embeddable, no-JavaScript charting layer that HtmlForgeX, DomainDetective, TestimoX, GPOZaurr, ADEssentials, and OfficeIMO-style outputs can reuse.

## Targets

ChartForgeX builds for `net472`, `netstandard2.0`, `net8.0`, and `net10.0`.

The library has no runtime package dependencies. The `net472` target uses `Microsoft.NETFramework.ReferenceAssemblies.net472` only as a private build-time reference so the project can compile on non-Windows machines.

## Install

```powershell
dotnet add package ChartForgeX
```

For local development from this repository, reference `ChartForgeX/ChartForgeX.csproj` directly for static rendering, add `ChartForgeX.Interactivity.Html/ChartForgeX.Interactivity.Html.csproj` when testing the optional HTML adapter, and run the quality loop before publishing packages.

Contribution and release notes live in `CONTRIBUTING.md`, `RELEASING.md`, and `CHANGELOG.md`.
The dependency-free rendering quality direction is documented in `docs/dependency-free-quality-roadmap.md`.

## Quality gates

Warnings are treated as errors from day one, including XML documentation warnings for public APIs. The repository also includes a smoke test suite that verifies static SVG/HTML/PNG rendering behavior, keeps source files under the architecture line budget, rejects `NoWarn`, protects the core package from runtime package dependencies, and guards generated markup against scripts or external resources.

Run the smoke suite directly with:

```powershell
dotnet test .\ChartForgeX.Tests\ChartForgeX.Tests.csproj -c Release
```

Run the full local quality loop with:

```powershell
./Build.ps1
```

That restores, builds, runs smoke tests through `dotnet test`, regenerates example chart outputs and the static example gallery, packs `ChartForgeX`, `ChartForgeX.Interactivity`, and `ChartForgeX.Interactivity.Html` into `artifacts/packages/Release`, creates matching `.snupkg` symbol packages, verifies package contents and dependency invariants, and installs the freshly packed HTML interactivity adapter into a clean temporary console app. The generated demo entry points are `ChartForgeX.Examples/bin/Release/net8.0/output/index.html`, `ChartForgeX.Examples/bin/Release/net8.0/output/catalog.html`, `ChartForgeX.Examples/bin/Release/net8.0/output/quality-dashboard.html`, `ChartForgeX.Examples/bin/Release/net8.0/output/svg-png-comparison.html`, `ChartForgeX.Examples/bin/Release/net8.0/output/domain-security-interactive.html`, and `ChartForgeX.Examples/bin/Release/net8.0/output/executive-interactive-dashboard.html`. The catalog includes map/geography, theme, brand-kit, palette-swatch, pictorial-symbol, pictorial-Isotype, point-color customization, word-cloud-control, and interactive demo pages so visual choices can be reviewed in HTML, SVG, PNG, and opt-in self-contained HTML interactivity.

The GitHub Actions workflow is configured for private repositories and requires a self-hosted runner with the labels `self-hosted` and `private`.

## Design principles

- Zero runtime dependencies in the core package.
- SVG-first rendering for beautiful static HTML reports.
- PNG export with real alpha transparency.
- One chart model, multiple renderers.
- JavaScript-free by default.
- Public chart, option, theme, and primitive APIs fail fast on invalid values.
- Optional interactive renderers live in adapter packages without changing static renderer behavior.
- Themes are first-class, especially polished dark/light report modes.

## Current package layout

```text
ChartForgeX
├── ChartForgeX                 # library
│   ├── Core                    # chart model, series, options
│   ├── Primitives              # colors, points, rects, padding
│   ├── Themes                  # light/dark themes
│   ├── Svg                     # beautiful SVG renderer
│   ├── Html                    # standalone page and HTML fragment renderer
│   └── Raster                  # minimal PNG renderer and PNG writer
├── ChartForgeX.Interactivity    # host-neutral interaction contracts
├── ChartForgeX.Interactivity.Html # self-contained HTML/SVG adapter
├── ChartForgeX.Examples         # sample console app
├── docs
└── Build.ps1
```

## Example

```csharp
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

var chart = Chart.Create()
    .WithTitle("Domain Security Checks")
    .WithSubtitle("Dependency-free SVG, HTML and PNG chart rendering")
    .WithXAxis("Run")
    .WithYAxis("Checks")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(1180, 640)
    .WithTransparentBackground(true)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Next")
    .AddSmoothArea("Passed", Points(820, 940, 980, 1040, 1120, 1180, 1230, 1260))
    .AddSmoothLine("Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36))
    .AddSmoothLine("Failed", Points(22, 30, 28, 21, 18, 15, 13, 10), ChartColor.FromRgb(248, 113, 113))
    .AddHorizontalLine(100, "warning target", ChartColor.FromRgb(251, 191, 36))
    .AddVerticalBand(6, 7, "weekend", ChartColor.FromRgb(96, 165, 250), 0.10);

chart.SaveSvg("chart.svg");
chart.SaveHtml("chart.html");
chart.SavePng("chart.png");

static IEnumerable<ChartPoint> Points(params double[] y) {
    for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
}
```

Small-multiple reports can compose several charts into one static, responsive HTML page:

```csharp
var report = ChartGrid.Create()
    .WithTitle("Control Scorecards")
    .WithSubtitle("Dependency-free small multiples for generated reports")
    .WithTheme(ChartTheme.ReportLight())
    .WithColumns(2)
    .WithPadding(32)
    .WithPanelSize(520, 320)
    .WithPanelFit(ChartGridPanelFit.Contain)
    .Add(gaugeChart, columnSpan: 2)
    .Add(trendChart)
    .Add(coverageChart)
    .Add(findingsChart)
    .WithPanelSpan(2, columnSpan: 2)
    .WithSharedAxes();

report.SaveHtml("scorecards.html");
report.SaveSvg("scorecards.svg");
report.SavePng("scorecards.png");
```

Use `Add(chart, columnSpan, rowSpan)` or `WithPanelSpan(index, columnSpan, rowSpan)` when a report needs a hero panel, wide trend, or tall narrative chart without creating a new chart type just for layout.

ChartForgeX validates chart data before rendering so invalid report payloads fail close to the caller instead of producing partial markup or malformed PNGs. Public APIs reject non-finite numbers, invalid enum values, empty required data sets, malformed specialized series, negative proportional values, cyclic Sankey flows, and tree data with multiple roots or parents. HTML fragments and grids scope child SVG IDs per render, so repeated or identical charts can be safely embedded on the same page. When embedding raw SVG strings yourself, pass a stable scope such as `chart.ToSvg("panel-a")` or `grid.ToSvg("report-a")` to keep element IDs unique without giving up deterministic output.

For a single chart, `WithXAxisBounds(minimum, maximum)` and `WithYAxisBounds(minimum, maximum)` pin cartesian axes explicitly. `WithAutomaticXAxisBounds()` and `WithAutomaticYAxisBounds()` return them to data-driven scaling.

Use `WithXAxisVisible(false)` or `WithYAxisVisible(false)` when an infographic-style chart should keep one axis worth of labels while removing the other axis line, ticks, and title. Use `WithAxisLines(false)` when tick labels and titles should remain but the axis rules and zero-lines should disappear. For example, horizontal scorecards often keep category labels on the left and hide the numeric value axis plus the remaining axis rule.

Generated numeric x-axis ticks can be formatted independently from y-axis values and data labels. The same formatter is used by numeric timeline and Gantt summaries:

```csharp
var timeline = Chart.Create()
    .WithXAxisBounds(0, 4)
    .WithXAxisValueFormatter(value => "D+" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
    .AddLine("Incidents", new[] {
        new ChartPoint(0, 12),
        new ChartPoint(2, 18),
        new ChartPoint(4, 16)
    });
```

Data labels can be controlled globally or per series:

```csharp
var labels = Chart.Create()
    .WithDataLabels()
    .AddBar("Visible", Points(42))
    .AddLine("Hidden", Points(84));

labels.Series[1].WithDataLabels(false);
```

Series can also be styled after data is added:

```csharp
labels.Series[0]
    .WithColor(ChartColor.FromHex("#EC4899"))
    .WithPointColor(0, "#14B8A6")
    .WithStrokeWidth(4)
    .WithSmooth();

labels.Series[0].UseThemeColor();
```

Bar-line combo charts add a bar series first and render the line on top:

```csharp
var combo = Chart.Create()
    .WithYAxis("Volume")
    .WithSecondaryYAxis("Pass rate", value => value.ToString("0") + "%")
    .WithSecondaryYAxisBounds(0, 100)
    .WithXLabels("Mon", "Tue", "Wed")
    .AddBarLineCombo(
        "Volume",
        Points(4200, 6400, 5800),
        "Pass rate",
        Points(88, 93, 91),
        smoothLine: true,
        lineAxis: ChartAxisSide.Secondary);
```

Column/area and scatter/line helpers are available for common mixed-report panels:

```csharp
var forecast = Chart.Create()
    .WithXLabels("Q1", "Q2", "Q3")
    .AddColumnAreaCombo("Actual", Points(42, 64, 58), "Projected", Points(46, 68, 62));

var observed = Chart.Create()
    .AddScatterLineCombo("Observed", Points(12, 18, 17), "Target", Points(14, 16, 20));
```

## Chart catalog

| Chart type | API | Good for |
| --- | --- | --- |
| Line | `AddLine`, `AddSmoothLine` | Trends, time series, thresholds |
| Step line | `AddStepLine` | State changes, threshold-like movement |
| Area | `AddArea`, `AddStepArea`, `AddSmoothArea`, `AddStackedArea`, `AddSmoothStackedArea` | Filled trends, step-filled states, stacked contribution bands, and cumulative-looking report areas |
| Scatter | `AddScatter`, `AddTrendLine` | Independent point distributions and fitted trend lines |
| Statistical overlays | `AddMeanLine`, `AddMedianLine`, `AddStandardDeviationBand` | Computed mean, median, and sigma report annotations |
| Slope | `AddSlope` | Before/after endpoint comparisons |
| Combo | `AddBarLineCombo`, `AddColumnLineCombo`, `AddBarAreaCombo`, `AddColumnAreaCombo`, `AddScatterLineCombo`, `UseSecondaryYAxis` | Shared-axis or secondary-axis mixed charts for volume, targets, forecasts, and observed-vs-trend panels |
| Bar | `AddBar` | Category comparison, grouped and stacked bars |
| Histogram | `AddHistogram` | Raw numeric distributions |
| Lollipop | `AddLollipop` | Light-weight category comparison |
| Bubble | `AddBubble` | X/Y points with a third size value |
| Error bar | `AddErrorBar` | Point estimates with lower and upper bounds |
| Candlestick / OHLC | `AddCandlestick`, `AddOhlc` | Open/high/low/close windows |
| Range band | `AddRangeBand` | Forecast, tolerance, and confidence envelopes |
| Range area | `AddRangeArea` | Smoothed prediction intervals with emphasized upper and lower bounds |
| Dumbbell | `AddDumbbell` | Before/after or paired-value comparisons |
| Pareto | `AddPareto` | Sorted contributions plus cumulative percentage |
| Range bar | `AddRangeBar` | Min/max or observed interval values |
| Box plot | `AddBoxPlot` | Quartiles, median, whiskers, raw sample summaries |
| Horizontal bar | `AddHorizontalBar`, `WithStackedHorizontalBars` | Long category names and stacked composition across long categories |
| Heatmap | `AddHeatmapRow` | Matrix values and coverage grids |
| Calendar heatmap | `AddCalendarHeatmap`, `ChartCalendarHeatmapItem` | Contribution-style day grids for activity, uptime, and habit tracking |
| Dotted map | `AddDottedMap`, `ChartMapPoint`, `ChartMapViewport`, `WithMapViewport`, `AddMapConnector`, `AddMapRoute`, `AddMapConnectorBetweenPoints`, `AddMapRouteBetweenPoints` | Travel-map and operations-map views with generated land-dot layers, focused regional viewports, highlighted points, and connector routes |
| US state geographic map | `AddUsStateGeoMap`, `ChartRegionMapItem`, `WithMapLabels`, `WithMapScaleLegend` | Projected geographic choropleth maps for state-level dashboards |
| US state tile map | `AddUsStateTileMap`, `ChartRegionMapItem`, `WithMapLabels`, `WithMapScaleLegend` | Dependency-free regional choropleth tile maps for equal-area state dashboards |
| Gauge | `AddGauge` | Single score or KPI |
| Circle | `AddCircle`, `WithCircleStatusLabel`, `WithCircleRadiusScale`, `WithCircleStrokeScale` | Compact single-value progress KPIs with tunable ring sizing |
| Radial bar | `AddRadialBar`, `WithRadialBarCenterLabel`, `WithRadialBarRadiusScale`, `WithRadialBarStrokeScale` | Circular progress rings for multiple KPI percentages with tunable radius and stroke weight |
| Bullet | `AddBullet` | Value, target, and qualitative ranges |
| Waterfall | `AddWaterfall` | Cumulative positive and negative changes |
| Radar | `AddRadar` | Multi-axis profile comparison |
| Polar area | `AddPolarArea` | Equal-angle radial contribution comparison |
| Funnel | `AddFunnel` | Stage drop-off |
| Treemap | `AddTreemap` | Part-to-whole composition across many labeled items |
| Pictorial | `AddPictorial`, `ChartPictorialItem`, `ChartPictorialShape`, `WithPictorialShape`, `WithPictorialColumns`, `WithPictorialMaximum`, `WithPictorialValuePerSymbol`, `WithPictorialValues`, `WithPictorialSymbolScale`, `WithPictorialEmptyOpacity`, `WithPictorialSvgPath` | Icon-row charts for playful scorecards and audience-friendly comparisons. Built-in shapes include `Circle`, `Square`, `Diamond`, `Triangle`, `Star`, `Heart`, `Shield`, `Check`, `Person`, and `PersonDress` |
| Progress bar | `AddProgressBars`, `ChartProgressItem`, `WithProgressMaximum`, `WithProgressValues`, `WithProgressHandles`, `WithProgressBarThickness`, `WithProgressTrackOpacity` | Slider-style or clean horizontal progress rows for infographic controls, survey shares, and compact percent scorecards |
| Word cloud | `AddWordCloud`, `ChartWordCloudItem`, `WithWordCloudFontRange`, `WithWordCloudAngles`, `WithWordCloudMaximumTerms`, `WithWordCloudDensity` | Weighted term clouds for themes, tags, and narrative summaries |
| Timeline | `AddTimelineItem`, `AddTimelineRange` | Date or numeric ranges |
| Gantt | `AddGanttTask`, `AddGanttMilestone`, `WithGanttToday` | Project schedules with progress, dependencies, milestones, and current-date markers |
| Sankey | `AddSankey`, `ChartSankeyLink` | Weighted flows between stages, systems, or outcomes |
| Tree | `AddTree`, `ChartTreeLink` | Hierarchies, ownership maps, and control structures |
| Sunburst | `AddSunburst`, `ChartTreeLink` | Radial partition charts for nested part-to-whole hierarchies |
| Pie and donut | `AddPie`, `AddDonut`, `WithDonutCenterLabel`, `WithDonutCenterText`, `WithDonutInnerRadiusRatio` | Proportional breakdowns with optional custom center text and tunable donut ring thickness |

## Customization cookbook

Use a built-in theme first, then tweak only the tokens that matter for the report audience:

```csharp
var chart = Chart.Create()
    .WithTitle("Adoption Pulse")
    .WithTheme(ChartTheme.Aurora())
    .WithPalette(ChartPalettes.Vivid)
    .AddSmoothLine("Active users", Points(42, 58, 71, 86));
```

When the same colors and type should carry across a report, package them as a brand kit:

```csharp
var brand = ChartBrandKit.Create()
    .WithPalette("#0EA5E9", "#EC4899", "#14B8A6")
    .WithFontFamily(ChartFontStacks.Geometric)
    .WithTextColors(ChartColor.FromHex("#111827"), ChartColor.FromHex("#4B5563"))
    .WithGuideColors(ChartColor.FromHex("#CBD5E1"), ChartColor.FromHex("#64748B"))
    .WithSemanticColors(ChartColor.FromHex("#059669"), ChartColor.FromHex("#F59E0B"), ChartColor.FromHex("#DC2626"))
    .WithSurfaceStyle(ChartSurfaceStyle.Framed);

var brandedChart = Chart.Create()
    .WithBrandKit(brand)
    .AddBar("Launch", Points(42, 58, 71));

var brandedGrid = ChartGrid.Create()
    .WithBrandKit(brand)
    .Add(brandedChart);
```

`ChartBrandKit` works with `WithBrandKit(...)` on charts and grids, and can also be applied directly to a `ChartTheme`. Presets such as `ChartBrandKit.Executive()`, `Product()`, `PeopleInfographic()`, `Editorial()`, and `Accessible()` provide polished starting points.

For publication, executive, or customer-facing output, tune typography and surface shape in one callback:

```csharp
var editorial = Chart.Create()
    .WithTheme(theme => theme
        .WithFontFamily(ChartFontStacks.Serif)
        .WithSurfaceColors(ChartColor.Transparent, ChartColor.White, ChartColor.FromRgb(250, 246, 238), ChartColor.FromRgba(120, 113, 108, 72), ChartColor.FromRgba(168, 162, 158, 58))
        .WithTextColors(ChartColor.FromRgb(28, 25, 23), ChartColor.FromRgb(87, 83, 78))
        .WithGuideColors(ChartColor.FromRgba(168, 162, 158, 74), ChartColor.FromRgba(68, 64, 60, 140))
        .WithSemanticColors(ChartColor.FromRgb(22, 101, 52), ChartColor.FromRgb(180, 83, 9), ChartColor.FromRgb(185, 28, 28))
        .WithTypography(title: 30, subtitle: 14, axisTitle: 12, tickLabel: 11, legend: 12, dataLabel: 11)
        .WithCornerRadius(card: 8, plot: 6)
        .WithStrokeWidth(2.8)
        .WithMarkerRadius(3.5)
        .WithShadowOpacity(0.08)
        .WithSurfaceStyle(ChartSurfaceStyle.Framed))
    .WithXLabels("Q1", "Q2", "Q3", "Q4")
    .AddBar("Revenue", Points(120, 148, 173, 210));
```

Surface presets are available when the chart needs a different container treatment without hand-tuning radii and shadows:

```csharp
var embedded = Chart.Create()
    .WithTheme(theme => theme.WithSurfaceStyle(ChartSurfaceStyle.Bare))
    .AddSmoothLine("Signals", Points(42, 58, 71, 86));

var glassy = Chart.Create()
    .WithTheme(theme => theme
        .WithSurfaceStyle(ChartSurfaceStyle.Glass)
        .WithPalette(ChartPalettes.Pastel))
    .AddArea("Adoption", Points(18, 32, 49, 66));
```

Surface styles include `Default`, `Flat`, `Framed`, `Floating`, `Glass`, `Bare`, and `Compact`.

For multi-panel reports, configure the grid heading/background separately from the child charts:

```csharp
var grid = ChartGrid.Create()
    .WithTitle("Theme Review")
    .WithTheme(theme => theme
        .WithFontFamily(ChartFontStacks.Geometric)
        .WithTypography(title: 28, subtitle: 13, axisTitle: 12, tickLabel: 11, legend: 12, dataLabel: 11))
    .Add(chartA)
    .Add(chartB);
```

Choose a visual system by audience first, then adjust palette or surface details only where the report needs it:

| Report intent | Theme starting point | Brand kit starting point |
| --- | --- | --- |
| Executive summary, board deck, customer PDF | `ChartTheme.ReportLight()` or `ChartTheme.Minimal()` | `ChartBrandKit.Executive()` |
| Product dashboard, launch review, adoption story | `ChartTheme.Aurora()` | `ChartBrandKit.Product()` |
| Demographic infographic, survey poster, people dashboard | `ChartTheme.PeopleInfographic()` | `ChartBrandKit.PeopleInfographic()` |
| Publication, narrative report, static document | `ChartTheme.Editorial()` | `ChartBrandKit.Editorial()` |
| Accessibility-first categorical comparison | `ChartTheme.Colorblind()` | `ChartBrandKit.Accessible()` |
| Playful scorecard, education, pictorial view | `ChartTheme.Candy()` | `ChartBrandKit.Product()` with pictorial shapes |
| Dense operations or command-center view | `ChartTheme.Terminal()` | `ChartBrandKit.Accessible()` with `ChartSurfaceStyle.Compact` |

Themes are complete visual presets for one chart. Brand kits are reusable brand tokens that can be layered onto charts, grids, or themes.

Reusable palette presets are available through `ChartPalettes.Report`, `Colorblind`, `Vivid`, `Pastel`, `PeopleInfographic`, `Editorial`, `Jewel`, and `Terminal`. Each property returns a fresh array, so callers can safely modify a local copy before passing it to `WithPalette(...)`.

You can also paste colors directly from a design tool:

```csharp
var branded = Chart.Create()
    .WithPalette("#0EA5E9", "#EC4899", "#14B8A6", "#F59E0B")
    .AddBar("Launch", Points(42, 58, 71));

var brandedTheme = ChartTheme.Light()
    .WithPalette("#1E40AF", "#BE123C", "#059669");
```

Use `ChartColor.FromHex(...)` and `ChartPalettes.FromHex(...)` for `#RGB`, `#RGBA`, `#RRGGBB`, and `#RRGGBBAA` notation.

SVG and HTML use `ChartTheme.FontFamily` and CSS font stacks. PNG can use a specific `.ttf` or `.ttc` file with `WithPngFont(...)`; otherwise it auto-detects a platform font and then falls back to the built-in tiny chart font.

PNG output uses a dependency-free rasterizer. When a platform TrueType font can be loaded, PNG text is rendered from real font outlines; otherwise ChartForgeX falls back to its built-in tiny chart font. You can prefer a specific `.ttf` file or `.ttc` collection for report consistency:

```csharp
chart.WithPngFont("/Library/Fonts/Arial.ttf")
     .SavePng("chart.png");
```

For TrueType collections, pass the optional zero-based face index when you need a specific face:

```csharp
chart.WithPngFont("/System/Library/Fonts/HelveticaNeue.ttc", collectionIndex: 0)
     .SavePng("chart.png");
```

You can also select by family, subfamily, full, or PostScript face name:

```csharp
chart.WithPngFont("/System/Library/Fonts/HelveticaNeue.ttc", faceName: "Helvetica Neue")
     .SavePng("chart.png");
```

PNG edges are supersampled by default. You can raise or lower the internal supersampling scale when you need to trade render time for raster edge quality:

```csharp
chart.WithPngSupersampling(4)
     .SavePng("chart.png");
```

Supersampling preserves the requested chart dimensions. For high-DPI assets, use a PNG output scale; the chart keeps the same logical layout but emits more pixels:

```csharp
chart.WithPngOutputScale(2)
     .SavePng("chart@2x.png");
```

Named presets are available when you want the export density to read like an intentional quality choice:

```csharp
chart.WithPngOutputScale(ChartPngOutputScale.Retina)       // 2x
     .SavePng("chart@2x.png");

chart.WithPngOutputScale(ChartPngOutputScale.Print)        // 4x
     .SavePng("chart@4x.png");
```

The same output scale is available for composed grid PNG exports:

```csharp
report.WithPngOutputScale(ChartPngOutputScale.Retina)
      .SavePng("scorecards@2x.png");
```

SVG and HTML still use the theme `FontFamily` CSS stack. The PNG font path is optional and falls back automatically when the file is missing or unsupported.

Use `GetPngFontInfo()` when you want to verify which font source PNG rendering will use:

```csharp
var font = chart.GetPngFontInfo();
Console.WriteLine($"{font.Source}: {font.ResolvedFaceName}");
```

Date/time x-values can be used without manual numeric conversion:

```csharp
var dates = new[] {
    new DateTime(2026, 1, 1),
    new DateTime(2026, 1, 2),
    new DateTime(2026, 1, 3)
};

var dated = Chart.Create()
    .WithTitle("Daily Trend")
    .WithXDateLabels(dates, "MMM dd")
    .AddSmoothLine("Checks", dates.Select((date, index) => new ChartPoint(date, 100 + index * 20)));
```

Compact sparklines are available for dashboard cards and table cells:

```csharp
var sparkline = Chart.Create()
    .WithSize(360, 90)
    .WithSparkline()
    .AddSmoothArea("Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36));
```

Dense report axes can be kept readable with automatic x-axis label thinning:

```csharp
var monthly = Chart.Create()
    .WithTitle("Monthly Security Posture")
    .WithXAxisLabelDensity(ChartLabelDensity.Auto)
    .WithXLabels("January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December")
    .AddSmoothLine("Primary domain checks", Points(82, 84, 86, 87, 88, 89, 91, 92, 93, 94, 95, 96));
```

Step line charts are available for state changes and threshold-like report series:

```csharp
var stateChanges = Chart.Create()
    .WithTitle("Policy State Changes")
    .WithXLabels("Draft", "Review", "Approved", "Published")
    .AddStepLine("Controls", Points(12, 18, 18, 26));
```

Step-area charts fill discrete state changes when the magnitude matters:

```csharp
var stepArea = Chart.Create()
    .WithTitle("Policy Backlog")
    .WithXLabels("Draft", "Review", "Approved", "Published")
    .AddStepArea("Open controls", Points(12, 18, 18, 26));
```

Long category labels can be rotated when every label matters:

```csharp
var regional = Chart.Create()
    .WithTitle("Certificate Transparency by Region")
    .WithXAxisLabelDensity(ChartLabelDensity.All)
    .WithXAxisLabelAngle(-35)
    .WithXLabels("North America", "Western Europe", "Central Europe", "Asia Pacific", "Latin America", "Middle East")
    .AddBar("Logged certificates", Points(1200000, 2350000, 1840000, 3120000, 980000, 760000));
```

Multiple bar series render as grouped category bars:

```csharp
var grouped = Chart.Create()
    .WithTitle("Security Findings by Severity")
    .WithXLabels("Critical", "High", "Medium", "Low", "Informational")
    .AddBar("Current run", Points(8, 32, 84, 126, 210))
    .AddBar("Previous run", Points(12, 41, 97, 118, 188));
```

Histograms can bin raw values directly:

```csharp
var latencyDistribution = Chart.Create()
    .WithTitle("Endpoint Latency Distribution")
    .WithXAxis("Latency")
    .WithYAxis("Samples")
    .AddHistogram("P95 samples", new[] { 28d, 31d, 36d, 42d, 47d, 58d, 64d, 72d }, binCount: 4);
```

Lollipop charts provide bar-like comparison with lighter visual weight:

```csharp
var posture = Chart.Create()
    .WithTitle("Control Readiness")
    .WithDataLabels()
    .WithXLabels("SPF", "DMARC", "DNSSEC")
    .AddLollipop("Coverage", Points(96, 88, 74));
```

Bubble charts encode x, y, and marker size in one report panel:

```csharp
var exposure = Chart.Create()
    .WithTitle("Exposure Clusters")
    .WithXAxis("Reachability")
    .WithYAxis("Exploitability")
    .AddBubble("Assets", new[] {
        new ChartBubble(1, 24, 8),
        new ChartBubble(2, 42, 18),
        new ChartBubble(3, 31, 42)
    });
```

Trend lines can be computed from scatter or other cartesian points:

```csharp
var observations = Points(12, 19, 24, 33, 41);
var trend = Chart.Create()
    .WithTitle("Observed Remediation Trend")
    .AddScatter("Observed", observations)
    .AddTrendLine("Trend", observations);
```

Statistical overlays compute report annotations from source points:

```csharp
var observed = Points(12, 19, 24, 33, 41);
var stats = Chart.Create()
    .AddLine("Observed", observed)
    .AddMeanLine("mean", observed)
    .AddMedianLine("median", observed)
    .AddStandardDeviationBand("1 sigma", observed);
```

Error bars show uncertainty around point estimates:

```csharp
var confidence = Chart.Create()
    .WithTitle("Detection Confidence")
    .WithXAxis("Run")
    .WithYAxis("Score")
    .AddErrorBar("Confidence", new[] {
        new ChartErrorBar(1, 42, 35, 51),
        new ChartErrorBar(2, 58, 49, 66),
        new ChartErrorBar(3, 63, 54, 78)
    });
```

Candlestick charts show open, high, low, and close windows:

```csharp
var candles = Chart.Create()
    .WithTitle("Signal Windows")
    .WithXAxis("Window")
    .WithYAxis("Score")
    .AddCandlestick("Window", new[] {
        new ChartCandlestick(1, 42, 51, 35, 48),
        new ChartCandlestick(2, 58, 66, 49, 54),
        new ChartCandlestick(3, 63, 78, 54, 72)
    });
```

OHLC charts use the same `ChartCandlestick` values with compact open and close ticks:

```csharp
var ohlc = Chart.Create()
    .WithTitle("Signal Windows")
    .WithXAxis("Window")
    .WithYAxis("Score")
    .AddOhlc("Window", new[] {
        new ChartCandlestick(1, 42, 51, 35, 48),
        new ChartCandlestick(2, 58, 66, 49, 54),
        new ChartCandlestick(3, 63, 78, 54, 72)
    });
```

Range-band charts draw forecast, tolerance, or confidence envelopes:

```csharp
var forecast = Chart.Create()
    .WithTitle("Forecast Envelope")
    .WithXAxis("Run")
    .WithYAxis("Expected Range")
    .AddRangeBand("Expected", new[] {
        new ChartRangeBand(1, 32, 44),
        new ChartRangeBand(2, 38, 58),
        new ChartRangeBand(3, 51, 72)
    });
```

Range-area charts are interval envelopes with stronger area emphasis and optional smoothing:

```csharp
var interval = Chart.Create()
    .WithTitle("Forecast Interval")
    .WithXAxis("Run")
    .WithYAxis("Expected Range")
    .AddRangeArea("Prediction interval", new[] {
        new ChartRangeBand(1, 32, 44),
        new ChartRangeBand(2, 38, 58),
        new ChartRangeBand(3, 51, 72)
    });
```

Dumbbell charts compare paired values across categories:

```csharp
var remediation = Chart.Create()
    .WithTitle("Remediation Lift")
    .WithXLabels("SPF", "DMARC", "DNSSEC")
    .AddDumbbell("Before/after", new[] {
        new ChartDumbbell(1, 32, 44),
        new ChartDumbbell(2, 38, 58),
        new ChartDumbbell(3, 51, 72)
    });
```

Pareto charts sort raw category values and add a cumulative percentage line:

```csharp
var pareto = Chart.Create()
    .WithTitle("Findings Pareto")
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddPareto("Findings", new[] {
        new ChartParetoItem("Critical", 50),
        new ChartParetoItem("Medium", 30),
        new ChartParetoItem("Low", 20)
    });
```

Range bars show interval values such as min/max observations:

```csharp
var latencyRanges = Chart.Create()
    .WithTitle("Endpoint Latency Ranges")
    .WithXLabels("DNS", "TCP", "TLS")
    .AddRangeBar("Observed", new[] {
        new ChartInterval(1, 20, 42),
        new ChartInterval(2, 44, 88),
        new ChartInterval(3, 96, 142)
    });
```

Box plots summarize distributions with whiskers, quartiles, and medians:

```csharp
var latencySpread = Chart.Create()
    .WithTitle("Endpoint Latency Spread")
    .WithXLabels("DNS", "TCP")
    .AddBoxPlot("Latency", new[] {
        new ChartBoxPlot(1, 18, 24, 31, 38, 48),
        new ChartBoxPlot(2, 42, 56, 64, 82, 104)
    });
```

Box plots can also compute the five-number summary from raw samples:

```csharp
var rawLatency = Chart.Create()
    .WithTitle("Raw Latency Spread")
    .AddBoxPlot("Latency", 1, new[] { 18d, 24d, 31d, 38d, 48d });
```

Horizontal bars are available for long report categories:

```csharp
var coverage = Chart.Create()
    .WithTitle("Domain Control Coverage")
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF alignment", "DMARC policy enforcement", "DNSSEC coverage")
    .AddHorizontalBar("Coverage", Points(96, 88, 74));
```

Multiple horizontal bar series can be stacked for composition reports:

```csharp
var composition = Chart.Create()
    .WithStackedHorizontalBars()
    .WithStackTotals()
    .WithXLabels("Mail authentication", "Transport security")
    .AddHorizontalBar("Complete", Points(40, 55))
    .AddHorizontalBar("Partial", Points(15, 20))
    .AddHorizontalBar("Missing", Points(5, 10));
```

Heatmaps are available for compact report matrices. Each heatmap series is one row; point `X` chooses the column and point `Y` is the cell value:

```csharp
var matrix = Chart.Create()
    .WithTitle("Control Coverage Matrix")
    .WithDataLabels()
    .WithHeatmapScale(ChartHeatmapScale.Semantic)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC")
    .AddHeatmapRow("Primary domains", Points(96, 88, 74))
    .AddHeatmapRow("Parked domains", Points(74, 62, 51));
```

Use `WithHeatmapScaleLegend(false)` and `WithHeatmapColumnLabels(false)` for compact scorecard-style heatmaps when row labels and data labels already explain the scale.

Calendar heatmaps render date-indexed values as contribution-style day grids. Duplicate dates are summed so callers can pass event-level data without pre-aggregation:

```csharp
var journey = Chart.Create()
    .WithTitle("Consistency Journey")
    .AddCalendarHeatmap("Commits", new[] {
        new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 1),
        new ChartCalendarHeatmapItem(new DateTime(2026, 1, 6), 4),
        new ChartCalendarHeatmapItem(new DateTime(2026, 2, 12), 7)
    });
```

Calendar heatmap SVG output marks padded or missing days with `data-cfx-empty="true"` and `data-cfx-status="empty"` so consumers can distinguish no data from an explicit zero value. Calendar heatmap cells also expose `data-cfx-week-index` and `data-cfx-weekday-index` for downstream hover, selection, or export workflows. Calendar heatmap containers expose `data-cfx-label`, `data-cfx-start-date`, `data-cfx-end-date`, and filled/empty day counts; when missing days are present, the scale includes a separate no-data swatch before the real low-to-high value ramp.

Dotted maps render a dependency-free dotted world land layer with highlighted longitude/latitude points:

```csharp
var travel = Chart.Create()
    .WithTitle("Travel Map")
    .WithMapViewport(ChartMapViewport.Europe())
    .AddDottedMap("Visited", new[] {
        new ChartMapPoint("Spain", -3.7038, 40.4168),
        new ChartMapPoint("Poland", 21.0122, 52.2297),
        new ChartMapPoint("Norway", 10.7522, 59.9139)
    })
    .AddMapRouteBetweenPoints("Spain to Poland", "Spain", "Poland");
```

Pass a non-negative value to `ChartMapPoint` when the same map should read as a market, country, or city revenue map. Weighted points scale their marker radius and expose both raw and formatted values in SVG metadata and hover titles:

```csharp
var markets = Chart.Create()
    .WithTitle("Revenue by European Market")
    .WithMapViewport(ChartMapViewport.Europe())
    .WithValueFormatter(value => "$" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "k")
    .AddDottedMap("Revenue", new[] {
        new ChartMapPoint("United Kingdom", -1.1743, 52.3555, 188),
        new ChartMapPoint("Poland", 19.1451, 51.9194, 142),
        new ChartMapPoint("Germany", 10.4515, 51.1657, 214)
    })
    .AddMapRouteBetweenPoints("United Kingdom to Poland", "United Kingdom", "Poland");
```

Use `ChartMapViewport.World()`, `Europe()`, `NorthAmerica()`, `SouthAmerica()`, `Africa()`, `Asia()`, `Oceania()`, `Poland()`, or `new ChartMapViewport(...)` to focus the dotted layer on a continent, country, or custom bounding box. Route and connector lines render on dotted maps when both endpoints are inside the selected viewport. Prefer `AddMapRouteBetweenPoints` or `AddMapConnectorBetweenPoints` when the line should target existing rendered markers; use coordinate-based `AddMapRoute` and `AddMapConnector` for arbitrary longitude/latitude overlays.

US state geographic maps render projected state outlines with Alaska and Hawaii included:

```csharp
var states = Chart.Create()
    .WithTitle("Revenue by state")
    .WithMapLabels(false)
    .AddUsStateGeoMap("Revenue", new[] {
        new ChartRegionMapItem("California", 95),
        new ChartRegionMapItem("New York", 82),
        new ChartRegionMapItem("TX", 74),
        new ChartRegionMapItem("FL", 61)
    });
```

US state tile maps render state and DC values as a compact equal-area choropleth grid. This is a cartogram, not a geographic outline map:

```csharp
var states = Chart.Create()
    .WithTitle("Revenue by state")
    .AddUsStateTileMap("Revenue", new[] {
        new ChartRegionMapItem("California", 95),
        new ChartRegionMapItem("New York", 82),
        new ChartRegionMapItem("TX", 74),
        new ChartRegionMapItem("FL", 61)
    });
```

US state maps accept two-letter state codes, full state names, and common District of Columbia aliases, normalizing all rendered metadata to canonical codes.
Geographic state-map labels render only when a region has enough room for the two-letter code. Use `WithMapLabels(false)` or `WithMapScaleLegend(false)` when a map is embedded in a compact card and hover/focus titles can carry the region identity.
Map SVG containers expose summary metadata such as `data-cfx-label`, `data-cfx-projection`, `data-cfx-map-kind`, `data-cfx-point-count`, `data-cfx-visible-point-count`, `data-cfx-valued-point-count`, `data-cfx-viewport`, region coverage counts, and `data-cfx-min-value`/`data-cfx-max-value` for value-colored maps, while highlighted points, connectors, and regions remain keyboard-focusable or hover-descriptive and include native hover titles.

Gauges are available for single-value dashboard summaries:

```csharp
var score = Chart.Create()
    .WithTitle("Security Posture Score")
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddGauge("Overall domain readiness", 87, 0, 100);
```

Bullet charts are available when a report needs value-versus-target rows with qualitative bands:

```csharp
var targets = Chart.Create()
    .WithTitle("Control Targets")
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddBullet("DMARC enforcement", 88, 95, 0, 100, new[] { 60d, 80d })
    .AddBullet("DNSSEC coverage", 74, 90, 0, 100, new[] { 55d, 78d });
```

Circle charts show compact single-value progress KPIs:

```csharp
var ready = Chart.Create()
    .WithTitle("Policy Readiness")
    .WithCircleRadiusScale(1.12)
    .WithCircleStrokeScale(1.25)
    .AddCircle("Ready", 87, 0, 100);
```

Radial bars use the same scale approach for denser or airier multi-ring KPI panels:

```csharp
var coverage = Chart.Create()
    .WithTitle("Coverage Rings")
    .WithRadialBarRadiusScale(1.08)
    .WithRadialBarStrokeScale(1.18)
    .AddRadialBar("Average coverage", Points(92, 74, 88, 96));
```

Waterfall charts are available for explaining cumulative change:

```csharp
var impact = Chart.Create()
    .WithTitle("Remediation Impact")
    .WithDataLabels()
    .WithXLabels("Opened", "Resolved", "Suppressed", "Accepted")
    .AddWaterfall("Finding delta", Points(24, -68, -18, -9));
```

Radar charts are available for posture and scorecard comparisons:

```csharp
var posture = Chart.Create()
    .WithTitle("Security Posture Radar")
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy", "Monitoring")
    .AddRadar("Current posture", Points(92, 74, 88, 96, 81, 84))
    .AddRadar("Target posture", Points(96, 90, 94, 98, 92, 90));
```

Polar area charts use equal-angle radial segments whose radius is scaled by value:

```csharp
var polar = Chart.Create()
    .WithTitle("Control Contribution")
    .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT")
    .AddPolarArea("Control share", Points(92, 74, 88, 96));
```

Funnel charts are available for staged report flows:

```csharp
var funnel = Chart.Create()
    .WithTitle("Domain Remediation Funnel")
    .WithXLabels("Discovered", "Verified", "Prioritized", "Remediated")
    .AddFunnel("Domains", Points(420, 318, 174, 96));
```

Timeline charts render date or numeric ranges:

```csharp
var remediation = Chart.Create()
    .WithTitle("Domain Remediation Timeline")
    .WithDataLabels()
    .AddTimelineItem("Certificate renewal", new DateTime(2026, 1, 4), new DateTime(2026, 2, 10))
    .AddTimelineItem("DMARC enforcement", new DateTime(2026, 1, 18), new DateTime(2026, 3, 5));
```

Gantt charts add schedule progress, dependencies, milestones, and current-date markers:

```csharp
var plan = Chart.Create()
    .WithTitle("Domain Remediation Gantt")
    .WithDataLabels()
    .WithGanttToday(new DateTime(2026, 2, 18))
    .AddGanttTask("Inventory scope", new DateTime(2026, 1, 5), new DateTime(2026, 1, 24), 0.9)
    .AddGanttTask("Owner remediation", new DateTime(2026, 1, 20), new DateTime(2026, 2, 24), 0.62, dependsOn: 0)
    .AddGanttMilestone("Executive sign-off", new DateTime(2026, 3, 6), dependsOn: 1);
```

Sankey charts show weighted movement between named stages:

```csharp
var flow = Chart.Create()
    .WithTitle("Finding Flow")
    .WithDataLabels()
    .AddSankey("Findings", new[] {
        new ChartSankeyLink("Discovered", "Validated", 70),
        new ChartSankeyLink("Validated", "Remediated", 44),
        new ChartSankeyLink("Validated", "Monitoring", 26)
    });
```

Tree charts render parent-child hierarchy maps:

```csharp
var hierarchy = Chart.Create()
    .WithTitle("Control Hierarchy")
    .AddTree("Controls", new[] {
        new ChartTreeLink("Security posture", "Mail authentication"),
        new ChartTreeLink("Security posture", "Certificate lifecycle"),
        new ChartTreeLink("Mail authentication", "SPF"),
        new ChartTreeLink("Mail authentication", "DKIM")
    });
```

Sunburst charts use the same parent-child link model to show nested composition as radial partition rings:

```csharp
var partition = Chart.Create()
    .WithTitle("Control Coverage Partition")
    .WithTheme(ChartTheme.Aurora())
    .AddSunburst("Controls", new[] {
        new ChartTreeLink("Security posture", "Mail authentication", 42),
        new ChartTreeLink("Security posture", "Certificate lifecycle", 28),
        new ChartTreeLink("Mail authentication", "SPF", 16),
        new ChartTreeLink("Mail authentication", "DKIM", 14)
    });
```

Pictorial charts turn values into repeated built-in symbols:

```csharp
var audience = Chart.Create()
    .WithTitle("Audience Mix")
    .WithTheme(ChartTheme.Candy())
    .WithPictorialColumns(10)
    .WithPictorialMaximum(100)
    .WithPictorialValuePerSymbol(10)
    .WithPictorialValues(false)
    .WithPictorialSymbolScale(1.15)
    .WithPictorialEmptyOpacity(0.12)
    .WithPictorialSvgPath("M12 2 L15 9 L22 9 L17 14 L19 22 L12 17 L5 22 L7 14 L2 9 L9 9 Z", ChartPictorialShape.Star)
    .AddPictorial("Segments", new[] {
        new ChartPictorialItem("New users", 84, ChartColor.FromRgb(14, 165, 233)),
        new ChartPictorialItem("Returning users", 62, ChartColor.FromRgb(236, 72, 153)),
        new ChartPictorialItem("Advocates", 29, ChartColor.FromRgb(20, 184, 166))
    }, ChartPictorialShape.Person);
```

Use `WithPictorialColumns(5)` for rating-style rows, `10` for percentage-style scorecards, or a denser count when a report needs finer-grained comparison. Add `WithPictorialMaximum(5)` for fixed five-star scales or `WithPictorialMaximum(100)` for true percentage rows; omit it when each chart should scale to the largest item. Fractional values render as partial symbol fills in SVG, HTML, and PNG. Use `WithPictorialValuePerSymbol(...)` when one icon should represent an absolute unit such as one person, 5,000 residents, or 10%; values larger than one configured row wrap into additional symbol rows automatically. Use `WithPictorialValues(false)` for clean Isotype-style rows where the repeated symbols carry the number. `WithPictorialSymbolScale(...)` makes symbols feel denser or airier, and `WithPictorialEmptyOpacity(...)` tunes how strongly unfilled symbols remain visible. Each `ChartPictorialItem` can carry its own color.

Use `WithPictorialSvgPath` to bring a custom filled SVG path into SVG and HTML output. PNG output remains dependency-free by using the fallback `ChartPictorialShape` supplied to the method. Use `WithPictorialShape(...)` after `AddPictorial(...)` to switch an existing chart back to one of the built-in symbols; it clears any custom SVG path.

Progress bars render slider-style infographic rows:

```csharp
var preference = Chart.Create()
    .WithTitle("Preference Sliders")
    .WithTheme(ChartTheme.PeopleInfographic())
    .WithProgressBarThickness(0.28)
    .WithProgressTrackOpacity(0.20)
    .WithValueFormatter(value => value.ToString("0") + "%")
    .AddProgressBars("Preference", new[] {
        new ChartProgressItem("Male", 40, ChartColor.FromHex("#06B6D4")),
        new ChartProgressItem("Female", 60, ChartColor.FromHex("#DB2777"))
    });
```

Use `WithProgressMaximum(...)` when rows compare against something other than 100, `WithProgressValues(false)` when the slider handle is enough, `WithProgressHandles(false)` for clean completion bars, and `WithProgressBarThickness(...)` / `WithProgressTrackOpacity(...)` to tune dense KPI rows without introducing a separate chart type.

Word clouds place weighted terms deterministically, so static reports stay reproducible:

```csharp
var topics = Chart.Create()
    .WithTitle("Support Themes")
    .WithTheme(ChartTheme.Editorial())
    .WithWordCloudFontRange(14, 52)
    .WithWordCloudAngles(-18, 0, 12)
    .WithWordCloudMaximumTerms(24)
    .WithWordCloudDensity(0.9)
    .AddWordCloud("Themes", new[] {
        new ChartWordCloudItem("Automation", 100, ChartColor.FromRgb(30, 64, 175)),
        new ChartWordCloudItem("Reports", 82, ChartColor.FromRgb(190, 18, 60)),
        new ChartWordCloudItem("Charts", 64),
        new ChartWordCloudItem("DNS", 48)
    });
```

Use `WithWordCloudFontRange` to make the weight contrast calmer or louder, `WithWordCloudAngles` to choose a restrained editorial layout or a more playful rotated pattern, `WithWordCloudMaximumTerms` to curate noisy text sources, and `WithWordCloudDensity` from `0.5` to `2.0` to move from spacious editorial layouts to denser poster-style clouds. Optional `ChartWordCloudItem` colors emphasize selected terms.

They can also be stacked when the report needs category totals:

```csharp
var stacked = Chart.Create()
    .WithTitle("Domain Findings Composition")
    .WithStackedBars()
    .WithStackTotals()
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
    .AddBar("Passed", Points(180, 220, 245, 260, 280))
    .AddBar("Warnings", Points(42, 38, 32, 28, 24))
    .AddBar("Failed", Points(12, 10, 8, 6, 5));
```

Report-specific units can be applied with a custom value formatter:

```csharp
var latency = Chart.Create()
    .WithTitle("Endpoint Latency")
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
    .WithXLabels("DNS", "TCP", "TLS", "HTTP", "Render")
    .AddSmoothLine("P95", Points(28, 64, 118, 146, 182));
```

SVG layout reserves extra plot space when formatted y-axis labels are long, so currency and unit-heavy labels do not spill into the card margin.

Typography defaults to a native system font stack for crisp SVG output. Reports can override it per chart:

```csharp
chart.WithFontFamily("-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif");
```

ChartForgeX also includes expressive, dependency-free style presets for different audiences:

```csharp
var boardDeck = Chart.Create()
    .WithTheme(ChartTheme.Editorial())
    .AddSmoothLine("Revenue", Points(120, 148, 173, 210));

var productPulse = Chart.Create()
    .WithTheme(ChartTheme.Aurora())
    .WithPalette(ChartPalettes.Vivid)
    .AddBar("Adoption", Points(42, 58, 71, 86));
```

Built-in themes include `ReportLight()`, `ReportDark()`, `Colorblind()`, `Aurora()`, `Editorial()`, `Candy()`, `Terminal()`, and `Minimal()`. Font helpers such as `ChartFontStacks.Serif`, `ChartFontStacks.Geometric`, `ChartFontStacks.Rounded`, and `ChartFontStacks.Mono` keep SVG/HTML typography readable without bundling external assets:

```csharp
chart.WithTheme(theme => theme
        .WithFontFamily(ChartFontStacks.Geometric)
        .WithTypography(title: 30, subtitle: 14, axisTitle: 12, tickLabel: 11, legend: 12, dataLabel: 11)
        .WithCornerRadius(card: 20, plot: 14)
        .WithStrokeWidth(3.4)
        .WithSurfaceStyle(ChartSurfaceStyle.Floating));
```

Place legends where the chart has room with `WithLegendPosition(...)`. `ChartLegendPosition` supports `Bottom`, `Top`, `Left`, `Right`, `TopLeft`, `TopRight`, `BottomLeft`, and `BottomRight`; renderers reserve the matching lane so long labels truncate inside the chart instead of touching plot borders. Use `WithPointLegend()` for single-series charts where individual point colors need their own legend keys.

Use role-based text styles when a report needs more visual personality without forking chart types. `ChartTextRole` and `ChartTextStyle` power the generic `WithTextStyle(...)` API plus convenience helpers such as `WithTitleStyle`, `WithSubtitleStyle`, `WithAxisTitleStyle`, `WithTickLabelStyle`, `WithLegendStyle`, and `WithDataLabelStyle`:

```csharp
chart
    .WithTitleStyle(style => style
        .WithColor("#be123c")
        .WithFontFamily("Georgia, 'Times New Roman', serif")
        .WithWeight("900")
        .WithItalic()
        .WithUnderline())
    .WithTickLabelStyle(style => style.WithColor("#2563eb").WithItalic())
    .WithLegendStyle(style => style.WithColor("#15803d").WithUnderline())
    .WithDataLabelStyle(style => style.WithColor("#b45309").WithWeight("800"));
```

SVG and HTML honor color, font family, weight, italic, underline, and role font size. PNG keeps its dependency-free text renderer but honors role color, role font size, and underline where the raster text path supports it.
Use `chart.Series[index].WithPointColor(pointIndex, "#14B8A6")` when one bar, horizontal bar, scatter marker, line marker, lollipop mark, bubble, error-bar mark, range-bar interval, dumbbell comparison, box-plot summary, candlestick/OHLC window, slope endpoint, pie/donut slice, polar-area segment, funnel segment, treemap tile, pictorial item, progress row, or word-cloud term needs its own color while the rest of the series keeps the normal palette. Pie/donut legends and outside callout leaders follow the same point color. Zero-value pie, donut, and polar-area points do not draw visible slices or segments, but point indexes remain stable for labels, colors, offsets, legends, and exported SVG metadata; zero-value funnel stages render as explicit stages.
Use `chart.Series[index].WithDataLabelStyle(...)` when one series should call attention to its labels without changing every label in the chart. These overrides apply to cartesian labels and specialized labels such as pie slices, heatmap cells, radar points, and waterfall deltas. Use `WithPointDataLabelStyle(pointIndex, ...)` when a single bar, point, slice, cell, or delta needs its own callout styling.
Use `WithDataLabelPlacement(ChartDataLabelPlacement.Center)` at the chart level, or `chart.Series[index].WithDataLabelPlacement(...)` for one series, when labels should sit inside, outside, above, below, left, or right of capable cartesian and specialized marks. Bubbles, error bars, range bands, range areas, range bars, dumbbells, box plots, candlesticks, and OHLC charts honor the same placement model for their intrinsic labels in SVG and PNG. Outside pie/donut labels use side-aware lanes with connector lines, and side heatmap labels include connector ticks in SVG and PNG exports. Tune those connectors with `WithDataLabelConnectorColor(...)`, `WithDataLabelConnectorOpacity(...)`, `WithDataLabelConnectorStrokeWidth(...)`, and `WithDataLabelConnectorStyle(ChartDataLabelConnectorStyle.Curve)` for straight, elbow, or curved leaders. When no explicit connector color is set, pie and donut callout leaders use the matching slice color. Use `WithPieSliceLabelContent(...)` when pie/donut data labels should show percentage, value, category, category plus percentage, or category plus value. Use `WithPieSliceLabelFormatter(slice => slice.Label + ": " + slice.FormattedPercent)` for fully custom pie/donut callout text. Use `WithPieOutsideLabelDistance(...)` to move outside pie/donut labels closer to or farther from the ring, and `chart.Series[index].WithPointSliceOffset(pointIndex, ratio)` to pull out one pie or donut slice for emphasis.

`ChartGrid` also supports `WithTitleStyle(...)` and `WithSubtitleStyle(...)` for styled report headers in SVG, PNG, and static HTML grid exports.

Pie and donut charts use the same model and x-axis labels for slice names:

```csharp
var donut = Chart.Create()
    .WithTitle("Domain Check Result Mix")
    .WithDonutCenterText("94%", "Passed")
    .WithDonutInnerRadiusRatio(0.68)
    .WithXLabels("Passed", "Warnings", "Failed")
    .AddDonut("Checks", Points(1260, 68, 10));
```

## HtmlForgeX integration model

HtmlForgeX should not depend on ApexCharts or Chart.js for static reports. Instead, it can consume ChartForgeX objects directly:

```csharp
var chartHtml = chart.ToHtmlFragment();
```

That gives HtmlForgeX:

- inline SVG charts;
- no external CDN;
- no JavaScript requirement;
- no runtime chart library payload;
- easy email/report/document embedding;
- deterministic output for screenshots and PDFs.

For interactive dashboards, keep the static renderer untouched and opt into host-specific interactivity:

```text
ChartForgeX.Interactivity
ChartForgeX.Interactivity.Html
```

`ChartForgeX.Interactivity` holds host-neutral contracts for tooltips, selection, legend toggles, keyboard navigation, zoom, pan, brush ranges, synchronized charts, and export commands. `ChartForgeX.Interactivity.Html` is the first adapter and emits self-contained HTML/SVG with small vanilla JavaScript for report-friendly tooltips, click/keyboard selection, legend toggles, zoom buttons, wheel zoom, pan mode, brush-region selection events, same-page chart group synchronization, inline SVG/PNG download, and reset controls:

```csharp
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

chart.SaveInteractiveHtml("interactive-report.html", options => {
    options.Interaction.GroupName = "executive-dashboard";
    options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts);
});

new[] { scoreChart, trendChart, findingsChart }.SaveInteractiveHtmlDashboard("interactive-dashboard.html", options => {
    options.Columns = 2;
    options.Interaction.GroupName = "executive-dashboard";
    options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts);
});
```

Host pages can listen for `cfxviewport`, `cfxselect`, `cfxseries`, `cfxbrush`, `cfxexport`, `cfxreset`, and `cfxsync` on each `.cfx-interactive-chart` element to connect ChartForgeX dashboards to filters, details panes, topology inspectors, or external report controls.

Other adapters such as Blazor, WinForms, WPF, MAUI, or Web Components can consume the same interaction contracts without changing the core SVG/HTML/PNG renderers.

## Why not Chart.js / ApexCharts?

Chart.js and ApexCharts are good interactive web libraries, but for generated HTML reports they create several problems:

- JavaScript dependency;
- CDN/local asset handling;
- harder offline mode;
- harder email embedding;
- harder deterministic rendering;
- heavier reports;
- more moving parts for SharePoint/static hosting.

ChartForgeX should replace them for static report visuals. Keep JavaScript charting only where interaction is genuinely required.

## PNG/JPG approach

PNG is supported as a dependency-free raster output. The current PNG renderer is intentionally simple, but it now shares the important report-layout rules: title/subtitle headers, scaled cartesian legends, rounded theme surfaces and borders, explicit x-axis label density, edge-aware tick labels, long formatted y-axis label space, axis titles, supersampled edges, compressed output, and alpha transparency. SVG remains the source of truth for beauty, typography, gradients, annotation labels, and report-grade visual polish.

JPG is not included. JPG has no transparency and is a poor fit for charts with sharp lines/text. If needed later, expose it as optional export by flattening PNG/SVG onto a solid background.

## Roadmap

### v0.1

- Line chart
- Step line chart
- Area chart
- Stacked area chart
- Scatter chart
- Slope chart
- Bar chart
- Histogram chart
- Lollipop chart
- Bubble chart
- Error-bar chart
- Candlestick chart
- OHLC chart
- Range-band chart
- Dumbbell chart
- Pareto chart helper
- Range bar chart
- Box plot chart
- Horizontal bar chart
- Pie and donut charts
- Static line and band annotations
- SVG renderer
- HTML renderer
- Minimal PNG renderer
- Light/dark themes
- Report theme presets and visual styling tokens
- Automatic x-axis label density for crowded reports
- Rotated x-axis labels for long categories
- Wrapped and positionable SVG/PNG legends
- Grouped SVG bar rendering for multiple bar series
- Stacked bar rendering for category totals
- Horizontal bar rendering for long categories
- Step line, histogram, lollipop, range bar, range band, dumbbell, and slope charts
- Bubble charts, trend lines, statistical overlays, and error bars
- Box plots, Pareto charts, candlestick charts, and OHLC charts
- Heatmap matrices
- Gauge charts
- Bullet charts
- Waterfall charts
- Radar charts
- Polar area charts
- Funnel charts
- Treemap charts
- Pictorial charts
- Word clouds
- Timeline charts
- Gantt charts
- Sankey charts
- Tree charts
- Sunburst partition charts
- Sparklines
- Date/time axes
- Data labels

### v0.2

- Responsive HTML containers

### v0.3

- Multiple Y axes
- Annotation lines/ranges
- Better PNG antialiasing
- Embedded vector font or compact Hershey-style text

### v1.0

- Stable public API
- HtmlForgeX integration package
- OfficeIMO embedding helpers
- Report theme presets
- Snapshot tests
- Benchmarks
