# ChartForgeX - Dependency-Free Chart Rendering for .NET

ChartForgeX renders polished SVG, static HTML, and PNG charts for .NET reports, dashboards, documentation, email, generated websites, and other static-output hosts.

The goal is not to clone browser-first charting libraries. The goal is a reusable, no-JavaScript-by-default rendering layer that HtmlForgeX, DomainDetective, TestimoX, GPOZaurr, ADEssentials, OfficeIMO-style generators, and similar tools can embed without bringing a graphics stack with them.

## Package

[![nuget version](https://img.shields.io/nuget/v/ChartForgeX)](https://www.nuget.org/packages/ChartForgeX)
[![top language](https://img.shields.io/github/languages/top/EvotecIT/ChartForgeX.svg)](https://github.com/EvotecIT/ChartForgeX)
[![license](https://img.shields.io/github/license/EvotecIT/ChartForgeX.svg)](https://github.com/EvotecIT/ChartForgeX)

```powershell
dotnet add package ChartForgeX
```

ChartForgeX targets `net472`, `netstandard2.0`, `net8.0`, and `net10.0`. The core package has no runtime package dependencies. The `net472` target uses `Microsoft.NETFramework.ReferenceAssemblies.net472` as a private build-time reference only.

Optional interaction support is split into separate packages:

| Package | Purpose |
| --- | --- |
| `ChartForgeX` | Static SVG, HTML, and PNG rendering. |
| `ChartForgeX.Interactivity` | Host-neutral interaction contracts. |
| `ChartForgeX.Interactivity.Html` | Self-contained HTML/SVG interaction adapter. |

## What It Does

- Renders static charts as SVG, HTML fragments/pages, and PNG files.
- Keeps the default HTML output script-free and self-contained.
- Shares chart models, themes, validation, layout rules, and visual tokens across SVG and PNG.
- Provides reusable visual blocks for exact report facts: tables, lists, metric cards, and mixed grids.
- Includes product-neutral topology diagrams for deterministic infrastructure, dependency, relationship, and geographic-style views.
- validates chart data before rendering so invalid payloads fail near the caller instead of producing partial markup or malformed PNGs.
- Rejects invalid specialized data such as non-finite values, malformed trees, multiple tree roots, and cyclic Sankey flows.
- Supports scoped inline SVG ids through `chart.ToSvg("panel-a")` and `grid.ToSvg("report-a")` so repeated charts can be embedded safely.

## Quick Start

```csharp
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

var chart = Chart.Create()
    .WithTitle("Domain Security Checks")
    .WithSubtitle("Dependency-free SVG, HTML, and PNG chart rendering")
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

static IEnumerable<ChartPoint> Points(params double[] y) {
    for (var i = 0; i < y.Length; i++) {
        yield return new ChartPoint(i + 1, y[i]);
    }
}
```

## Composition

Use `ChartGrid` for chart-only small multiples, comparison grids, and mosaic reports. Panel spans allow a report to mix hero panels with smaller supporting charts without creating a new chart type for layout.

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
```

Use `ChartForgeX.VisualBlocks` when a report needs exact facts beside charts instead of pretending tables, lists, metric cards, status panels, or infographic snippets are chart series.

```csharp
using ChartForgeX.VisualBlocks;

var table = ChartTable.Create()
    .WithTitle("Drive Summary")
    .AddColumn("Drive")
    .AddColumn("Used", VisualTextAlignment.Right, format: "0%")
    .AddColumn("Free", VisualTextAlignment.Right)
    .AddColumn("Status")
    .AddRow("C:", 0.72, "128 GB", "OK")
    .AddRow("D:", 0.91, "34 GB", "Warning")
    .WithStatusColumn("Status")
    .WithDenseMode();

var strip = VisualGrid.CreateMetricStrip("Endpoint Snapshot", new[] {
    MetricCard.Create().WithMetric("CPU Load", "38%").WithMiniSparkline(new[] { 52d, 48d, 44d, 41d, 38d }),
    MetricCard.Create().WithMetric("Memory Used", "71%").WithMiniBars(new[] { 55d, 59d, 63d, 68d, 71d }, maximum: 100)
});
```

## Topology Diagrams

`ChartForgeX.Topology` is for reusable, deterministic diagrams. It owns the product-neutral model, validation, layout helpers, SVG rendering, PNG rendering, and static HTML wrapper. Host projects own dashboard shells, data collection, filters, inspectors, and product-specific calculations.

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
| Maps | `AddDottedMap`, `ChartMapPoint`, `ChartMapViewport`, `WithMapViewport`, `AddMapConnector`, `AddMapRoute`, `AddMapConnectorBetweenPoints`, `AddMapRouteBetweenPoints`, `AddRegionMap`, `AddTileMap`, `ChartMapCatalog`, `ChartMapDefinition`, `ChartMapRegion`, `ChartTileMapCatalog`, `ChartTileMapDefinition`, `ChartTileMapRegion`, `ChartRegionMapItem`, `WithMapLabels`, `WithMapScaleLegend` |
| KPI and radial visuals | `AddGauge`, `AddCircle`, `AddRadialBar`, `AddLayeredRadial`, `ChartRadialLayer`, `ChartRadialLayerCap`, `AddBullet`, `AddWaterfall`, `AddRadar`, `AddPolarArea` |
| Hierarchy and flow | `AddFunnel`, `AddTreemap`, `AddSankey`, `ChartSankeyLink`, `AddTree`, `ChartTreeLink`, `AddSunburst`, `AddPie`, `AddDonut` |
| Pictorial and progress | `AddPictorial`, `ChartPictorialItem`, `ChartPictorialShape`, `ChartPictorialShape.Person`, `WithPictorialShape`, `WithPictorialColumns`, `WithPictorialMaximum`, `WithPictorialValuePerSymbol`, `WithPictorialValues`, `WithPictorialSymbolScale`, `WithPictorialEmptyOpacity`, `WithPictorialSvgPath`, `AddProgressBars`, `ChartProgressItem`, `WithProgressMaximum`, `WithProgressValues`, `WithProgressHandles`, `WithProgressBarThickness`, `WithProgressTrackOpacity` |
| Text, labels, and legends | `WithLegendPosition`, `WithPointLegend`, `ChartTextRole`, `ChartTextStyle`, `WithTextStyle`, `WithTitleStyle`, `WithSubtitleStyle`, `WithAxisTitleStyle`, `WithTickLabelStyle`, `WithLegendStyle`, `WithDataLabelStyle`, `WithDonutCenterLabel`, `WithDonutCenterText`, `WithDonutInnerRadiusRatio`, `WithRadialBarCenterLabel`, `WithCircleStatusLabel`, `WithCircleRadiusScale`, `WithCircleStrokeScale`, `WithRadialBarRadiusScale`, `WithRadialBarStrokeScale` |
| Branding and themes | `ChartBrandKit`, `WithBrandKit`, `ChartBrandKit.Executive()`, `PeopleInfographic()`, `Accessible()`, `ChartTheme.Aurora()`, `ChartTheme.Colorblind()`, `ChartTheme.DashboardLight()`, `ChartTheme.SaasDashboardLight()`, `ChartFontStacks`, `ChartPalettes.Vivid` |
| Text-heavy and schedule visuals | `AddWordCloud`, `ChartWordCloudItem`, `WithWordCloudFontRange`, `WithWordCloudAngles`, `WithWordCloudMaximumTerms`, `WithWordCloudDensity`, `AddTimelineItem`, `AddTimelineRange`, `AddGanttTask`, `AddGanttMilestone`, `WithGanttToday` |

Specialized SVG containers expose stable metadata for downstream QA and host integration. Heatmaps distinguish no-data cells through `data-cfx-status="empty"` while keeping an explicit zero value as real data. Matrix heatmaps expose `data-cfx-row-count`, `data-cfx-column-count`, `data-cfx-min`, and `data-cfx-max`. Calendar heatmaps expose `data-cfx-start-date` plus filled/empty day counts. Map outputs expose `data-cfx-label`, `data-cfx-projection`, `data-cfx-map-kind`, and `data-cfx-point-count`.

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
- HTML wraps inline SVG into static, self-contained pages or fragments.
- PNG uses ChartForgeX's dependency-free raster path and supports real alpha transparency.
- JavaScript belongs in opt-in adapter packages, not in the default static renderer.
- Unsafe `javascript:`, `data:`, and `vbscript:` hrefs are skipped.
- Public APIs fail fast on invalid sizes, ranges, enum values, and specialized series payloads.

## Repository Map

```text
ChartForgeX
|-- ChartForgeX                    # core chart model and static renderers
|   |-- Core                       # chart model, series, options
|   |-- Primitives                 # colors, points, rects, padding
|   |-- Rendering                  # shared rendering math and polish helpers
|   |-- Svg                        # SVG renderer
|   |-- Html                       # static HTML renderer
|   |-- Raster                     # PNG renderer and writer
|   |-- Topology                   # product-neutral topology model/renderers
|   `-- VisualBlocks               # tables, lists, metric cards, visual grids
|-- ChartForgeX.Interactivity       # host-neutral interaction contracts
|-- ChartForgeX.Interactivity.Html  # self-contained HTML interaction adapter
|-- ChartForgeX.Examples            # generated gallery and smoke examples
|-- ChartForgeX.Tests               # smoke and repository quality tests
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
- [Rendering engine benchmarking](docs/rendering-engine-benchmarking.md)
- [Contributing and release workflow](CONTRIBUTING.md)
- [Centralized TODO](TODO.md)
