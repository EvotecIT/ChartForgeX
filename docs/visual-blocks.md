# Visual Blocks

`ChartGrid` remains a chart-only composition surface. Non-chart facts should use `ChartForgeX.VisualBlocks` so tables, lists, metric cards, status panels, and infographic snippets do not have to pretend they are chart series.

Current visual-block primitives:

- `ChartTable` for structured rows, columns, headers, alignment, formattable values, row striping, status columns, conditional row/cell colors, dense mode, transparent backgrounds, and SVG/PNG/HTML export.
- `ChartList` for bullets, numbered lists, key/value rows, checklists, status lists, and compact inventory summaries.
- `MetricCard` for one KPI with label, value, trend, status, and optional comparison/supporting text.
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

Next extension points should be driven by real PowerBGInfo, ImagePlayground, email, Word, and wallpaper scenarios:

- richer table/list style presets
- optional small icon symbols for status cells and list markers
- reusable status palettes
- compact infographic snippets that are still data-driven blocks, not arbitrary markup
- more examples and visual-baseline candidates once layouts stabilize
