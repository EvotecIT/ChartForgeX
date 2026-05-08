# Visual Blocks

`ChartGrid` remains a chart-only composition surface. Non-chart facts should use `ChartForgeX.VisualBlocks` so tables, lists, metric cards, status panels, and infographic snippets do not have to pretend they are chart series.

Current visual-block primitives:

- `ChartTable` for structured rows, columns, headers, alignment, formattable values, row striping, status columns, conditional row/cell colors, dense mode, transparent backgrounds, and SVG/PNG/HTML export.
- `ChartList` for bullets, numbered lists, key/value rows, checklists, status lists, and compact inventory summaries.
- `MetricCard` for one KPI with label, value, trend, status, optional comparison/supporting text, footer action text, and embedded mini bars or sparklines for compact history/current-state cards.
- `VisualGrid` for composing charts and visual blocks side by side without forcing non-chart content into `ChartGrid`.

The first API is intentionally generic and bounded:

- no spreadsheet engine
- no arbitrary HTML renderer
- no region-specific assumptions
- no dependency on `System.Drawing` or external table/list libraries
- static SVG/HTML/PNG output by default
- shared `ChartTheme`, `ChartColor`, `ChartPalettes`, transparent background, and PNG density concepts

Example:

```csharp
using ChartForgeX;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

var table = ChartTable.Create()
    .WithTitle("Drive Summary")
    .WithTheme(ChartTheme.TransparentOverlayDark())
    .WithTransparentBackground()
    .AddColumn("Drive")
    .AddColumn("Used", VisualTextAlignment.Right, format: "0%")
    .AddColumn("Free", VisualTextAlignment.Right)
    .AddColumn("Status")
    .AddRow("C:", 0.72, "128 GB", "OK")
    .AddRow("D:", 0.91, "34 GB", "Warning")
    .WithStatusColumn("Status")
    .WithDenseMode();

table.SaveSvg("drives.svg");
table.SaveHtml("drives.html");
table.SavePng("drives.png");
```

Metric cards can carry a short micro bar history without becoming a full chart:

```csharp
var cpu = MetricCard.Create()
    .WithMetric("CPU Load", "38%")
    .WithTrend("-6%")
    .WithCaption("5 minute trend")
    .WithSymbol("CPU")
    .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
    .WithStatus(VisualStatus.Positive)
    .WithAction("View details", url: "#cpu-load")
    .WithMiniBars(new[] { 48d, 52d, 44d, 41d, 38d }, maximum: 100);
```

Use a mini sparkline when the shape of recent movement matters more than discrete columns:

```csharp
var latency = MetricCard.Create()
    .WithMetric("Latency", "18 ms")
    .WithTrend("-12 ms")
    .WithCaption("last samples")
    .WithStatus(VisualStatus.Info)
    .WithAction("Open samples", url: "#latency")
    .WithMiniSparkline(new[] { 42d, 36d, 31d, 28d, 24d, 18d });
```

Metric strips provide a reusable section preset for PowerBGInfo-style card rows:

```csharp
var section = VisualGrid.CreateMetricStrip("Endpoint Snapshot", new[] {
    MetricCard.Create().WithMetric("CPU Load", "38%").WithSymbol("CPU").WithBadgePlacement(MetricCardBadgePlacement.TopLeft).WithAction("View details", url: "#cpu-load").WithMiniSparkline(new[] { 52d, 48d, 44d, 41d, 38d }),
    MetricCard.Create().WithMetric("Memory Used", "71%").WithAction("View details").WithMiniBars(new[] { 55d, 59d, 63d, 68d, 71d }, maximum: 100)
});
```

`WithAction(...)` is still static-renderer friendly. SVG/HTML outputs render safe relative, `http(s)`, and `mailto` action URLs when one is supplied; PNG keeps the same visual affordance without embedding a link.

The mini bar and mini sparkline geometry is shared by the SVG and PNG visual-block renderers, so improvements to compact line/bar polish can be applied once instead of redoing each output format separately.

Next extension points should be driven by real PowerBGInfo, ImagePlayground, email, Word, and wallpaper scenarios:

- richer table/list style presets
- optional small icon symbols for status cells and list markers
- reusable status palettes
- compact infographic snippets that reuse shared primitive layout/styling, not arbitrary markup
- more examples and visual-baseline candidates once layouts stabilize
