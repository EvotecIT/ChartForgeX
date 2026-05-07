# Visual Blocks Direction

`ChartGrid` remains a chart composition surface. Tables, lists, metric cards, status panels, and future infographic snippets should not be modeled as charts pretending to be series.

The next composition family should use neutral visual blocks that can share the chart theme, color, palette, and export infrastructure without depending on chart axes or series models:

- `ChartTable` for structured rows, columns, headers, alignment, numeric formatting, striping, status cells, conditional colors, dense mode, transparent backgrounds, and SVG/PNG/HTML export.
- `ChartList` for bullet lists, key/value rows, checklists, status lists, and compact inventory summaries.
- `MetricCard` or `StatBlock` for one KPI with label, value, trend, status, icon, and optional comparison text.
- `VisualGrid` for composing charts and visual blocks side by side without forcing non-chart content into `ChartGrid`.

The scope should stay intentionally bounded: no spreadsheet engine, no arbitrary HTML renderer, no region-specific assumptions, and no dependency on `System.Drawing` or external table libraries. This keeps PowerBGInfo, ImagePlayground, email, Word, HTML snippets, wallpapers, and report-card scenarios reusable across US, Europe, cloud, tenant, inventory, compliance, and custom domain data.
