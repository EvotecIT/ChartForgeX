# Visual Blocks

`ChartGrid` remains a chart-only composition surface. Non-chart facts should use `ChartForgeX.VisualBlocks` so tables, lists, metric cards, status panels, and infographic snippets do not have to pretend they are chart series.

Current visual-block primitives:

- `ChartTable` for structured rows, columns, headers, alignment, formattable values, row striping, status columns, conditional row/cell colors, dense mode, table-cell badges/chips, mini bars/sparklines, transparent backgrounds, and SVG/PNG/HTML export.
- `ChartList` for bullets, numbered lists, key/value rows, checklists, status lists, and compact inventory summaries.
- `MetricCard` for one KPI with label, value, trend, status, optional comparison/supporting text, footer action text, and embedded mini bars or sparklines for compact history/current-state cards.
- `SegmentedMetricBlock` for fixed-count progress rows, ordered funnel columns, balanced capsule loops, part-to-whole strips, and distribution rows using one generic item model and a `SegmentedMetricStyle` visual-treatment enum.
- `HeatmapInsightCard` for dashboard matrix cards with controls, value cells, a right-side insight rail, and a color key.
- `WorkloadListBlock` for staff, ranked people, or merchant rows with avatar/initial slots, progress rails, status notes, optional checkbox controls, and right-aligned values.
- `ActivityTimelineBlock` for static SVG/PNG timeline overlays with section labels, status nodes, connector spines, nested checklist rows, hidden-item summaries, timestamps, badges, compact event rows, and node symbols.
- `ScheduleTimelineBlock` for dense time-of-day swimlanes with optional header action chips, rounded event pills, status stripes, current-time markers, clipped-event metadata, badges, and avatar stacks.
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
using ChartForgeX.Typography;
using ChartForgeX.VisualBlocks;

var table = ChartTable.Create()
    .WithTitle("Drive Summary")
    .WithTheme(ChartTheme.TransparentOverlayDark())
    .WithTransparentBackground()
    .AddColumn("Drive")
    .AddColumn("Used", TextAlignment.Right, format: "0%")
    .AddColumn("Free", TextAlignment.Right)
    .AddColumn("Status")
    .AddRow("C:", 0.72, "128 GB", "OK")
    .AddRow("D:", 0.91, "34 GB", "Warning")
    .WithStatusColumn("Status")
    .WithDenseMode();

table.SaveSvg("drives.svg");
table.SaveHtml("drives.html");
table.SavePng("drives.png");
```

Table cells can host bounded badges, chips, and microvisuals for dashboard tables without embedding full charts in each row:

```csharp
var vacancies = ChartTable.Create()
    .WithTitle("Recent Vacancies")
    .WithDenseMode()
    .WithColumns("Company", "Job Title", "Applications", "New", "Trend")
    .AddRow("Google", "Software Engineer", "92", "", "")
    .AddRow("Microsoft", "Software Engineer", "92", "", "")
    .WithRow(0, row => {
        row.Cells[3].WithBadge("22 new", VisualStatus.Info, ChartColor.FromHex("#7C3AED"));
        row.Cells[4].WithSparkline(new[] { 12d, 16d, 13d, 19d, 22d }, color: ChartColor.FromHex("#7C3AED"));
    })
    .WithRow(1, row => {
        row.Cells[3].WithBadge("12 new", VisualStatus.Info, ChartColor.FromHex("#7C3AED"));
        row.Cells[4].WithMiniBars(new[] { 10d, 14d, 12d, 11d, 12d }, color: ChartColor.FromHex("#7C3AED"));
    });
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

## Terminal Presentations

`TerminalStory` creates a deterministic console presentation from structured commands and output. It does not execute commands. That separation makes the same renderer suitable for authored product demos, documentation, release evidence, and transcripts captured by a caller-controlled script:

```csharp
using ChartForgeX.Terminal;

var results = TerminalTable.Create()
    .WithColumns("CHECK", "STATUS")
    .AddRow("Restore", "PASS")
    .AddRow("Tests", "PASS");

var console = TerminalStory.Create()
    .WithTitle(@"pwsh - C:\OpenSource")
    .WithDialect(TerminalDialect.PowerShell)
    .WithTheme(TerminalTheme.PowerShell())
    .WithWindowStyle(TerminalWindowStyle.WindowsTerminal)
    .WithWorkingDirectory(@"C:\OpenSource")
    .Command(@".\Invoke-Validation.ps1")
    .Output("Running release validation...", TerminalTextTone.Muted)
    .Table(results)
    .Output("Ready", TerminalTextTone.Success);

console.SaveSvg("validation.svg");
console.SaveGif("validation.gif");
```

Available dialects are PowerShell, Bash, command prompt, Python, C#, and custom prompts. Dialect, palette, and window chrome are independent: a PowerShell prompt can use any theme with macOS, Windows Terminal, minimal, or no title-bar chrome. The structured model also supports blank lines, bounded pauses, semantic output tones, progress bars, and compact monospace tables. `WithPlaybackSpeed` applies a coherent Slow, Normal, or Fast pace, while `WithTiming` and `WithTabHold` independently tune code typing and the minimum reading time before a tab switch. SVG and HTML animate without JavaScript. GIF and APNG reuse the exact terminal timeline for portable chat, issue, and documentation embeds. PNG, print, and `prefers-reduced-motion` expose the same completed transcript immediately.

Use `TerminalStoryAnimationOptions` when a host needs a different frame rate, single-play output, a longer completed-state hold, a denser raster, or a different frame budget. The default 10 FPS, one-times-density GIF is intentionally suitable for Discord-style sharing without turning the export into a browser recording.

Use `WithInitialTab`, `DeclareTab`, and `SelectTab` for an explicit multi-shell walkthrough. `OpenTab` is the shorter declare-and-select operation. Tabs preserve their own transcript buffers, prompts, directories, semantic icons, and palettes. The default initial session is named `main`; static and reduced-motion output show the completed active tab, while accessible transcript text includes every session.

## Generic source-to-result stories

Use `VisualStory` when the presentation contains more than a terminal transcript. Each scene contains named source, terminal, media, or text panels, while each declared outcome points at the panel that proves the promised result. The last scene must contain every outcome panel. This makes “code creates a chart,” “request returns this response,” and “filter produces this image” enforceable story contracts instead of captions that can drift from the rendered demo.

The core accepts exact `StorySourceText` plus optional renderer-neutral syntax spans. It does not execute source or depend on PowerShell, Roslyn, Tree-sitter, or regex coloring. Hosts can implement `IStorySourceTokenizer`; production tooling can execute an explicitly trusted producer before it hands ChartForgeX the resolved artifacts.

## Script-Free Visual Stories

`VisualMotionTimeline` turns any `VisualGrid` into a deterministic visual story without JavaScript. Assign stable target IDs when adding panels, then sequence restrained entrances or emphasis cues:

```csharp
using ChartForgeX.Motion;

var motion = VisualMotionTimeline.Create()
    .Reveal("title", durationSeconds: 0.65)
    .Fade("subtitle", delaySeconds: 0.12, durationSeconds: 0.5)
    .Cascade(new[] { "projects", "users", "releases" }, initialDelaySeconds: 0.28)
    .Rise("portfolio", delaySeconds: 0.72);

var story = VisualGrid.Create()
    .WithTitle("Engineering Portfolio")
    .WithSubtitle("A reusable story for profiles, releases, reports, or dashboards")
    .WithColumns(3)
    .Add("projects", projectsCard)
    .Add("users", usersCard)
    .Add("releases", releasesCard)
    .Add("portfolio", portfolioTable, columnSpan: 3)
    .WithMotion(motion);

story.SaveSvg("portfolio.svg");
story.SaveHtml("portfolio.html");
story.SavePng("portfolio.png");
```

Motion applies to SVG and complete HTML-page output. PNG always renders the exact completed state. The generated CSS also exposes that completed state for `prefers-reduced-motion` and print, so motion stays decorative rather than becoming a content dependency.

`WithAction(...)` is still static-renderer friendly. SVG/HTML outputs render safe relative, `http(s)`, and `mailto` action URLs when one is supplied; PNG keeps the same visual affordance without embedding a link.

The mini bar and mini sparkline geometry is shared by the SVG and PNG visual-block renderers, so improvements to compact line/bar polish can be applied once instead of redoing each output format separately.

Heatmaps can be tuned for dashboard matrix cards without hand-editing renderer output:

```csharp
var appointmentVolume = Chart.Create()
    .WithTitle("Appointment Volume")
    .WithHeatmapCellGap(5)
    .WithHeatmapCellRadius(7)
    .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always)
    .WithHeatmapScaleLegend(false)
    .WithXLabels("Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
    .AddHeatmapRows(new[] {
        ChartHeatmapRow.Create("9 AM", 9, 3, 2, 6, 4, 4, 12),
        ChartHeatmapRow.Create("10 AM", 11, 2, 4, 6, 7, 6, 9)
    });
```

Point-color ranges keep compact peak-window bar cards readable without one call per highlighted point:

```csharp
var peakHours = Chart.Create()
    .WithDashboardBarPanelStyle()
    .WithHighlightedXAxisRange(7.5, 11.5, ChartColor.FromHex("#DE442F"), opacity: 0.08, label: "review-peak-window")
    .AddBar("Reviews", new[] { 3d, 1, 3, 1, 0, 0, 0, 9, 10, 9, 7 }, ChartColor.FromHex("#D9DCE3"));

peakHours.Series[0].WithPointColorRange(7, 4, ChartColor.FromHex("#DE442F"));
```

Stacked horizontal row cards can use a dashboard preset for department or status splits:

```csharp
var departments = Chart.Create()
    .WithTitle("Employer by Department")
    .WithDashboardStackedRowStyle(showTotals: true)
    .WithXLabels("Engineering", "Maintenance", "Human Resources", "IT")
    .AddHorizontalBar("All employee", new[] { 68d, 62, 65, 70 }, ChartColor.FromHex("#7057E6"))
    .AddHorizontalBar("Terminated", new[] { 25d, 28, 27, 25 }, ChartColor.FromHex("#5FD3D9"))
    .AddHorizontalBar("New hires", new[] { 14d, 12, 13, 15 }, ChartColor.FromHex("#FFB05C"));
```

Multi-line trend cards can pair premium strokes with a reusable focus marker:

```csharp
var attendance = Chart.Create()
    .WithTitle("Attendance Rate")
    .WithDashboardTrendPanelStyle(showLegend: true)
    .WithXLabels("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul")
    .AddSmoothLine("On-time", new[] { 21d, 23, 25, 37, 31, 28, 47 }, ChartColor.FromHex("#7057E6"))
    .AddSmoothLine("Late attend", new[] { 24d, 20, 28, 22, 30, 34, 39 }, ChartColor.FromHex("#FFB05C"))
    .AddSmoothLine("Absent", new[] { 18d, 22, 17, 14, 23, 20, 31 }, ChartColor.FromHex("#5FD3D9"))
    .WithDashboardTrendFocus(4, 37, "Apr", ChartColor.FromHex("#7057E6"), ChartDataLabelPlacement.Right);
```

Segmented metric blocks provide fixed-count progress rows, exact-value performance rows, capsule loops, part-to-whole status strips, funnel columns, and distribution rows without domain-specific card APIs. Items use the theme palette by default; provide a color or semantic status only when a specific item needs one. Filled and empty ticks include renderer-owned shadow/highlight layers, so cards can keep a polished report look in both SVG and PNG without hand-drawing rectangles:

Header chrome is shared across styles: `WithHeaderSymbol()` and `WithMenu()` work the same way for progress rows, capsule loops, funnel columns, composition strips, and distribution rows.

```csharp
var progress = SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows)
    .WithTitle("Project Progress")
    .WithSubtitle("Overall completion rate all projects.")
    .WithHeaderSymbol("%")
    .WithMenu()
    .AddItem("Performing Progress", 89, segments: 44, delta: "+10.2%", status: VisualStatus.Positive)
    .AddItem("Target Sales", 67, segments: 44, delta: "+2.2%", status: VisualStatus.Info)
    .WithAction("Up by 6% compared to last week");
```

When an item needs several options, pass a callback or a prebuilt `SegmentedMetricItem` instead of relying on more domain-specific overloads:

```csharp
var progressWithConfiguredRows = SegmentedMetricBlock.Create()
    .AddItem("Revenue Growth", 87, item => {
        item.Maximum = 100;
        item.Segments = 35;
        item.Delta = "+4.3%";
        item.Status = VisualStatus.Positive;
    })
    .AddItem(new SegmentedMetricItem("Operational Costs", 58)
        .WithProgress(100, 35)
        .WithDelta("-6.4%")
        .WithStatus(VisualStatus.Negative));
```

Set `DisplayValue` when progress rows should show exact values instead of derived percentages. The row can still use `Value` and `Maximum` for filled segment geometry. `DisplayValue` accepts ordinary values plus an optional format, so callers do not need to pre-format simple counts or currency strings:

```csharp
var performance = SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows)
    .WithTitle("Content Performance")
    .AddItem(new SegmentedMetricItem("Posts", 86)
        .WithProgress(100, 44)
        .WithDisplayValue(132034, "N0")
        .WithDelta("+4.3%")
        .WithStatus(VisualStatus.Positive));
```

Part-to-whole strips are the same model again: a calmer visual treatment for certificate counts, sales platforms, checks, inventory states, or any other split:

```csharp
var certificates = SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip)
    .WithTitle("Certificate Count")
    .WithSubtitle("Inventory split by lifecycle state.")
    .WithMetric("Certificates", 277)
    .AddItem("Valid", 164, displayValue: "164")
    .AddItem("Expiring", 48, displayValue: "48")
    .AddItem("Revoked", 24, displayValue: "24")
    .AddItem("Unknown", 41, displayValue: "41");
```

Use `CapsuleLoop` for intentionally balanced compact part-to-whole compositions. Skewed inventory and count data should use `CompositionStrip` or `DistributionRows` so exact values stay calm and readable.

```csharp
var channels = SegmentedMetricBlock.Create(SegmentedMetricStyle.CapsuleLoop)
    .WithTitle("Channel Share")
    .AddItem("Direct", 40, displayValue: "24,000")
    .AddItem("Partner", 35, displayValue: "21,000")
    .AddItem("Referral", 15, displayValue: "9,000")
    .AddItem("Other", 10, displayValue: "6,000");
```

Ordered stage visuals use the same item model. `segments` controls the compact vertical tick count for each stage:

```csharp
var funnel = SegmentedMetricBlock.Create(SegmentedMetricStyle.FunnelColumns)
    .WithTitle("Conversion Funnel")
    .AddItem("Clicks", 82000, segments: 24, displayValue: "82,000")
    .AddItem("Added to Cart", 7200, segments: 16, displayValue: "7,200")
    .AddItem("Payment", 1230, segments: 12, displayValue: "1,230")
    .AddItem("Abandoned Cart", 5970, segments: 15, displayValue: "5,970");
```

Heatmap insight cards cover the appointment-volume pattern where the matrix needs a reusable right rail instead of an ordinary legend:

```csharp
var appointmentVolume = HeatmapInsightCard.Create()
    .WithTitle("Appointment Volume")
    .WithControls("Day", "Week", "Week 1 (Jan 1 - Jan 7, 2024)")
    .WithColumns("S", "M", "T", "W", "T", "F", "S")
    .WithColorKey(0, 12, ChartColor.FromHex("#D7F5F7"), ChartColor.FromHex("#08798C"))
    .AddRow("9 AM", 9, 3, 2, 6, 4, 4, 12)
    .AddRow("10 AM", 11, 2, 4, 6, 7, 6, 9)
    .AddInsight("Fri, 5 PM - 6 PM", "16 appointments")
    .AddInsight("Mon, 7 PM - 9 PM", "12 appointments");
```

Composition strips use the same `SegmentedMetricBlock` surface with a different visual treatment:

```csharp
var tasks = SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip)
    .WithTitle("Overall Tasks")
    .WithMetric("Tasks", 23, "Task")
    .AddItem("On Going", 12, pattern: ChartFillPattern.DiagonalForward)
    .AddItem("Under Review", 6)
    .AddItem("Finish", 4)
    .WithAction("View details task");
```

Distribution rows also use `SegmentedMetricBlock`; the optional symbol/display-value fields stay generic:

```csharp
var currencies = SegmentedMetricBlock.Create(SegmentedMetricStyle.DistributionRows)
    .WithTitle("Net Earning")
    .WithMetric("Net earning", "EUR 56,980.00", caption: "Last month")
    .AddItem("Euro (EUR)", 38.48, color: ChartColor.FromHex("#1389F2"), symbol: "EUR", displayValue: "EUR 20.23")
    .AddItem("United States Dollar (USD)", 14.11, color: ChartColor.FromHex("#24D47B"), symbol: "USD", displayValue: "EUR 12.00")
    .AddItem("British Pound Sterling (GBP)", 12.55, color: ChartColor.FromHex("#5FD3D9"), symbol: "GBP", displayValue: "EUR 10.00");
```

Workload list blocks cover staff-capacity rows and selectable people lists:

```csharp
var workload = WorkloadListBlock.Create()
    .WithTitle("Today Staff Workload")
    .AddPerson("Panji Dwi", "Zumba Trainer", 4, 8, VisualStatus.Neutral, "PD", "4/8")
    .AddPerson("Raihan Fikri", "Aerobik Trainer", 10, 8, VisualStatus.Negative, "RF", "10/8", note: "Overload")
    .AddPerson("Mufti Hidayat", "Massage Specialist", 6, 8, VisualStatus.Positive, selected: true)
    .WithSelectionControls();
```

Activity timelines provide the chart-like vertical event-spine pattern without flattening nested checklist rows into a generic list. App chrome such as tabs, notes, and action buttons belongs in semantic HTML/interactivity rather than this static SVG/PNG block:

```csharp
var timeline = ActivityTimelineBlock.Create()
    .WithTransparentBackground()
    .WithCard(false)
    .WithEventSurfaces(false)
    .AddSection("In-progress")
    .AddEvent("Shipment", status: VisualStatus.Info, detail: "Delivery by Royal Mail Standard", symbol: "S")
    .AddEvent("Shipment 1", status: VisualStatus.Neutral, symbol: "1")
    .AddChecklistItem("Carrier confirmed", completed: true, muted: true)
    .AddChecklistItem("Packing in progress", completed: false)
    .AddHiddenSummary(6, "items hidden")
    .AddSection("Completed")
    .AddEvent("Order created", status: VisualStatus.Positive, symbol: "OK");
```

Schedule timelines cover planner-style time-of-day swimlanes:

```csharp
var schedule = ScheduleTimelineBlock.Create()
    .WithTitle("Project Timeline")
    .WithTimeRange(8, 17, tickInterval: 1)
    .WithCurrentTime(14.2)
    .WithHeaderActions("12/Feb/2025", "Filter", "+ Add Schedule")
    .AddEvent("Meeting Brief Project", 8, 10, lane: 0, color: ChartColor.FromHex("#5EA2F6"), avatars: new[] { "AM", "RF", "PD" })
    .AddEvent("Research Analyze Content", 9, 11, lane: 1, color: ChartColor.FromHex("#8B5CF6"), avatars: new[] { "SC", "MR" })
    .AddEvent("Report Review", 16, 17.2, lane: 0, color: ChartColor.FromHex("#5EA2F6"), badge: "Report");
```

Visual blocks are exercised through real host-surface contracts as well as isolated component examples. The generated gallery includes a compact 584-pixel email summary, report strips, and a transparent overlay; those artifacts share the same SVG/PNG numeric baseline as charts, topology, wallpapers, and social previews.

Further extension points should continue to be driven by real PowerBGInfo, ImagePlayground, email, Word, and wallpaper scenarios:

- richer table/list style presets
- optional small icon symbols for status cells and list markers
- reusable status palettes
- compact infographic snippets that reuse shared primitive layout/styling, not arbitrary markup
- additional host-sized fixtures when a new reusable layout contract lands
