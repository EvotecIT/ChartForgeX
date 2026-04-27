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

For local development from this repository, reference `ChartForgeX/ChartForgeX.csproj` directly and run the quality loop before publishing packages.

Contribution and release notes live in `CONTRIBUTING.md`, `RELEASING.md`, and `CHANGELOG.md`.

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

That restores, builds, runs smoke tests through `dotnet test`, regenerates example chart outputs and the static example gallery, packs the core library, creates a `.snupkg` symbol package, verifies package contents, verifies the package has no runtime NuGet dependencies, and installs the freshly packed package into a clean temporary console app. The gallery is written to `ChartForgeX.Examples/bin/Release/net8.0/output/index.html`.

The GitHub Actions workflow is configured for private repositories and requires a self-hosted runner with the labels `self-hosted` and `private`.

## Design principles

- Zero runtime dependencies in the core package.
- SVG-first rendering for beautiful static HTML reports.
- PNG export with real alpha transparency.
- One chart model, multiple renderers.
- JavaScript-free by default.
- Public chart, option, theme, and primitive APIs fail fast on invalid values.
- Optional interactive renderers can be added later without changing user code.
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

Horizontal bars are available for long report categories:

```csharp
var coverage = Chart.Create()
    .WithTitle("Domain Control Coverage")
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF alignment", "DMARC policy enforcement", "DNSSEC coverage")
    .AddHorizontalBar("Coverage", Points(96, 88, 74));
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

Pie and donut charts use the same model and x-axis labels for slice names:

```csharp
var donut = Chart.Create()
    .WithTitle("Domain Check Result Mix")
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

For interactive dashboards, add a separate package later:

```text
ChartForgeX.Interactive.Html
```

That package may optionally emit small vanilla JS for hover, tooltip, zoom, selection, and data toggles. It should not affect the static renderer.

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
- Area chart
- Scatter chart
- Bar chart
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
- Wrapped SVG legends
- Grouped SVG bar rendering for multiple bar series
- Stacked bar rendering for category totals
- Horizontal bar rendering for long categories
- Heatmap matrices
- Gauge charts
- Bullet charts
- Waterfall charts
- Radar charts
- Funnel charts
- Timeline charts
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
