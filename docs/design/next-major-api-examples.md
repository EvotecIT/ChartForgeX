# Next-major API: example migrations

This is the example companion to the [API design](next-major-api.md). I use the current README and gallery at `cee06bbee0d5ef2148db7622ebdd70cfe1954e56` for the before side. Every after block is a proposed API, not a runnable example for the current package. I retain the original data and meaningful settings; imports and repeated file exports are omitted where they do not change.

## README coverage

| README example | Migration |
| --- | --- |
| Install core / Mermaid / interactivity | `dotnet add package` names stay the same. |
| Animate existing pixels | `RasterAnimationFrame`, `RasterAnimationOptions`, `Encode` and `WriteTo` stay the same. |
| Your first chart | R1 below. |
| Generate the examples locally | `dotnet run --project ChartForgeX.Examples -c Release -- --output ./artifacts/gallery` stays the same. |
| Work with your own data | R2 below. |
| Configure and update a series | R3 below. |
| One layout, several outputs | R4 below. |
| Reports, dashboards, and composed images | R5 below. |
| Diagrams and Mermaid | R6 below; topology is also covered by G4. |
| Optional interactive HTML | R7 below. |
| SVG/HTML/PNG/image/artifact output table | `To*`, `Save*`, `Write*`, `Prepare` and semantic artifact operations retain their purposes. |

### R1. Weekly orders

Before, from [README](../../README.md#your-first-chart):

```csharp
var chart = Chart.Create()
    .WithTitle("Weekly orders")
    .WithSubtitle("Completed orders across the working week")
    .WithSize(800, 440)
    .WithXAxis("Day").WithYAxis("Orders")
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
    .AddLine("Completed", ChartPoints.FromValues(28, 34, 41, 48, 32))
    .AddLine("Target", ChartPoints.FromValues(30, 35, 40, 45, 50));
```

After:

```csharp
var chart = Chart.Line("Weekly orders", c => c
    .Subtitle("Completed orders across the working week")
    .Size(800, 440)
    .X("Day", x => x.Labels("Mon", "Tue", "Wed", "Thu", "Fri"))
    .Y("Orders")
    .Series("Completed", new double[] { 28, 34, 41, 48, 32 })
    .Series("Target", new double[] { 30, 35, 40, 45, 50 }));
chart.SaveSvg("orders.svg");
chart.SavePng("orders.png");
chart.SaveHtml("orders.html");
```

The shorthand still assigns X = 1 through N. The README's count-axis note becomes `Y(y => y.Ticks(t => t.Integer(true)))`, or physical `X` for horizontal bars. A dashed target is an optional deliberate style change, not part of this behavior-preserving migration.

### R2. Typed application data

Keep the original input:

```csharp
var readings = new[] {
    new { Minute = 0d, Cpu = 35d },
    new { Minute = 5d, Cpu = 42d },
    new { Minute = 10d, Cpu = 61d }
};
```

Before:

```csharp
var chart = Chart.Create().WithTitle("CPU usage")
    .WithXAxis("Minutes").WithYAxis("CPU (%)")
    .ConfigureYAxis(axis => axis.WithBounds(0, 100))
    .AddLine("CPU", readings, row => row.Minute, row => row.Cpu);
```

After:

```csharp
var chart = Chart.Line("CPU usage", c => c
    .X("Minutes").Y("CPU (%)", y => y.Bounds(0, 100))
    .Series("CPU", readings, row => new ChartPoint(row.Minute, row.Cpu)));
chart.SavePng("cpu.png");
```

The typed selector executes once at ingestion. It preserves numeric X = 0, 5, 10 and does not introduce category indexing or reflection.

### R3. Retained series and dark theme

Before:

```csharp
var orders = ChartSeries.CreateLine(new ChartSeriesId("completed-orders"), "Completed",
    ChartPoints.FromValues(28, 34, 41, 48, 32));
orders.ConfigureMarkers(markers => markers.Radius = 4);
var chart = Chart.Create().WithTitle("Weekly orders")
    .WithTheme(VisualTheme.Graphite(), VisualThemeMode.Dark).AddSeries(orders);
orders.WithName("Completed orders");
```

After:

```csharp
var chart = Chart.Line("Weekly orders", c => c
    .Theme(VisualTheme.Graphite()).Mode(ThemeMode.Dark)
    .Series("Completed", new double[] { 28, 34, 41, 48, 32 },
        s => s.Id("completed-orders").Marker(m => m.Radius(4))));
var orders = chart.Series.Get(new SeriesId("completed-orders"));
orders.Name = "Completed orders";
chart.SavePng("orders-dark.png");
```

The handle is the typed child model, with the same identity after renaming. Further fluent edits use `orders.Edit(s => s.Marker(...))`. Updating complete typed observations and replacing the collection keep their distinct retention/clearing rules from the main design.

### R4. Prepare once, export several ways

Before:

```csharp
var chart = Chart.Create().WithXLabels("North", "South", "East", "West")
    .AddBar("Orders", ChartPoints.FromValues(28, 34, 41, 48));
var context = new VisualRenderContext(
    new VisualLayoutOptions(new VisualSize(800, 440)),
    themeMode: VisualThemeMode.Dark,
    frame: new VisualFrame("Orders by region"));
var prepared = chart.Prepare(context);
string svg = prepared.ToSvg();
byte[] png = prepared.ToPng();
```

After:

```csharp
var chart = Chart.Bar("Orders by region", c => c
    .X(x => x.Labels("North", "South", "East", "West"))
    .Series("Orders", new double[] { 28, 34, 41, 48 }));
var context = new VisualRenderContext(
    new VisualLayoutOptions(new VisualSize(800, 440)),
    themeMode: ThemeMode.Dark);
var prepared = chart.Prepare(context);
string svg = prepared.ToSvg();
byte[] png = prepared.ToPng();
```

The frame title moves into the model here; an explicit `VisualFrame` remains available as a render-request override. There is still one layout pass, with the same explicit font registration for repeatable cross-host text.

### R5. Two chart panels

Before:

```csharp
var trend = Chart.Create().WithTitle("Orders over time")
    .AddLine("Orders", ChartPoints.FromValues(28, 34, 41, 48, 32));
var regions = Chart.Create().WithTitle("Orders by region")
    .WithXLabels("North", "South", "East")
    .AddBar("Orders", ChartPoints.FromValues(52, 38, 46));
var report = ChartGrid.Create().WithTitle("Order summary")
    .WithColumns(2).WithPanelSize(520, 320).Add(trend).Add(regions);
```

After:

```csharp
var report = Chart.Grid("Order summary", g => {
    g.Columns(2).PanelSize(520, 320);
    g.Panel(Chart.Line("Orders over time", c =>
        c.Series("Orders", new double[] { 28, 34, 41, 48, 32 })));
    g.Panel(Chart.Bar("Orders by region", c => c
        .X(x => x.Labels("North", "South", "East"))
        .Series("Orders", new double[] { 52, 38, 46 })));
});
report.SaveHtml("summary.html");
report.SavePng("summary.png");
```

`Panel` is the repeated grid child. This is a visual arrangement for export; the report host supplies report pages, tabs, filters and other page structure.

### R6. Mermaid result and diagnostics

Before:

```csharp
var result = MermaidRenderer.Render(@"flowchart LR
    request[Request] --> review{Approved?}
    review -->|Yes| complete[Complete]
    review -->|No| revise[Revise]");
foreach (var diagnostic in result.Diagnostics)
    Console.WriteLine(diagnostic.Code + ": " + diagnostic.Message);
if (result.Artifact != null) {
    File.WriteAllText("process.svg", result.Artifact.ToSvg());
    File.WriteAllBytes("process.png", result.Artifact.ToPng());
}
```

After, using a generated options builder on the existing optional Mermaid entry point:

```csharp
var result = MermaidRenderer.Render(@"flowchart LR
    request[Request] --> review{Approved?}
    review -->|Yes| complete[Complete]
    review -->|No| revise[Revise]", m =>
        m.Theme(productTheme).Mode(ThemeMode.Light));
foreach (var diagnostic in result.Diagnostics)
    Console.WriteLine(diagnostic.Code + ": " + diagnostic.Message);
if (result.Artifact != null) {
    File.WriteAllText("process.svg", result.Artifact.ToSvg());
    File.WriteAllBytes("process.png", result.Artifact.ToPng());
}
```

`productTheme` is the host's paired `VisualTheme`; omitting the callback uses shared defaults. Result/diagnostic handling and warning policy remain visible.

### R7. Explicit interactive export

Before:

```csharp
var chart = Chart.Create().WithTitle("Weekly orders")
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
    .AddLine("Orders", ChartPoints.FromValues(28, 34, 41, 48, 32));
chart.SaveInteractiveHtml("explore.html", options => {
    options.Interaction.Enable(
        ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection |
        ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Export);
});
```

After:

```csharp
var chart = Chart.Line("Weekly orders", c => c
    .X(x => x.Labels("Mon", "Tue", "Wed", "Thu", "Fri"))
    .Series("Orders", new double[] { 28, 34, 41, 48, 32 }));
chart.SaveInteractiveHtml("explore.html", html => html
    .Interaction(i => i.Tooltips(true).Selection(true).KeyboardNavigation(true).Export(true)));
```

The optional adapter still owns the export and runtime. `SaveHtml` alone does not acquire scripting. The adapter's default runtime remains the landed default; selecting `Standalone` is an explicit setting.

## Selecting the ten gallery examples

There is no popularity dataset in the inspected gallery sources. I use a reproducible repository-reference proxy instead of claiming measured audience usage. I count distinct authored files mentioning each existing downloadable recipe ID, grouping only terminal `-light`/`-dark` variants. I search `.cs`, `.md`, `.ps1` and `.html` in `ChartForgeX.Examples`, `ChartForgeX.Tests`, `docs`, `Website/content`, `Website/tools`, plus `README.md`. Generated output and JSON baselines do not contribute counts. A mention must match a complete slug token; IDs split across string interpolation do not count. Sort by count descending, then normalized ID using ordinal ordering.

Counts indicate visibility in this repository, not application runtime frequency. The one-reference ties are weak evidence; I would replace them if actual usage data is available.

| Rank | Normalized gallery ID | Referencing authored files | Representative source |
| --- | --- | ---: | --- |
| G1 | `control-coverage-heatmap` | 5 | [ControlCoverageRecipe](../../ChartForgeX.Examples/Recipes/ControlCoverageRecipe.cs) |
| G2 | `family-heatmap-wide` | 2 | [HeatmapRecipe](../../ChartForgeX.Examples/Recipes/HeatmapRecipe.cs) |
| G3 | `sequence-expanded` | 2 | [SequenceRecipe](../../ChartForgeX.Examples/Recipes/SequenceRecipe.cs) |
| G4 | `topology-expanded` | 2 | [TopologyRecipe](../../ChartForgeX.Examples/Recipes/TopologyRecipe.cs) |
| G5 | `chart-grid-wide` | 1 | [ChartGridRecipe](../../ChartForgeX.Examples/Recipes/ChartGridRecipe.cs) |
| G6 | `family-bar-issuers` | 1 | [CertificatesByIssuerRecipe](../../ChartForgeX.Examples/Recipes/CertificatesByIssuerRecipe.cs) |
| G7 | `family-donut-wide` | 1 | [DonutRecipe](../../ChartForgeX.Examples/Recipes/DonutRecipe.cs) |
| G8 | `family-line-latency-week` | 1 | [LatencyWeekRecipe](../../ChartForgeX.Examples/Recipes/LatencyWeekRecipe.cs) |
| G9 | `family-line-wide` | 1 | [LineRecipe](../../ChartForgeX.Examples/Recipes/LineRecipe.cs) |
| G10 | `family-trend-line-compact-precision` | 1 | [TrendLineRecipe](../../ChartForgeX.Examples/Recipes/TrendLineRecipe.cs) |

The code below shows focused authoring excerpts. Each migration keeps the source recipe's data-generation helper unchanged unless the same data is written explicitly. Existing setup uses one `VisualRenderContext` for the title, subtitle, dimensions, theme and surface; after migration that context can stay unchanged except for `ThemeMode`, or those properties can move into the factory callback. The generated recipe wrapper still prepares once and exports SVG, PNG and static HTML.

### G1. Control coverage matrix

Before:

```csharp
return Chart.Create().WithTitle("Control Coverage Matrix")
    .WithSubtitle("Heatmap rows for comparing domain groups across security controls")
    .WithXAxis("Control").WithYAxis("Domain group")
    .WithTheme(VisualTheme.Report(), mode).WithSize(980, 560)
    .WithLegend(false).WithDataLabels().WithHeatmapScale(ChartHeatmapScale.Semantic)
    .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS", "TLS-RPT", "CT")
    .AddHeatmapRow("Primary domains", Points(96, 88, 74, 63, 58, 92))
    .AddHeatmapRow("Parked domains", Points(74, 62, 51, 42, 38, 66))
    .AddHeatmapRow("Regional domains", Points(82, 77, 68, 54, 49, 80))
    .AddHeatmapRow("Acquired domains", Points(58, 43, 36, 28, 25, 52));
```

After:

```csharp
return Chart.Heatmap("Control Coverage Matrix", c => c
    .Subtitle("Heatmap rows for comparing domain groups across security controls")
    .X("Control", x => x.Labels("SPF", "DMARC", "DNSSEC", "MTA-STS", "TLS-RPT", "CT"))
    .Y("Domain group").Theme(VisualTheme.Report()).Mode(mode).Size(980, 560)
    .Legend(l => l.Visible(false)).Labels(l => l.Visible(true))
    .ColorScale(scale => scale.Kind(ColorScaleKind.Semantic))
    .Values(v => v.Formatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%"))
    .Row("primary", "Primary domains", new double?[] { 96, 88, 74, 63, 58, 92 })
    .Row("parked", "Parked domains", new double?[] { 74, 62, 51, 42, 38, 66 })
    .Row("regional", "Regional domains", new double?[] { 82, 77, 68, 54, 49, 80 })
    .Row("acquired", "Acquired domains", new double?[] { 58, 43, 36, 28, 25, 52 }));
```

The semantic scale remains explicit; the shared theme provides its role values. These measurements are 0–100, so the formatter appends `%` without multiplying them by 100.

### G2. Coverage across service areas

Keep `areas`, `sites`, `baselines`, `siteOffsets` and the source's deterministic `Math.Sin` cell calculation.

Before:

```csharp
var chart = Chart.Create().WithXLabels(areas);
for (var row = 0; row < sites.Length; row++) {
    var site = Array.IndexOf(new[] { "London", "Frankfurt", "Warsaw", "New York", "Singapore", "Sydney" }, sites[row]);
    var values = areas.Select((_, column) => (double?)Math.Min(99, Math.Round(
        baselines[column] + siteOffsets[site] + Math.Sin(column * 1.9 + site * 1.3) * 6))).ToArray();
    chart.AddHeatmapRow(sites[row], values,
        id: new ChartSeriesId(sites[row].Replace(" ", "-").ToLowerInvariant() + "-coverage"));
}
return chart.ConfigureHeatmapColorDimension(d => d.Name = "Coverage")
    .WithHeatmapScale(ChartHeatmapScale.Sequential).WithHeatmapCellGap(3)
    .WithHeatmapCellRadius(3).WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Auto)
    .WithValueFormat(ChartValueFormat.Number("0'%'", CultureInfo.InvariantCulture));
```

After:

```csharp
return Chart.Heatmap("Coverage across service areas", c => {
    c.X(x => x.Labels(areas));
    c.ColorScale(scale => scale.Name("Coverage").Kind(ColorScaleKind.Sequential));
    c.Cells(cell => cell.Gap(3).Radius(3).ValueText(ValueTextMode.Auto));
    c.Values(v => v.Format(ChartValueFormat.Number("0'%'", CultureInfo.InvariantCulture)));
    for (var row = 0; row < sites.Length; row++) {
        var site = Array.IndexOf(new[] { "London", "Frankfurt", "Warsaw", "New York", "Singapore", "Sydney" }, sites[row]);
        var values = areas.Select((_, column) => (double?)Math.Min(99, Math.Round(
            baselines[column] + siteOffsets[site] + Math.Sin(column * 1.9 + site * 1.3) * 6))).ToArray();
        c.Row(sites[row].Replace(" ", "-").ToLowerInvariant() + "-coverage", sites[row], values);
    }
});
```

The sparse variant uses the same `Row` operation with null cells, retains the explicit primary/secondary IDs, and never treats absent coverage as zero.

### G3. Request processing sequence

Before, focused on the repeated-child and positional-span operations:

```csharp
var model = SequenceArtifact.Create("request-sequence").WithSize(960, 760)
    .AddParticipant("client", "Client", SequenceArtifactParticipantKind.Actor)
    .AddParticipant("service", "Service", SequenceArtifactParticipantKind.Control)
    .AddParticipant("store", "Store", SequenceArtifactParticipantKind.Database)
    .AddParticipant("queue", "Queue", SequenceArtifactParticipantKind.Queue)
    .AddMessage("client", "service", "Submit request")
    .AddMessage("service", "service", "Validate payload")
    .AddMessage("service", "store", "Persist result")
    .AddMessage("service", "queue", "Publish event", kind: SequenceArtifactMessageKind.Async)
    .AddMessage("service", "client", "Return result", SequenceArtifactMessageLineStyle.Dashed,
        SequenceArtifactMessageKind.Return)
    .AddBlock(SequenceArtifactBlockKind.Alt, "Validation outcome", 1, 4)
    .AddBlock(SequenceArtifactBlockKind.Loop, "Retry storage", 2, 2, depth: 1)
    .AddActivation("service", true, 0).AddActivation("service", false, 5)
    .AddNote(SequenceArtifactNotePlacement.Over, new[] { "service", "store" }, "Durable result");
model.Notes[0].StepIndex = 2;
```

After:

```csharp
var model = Diagram.Sequence("Request processing sequence", d => {
    d.Id("request-sequence").Size(960, 760);
    d.Participant("client", "Client", p => p.Kind(ParticipantKind.Actor));
    d.Participant("service", "Service", p => p.Kind(ParticipantKind.Control));
    d.Participant("store", "Store", p => p.Kind(ParticipantKind.Database));
    d.Participant("queue", "Queue", p => p.Kind(ParticipantKind.Queue));
    d.Message("submit", "client", "service", "Submit request");
    d.Message("validate", "service", "service", "Validate payload");
    d.Message("persist", "service", "store", "Persist result");
    d.Message("publish", "service", "queue", "Publish event", m => m.Kind(MessageKind.Async));
    d.Message("return", "service", "client", "Return result",
        m => m.Kind(MessageKind.Return).LineStyle(LineStyle.Dashed));
    d.Block("validation", b => b.Kind(BlockKind.Alt).Label("Validation outcome")
        .From("validate").Through("return")
        .Block("retry", retry => retry.Kind(BlockKind.Loop).Label("Retry storage")
            .From("persist").Through("persist")));
    d.Activation("service", a => a.From("submit").Through("return"));
    d.Note("durable", "Durable result", n => n.Over("service", "store").At("persist"));
});
```

`From`/`Through` include the named messages; the activation closes after the final message, matching the original step-5 boundary. The dashed stroke uses the landed return-line preset. Nested blocks replace the handwritten depth value. Future branches are repeated nested `Branch` children, not separate global lists with parallel indexes.

### G4. Service request topology

Before uses mutable properties and lists. Representative source operations are:

```csharp
var chart = TopologyChart.Create();
chart.Id = "service-topology";
chart.DefaultRenderOptions = new TopologyRenderOptions {
    FitContentToViewport = true, WrapNodeLabels = true,
    IncludeLegend = true, IncludeGroupStatusDots = true
};
chart.Groups.Add(new TopologyGroup {
    Id = "primary", Label = "Primary region", X = 0, Y = 0, Width = 340, Height = 300,
    Status = TopologyHealthStatus.Healthy
});
chart.Nodes.Add(new TopologyNode {
    Id = "cache", Label = "Cache", X = 70, Y = 210, Width = 150, Height = 65,
    Shape = TopologyNodeShape.Cylinder, GroupId = "primary"
});
chart.Edges.Add(new TopologyEdge {
    Id = "request", SourceNodeId = "client", TargetNodeId = "worker", Label = "Requests",
    SecondaryLabel = "Active", Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal,
    Direction = VisualLinkDirection.Forward
});
```

After includes the source's two groups, four nodes, three edges and legend:

```csharp
var chart = Diagram.Topology("Service request topology", d => {
    d.Id("service-topology");
    d.Layout(l => l.FitContent(true).WrapLabels(true).GroupStatusDots(true));
    d.Group("primary", "Primary region", g => {
        g.Position(0, 0).Size(340, 300).State(TopologyHealthStatus.Healthy).Tone(Tone.Success);
        g.Node("client", "Client service", n => n.Position(30, 65).Size(240, 85)
            .Subtitle("Requests").Kind(NodeKind.Server).State(TopologyHealthStatus.Healthy).Tone(Tone.Success)
            .Detail("State", "Available", detail => detail.State(TopologyHealthStatus.Healthy).Tone(Tone.Success)));
        g.Node("cache", "Cache", n => n.Position(70, 210).Size(150, 65).Shape(NodeShape.Cylinder));
    });
    d.Group("secondary", "Secondary region", g => {
        g.Position(390, 0).Size(340, 300).State(TopologyHealthStatus.Warning).Tone(Tone.Warning);
        g.Node("worker", "Processing service", n => n.Position(430, 65).Size(240, 85)
            .Kind(NodeKind.Server).State(TopologyHealthStatus.Warning).Tone(Tone.Warning));
        g.Node("store", "Result store", n => n.Position(470, 210).Size(150, 65).Shape(NodeShape.Cylinder));
    });
    d.Edge("request", "client", "worker", e => e.Label("Requests").SecondaryLabel("Active")
        .Route(r => r.Kind(RouteKind.ObstacleAvoidingOrthogonal)).Direction(LinkDirection.Forward));
    d.Edge("cache-read", "client", "cache", e => e.Label("Lookup")
        .Route(r => r.Kind(RouteKind.Curved)).LineStyle(LineStyle.Dashed)
        .Direction(LinkDirection.Bidirectional));
    d.Edge("persist", "worker", "store", e => e.Label("Persist")
        .Route(r => r.Kind(RouteKind.Orthogonal)));
    d.Legend(l => l.Visible(true)
        .Item("available", "Available", i => i.State(TopologyHealthStatus.Healthy).Tone(Tone.Success))
        .Item("review", "Review", i => i.State(TopologyHealthStatus.Warning).Tone(Tone.Warning))
        .Item("service", "Service", i => i.NodeKind(NodeKind.Server))
        .Item("dependency", "Dependency", i => i.EdgeKind(EdgeKind.Dependency).LineStyle(LineStyle.Dashed)));
});
```

Positions remain absolute model coordinates even inside a group lambda, matching the current model. Health states remain domain facts in `State` properties; `Tone` expresses their shared presentation and can be omitted when the theme derives it from state. New automatic-layout recipes can omit positions. The source's shape-atlas variant remains a typed loop over shapes with `PreserveDisplayModeSize` and hidden status badges.

### G5. Shared-axis chart grid

Keep `observations`, the six weekday labels and six region labels from `ChartGridRecipe`.

Before:

```csharp
var requests = Chart.Create().WithTitle("Observed requests").WithXLabels(days)
    .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("requests"), "Requests", observations));
var reviewed = Chart.Create().WithTitle("Reviewed requests").WithXLabels(regions)
    .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("orders"), "Orders", observations));
var capacity = Chart.Create().WithTitle("Capacity used").WithXLabels(days)
    .AddSeries(ChartSeries.CreateArea(new ChartSeriesId("used-capacity"), "Used capacity", observations));
return ChartGrid.Create().WithColumns(2).WithGap(16).WithPanelSize(640, 400)
    .Add(requests).Add(reviewed).Add(capacity, 2).WithSharedAxes();
```

After:

```csharp
return Chart.Grid("Requests and capacity", g => {
    g.Columns(2).Gap(16).PanelSize(640, 400).Axes(a => a.Shared(true));
    g.Panel(Chart.Line("Observed requests", c => c.X(x => x.Labels(days))
        .Values(v => v.Format(numberFormat))
        .Series("Requests", observations, s => s.Id("requests"))));
    g.Panel(Chart.Bar("Reviewed requests", c => c.X(x => x.Labels(regions))
        .Values(v => v.Format(numberFormat))
        .Series("Orders", observations, s => s.Id("orders"))));
    g.Panel(Chart.Area("Capacity used", c => c.X(x => x.Labels(days))
        .Values(v => v.Format(numberFormat))
        .Series("Used capacity", observations, s => s.Id("used-capacity"))), span: 2);
});
```

`numberFormat` is the original `ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture)`, present on all three source charts. Shared domains do not imply that region categories and weekdays have the same semantic keys; linking uses their own facts.

### G6. Certificates issued per month

Keep the source's 12 month labels and issuer tuples: `("lets-encrypt", "Let's Encrypt", 64)`, `("digicert", "DigiCert", 18)`, `("sectigo", "Sectigo", 11)`, `("internal-ca", "Internal CA", 7)`.

Before:

```csharp
var chart = Chart.Create().WithXLabels(months).WithStackedBars().WithYAxis("Certificates")
    .WithValueFormat(ChartValueFormat.Number("0", CultureInfo.InvariantCulture));
foreach (var (id, name, typical) in issuers) {
    var values = Enumerable.Range(0, 12).Select(month => new ChartPoint(month + 1,
        typical + (month % 3 == 0 ? typical / 3 : 0) - month % 4));
    chart.AddSeries(ChartSeries.CreateBar(new ChartSeriesId(id), name, values));
}
```

After:

```csharp
var chart = Chart.Bar("Certificates issued per month", c => {
    c.X(x => x.Labels(months)).Y("Certificates");
    c.Values(v => v.Format(ChartValueFormat.Number("0", CultureInfo.InvariantCulture)));
    foreach (var (id, name, typical) in issuers) {
        var values = Enumerable.Range(0, 12).Select(month => new ChartPoint(month + 1,
            typical + (month % 3 == 0 ? typical / 3 : 0) - month % 4));
        c.Series(name, values, s => s.Id(id).Stack(stack => stack.Group("issuers")));
    }
});
```

All four datasets join one explicit stack group. Positive/negative stack and normalized/raw-source semantics come from core; a global bar switch no longer hides which series belong together.

### G7. Services contributing to revenue

Before:

```csharp
var revenue = new ChartSeries(new ChartSeriesId("revenue"), "Revenue",
    ChartSeriesKind.Donut, Observations(variant));
var chart = Chart.Create()
    .WithXLabels("Subscriptions", "Consulting", "Support", "Licenses", "Training", "Other")
    .AddSeries(revenue).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
```

After, preserving the wide recipe's values totaling 1,000:

```csharp
var chart = Chart.Donut("Services contributing to revenue", c => c
    .X(x => x.Labels("Subscriptions", "Consulting", "Support", "Licenses", "Training", "Other"))
    .Values(v => v.Format(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture)))
    .Series("Revenue", new double[] { 312, 241, 187, 133, 79, 48 }, s => s.Id("revenue")));
```

The `options` variant's center and slice treatments also fit the same model:

```csharp
chart.Edit(c => c.Labels(l => l.Visible(true).Content(SliceLabelContent.Label))
    .Center(center => center.Value("1,000").Caption("Revenue (k)")));
var revenue = chart.Series.Get(new SeriesId("revenue"));
revenue.Edit(s => s.Item(0, item => item.Offset(.12))
    .Item(1, item => item.Pattern(FillPattern.Crosshatch)));
```

Here ordinal item edits retain the source recipe's one-shot semantics. Live consumers should assign observation IDs and use keyed `Item(id, ...)` edits. Sparse and automatic Other behavior follow the landed core contract.

### G8. Domain controller round-trip time

Keep all 168 UTC hours per controller and the source recipe's baseline/load/ripple/incident formula, including the four-hour incident on `dc-03`.

Before:

```csharp
var chart = Chart.Create().WithXAxisTimeScale(TimeZoneInfo.Utc)
    .WithYAxis("Round-trip time (ms)").WithValueUnit("ms")
    .WithValueFormat(ChartValueFormat.Number("0.#", CultureInfo.InvariantCulture));
// The source loop builds each controller's points.
chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId(name), name, points));
```

After:

```csharp
var chart = Chart.Line("Domain controller round-trip time", c => {
    c.X(x => x.Time(t => t.Zone(TimeZoneInfo.Utc)));
    c.Y("Round-trip time (ms)");
    c.Values(v => v.Unit("ms").Format(ChartValueFormat.Number("0.#", CultureInfo.InvariantCulture)));
    var start = new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);
    var controllers = new[] { ("dc-01", 18d), ("dc-02", 27d), ("dc-03", 41d) };
    for (var controller = 0; controller < controllers.Length; controller++) {
        var (name, baseline) = controllers[controller];
        var points = new List<ChartPoint>();
        for (var hour = 0; hour < 7 * 24; hour++) {
            var load = hour % 24 is >= 8 and < 18 && hour / 24 % 7 < 5 ? 9 : 0;
            var noise = Math.Sin(hour * 12.9898 + controller * 78.233) * 43758.5453;
            var ripple = (noise - Math.Floor(noise)) * 6;
            var incident = controller == 2 && hour is >= 98 and < 102 ? 46 : 0;
            points.Add(new ChartPoint(start.AddHours(hour), Math.Round(baseline + load + ripple + incident, 1)));
        }
        c.Series(name, points, s => s.Id(name));
    }
});
```

UTC timestamps stay real time coordinates. This recipe does not imply standalone browser time-window support beyond the adapter's declared scope.

### G9. Weekly request volume

Before, the source's wide branch:

```csharp
var weeks = Enumerable.Range(1, 12).ToArray();
chart.WithXLabels(weeks.Select(week => "W" + week).ToArray()).WithYAxis("Requests");
chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("requests"), "Portal",
    weeks.Select(week => new ChartPoint(week, Math.Round(820 + week * 64 + Math.Sin(week * 1.1) * 60)))));
chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("api"), "API",
    weeks.Select(week => new ChartPoint(week, Math.Round(640 + Math.Sin(week * .7) * 140)))));
chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("email"), "Email",
    weeks.Select(week => new ChartPoint(week, Math.Round(760 - week * 38 + Math.Cos(week * 1.4) * 40)))));
```

After:

```csharp
var weeks = Enumerable.Range(1, 12).ToArray();
var chart = Chart.Line("Weekly request volume", c => c
    .X(x => x.Labels(weeks.Select(week => "W" + week).ToArray())).Y("Requests")
    .Values(v => v.Format(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture)))
    .Series("Portal", weeks.Select(week => new ChartPoint(week,
        Math.Round(820 + week * 64 + Math.Sin(week * 1.1) * 60))), s => s.Id("requests"))
    .Series("API", weeks.Select(week => new ChartPoint(week,
        Math.Round(640 + Math.Sin(week * .7) * 140))), s => s.Id("api"))
    .Series("Email", weeks.Select(week => new ChartPoint(week,
        Math.Round(760 - week * 38 + Math.Cos(week * 1.4) * 40))), s => s.Id("email")));
```

The `options` variant uses `Interpolation(Step)`, nested `Step(Position)` and `Marker(Shape, Enabled, Radius)` settings; the sparse variant retains the original X positions and break before the last observation. Neither becomes a dense value array accidentally.

### G10. Small signed fitted drift

Before:

```csharp
var drift = ChartSeries.CreateTrendLine(new ChartSeriesId("least-squares-drift"), "Least-squares drift",
    Enumerable.Range(0, 4096).Select(index => new ChartPoint(index % 10 + .5, index % 7 - 3)));
return Chart.Create().WithXAxis("Measurement").WithYAxis("Fitted drift").AddSeries(drift);
```

After:

```csharp
return Chart.TrendLine("Small signed drift", c => c.Size(360, 360)
    .Subtitle("Measured differences stay in their original units")
    .X("Measurement").Y("Fitted drift")
    .Series("Least-squares drift",
        Enumerable.Range(0, 4096).Select(index => new ChartPoint(index % 10 + .5, index % 7 - 3)),
        s => s.Id("least-squares-drift").Fit(fit => fit.Method(FitMethod.LeastSquares))));
```

Retain all 4,096 source samples and their signed units. Fitted endpoints are derived observations, not replacements for the source facts. Automatic precision must still show the small drift; this migration does not round it away or rescale undeclared units.
