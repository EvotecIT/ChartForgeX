# Next-major API design

Design proposal, 2 November 2026. I want fluent settings, lambdas for repeated children and nested settings, and properties as the source of truth for the model. This document proposes the next major API; none of its new signatures are implemented by this change.

The [before/after examples](next-major-api-examples.md) are part of this design. They cover every API-bearing README example and ten gallery recipes. The [capability inventory](../v2/chart-capabilities.csv), [existing API contracts](../v2/api-conventions.md), and [diagram support matrix](../mermaid-support-matrix.md) remain the references for existing behavior.

## Decisions

1. `Chart.Line("Title", chart => ...)` is the normal entry point. `ChartForgeX.Chart` becomes the factory facade; typed models replace the current all-family `Core.Chart` model. Every supported family has a discoverable factory; `Chart.Mixed` accepts compatible typed series. `ChartForgeX.Diagram` supplies `Topology`, `Flow` and `Sequence` for native diagrams.
2. Model properties are the source of truth. Generated, separate builder types provide methods such as `Title`, `Size`, `X`, `Series`, `Legend` and `Labels`. There are no `With*`, `Configure*`, or `Add*` configuration aliases in the new authoring surface.
3. Typed observations, IDs, detached preparation, and keyed updates survive. I am changing authoring, not replacing the prepared rendering architecture or flattening specialized data into X/Y points.
4. Themes consume the shared visual-token contract. ChartForgeX stays free of runtime package dependencies and has no runtime dependency on another engine.
5. Browser behavior stays in `ChartForgeX.Interactivity.Html`. Static HTML remains script-free unless an adapter is explicitly selected.
6. I migrate consumers directly and remove the superseded API. There is no compatibility layer and no obsolete forwarding surface.

I settled these three choices on 2 November 2026:

| Decision | Design consequence |
| --- | --- |
| One shared build-time generator in PowerForge/PSPublishModule for independent engines, including ChartForgeX and CanopyX. | Each engine declares its domain metadata locally and consumes the same generator implementation. ChartForgeX gains no runtime package dependency. |
| Engine-local `Tone`, `Icon` and `ThemeMode`, with shared wire names and explicit bridge mappings. | The engines stay independent; a common runtime primitives package is unnecessary. |
| `New-Cfx*` PowerShell commands in ImagePlayground. | The generated PowerShell surface stays thin and uses the same property model, without another module or a command per chart family. |

## One model, three ways to author it

```csharp
var chart = Chart.Line("Weekly orders", c => c
    .Subtitle("Completed orders across the working week")
    .Size(800, 440)
    .X("Day", x => x.Labels("Mon", "Tue", "Wed", "Thu", "Fri"))
    .Y("Orders", y => y.Bounds(0, 60))
    .Series("Completed", new double[] { 28, 34, 41, 48, 32 },
        s => s.Id("completed-orders"))
    .Series("Target", new double[] { 30, 35, 40, 45, 50 },
        s => s.Id("target-orders").Stroke(stroke => stroke.Dash(6, 3))));

chart.SavePng("orders.png");
```

`Chart.Line` returns a `LineChart`, which implements `IVisualRenderable`. Its callback receives a generated `LineChartBuilder`. This distinction avoids the C# property/method name collision: `LineChart.Title` and `LineChartBuilder.Title(...)` can coexist because they are on different types. The builder holds only the model it edits; it has no parallel options or renderer.

The same definition uses ordinary initializers:

```csharp
var chart = new LineChart {
    Title = "Weekly orders",
    Subtitle = "Completed orders across the working week",
    Size = new VisualSize(800, 440),
    X = new AxisSettings { Title = "Day", Labels = { "Mon", "Tue", "Wed", "Thu", "Fri" } },
    Y = new AxisSettings { Title = "Orders", Minimum = 0, Maximum = 60 },
    Series = {
        new LineSeries {
            Id = new SeriesId("completed-orders"), Name = "Completed",
            Data = Observation.Values(28, 34, 41, 48, 32)
        },
        new LineSeries {
            Id = new SeriesId("target-orders"), Name = "Target",
            Data = Observation.Values(30, 35, 40, 45, 50),
            Stroke = new StrokeSettings { Dash = new double[] { 6, 3 } }
        }
    }
};
```

The factory's `double[]` overload calls `Observation.Values`: X is 1 through N. Typed `IEnumerable<ChartPoint>` retains its actual coordinates, gaps and IDs. The initializer helper and generated builder use the same ingestion operation. PowerShell sets these same properties through generated parameter bindings; it does not maintain its own chart options.

### Editing and ownership

| Operation | Proposed contract |
| --- | --- |
| `Title(value)`, `Minimum(value)` | Replace one property; the last assignment wins. Null clears only a nullable property. |
| `Size(width, height)`, `Bounds(minimum, maximum)` | Small compound conveniences over existing properties, with atomic validation. Metadata names their backing properties. |
| `X(title, axis => ...)`, `Legend(legend => ...)` | Configure a copy of the existing group and install an independent snapshot after success. Repeated settings lambdas merge edits, rather than resetting other fields. |
| `Series(name, data, series => ...)`, `Node(id, label, node => ...)` | Append one child after its callback succeeds. Child callbacks never partially append. Duplicate explicit IDs reject. |
| `chart.Edit(c => ...)` | Edit a transactional copy and install after success; returns the same model. Used when a named model needs further fluent editing. |
| `chart.Series.Get(new SeriesId("completed-orders"))` | Return the retained typed child model. `Name` changes display text without changing identity. |
| `series.Data.Replace(observations)` | Validate and snapshot a complete replacement; clears observation overrides as today's replacement does. |
| `series.Data.Update(id, observation)` / `ApplyChanges(changes)` | Replace complete typed observations or apply a bounded keyed transaction while retaining surviving identity and presentation. |
| `Prepare(context)` | Validate, resolve theme and callbacks, then return a detached prepared visual. A later model edit has no effect on it. |

Collections expose typed, validated mutation rather than unrestricted `List<object>`. Initializer `Add` and fluent child methods call that same collection operation. Whole-model invariants, such as matching category counts or graph endpoints, are checked before preparation; local setters validate their own finite values immediately. Stored builders expire when their callback returns. Retaining a builder cannot edit an installed snapshot later. Models are mutable and require caller synchronization; prepared results are detached and reusable.

A successful root `Edit` reconciles surviving child IDs into the existing child objects, preserving retained series handles. Removing a child detaches its handle; replacing a child explicitly returns a new handle. Candidate copies never leak through builder callbacks. Copy/apply generation must preserve these rules, including when a callback throws after adding another child. Render-request overrides take precedence over model frame/theme settings for that preparation only and never write back into the model.

`AxisSettings` holds the existing shared axis contract, with explicit scale/category/time settings. The builder validates supported combinations rather than switching to an unrelated axis CLR type when labels or time coordinates are supplied. Getter-owned collection initializers remain valid; setters snapshot supplied nested settings and data.

Explicit IDs are required for live replacement, linking and cross-view state. Static one-shot recipes can omit them; generated positional IDs are stable only while insertion order stays unchanged. Display labels, sort order and colour rank never become identity. `SeriesId` and `ObservationId` are intentional next-major names for the current prefixed IDs, not stringly typed replacements.

## Shared settings and axes

| Concern | Proposed shape | Behavior retained or added |
| --- | --- | --- |
| Frame | `Title`, `Subtitle`, `Size`, `Padding`, `Surface` | One measured frame; explicit logical dimensions remain available for images. |
| Theme | `Theme(VisualTheme)`, `Mode(ThemeMode.Light/Dark)` | Paired immutable theme; no process-wide theme mutation. |
| Accessibility | `Accessibility(a => a.Title(...).Description(...).Language(...))` | Plain text, complete data facts and stable semantic regions. |
| Values | `Values(v => v.Format(format).Unit("ms"))` | Shared format fallback; invariant defaults, explicit culture for local presentation. |
| Labels | `Labels(l => l.Visible(true).Placement(...).Formatter(...))` | Layout visibility is separate from formatter output. |
| Legend | `Legend(l => l.Visible(...).Position(...).MaximumRows(...))` | Existing eight positions, density budget and per-series entry visibility. |
| Typed series | `Series(name, observations, s => ...)` | Family-specific settings and observation type; no irrelevant family option bag. |
| Annotations | `ReferenceLine(id, value, r => ...)`, `Annotation(id, a => ...)` | Source coordinates, IDs, clipping and semantic facts retained. |
| Export | `Prepare`, `ToSvg`, `ToPng`, `ToHtmlFragment`, `ToHtmlPage`, `Save*`, `Write*` | Keep the existing meaningful operation vocabulary. No ambiguous parameterless `Save`. |

`X` and `Y` always mean physical axes. `Orientation(Horizontal)` transposes category/value projection without silently rebinding a named physical axis. Mixed charts declare compatible coordinates once. Semantic series bindings use axis IDs (`ValueAxis("latency")`); adding extra axes never changes which existing series uses which scale.

```csharp
var mixed = Chart.Mixed("Orders and latency", c => {
    c.X("Day", x => x.Labels("Mon", "Tue", "Wed"));
    c.Y("Orders", y => y.Id("orders").Ticks(t => t.Integer(true)));
    c.Axis("latency", a => a.Side(AxisSide.Right).Title("Latency (ms)")
        .Bounds(0, 100).Ticks(t => t.Values(0, 25, 50, 75, 100)));
    c.Bar("Orders", new double[] { 28, 34, 41 }, s => s.ValueAxis("orders"));
    c.Line("Latency", new double[] { 22, 24, 20 }, s => s.ValueAxis("latency"));
    c.Legend(l => l.Position(LegendPosition.Bottom)
        .Item("target", "SLA", i => i.Symbol(LegendSymbol.Line)));
});
```

The next-major axis model reserves the roadmap's control surface now: `Ticks(Step, Values, MinorCount, Formatter)`, `Grid(Visible, Stroke)`, `Bands(...)`, `Scale`, `Reversed`, `LabelPolicy`, `CategoryPaths`, and time-zone/session settings. `Values` is an explicit tick list and `Step` is automatic tick spacing; setting both rejects rather than guessing. Unsupported time/log combinations reject at the responsible scale. `Bounds(null, maximum)` clears the lower bound; automatic bounds preserve nice ticks and count policies.

Multiple axes are an ordered, ID-keyed collection with a declared side and measured outward offset. A series refers to an axis ID. Existing primary/secondary semantics migrate to the first corresponding IDs. This is an API design for roadmap work, not a claim that arbitrary extra axes already render. Existing combination restrictions remain in force; `Mixed` does not permit pie/Cartesian or incompatible orientation overlays.

Legend controls use `Items`, `Symbol`, `SymbolSize`, `Formatter`, `MaximumRows`, `MaximumHeightFraction`, `Visible` and `Position` properties. Custom reference-line items are repeated `Item` children. `Formatter` receives immutable item/series facts and returns plain text. Static legend composition belongs to core; toggle, isolate and rescale policy belongs to the browser adapter.

### Incoming per-datum styling and formatters

```csharp
var sales = Chart.Bar("Monthly orders", c => {
    c.X("Month", x => x.Labels("Jan", "Feb", "Mar", "Apr"));
    c.Series("Sales", new double[] { 62, 78, 71, 91 }, s => s
        .Id("sales")
        .ItemStyle(item => item.Datum.Y >= 90
            ? new ItemStyle { Tone = Tone.Warning }
            : null)
        .Labels(l => l.Visible(true).Formatter(v => $"{v.Value:0}"))
        .Tooltip(t => t.Formatter(v => $"{v.Category}: {v.Value:0} orders")));
});
```

`LineSeries`/`BarSeries` builders infer `ChartPoint` for `ItemStyle`; bubble builders infer `ChartBubble`, range builders `ChartRangeBand`, and so on. Relationship series have separately typed `Nodes(n => n.ItemStyle(...))` and `Links(l => l.ItemStyle(...))` settings, so installing link styling cannot erase node styling. Categorical missing heatmap cells retain nullable datum contexts.

I preserve the configurability branch's preparation contract: callbacks run once per authored display observation per preparation, never during SVG/PNG export. Context includes authored ordinal, series identity, semantic state, current/selected/highlighted state, and the complete datum. Derived bins/fitted endpoints are display observations; aggregates never borrow an arbitrary contributor's style. Bubble radius remains controlled by its quantitative scale. Callback exceptions leave the authored model unchanged and publish no prepared result.

Paint resolution is theme/family defaults → series properties → keyed observation overrides → non-null callback fields → transient adapter state overlay. Authored colour/fill overrides beat semantic tone for that channel; tone still remains available as metadata. Per-item fill beats series fill; marker outline is independent of line stroke. A null callback result inherits; formatter null retains the authored/default caption, while an empty string suppresses text. Formatting never enables a hidden label. Tooltips stay plain text. The scalar `Value` remains nullable for compound observations and `Percent` uses 1 = 100%; typed datum access retains the other fields.

Fills keep the existing shared gradient, image, pattern and detached-pixel contracts. They gain builder names `Fill`, `Stroke`, `Marker` and `ItemStyle`; this redesign does not create a second paint engine.

Per-segment dash belongs on typed segment presentation (`Segments(segment => segment.Stroke(...))`), with stable adjacent observation IDs for live data. It reuses the same stroke model as a series and its legend sample; breaks remain actual data-path breaks. Per-series-kind theme defaults belong in `VisualTheme`'s typed `SeriesDefaults` collection, applied before explicit series properties. A theme-edit callback returns a new paired theme. Shared token roles supply its colours and typography; any new exported geometry/default role must first enter the canonical token contract, rather than becoming a private palette file in a product.

### Highlight part of a series

Email report hosts need emphasis on a range or set of observations within one series, such as its last point or an incident window. I express this through the same keyed item-style and segment presentation model, not a duplicate overlay series or browser-only highlight.

An item-style callback can emphasize selected observations' marks and labels. Segment styling can emphasize the stroke or area treatment between selected adjacent observations. A range uses the authored observation order or explicit source-coordinate bounds; a set uses observation IDs. Resolve membership from source facts so sorting, clipping or decimation does not change which observations the highlight means. A last-point highlight identifies the last authored observation, not whichever mark happens to remain visible. Gaps and nonadjacent selected observations never gain connecting segments.

Highlight paint resolves from shared theme roles, tones and the existing fill/stroke/marker settings during preparation. The detached scene carries the same highlighted subset into both SVG and native PNG, including scaled PNG exports. Highlighting changes presentation only; source values, IDs and missing-data facts remain intact.

## Every chart family

The factory takes `(string title, Action<FamilyBuilder> configure)` and returns the corresponding property model. Unless stated otherwise, repeated datasets use `Series(name, typedData, settingsLambda)`. Existing observation types keep their fields and validation; names below beginning with `Chart` refer to those current types unless explicitly marked proposed.

| Existing kind / capability | Proposed factory | Data and nested family settings |
| --- | --- | --- |
| Line | `Chart.Line` | `ChartPoint`; `Series(..., s => s.Interpolation(...).Stroke(...).Marker(...))`. |
| StepLine | `Chart.StepLine` | `ChartPoint`; `Step(Position)` in series settings. Uses the same line model with a step preset. |
| Area | `Chart.Area` | `ChartPoint`; `Fill`, `Stroke`, interpolation and baseline. |
| StepArea | `Chart.StepArea` | `ChartPoint`; area with `Step` settings. |
| StackedArea | `Chart.StackedArea` | `ChartPoint`; `Stack(s => s.Group(...).NormalizedTo(...).Missing(...))`. |
| Scatter | `Chart.Scatter` | `ChartPoint`; numeric X/Y and shared markers. |
| Bubble | `Chart.Bubble` | `ChartBubble`; `SizeScale(s => s.Domain(...).Range(...).Dimension(...))`. |
| ErrorBar | `Chart.ErrorBar` | `ChartErrorBar`; independent lower/upper uncertainty, cap and stem settings. |
| Candlestick | `Chart.Candlestick` | `ChartCandlestick`; `Financial(f => f.Rising(...).Falling(...).Width(...))`. |
| Ohlc | `Chart.Ohlc` | Same OHLC observation contract; financial tick/stem settings. |
| RangeBand | `Chart.RangeBand` | `ChartRangeBand`; lower/upper fields, gap and fill settings. |
| RangeArea | `Chart.RangeArea` | `ChartRangeBand`; measured boundaries, interpolation and labels. |
| DifferenceArea | `Chart.DifferenceArea` | `ChartDifferencePoint` or two independently mapped boundaries; alignment, positive/negative fill. |
| Bar | `Chart.Bar` | `ChartPoint`; `Bars(b => b.Width(...).Corners(...))`, series `Stack(...)`. |
| HorizontalBar | `Chart.HorizontalBar` | Bar model with horizontal orientation; physical axes remain explicit. |
| Lollipop | `Chart.Lollipop` | `ChartPoint`; shared stem and marker settings. |
| Dumbbell | `Chart.Dumbbell` | `ChartDumbbell`; paired endpoint names, markers and connector. |
| RangeBar | `Chart.RangeBar` | `ChartInterval`; grouping, orientation and endpoint captions. |
| BoxPlot | `Chart.BoxPlot` | `ChartBoxPlot`; whiskers, outliers, grouping and orientation. |
| Heatmap | `Chart.Heatmap` | `Row(id, label, double?[], settings)` or typed `ChartHeatmapRow`; `Cells`, `ColorScale`, column categories. |
| HexbinHeatmap | `Chart.HexbinHeatmap` | `Row` with `ChartHeatmapRow` or nullable numeric cells; hexagonal `Cells` geometry and `ColorScale`, retaining the existing matrix coordinates. |
| CalendarHeatmap | `Chart.CalendarHeatmap` | `ChartCalendarHeatmapItem`; `Calendar(FirstDay, Range, Labels)`. |
| DottedMap | `Chart.DottedMap` | `ChartMapPoint` longitude/latitude observations; `Map`, `Viewport`, `Route` children and callouts. |
| TileMap | `Chart.TileMap` | `ChartRegionMapItem` keyed to a supplied `ChartTileMapDefinition`; `Tiles`, `ColorScale`. |
| RegionMap | `Chart.RegionMap` | `ChartRegionMapItem` keyed to `ChartMapDefinition`; viewport, seam-aware polygons and routes. |
| Gauge | `Chart.Gauge` | `Reading(ChartGaugeValue)`; `Gauge(Form, Orientation, Ticks, Bands)`. |
| Circle | `Chart.Circle` | `ChartGaugeValue` reading; `Progress` geometry and center text. |
| ProgressRing | `Chart.ProgressRing` | `ChartPoint` ring observations; repeated `Ring` children and track/value settings. |
| LayeredRadial | `Chart.LayeredRadial` | `ChartRadialLayer`; repeated `Layer` children with independent spans and paint. |
| Bullet | `Chart.Bullet` | `ChartBulletValue` with targets/ranges; `Row` children and qualitative bands. |
| Waterfall | `Chart.Waterfall` | `ChartWaterfallItem`; opening balance, checkpoints, totals and connectors. |
| Radar | `Chart.Radar` | `ChartPoint`; category labels, missing policy, line/fill and radial grid. |
| PolarArea | `Chart.PolarArea` | `ChartPoint`; area-proportional equal-angle segments; zero placeholders preserved. |
| Polar | `Chart.Polar` | Ordered angle/radius `ChartPoint`; angular/radial axes and continuous source windows. |
| RadialBar | `Chart.RadialBar` | `ChartPoint`; radial category bands and angular value settings. |
| RadialColumn | `Chart.RadialColumn` | `ChartPoint`; angular categories and radial value settings. |
| Funnel | `Chart.Funnel` | Ordered `ChartPoint` stages; `Stage` style, sizing and captions. |
| Pyramid | `Chart.Pyramid` | Ordered nonnegative `ChartPoint`; height/area proportions and orientation. |
| Timeline | `Chart.Timeline` | Proposed typed `TimelineItem` (ID, label, start/end), replacing today's start/end packed into a `ChartPoint`; ordered `Row`/`Item` children and time/numeric range settings. |
| Gantt | `Chart.Gantt` | `ChartGanttItem`; `Task`, `Dependency`, `Milestone`, calendar and progress settings. |
| StateTimeline | `Chart.StateTimeline` | `ChartStateTimelineSegment`; `Lane` children, state categories and real time gaps. |
| GanttLane | `Chart.GanttLane` | `ChartGanttLaneItem`; packed lane overlaps, open ends and explicit clock input. |
| Sankey | `Chart.Sankey` | `Series(name, nodes, links, settings)` with `ChartNode`/`ChartFlowLink`; alignment/order, node/link styles and flow colour mode. |
| Chord | `Chart.Chord` | Same weighted graph series overload; circular spans, display permutations, direction and ribbon settings. |
| Tree | `Chart.Tree` | `Series(name, nodes, links, settings)` with `ChartNode`/`ChartTreeLink`; graph layout, node/link settings and IDs. |
| Sunburst | `Chart.Sunburst` | `ChartHierarchyItem`; hierarchy, secondary captions, branch visibility and radial bands. |
| Treemap | `Chart.Treemap` | `ChartHierarchyItem`; hierarchy, gap, captions, ordering and colour dimension. |
| Pie | `Chart.Pie` | `ChartPoint`; labels, ranked folding/Other, slice styles and authored radius. |
| Donut | `Chart.Donut` | Pie observations; `Center(c => ...)`, bands, radius and slice settings. |
| Slope | `Chart.Slope` | Ordered endpoint `ChartPoint` pairs; end labels and connecting strokes. |
| Pictorial | `Chart.Pictorial` | `ChartPictorialItem`; `Icon`, captured artwork, units-per-symbol and fraction direction. |
| ProgressBar | `Chart.ProgressBar` | `ChartProgressItem`; `Row` children, maximum, targets, captions and tracks. |
| WordCloud | `Chart.WordCloud` | `ChartWordCloudItem`; deterministic seed/order, font range and placement. |
| TrendLine | `Chart.TrendLine` | `ChartPoint` samples; `Fit(Method, Extent)` and fitted/source semantic distinction. |
| Histogram (derived Bar) | `Chart.Histogram` | Raw numeric samples; `Bins(Count/Width/Edges)`, source membership and one derived observation per bin. |
| Pareto (derived combination) | `Chart.Pareto` | `ChartParetoItem`; preserve the current normalized-percentage bars and cumulative line. Any raw-count variant is a separate future contract, not an incidental migration change. |
| Quadrant (capability QUA) | `Chart.Quadrant` | Numeric X/Y observations; named quadrant regions and boundary settings. |
| Combination (COM) | `Chart.Mixed` | Typed `Line`, `Bar`, `Area`, etc. child methods; one validated coordinate system. |
| Org chart (ORG) | `Diagram.Topology` | `Layout(l => l.Hierarchy(...))`, typed nodes/groups/edges; no duplicate org engine. |
| Radar area (RDA) | `Chart.Radar` | `Series(..., s => s.Fill(...))`; no second radar factory. |
| Nightingale (NIG) | `Chart.PolarArea` | Same area-proportional contract; no additional factory alias. |
| Cone funnel (CON) | `Chart.Funnel` | `Funnel(f => f.Form(FunnelForm.Cone))`. |
| Linear gauge (LGU) | `Chart.Gauge` | `Gauge(g => g.Form(GaugeForm.Linear))`. |
| Map (MAP) | Three map factories above | Reuse current map definitions, imports, viewports, badges and routes. |
| Sparkline | `Chart.Sparkline` | Compact point-series preset over the same prepared core, with baseline/last-value options. |
| ChartGrid / small multiples | `Chart.Grid` | `Columns`, `PanelSize`, `Gap`, repeated `Panel(visual, span)`; `Axes` sharing and typed dataset `Facet`. |

Preset factories for StepLine, StepArea, StackedArea and HorizontalBar return the same relevant family model with explicit preset properties. They exist for family discovery; no duplicate renderer or parallel data model is created. The table includes all 54 `ChartSeriesKind` values and all 52 capability rows, including aliases and derived families that have no unique enum value.

`ChartDataset<T>` remains the typed data-preparation component for filtering, grouping, aggregating and faceting. Its fluent operations are meaningful data transformations, not configuration prefixes. Facets build children through the same family factories. Statistical helpers, decimation and map import remain in their current responsible components.

## Diagrams and source languages

```csharp
var topology = Diagram.Topology("Service request topology", d => {
    d.Id("service-topology").Layout(l => l.Direction(Direction.LeftToRight).FitContent(true));
    d.Group("primary", "Primary region", g => {
        g.Tone(Tone.Success);
        g.Node("client", "Client service", n => n.Icon(Icon.Named("server")));
        g.Node("cache", "Cache", n => n.Shape(NodeShape.Cylinder));
    });
    d.Node("worker", "Processing service", n => n.Tone(Tone.Warning));
    d.Edge("request", "client", "worker", e => e.Label("Requests")
        .Route(r => r.Kind(RouteKind.ObstacleAvoidingOrthogonal)));
});
```

Group nesting assigns group membership through typed IDs, not a second node list. Explicit geometry remains available through `Position` and `Size`, alongside automatic layout. Ports, alignment constraints, minimum rank spans, edge terminals, crossing paint, safe links, details, compartments and table cells retain their current typed properties. `Icon` denotes a finite catalog key or supplied captured artwork, never an implicit network fetch.

Native `Diagram.Flow` has `Lane`, `Step` and `Connector` children, plus typed layout/direction settings. Native `Diagram.Sequence` has `Participant`, `Message`, `Activation`, `Note`, `Block` and nested `Branch` children. Sequence spans use stable message IDs, replacing fragile numeric indexes in ordinary authoring; the model stores resolved ordered spans without changing message identity. Creation/destruction, self routes, parallel-over, exception blocks, number badges, participant views, presets and role typography remain expressible.

For other diagram notations, I keep the existing typed language documents rather than inventing a second native class/ER/Git model. `MermaidRenderer.Render(source, m => ...)` and `MermaidRenderer.Render(typedDocument, m => ...)` are proposed builder overloads in the optional Mermaid package. Both return a result with diagnostics and optional artifact. The second uses the same typed semantic model and converter as source parsing. Existing parsing preserves spans and unsupported statements. A family setting is a nested lambda; it is not an implicit fidelity promise. The meaningful `MermaidRenderer` entry point keeps its name.

| `MermaidDiagramKind` | API shape and responsible renderer |
| --- | --- |
| Flowchart | `MermaidFlowchartDocument` + `m.Flowchart(f => ...)`; topology nodes/groups/ports/routes. |
| Sequence | `MermaidSequenceDocument` + `m.Sequence(s => ...)`; the sequence model above. |
| Class | `MermaidClassDocument` + `m.Class(c => ...)`; topology compartments, members, namespaces and multiplicities. |
| State | `MermaidStateDocument` + `m.State(s => ...)`; topology states, transitions, nested groups and notes. |
| EntityRelationship | `MermaidEntityRelationshipDocument` + `m.EntityRelationship(e => ...)`; topology table compartments and cardinalities. |
| Gantt | Typed Gantt document + `m.Gantt(g => ...)`; chart tasks, dates, dependencies and calendar. |
| Pie | Typed pie document + `m.Pie(p => ...)`; chart pie observations. |
| Journey | Typed journey document + `m.Journey(j => ...)`; existing native journey projection and section/actor facts. |
| GitGraph | Typed Git graph document + `m.GitGraph(g => ...)`; existing commit/branch/merge lanes. |
| MindMap | Typed mind-map document + `m.MindMap(mind => ...)`; hierarchy/topology and depth styles. |
| Timeline | Typed timeline document + `m.Timeline(t => ...)`; native section/event diagram, distinct from interval charts. |
| Requirement | Typed requirement document + `m.Requirement(r => ...)`; requirement/element facts and typed relationships. |
| Quadrant | Typed quadrant document + `m.Quadrant(q => ...)`; chart quadrant points and region labels. |
| Sankey | Typed Sankey document + `m.Sankey(s => ...)`; weighted chart nodes/links. |
| XYChart | Typed XY document + `m.XYChart(xy => ...)`; supported mixed Cartesian bars/lines. |
| Block | Typed block document + `m.Block(b => ...)`; existing block grid/spans/connections. |
| Packet | Typed packet document + `m.Packet(p => ...)`; bit-field ranges and row settings. |
| Architecture | Typed architecture document + `m.Architecture(a => ...)`; topology groups/services/junctions. |
| Kanban | Typed Kanban document + `m.Kanban(k => ...)`; static board artifact, with no live data-view behavior. |
| Radar | Typed radar document + `m.Radar(r => ...)`; chart radar values and shared radial options. |
| Treemap | Typed treemap document + `m.Treemap(t => ...)`; chart hierarchy data. |
| C4 | Typed C4 document + `m.C4(c => ...)`; topology actors/boundaries/relationships and existing approximation diagnostics. |
| Venn | Typed Venn document + `m.Venn(v => ...)`; existing set/intersection projection and quantitative tolerance diagnostics. |
| Ishikawa | Typed Ishikawa document + `m.Ishikawa(i => ...)`; causes/effects through existing native diagram geometry. |
| Wardley | Typed Wardley document + `m.Wardley(w => ...)`; positions, evolution, pipelines and annotations. |
| EventModeling | Typed event-modeling document + `m.EventModeling(e => ...)`; existing event/command/read-model lanes. |
| TreeView | Typed tree-view document + `m.TreeView(t => ...)`; existing hierarchy projection. |
| ZenUml | Typed ZenUML document + `m.ZenUml(z => ...)`; bounded parsed statements through sequence rendering. |
| Swimlane | Typed swimlane document + `m.Swimlane(s => ...)`; native flow lanes/nesting/order. |
| UseCase | Typed use-case document + `m.UseCase(u => ...)`; topology actors, boundaries and include/extend links. |
| Cynefin | Typed Cynefin document + `m.Cynefin(c => ...)`; current native domain-map projection. |
| Agentflow | Typed workflow document + `m.Agentflow(a => ...)`; current static workflow nodes and edges. |
| Railroad | Typed grammar document + `m.Railroad(r => ...)`; railroad geometry for the supported grammar frontends. |

`Unknown` is an error result, not another family factory. Typed names in the table describe the current language model where available; short names for new builder settings are proposed. Unsupported grammar remains source-preserving with located diagnostics. No silent conversion to a generic X/Y chart, no browser Mermaid runtime, and no promise of upstream pixel equality.

Host theme supplies defaults; supported explicit source styles override them unless an explicit host presentation override is supplied. Invalid/unsafe source paint, assets, links or raw markup retain the existing rejection and diagnostics. Natural content sizing and `FixedViewport` stay explicit choices. Requested aspect guides automatic layout; fixed export bounds remain available.

`ChartForgeX.Visuals` continues to own non-chart facts, lists, metric cards, mixed grids and canvases. Its builders follow the same generated property rule. Stories retain scene/playback/terminal APIs and consume core visuals. Existing pixel animation encoders remain in core unchanged: their constructors and `Encode`/`WriteTo` operations already express their purpose.

## Shared visual-token contract

I consume the shared visual-token contract through its canonical export. The inspected 2 November candidate is `contractVersion: 2`, token `version: "1.6.0"`. Its root groups are `typography`, `palette`, `radius`, `light` and `dark`, plus provenance. Each mode includes surface/text/accent/chrome, status pairs, chart series, Other, ramps and elevation. Contract version, token content version and package version are distinct.

| Shared source / role | ChartForgeX consumption |
| --- | --- |
| Canonical token JSON and exporter | Build-time exported token document; bundled standalone defaults generated from that same source. No runtime producer reference or download. |
| `light` / `dark` | Paired `VisualTheme`; explicit `ThemeMode.Light`/`Dark` at preparation. A browser may select a mode before asking for an appropriate prepared result. |
| Surface, text, border, accent and focus roles | Frame/plot surfaces, axes, labels, annotations and focus; canonical `--hfx-*` variables for hosted SVG, concrete fallback values for static output. |
| Chart series and Other | `--hfx-color-chart-series-1..10` and `--hfx-color-chart-other` in the current candidate. Keep UI series inks separate. Stable entity-to-slot assignment; no assignment from current rank. |
| Sequential/diverging ramps | Numeric heatmap/map scales and legends; use the actual mode-specific ramp and missing-data policy. |
| Severity / outcome / state | Distinct semantic vocabularies and fill/ink pairs. `Tone` is presentation, not a replacement for domain facts. |
| Typography | Shared sans/display/mono stacks, tabular numerals, chart roles, compact breakpoint and minimum readable size, resolved before measurement. |
| Radius and elevation | Independent mark/card/plot shape and mode-specific frame/shadow recipes. Tooltip chrome stays available to adapters. |
| Chart-specific geometry not exported yet | Existing renderer defaults remain local until that role enters the shared contract. Do not invent unsupported density/spacing keys in the token schema. |

The `--hfx-*` spellings above describe the inspected token export. A neutral shared prefix for ChartForgeX and CanopyX output remains an [open choice](#choices-i-still-need-to-settle); the proposed replacement is `--evx-*`.

The ten-slot allocator, derived shades (`palette.extended`), parent-hue hierarchy treatment, ranked pie/donut folding and neutral Other come from the token branch. The API does not freeze those values or reimplement their allocation. The current six-slice pie/donut limit is independent of the number of palette slots. Theme changes invalidate preparation; already prepared output remains detached.

`VisualTheme.FromTokens(json)` is the proposed intake name for a validated full visual contract. Full authored theme round-tripping keeps its separate `FromJson`/`ToJson` schema; palette-only loading is not a full-theme operation. Reject unsupported contract versions, duplicate keys and invalid required values under the existing bounded parser. Missing optional fields use documented defaults from the same token version. Unknown extension fields can survive the producer contract without becoming guessed chart options.

Native SVG and PNG resolve colours and typography from the same snapshot. CSS variables can change hosted SVG paint, but cannot remeasure glyphs or update an already captured PNG. A host font, geometry or mode change needing different layout must prepare again. Browser export captures resolved host paint through the existing adapter path. Native font registration, browser font delivery and licensed bundled assets remain distinct, explicit responsibilities.

### Shared names without shared runtime dependencies

I keep `Tone`, `Icon` and `ThemeMode` local to each independent engine's namespace, with shared wire names and explicit bridge mappings. Qualify them at a bridge boundary (`ChartForgeX.Tone`, for example) where imports collide. The shared contract is the wire name and token key, not CLR type identity.

`Tone.None`, `Neutral`, `Info`, `Success`, `Warning`, `Danger`, `Muted` and validated `Tone.Named("severity-high")` follow CanopyX's named-tone contract. Standard tone names map to the shared status/presentation roles; named tones require a theme entry, and a missing entry produces a diagnostic and neutral rendering. No unbounded runtime reflection or implicit CSS-to-native colour lookup. Severity, outcome and operational-state properties remain separate metadata.

`Icon.Named("server")` is a catalog reference. The same spelling identifies the same semantic icon across hosts, but each renderer binds it to captured supported artwork. Icon absence uses the existing explicit fallback/diagnostic path. Arbitrary HTML or a network URL is not an icon key. `ChartColor` remains the literal colour value for custom themes, artwork and explicit overrides; normal product recipes use semantic roles and tones.

## Generation and AOT

I use one shared build-time generator in PowerForge/PSPublishModule for independent engines, including ChartForgeX and CanopyX. Each engine supplies its model metadata and a small handwritten domain layer; the generator implements the property-driven builder rules.

1. Annotate the property model with generation metadata: exported scalar, nested settings, repeated child, typed data, nullable reset, public name, and XML documentation. Inheritance carries common frame/axis settings. Attributes contain no executable rendering policy.
2. Use an incremental Roslyn generator to emit separate builder types, typed collection child methods, factory wiring, copy/apply plumbing and direct typed mapping code. Emit one file per semantic type. Build-time Roslyn references never enter core runtime dependencies or consumer output.
3. Generate scalar setters from setters; nested callbacks from nested-property types; child methods from collection element types. Only real compound conveniences (`Size`, `Bounds`, value-series ingestion) use explicit metadata/handwritten code. Preserve those backing-property relationships in the generated API manifest.
4. Generate the PowerShell parameter/child schema and documentation input from the same property metadata in a build-time export. PowerForge's existing module/build/documentation mechanisms consume it. Do not ask a source generator to write into the repository or execute PowerShell during C# compilation.
5. Diagnose public-name/signature collisions, unsupported property shapes, ambiguous child names, unsafe shallow copies and invalid mappings at build time. Generated code uses the model's validation/ingestion operations. It must not duplicate defaults, formatters, colour math, layout, or family validation.
6. Publish compiled builders in the core assembly. Users of ordinary factories/initializers need no generator. An optional consumer mapping analyzer is needed only for requested source-generated application-record mappings.

```csharp
// Proposed model metadata; illustrative, not added to the engine by this design.
[GenerateBuilder]
public sealed partial class LineChart {
    public string Title { get; set; } = "";
    public AxisSettings X { get; set; } = new AxisSettings();
    [Child("Series")]
    public SeriesCollection<LineSeries> Series { get; } = new SeriesCollection<LineSeries>();
}

// Representative generated signatures; edits use the same properties/collections.
public partial class LineChartBuilder {
    public LineChartBuilder Title(string value);
    public LineChartBuilder X(Action<AxisSettingsBuilder> configure);
    public LineChartBuilder X(string title, Action<AxisSettingsBuilder>? configure = null);
    public LineChartBuilder Series(string name, IEnumerable<double> values,
        Action<LineSeriesBuilder>? configure = null);
    public LineChartBuilder Series(string name, IEnumerable<ChartPoint> data,
        Action<LineSeriesBuilder>? configure = null);
    public LineChartBuilder Series<T>(string name, IEnumerable<T> data,
        Func<T, ChartPoint> selector, Action<LineSeriesBuilder>? configure = null);
}
```

The declarations above omit bodies to show the contract. No generator is proposed for data transformation or rendering algorithms. Abstract/base builders preserve the concrete return type; interface-only optional packages use extensions in their own assembly. Output ordering and public signatures are deterministic across all target frameworks.

### Typed mappings

```csharp
var cpu = Chart.Line("CPU usage", c => {
    c.X("Minutes");
    c.Y("CPU (%)", y => y.Bounds(0, 100));
    c.Series("CPU", readings, row => new ChartPoint(row.Minute, row.Cpu),
        s => s.Id("cpu"));
});
```

Selectors are ordinary compiled delegates, evaluated once during ingestion. Typed observations and input sequences are snapshotted; application records/selectors are not retained by the series. No expression compilation, `dynamic`, property-name lookup or assembly scan is needed. Typed selectors also support explicit source keys for linking, compound financial/range observations, nullable values and grouped categories.

For reusable mappings, an optional consumer analyzer takes an explicitly annotated partial mapping declaration and emits direct member access. Missing/incompatible members are compile errors; generated code applies the same ingestion validation. A mapping such as `CpuReading -> ChartPoint` includes typed X/Y/key fields, not a string dictionary. It is a convenience over typed selectors, never required to render.

PowerShell can bind property names at its own dynamic boundary; that convenience is not advertised as Native AOT. It projects records into the same typed observations before invoking core. If a future reflection convenience is added for .NET, it belongs in an explicitly non-AOT adapter and must carry appropriate trimming diagnostics. I do not include one in the proposed core.

## PowerShell shape

I put the `New-Cfx*` PowerShell commands in ImagePlayground, which already supplies the website's PowerShell examples. This design does not create a new runtime module or rename its established commands as a side effect. The `Cfx` noun family avoids the .NET `Chart` factory becoming a generic global PowerShell noun.

```powershell
$chart = New-CfxChart -Kind Line -Title 'Weekly orders' -Width 800 -Height 440 `
    -X @{ Title = 'Day'; Labels = @('Mon', 'Tue', 'Wed', 'Thu', 'Fri') } `
    -Y @{ Title = 'Orders'; Minimum = 0; Maximum = 60 } `
    -Theme $productTheme -Mode Dark -Content {
        New-CfxSeries -Id completed-orders -Name Completed -Values 28,34,41,48,32
        New-CfxSeries -Id target-orders -Name Target -Values 30,35,40,45,50 `
            -Stroke @{ Dash = @(6,3) }
    }
$chart.SavePng((Join-Path $PWD 'orders.png'))
```

Settings are parameters or typed nested settings/hashtables. Repeated children are commands with their own settings; there is no `New-CfxTitle`, axis-settings command, or command per enum value. Proposed command families:

| Command | Role |
| --- | --- |
| `New-CfxChart` | Typed family selection and one-off frame/axis/theme settings. |
| `New-CfxSeries` | Repeated typed dataset; `-Kind` is needed only in mixed charts. |
| `New-CfxDiagram` | Topology, flow or sequence selection. |
| `New-CfxNode`, `New-CfxEdge`, `New-CfxGroup`, `New-CfxPort` | Repeated topology entities with explicit IDs. |
| `New-CfxLane`, `New-CfxStep`, `New-CfxConnector` | Repeated flow/schedule entities. |
| `New-CfxParticipant`, `New-CfxMessage`, `New-CfxBlock`, `New-CfxNote`, `New-CfxActivation` | Repeated sequence entities; block children express nesting. |
| `New-CfxPanel` | Repeated chart-grid panels; no page/dashboard shell in ChartForgeX. |

This is a maximum initial set of 16 command names, grouped around real repeated entities, not one command per chart family. Rendering uses the returned model's export methods or existing module export commands. Mermaid source uses the optional package's result/diagnostic contract; it is not silently converted through `-Kind`.

`-Data $records -XProperty Minute -YProperty Cpu -IdProperty Key` is a PowerShell-side projection alternative to `-Values`. Compound families accept typed objects or their named field map (OHLC, interval, hierarchy, graph). Validate maps and conversions before attaching a series, including empty input. No silent coercion of null to zero. Conflicting `-Values`/`-Data` parameter sets reject.

Children write typed objects to the success stream; the parent validates compatible child types and installs them atomically. Incidental strings/output are errors. Child order is pipeline order. Scriptblocks run synchronously in the caller runspace during construction/preparation, never on a raster worker thread. Item-style/formatter scriptblocks are adapted to typed delegates; if invoked from an unsupported thread they reject clearly. File export remains explicit. Keep Windows PowerShell 5.1 and PowerShell 7 binder behavior aligned without requiring newer C# syntax in scripts.

## Browser adapters and embedding

```csharp
chart.SaveInteractiveHtml("orders.html", html => html
    .Runtime(HtmlChartRuntime.Standalone)
    .Interaction(i => i.Tooltips(true).Selection(true).Legend(l => l.Rescale(true)))
    .Navigation(n => n.X(true).Pan(true).Brush(true)));
```

This builder is generated in the HTML adapter from its own options. It composes the core model; it never puts DOM/runtime settings into chart properties. Full controller/native connection options remain distinct from a saved standalone report. Feature unions select shared bundles once per page. Existing JS controller commands, linking payloads and stable keys remain contracts; no browser API rename is needed merely because C# authoring changes.

The incoming standalone projection supports its declared numeric, vertical Cartesian Line/Bar/Area/Scatter/Candlestick scope. New C# names do not extend that scope to time, log, secondary axes, diagrams or unsupported mixes. Connected native preparation remains the route for native layout and broader supported viewports. Annotations needing a native commit remain unavailable without that connection.

HTML report hosts accept the chart model through a thin bridge and retain its series/axis models. CanopyX shares semantic keys and themes through its adapter. Email uses a solid-card PNG, explicit mode and scale, with live finding text/alt text supplied by its host. ConsoleMonster consumes artifacts/pixels/animations, not page configuration.

### Native PNG density for email

Email report hosts need native PNG export at 1x, 2x and 3x with the same logical layout. I reuse the prepared scene and its existing `VisualRenderOptions.Scale` request; ordinary PNG export helpers must expose the same request. An 800 × 440 logical chart produces 800 × 440, 1600 × 880 and 2400 × 1320 pixels respectively. Scaling changes raster density, not the chart's logical size, axis ticks, label wrapping, legend placement or highlighted subset.

```csharp
var prepared = chart.Prepare(context);
byte[] png1x = prepared.ToPng(new VisualRenderOptions(scale: 1));
byte[] png2x = prepared.ToPng(new VisualRenderOptions(scale: 2));
byte[] png3x = prepared.ToPng(new VisualRenderOptions(scale: 3));
```

Render directly from the captured native geometry, fonts and paint at the requested density, without enlarging a finished 1x bitmap, repeating layout or rerunning styling callbacks. No external rasteriser, SVG conversion service or browser is required. The email host declares the original logical display dimensions and supplies the image attachment and alt text.

An email host depending on scaled native PNG export must qualify that contract before shipping. Its release qualification must prove both 2x and 3x output through the consumed ChartForgeX package; a design document or source-only result does not satisfy that dependency.

## Migration from current source

Implementation starts from the current default branch and preserves the rendering contracts that have landed since the 2 November baseline used by the [before/after examples](next-major-api-examples.md). Reconcile the property, token and adapter surfaces before starting the breaking migration.

Implementation sequence:

1. Reconcile the landed APIs and token exporter against these snapshots; update this proposal for actual differences. Freeze the exact input for migration, including all supported chart/diagram families and consumers.
2. Use the shared PowerForge/PSPublishModule generator, engine-local vocabulary and ImagePlayground command decisions above. Build the generator and one vertical path: Line/Bar, axes/legend, initializer/fluent equivalence, typed mapping and one PowerShell recipe. The build and package machinery stays in PowerForge.
3. Extend the same metadata/builders across every family in the inventory, diagrams, grids and optional package settings. Migrate internal examples/tests as each family moves. Keep temporary translation private to the development branch and delete it before the candidate; ship no compatibility assembly or aliases.
4. Migrate the README and ten recipes in the companion document. Regenerate gallery/API docs from maintained example/model sources, including ordinary C#, fluent C# and PowerShell. Remove stale prefix-based guidance and superseded API-ledger entries that no longer describe the product.
5. Migrate a thin HTML chart bridge and a real report; a monitoring report with typed data and status colours; ConsoleMonster artifact/pixel handoff; and CanopyX linked-view examples. Retain their own layout, datasets and browser contracts. Also inspect ImagePlayground because it supplies today's PowerShell examples. Stage cross-repository consumers locally against the known candidate, then use published packages for normal consumption.
6. Run the stable-candidate quality loop and consumer qualification; remove the old public surface as one deliberate breaking candidate. Publish/re-pin only through a separately requested release workflow, with the consumer order coordinated against actual package availability.

Future acceptance checks belong to implementation, not this documentation change:

- Initializer, fluent and PowerShell recipes reach the same normalized property/observation model and decisive prepared facts. Exercise nested setting exceptions and retained builders, typed replacement/IDs, missing values and callback execution counts at their responsible boundary.
- Export one prepared chart as native PNG at 1x, 2x and 3x; verify the expected pixel dimensions with unchanged logical geometry, ticks, wrapping and legend placement, without additional layout or callback execution. Qualify the package-consumption path before a dependent email host ships.
- Exercise last-point, incident-range and noncontiguous observation-set highlights through item and segment styles in SVG and native PNG. Verify the selected subset, shared theme paint, preserved source IDs/values and unconnected gaps, including 2x and 3x PNG output.
- Build `net472`, `netstandard2.0`, `net8.0` and `net10.0`; run trim/AOT consumers for normal factories and generated mappings. Verify packed core has no runtime dependency, Roslyn payload or PowerShell host requirement.
- Run `./Build.ps1 -Configuration Release` on the completed candidate. Review regenerated gallery output before any intentional visual baseline update. The API migration should preserve landed rendering behavior; visual changes need their own stated reason.
- Inspect wide/compact, light/dark SVG and native PNG, callbacks, diagrams, maps, missing data, custom fonts, and keyboard/link/legend/export behavior in supported adapters. Verify the stable candidate after the final patch.
- Prove a normal installed-package consumer for the four integrations and the PowerShell route. Keep browser, package, source and release evidence distinct.

### Short migration note to ship

> I replaced prefix-based chart configuration with typed property models and generated builders. Start with `Chart.Line("Title", c => ...)` instead of `Chart.Create().WithTitle(...).AddLine(...)`; use `X`/`Y`, nested `Legend`/`Labels` settings, and `Series` children. Keep explicit IDs for updates and linked views. Build diagrams with `Diagram.Topology`, `Flow` or `Sequence`, and pass a shared `VisualTheme` with an explicit `ThemeMode`. Existing prepared-output and file-export operations keep their purpose. Update the ChartForgeX packages and consumers together; the old authoring methods have been removed.

A one-off migration script may rename mechanical settings, but it must leave mixed-chart binding, sequence indexes, per-item IDs, callbacks and theme intake for explicit review. It is a development aid, not a runtime bridge.

## Choices I still need to settle

| Choice | My recommendation | Consequence / alternative |
| --- | --- | --- |
| Shared CSS-token prefix | Replace the existing `--hfx-*` prefix with `--evx-*` (Evotec visual) for shared tokens consumed by ChartForgeX and CanopyX output. For example, `--hfx-color-chart-series-1` becomes `--evx-color-chart-series-1`, and `--hfx-font-sans` becomes `--evx-font-sans`. | Open proposal: one neutral namespace keeps a single product theme usable across engines. Coordinate the canonical exporter, HTML host CSS, ChartForgeX bindings and CanopyX bindings in the same breaking migration; separate `--cfx-*` and `--cx-*` token families would duplicate the shared contract. Confirm the neutral prefix before changing output. |
| Ten gallery examples | Use the repository-reference ranking explained in the companion document. | No traffic ranking is available in the inspected files. Replace the selection if actual usage data identifies a different top ten. |
