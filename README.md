# ChartForgeX - Dependency-Free Chart Rendering for .NET

ChartForgeX renders polished charts, visual blocks, topology diagrams, and static report visuals from .NET without adding a browser charting runtime to generated output.

## NuGet Package

[![nuget downloads](https://img.shields.io/nuget/dt/ChartForgeX?label=nuget%20downloads)](https://www.nuget.org/packages/ChartForgeX)
[![nuget version](https://img.shields.io/nuget/v/ChartForgeX)](https://www.nuget.org/packages/ChartForgeX)

## Project Information

[![top language](https://img.shields.io/github/languages/top/EvotecIT/ChartForgeX.svg)](https://github.com/EvotecIT/ChartForgeX)
[![license](https://img.shields.io/github/license/EvotecIT/ChartForgeX.svg)](https://github.com/EvotecIT/ChartForgeX)
[![quality](https://github.com/EvotecIT/ChartForgeX/actions/workflows/quality.yml/badge.svg)](https://github.com/EvotecIT/ChartForgeX/actions/workflows/quality.yml)

## Author & Social

[![Twitter follow](https://img.shields.io/twitter/follow/PrzemyslawKlys.svg?label=Twitter%20%40PrzemyslawKlys&style=social)](https://twitter.com/PrzemyslawKlys)
[![Blog](https://img.shields.io/badge/Blog-evotec.xyz-2A6496.svg)](https://evotec.xyz/hub)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-pklys-0077B5.svg?logo=LinkedIn)](https://www.linkedin.com/in/pklys)
[![Threads](https://img.shields.io/badge/Threads-@PrzemyslawKlys-000000.svg?logo=Threads&logoColor=White)](https://www.threads.net/@przemyslaw.klys)
[![Discord](https://img.shields.io/discord/508328927853281280?style=flat-square&label=discord%20chat)](https://evo.yt/discord)

## What It's For

ChartForgeX is for generated reports, documentation, email, static websites, dashboards, wallpapers, Office-style generators, and other hosts that need deterministic visuals without a JavaScript chart dependency.

The core package renders SVG, static HTML, PNG, BMP, PPM, and baseline TIFF. Static HTML is script-free by default. Optional interactions are split into separate adapter packages so a static report can stay static, while a dashboard can opt into tooltips, selection, zoom, pan, brush ranges, synchronized charts, and export controls.

## Visual Tour

The examples are generated from `ChartForgeX.Examples` and checked into the project site so reviewers can inspect the visual output without running a browser chart runtime. The dedicated examples page keeps SVG, PNG, HTML, and C# snippets together; the repository gallery keeps the wider catalog grouped by chart family.

| Dashboard grid | Heatmap | Topology |
| --- | --- | --- |
| [![Dashboard chart portfolio](Website/static/examples/generated/dashboard-chart-portfolio-grid.svg)](Website/static/examples/generated/dashboard-chart-portfolio-grid.svg) | [![Control coverage heatmap](Website/static/examples/generated/control-coverage-heatmap-dark.svg)](Website/static/examples/generated/control-coverage-heatmap-dark.svg) | [![Geographic topology map](Website/static/examples/generated/visual-geographic-topology-map.svg)](Website/static/examples/generated/visual-geographic-topology-map.svg) |
| [SVG](Website/static/examples/generated/dashboard-chart-portfolio-grid.svg) | [HTML](Website/static/examples/generated/control-coverage-heatmap-dark.html) / [SVG](Website/static/examples/generated/control-coverage-heatmap-dark.svg) / [PNG](Website/static/examples/generated/control-coverage-heatmap-dark.png) | [SVG](Website/static/examples/generated/visual-geographic-topology-map.svg) |

## Generated Examples

- Run `dotnet run --project .\ChartForgeX.Examples\ChartForgeX.Examples.csproj -c Release` to refresh `ChartForgeX.Examples/bin/Release/net8.0/output/`.
- Open `index.html` for the full generated gallery, `catalog.html` for grouped chart families, and `svg-png-comparison.html` for side-by-side SVG/PNG review.
- Generated cards link HTML, SVG, PNG, and C# sidecar snippets when the example has a checked source sample.
- The dedicated site uses `Website/data/showcase.json` and `Website/static/examples/promoted-cases.json` to keep promoted visuals source-linked and easy to copy.

## Install

```powershell
dotnet add package ChartForgeX
```

ChartForgeX targets `net472`, `netstandard2.0`, `net8.0`, and `net10.0`. The core package has no runtime package dependencies. The `net472` target uses `Microsoft.NETFramework.ReferenceAssemblies.net472` as a private build-time reference only.

Optional interaction support is split into separate packages:

| Package | Purpose |
| --- | --- |
| `ChartForgeX` | Static SVG, HTML, PNG, BMP, PPM, and TIFF rendering. |
| `ChartForgeX.Interactivity` | Host-neutral interaction contracts. |
| `ChartForgeX.Interactivity.Html` | Self-contained HTML/SVG interaction adapter. |

## Supported Outputs

| API shape | Purpose |
| --- | --- |
| `ToSvg`, `ToHtmlFragment`, `ToHtmlPage`, `ToPng` | Return the main report-grade formats directly. |
| `SaveSvg`, `SaveHtml`, `SavePng` | Save the main formats explicitly. |
| `ToBmp`, `ToPpm`, `ToTiff`, `ToRasterImage` | Return opaque flattened raster formats from the shared raster buffer. |
| `WriteBmp`, `WritePpm`, `WriteTiff`, `WriteRasterImage` | Stream opaque raster formats without forcing a final byte-array allocation. |
| `SaveBmp`, `SavePpm`, `SaveTiff`, `SaveRasterImage` | Save opaque raster formats explicitly or by raster extension. |
| `SaveImage`, `Save` | Infer the output format from the file extension. |

`SaveImage` and `Save` infer `.svg`, `.html`, `.htm`, `.png`, `.bmp`, `.ppm`, `.tiff`, and `.tif`. Unsupported or empty extensions fail before a file is opened. `RasterImageOptions` applies to opaque raster formats, where transparent pixels are flattened against a background color; PNG stays on the PNG renderer path and keeps chart/grid PNG options such as output scale.

## Release Maturity

The first release should be treated as a broad preview with a serious stability bar. The public surface is intended to be kept stable where it represents real charting concepts, and changed intentionally where a pre-release API would otherwise lock in awkward names or host-specific assumptions.

| Area | Release stance |
| --- | --- |
| Core charts, themes, SVG, static HTML, PNG, validation, and package layout | Supported first-release API. Avoid breaking changes except for clear correctness or naming fixes. |
| BMP, PPM, TIFF, raster helper APIs, and stream/file/byte-array export helpers | Supported utility API over the shared raster buffer. PNG remains the main raster target for report quality. |
| Topology diagrams, deterministic layouts, icon catalogs, and geographic topology | Supported, with dense routing and geographic polish still tracked before numeric visual-baseline promotion. |
| Visual blocks such as tables, metric cards, status cards, workload lists, activity timelines, and schedule strips | Supported report-fragment API, but advanced dashboard-card patterns should continue to be refined from real host usage. |
| External GeoJSON/map catalogs and large vendor icon-pack workflows | Opt-in extension workflow. Keep these generic, provenance-aware, and outside the core runtime dependency path. |

No chart type is marked for removal in this release pass. Active follow-up work belongs in `TODO.md` until it becomes durable reference documentation.

Release notes should be published through GitHub Releases. Keep NuGet `PackageReleaseNotes` short and package-focused, and avoid maintaining a second long-form changelog in the repository.

## Quick Start

```csharp
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

var chart = Chart.Create()
    .WithTitle("Domain Security Checks")
    .WithSubtitle("Dependency-free SVG, HTML, PNG, BMP, PPM, and TIFF chart rendering")
    .WithXAxis("Run")
    .WithYAxis("Checks")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(1180, 640)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
    .AddSmoothArea("Passed", Points(820, 940, 980, 1040, 1120, 1180, 1230))
    .AddSmoothLine("Warnings", Points(120, 138, 132, 110, 98, 86, 72), ChartColor.FromRgb(251, 191, 36))
    .AddSmoothLine("Failed", Points(22, 30, 28, 21, 18, 15, 13), ChartColor.FromRgb(248, 113, 113));

chart.SaveSvg("chart.svg");
chart.SaveHtml("chart.html");
chart.SavePng("chart.png");
chart.SaveBmp("chart.bmp");
chart.SavePpm("chart.ppm");
chart.SaveTiff("chart.tiff");

static IEnumerable<ChartPoint> Points(params double[] y) {
    for (var i = 0; i < y.Length; i++) {
        yield return new ChartPoint(i + 1, y[i]);
    }
}
```

## Composition

Use `ChartGrid` for chart-only small multiples, comparison grids, and mosaic reports. `Add(chart, columnSpan, rowSpan)` and `WithPanelSpan(index, columnSpan, rowSpan)` let a report mix hero panels with smaller supporting charts without creating a chart type just for layout.

```csharp
var report = ChartGrid.Create()
    .WithTitle("Control Scorecards")
    .WithTheme(ChartTheme.ReportLight())
    .WithColumns(2)
    .WithPanelSize(520, 320)
    .Add(gaugeChart, columnSpan: 2)
    .Add(trendChart)
    .Add(coverageChart)
    .WithPanelSpan(2, columnSpan: 2);

report.SaveHtml("scorecards.html");
report.SaveSvg("scorecards.svg");
report.SavePng("scorecards.png");
report.SaveBmp("scorecards.bmp");
report.SavePpm("scorecards.ppm");
report.SaveTiff("scorecards.tiff");
```

Use `ChartForgeX.VisualBlocks` when a report needs exact facts beside charts instead of pretending tables, lists, metric cards, status panels, or infographic snippets are chart series.

```csharp
using ChartForgeX.VisualBlocks;

var drives = ChartTable.Create()
    .WithTitle("Drive Summary")
    .AddColumn("Drive")
    .AddColumn("Used", VisualTextAlignment.Right, format: "0%")
    .AddColumn("Free", VisualTextAlignment.Right)
    .AddColumn("Status")
    .AddRow("C:", 0.72, "128 GB", "OK")
    .AddRow("D:", 0.91, "34 GB", "Warning")
    .WithStatusColumn("Status")
    .WithDenseMode();

var snapshot = VisualGrid.CreateMetricStrip("Endpoint Snapshot", new[] {
    MetricCard.Create().WithMetric("CPU Load", "38%").WithMiniSparkline(new[] { 52d, 48d, 44d, 41d, 38d }),
    MetricCard.Create().WithMetric("Memory Used", "71%").WithMiniBars(new[] { 55d, 59d, 63d, 68d, 71d }, maximum: 100)
});
```

## Topology Diagrams

`ChartForgeX.Topology` is for reusable deterministic diagrams. It owns the product-neutral model, validation, layout helpers, SVG rendering, PNG rendering, and static HTML wrapper. Host projects own dashboard shells, data collection, filters, inspectors, and product-specific calculations.

```csharp
using ChartForgeX.Topology;

var topology = TopologyChart.Create()
    .WithId("service-map")
    .WithTitle("Service Dependency Map")
    .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
    .WithLegend(TopologyLegend.Default()
        .AddNodeKind("Service", TopologyNodeKind.Service, symbol: "API")
        .AddNodeKind("Database", TopologyNodeKind.Database, symbol: "SQL")
        .AddEdgeKind("Dependency", TopologyEdgeKind.Dependency))
    .AddNode("api", "API", 0, 0, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "API")
    .AddNode("database", "Database", 0, 0, TopologyNodeKind.Database, TopologyHealthStatus.Warning, symbol: "SQL")
    .AddEdge("api-database", "api", "database", "32 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward);

topology.SaveSvg("service-map.svg");
topology.SaveHtml("service-map.html");
topology.SavePng("service-map.png");
topology.SaveBmp("service-map.bmp");
topology.SavePpm("service-map.ppm");
topology.SaveTiff("service-map.tiff");
```

Supported topology layout modes are `Manual`, `GroupGrid`, `HubAndSpoke`, `Layered`, `Matrix`, `DenseGrouped`, and `Geographic`. Geographic topology uses `ChartMapViewport` with typed coordinates, route arcs, region hulls, and optional callouts while keeping the model reusable across infrastructure, cloud, tenant, inventory, and domain-specific hosts.

## Chart catalog

The catalog is broad enough for generated reports, dashboards, operational summaries, and static documentation:

| Family | APIs |
| --- | --- |
| Cartesian lines and areas | `AddLine`, `AddSmoothLine`, `AddStepLine`, `AddArea`, `AddStepArea`, `AddSmoothArea`, `AddStackedArea`, `AddSmoothStackedArea`, `AddScatter`, `AddTrendLine`, `AddPointCallout`, `WithPointLabel`, `WithLegendEntry`, `WithSemanticRole`, `AddMeanLine`, `AddMedianLine`, `AddStandardDeviationBand`, `AddSlope` |
| Combo charts | `AddBarLineCombo`, `AddColumnLineCombo`, `AddBarAreaCombo`, `AddColumnAreaCombo`, `AddScatterLineCombo` |
| Bars and distributions | `AddBar`, `AddHistogram`, `AddLollipop`, `AddBubble`, `AddErrorBar`, `AddCandlestick`, `AddOhlc`, `AddRangeBand`, `AddRangeArea`, `AddDumbbell`, `AddPareto`, `AddRangeBar`, `AddBoxPlot`, `AddHorizontalBar`, `WithStackedHorizontalBars` |
| Heatmaps and calendars | `AddHeatmapRow`, `AddHeatmapRows`, `ChartHeatmapRow`, `AddHexbinHeatmapRow`, `AddHexbinHeatmapRows`, `AddCalendarHeatmap`, `ChartCalendarHeatmapItem` |
| Maps | `AddDottedMap`, `ChartMapPoint`, `ChartMapViewport`, `WithMapViewport`, `AddMapConnector`, `AddMapRoute`, `AddMapConnectorBetweenPoints`, `AddMapRouteBetweenPoints`, `AddRegionMap`, `AddTileMap`, `ChartMapCatalog`, `ChartMapCatalogEntry`, `ChartMapCatalogEntryKind`, `EmbeddedEntries`, `ExternalEntries`, `Load`, `FromAssetDirectory`, `ChartMapDefinition`, `ChartMapRegion`, `ChartTileMapCatalog`, `ChartTileMapDefinition`, `ChartTileMapRegion`, `ChartRegionMapItem`, `WithMapLabels`, `WithMapScaleLegend`, `WithMapScaleLegendPosition`, `WithMapSurface`, `WithMapRegionStroke`, `WithRegionMapBounds`, `WithRegionMapCoordinateBounds`, `AddMapBaseLayer`, `AddMapBoundaryLayer` |
| KPI and radial visuals | `AddGauge`, `AddCircle`, `AddRadialBar`, `AddLayeredRadial`, `ChartRadialLayer`, `ChartRadialLayerCap`, `AddBullet`, `AddWaterfall`, `AddRadar`, `AddPolarArea` |
| Hierarchy and flow | `AddFunnel`, `AddTreemap`, `AddSankey`, `ChartSankeyLink`, `AddTree`, `ChartTreeLink`, `AddSunburst`, `AddPie`, `AddDonut` |
| Pictorial and progress | `AddPictorial`, `ChartPictorialItem`, `ChartPictorialShape`, `ChartPictorialShape.Person`, `WithPictorialShape`, `WithPictorialColumns`, `WithPictorialMaximum`, `WithPictorialValuePerSymbol`, `WithPictorialValues`, `WithPictorialSymbolScale`, `WithPictorialEmptyOpacity`, `WithPictorialSvgPath`, `AddProgressBars`, `ChartProgressItem`, `WithProgressMaximum`, `WithProgressValues`, `WithProgressHandles`, `WithProgressBarThickness`, `WithProgressTrackOpacity` |
| Text, labels, and legends | `WithLegendPosition`, `WithPointLegend`, `ChartTextRole`, `ChartTextStyle`, `WithTextStyle`, `WithTitleStyle`, `WithSubtitleStyle`, `WithAxisTitleStyle`, `WithTickLabelStyle`, `WithLegendStyle`, `WithDataLabelStyle`, `WithDonutCenterLabel`, `WithDonutCenterText`, `WithDonutInnerRadiusRatio`, `WithRadialBarCenterLabel`, `WithCircleStatusLabel`, `WithCircleRadiusScale`, `WithCircleStrokeScale`, `WithRadialBarRadiusScale`, `WithRadialBarStrokeScale` |
| Branding and themes | `ChartBrandKit`, `WithBrandKit`, `ChartBrandKit.Executive()`, `PeopleInfographic()`, `Accessible()`, `ChartTheme.Aurora()`, `ChartTheme.Colorblind()`, `ChartTheme.DashboardLight()`, `ChartTheme.SaasDashboardLight()`, `ChartFontStacks`, `ChartPalettes.Vivid` |
| Text-heavy and schedule visuals | `AddWordCloud`, `ChartWordCloudItem`, `WithWordCloudFontRange`, `WithWordCloudAngles`, `WithWordCloudMaximumTerms`, `WithWordCloudDensity`, `AddTimelineItem`, `AddTimelineRange`, `AddGanttTask`, `AddGanttMilestone`, `WithGanttToday` |

## Renderer Contracts

- ChartForgeX validates chart data before rendering so invalid payloads fail near the caller instead of producing partial markup or malformed PNGs.
- Specialized data checks reject non-finite values, malformed trees, multiple tree roots, and cyclic Sankey flows.
- Scoped inline SVG ids are available through `chart.ToSvg("panel-a")` and `grid.ToSvg("report-a")`, so repeated charts can be embedded safely.
- Heatmaps distinguish no-data cells through `data-cfx-status="empty"` while keeping an explicit zero value as real data.
- Matrix heatmaps expose `data-cfx-row-count`, `data-cfx-column-count`, `data-cfx-min`, and `data-cfx-max`.
- Calendar heatmaps expose `data-cfx-start-date` plus filled/empty day counts.
- Map outputs expose `data-cfx-label`, `data-cfx-projection`, `data-cfx-map-kind`, and `data-cfx-point-count`.
- Unsafe `javascript:`, `data:`, and `vbscript:` hrefs are skipped.

## Customization cookbook

Use themes when you want a complete visual baseline:

```csharp
var chart = Chart.Create()
    .WithTheme(ChartTheme.Aurora())
    .WithSurfaceStyle(ChartSurfaceStyle.Glass)
    .WithPalette(ChartPalettes.Vivid)
    .AddSmoothLine("Warnings", points);
```

Use brand kits when a whole report family needs consistent typography, palette, surfaces, and semantic colors:

```csharp
var branded = Chart.Create()
    .WithBrandKit(ChartBrandKit.Executive())
    .WithTheme(theme => theme
        .WithSurfaceColors("#0F172A", "#111827", "#1F2937")
        .WithSemanticColors(success: "#22C55E", warning: "#F59E0B", danger: "#EF4444"));
```

Use pasted colors when matching an existing design system:

```csharp
var palette = ChartPalettes.FromHex("#2563EB", "#14B8A6", "#F59E0B", "#EF4444");
var color = ChartColor.FromHex("#2563EB");
```

Use fluent series styling for a single emphasized series:

```csharp
chart.Series[0]
    .WithStrokeWidth(4)
    .UseThemeColor();
```

| Report intent | Theme starting point | Brand kit starting point |
| --- | --- | --- |
| Executive report | `ChartTheme.ReportLight()` | `ChartBrandKit.Executive()` |
| Operational dashboard | `ChartTheme.DashboardLight()` | `ChartBrandKit.Accessible()` |
| SaaS-style dashboard | `ChartTheme.SaasDashboardLight()` | `ChartBrandKit.Product()` |
| People or editorial summary | `ChartTheme.Aurora()` | `ChartBrandKit.PeopleInfographic()` |
| Accessibility-first report | `ChartTheme.Colorblind()` | `ChartBrandKit.Accessible()` |

## Output and Safety

- SVG is the highest-fidelity static target.
- HTML wraps inline SVG into static self-contained pages or fragments.
- PNG uses ChartForgeX's dependency-free raster path and supports real alpha transparency.
- BMP, PPM, and TIFF are opaque utility exports over the same raster buffer.
- JavaScript belongs in opt-in adapter packages, not in the default static renderer.
- Public APIs fail fast on invalid sizes, ranges, enum values, and specialized series payloads.

## Website Pilot

`Website/` contains the dedicated PowerForge.Web pilot site for ChartForgeX. The central Evotec project hub remains the registry page, while the dedicated site is meant for the richer gallery and demo experience at `https://chartforgex.evotec.xyz/`.

Build the examples first with `./Build.ps1`, then build the site from `Website/`:

```powershell
.\build.ps1 -Dev
.\build.ps1 -Ci
```

Promoted website examples should be reproducible cases, not screenshots: show the rendered preview, link the HTML/SVG/PNG artifacts, and point to the source file or builder method that generates the same output.

## Repository Map

```text
ChartForgeX
|-- ChartForgeX                    # core chart model and static renderers
|   |-- Core                       # chart model, series, options
|   |-- Primitives                 # colors, points, rects, padding
|   |-- Rendering                  # shared rendering math and polish helpers
|   |-- Svg                        # SVG renderer
|   |-- Html                       # static HTML renderer
|   |-- Raster                     # PNG/BMP/PPM/TIFF renderer and encoders
|   |-- Topology                   # product-neutral topology model/renderers
|   `-- VisualBlocks               # tables, lists, metric cards, visual grids
|-- ChartForgeX.Interactivity       # host-neutral interaction contracts
|-- ChartForgeX.Interactivity.Html  # self-contained HTML interaction adapter
|-- ChartForgeX.Examples            # generated gallery and smoke examples
|-- ChartForgeX.Tests               # smoke and repository quality tests
|-- Website                         # dedicated PowerForge.Web pilot site
|-- docs                            # focused reference notes
|-- AGENTS.md                       # contributor/agent expectations
|-- CONTRIBUTING.md                 # development and release workflow
|-- TODO.md                         # centralized active work ledger
`-- Build.ps1                      # local quality and packaging gate
```

## Development

Run the full local quality loop before publishing a pull request:

```powershell
./Build.ps1 -Configuration Release
```

For a faster code/test loop:

```powershell
dotnet test .\ChartForgeX.sln -c Release
```

Generated example output is written under `ChartForgeX.Examples/bin/Release/net8.0/output/`. The most useful review pages are:

- `index.html`
- `catalog.html`
- `quality-dashboard.html`
- `svg-png-comparison.html`
- `domain-security-interactive.html`
- `executive-interactive-dashboard.html`

Refresh visual baselines only after reviewing the generated gallery:

```powershell
./Build.ps1 -Configuration Release -UpdateVisualBaseline
```

## Documentation

- [Architecture notes](docs/architecture.md)
- [Topology reference](docs/topology.md)
- [Visual blocks reference](docs/visual-blocks.md)
- [Contributing and release workflow](CONTRIBUTING.md)
- [Centralized TODO](TODO.md)
