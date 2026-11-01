using System;
using System.IO;
using ChartForgeX.Typography;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

// Portable handoffs capture owned font bytes; keep the smoke independent of installed host collections.
const string portableFontFamily = "CFX AOT Carlito";
foreach (var face in new[] { (Name: "Regular", Weight: 400), (Name: "Bold", Weight: 700) })
    FontRegistry.Register(portableFontFamily, Path.Combine(AppContext.BaseDirectory, "Fonts", "Carlito-" + face.Name + ".ttf"), face.Weight);

var chart = Chart.Create()
    .WithTitle("AOT smoke")
    .WithSubtitle("Static report output")
    .WithSize(420, 240)
    .WithTheme(VisualTheme.Report())
    .WithXAxis("Run")
    .WithYAxis("Value")
    .AddSmoothLine("Values", new[] { new ChartPoint(1, 2), new ChartPoint(2, 5), new ChartPoint(3, 4) })
    .AddBar("Warnings", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2), new ChartPoint(3, 1) }, ChartColor.FromRgb(249, 115, 22));

AssertContains(chart.ToSvg(), "<svg", "SVG render failed.");
AssertContains(chart.ToHtmlPage(), "<html", "HTML page render failed.");
AssertPng(chart.ToPng(), "PNG render failed.");

var origin = new DateTime(2026, 1, 28).ToOADate();
var sized = Chart.Create().WithSize(420, 280).WithXAxisScale(ChartScaleKind.Time)
    .AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("requests"), "Requests", new[] {
        new ChartBubble(origin, 2, 4), new ChartBubble(origin + 5, 4, 9), new ChartBubble(origin + 10, 8, 16)
    }).ConfigureMarkers(markers => markers.Shape = ChartMarkerShape.Star))
    .ConfigureBubble(options => options.SizeEncoding = ChartBubbleSizeEncoding.Area);
var window = sized.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(420, 280))), new ChartCartesianViewport(x: new ChartAxisWindow(origin + 2, origin + 8)));
if (window.XProjection?.Scale != ChartScaleKind.Time) throw new InvalidOperationException("AOT native time projection missing.");
AssertContains(window.ToSvg(), "data-cfx-size=\"9\"", "AOT native window lost Bubble source facts.");
AssertPng(window.ToPng(), "AOT area-sized marker failed.");

foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
    var theme = VisualTheme.Graphite();
    var graphite = Chart.Create().WithSize(420, 300).WithTheme(theme, mode).WithTitle("Graphite AOT")
        .AddSeries(ChartSeries.CreateGauge(new ChartSeriesId("readiness"), "Readiness",
            new ChartGaugeValue(87, targets: new[] { new ChartGaugeTarget(90) })))
        .ConfigureGauge(options => { options.Form = ChartGaugeForm.Linear; options.Bands.Add(new ChartGaugeBand(60, 80, ChartSeriesState.Warning)); });
    AssertContains(graphite.ToSvg(), "data-cfx-role=\"gauge-value-marker\"", "Graphite gauge SVG failed.");
    AssertContains(graphite.ToSvg(), "data-cfx-role=\"gauge-target\"", "Graphite gauge target failed.");
    AssertPng(graphite.ToPng(), "Graphite gauge PNG failed.");
    var graphiteHeatmap = Chart.Create().WithTheme(theme, mode).WithXLabels("A", "B", "C")
        .WithHeatmapValueTextMode(ChartHeatmapValueTextMode.Always).AddHeatmapRow("Counts", new[] { 0d, 4, 8 });
    AssertContains(graphiteHeatmap.WithSvgColorVariables((mode == VisualThemeMode.Light
        ? VisualDesignTokens.GraphiteLight() : VisualDesignTokens.GraphiteDark()).ToSvgColorVariables()).ToSvg(), "ramps-sequential", "Graphite token mapping failed.");
    AssertPng(graphiteHeatmap.ToPng(), "Graphite heatmap PNG failed.");
}

var grid = ChartGrid.Create()
    .WithTitle("AOT grid")
    .WithPanelSize(260, 180)
    .Add(chart)
    .Add(Chart.Create().WithSize(260, 180).WithXLabels("Ready", "Risk").AddDonut("Share", new[] { new ChartPoint(1, 72), new ChartPoint(2, 28) }));
var gridSvg = grid.ToSvg("aot-grid");
AssertContains(gridSvg, "data-cfx-role=\"panel\"", "Grid SVG render failed.");
AssertContains(gridSvg, "data-cfx-source-id=\"panel-1\"", "Grid SVG lost its second prepared panel.");
AssertPng(grid.ToPng(), "Grid PNG render failed.");

var metric = MetricCard.Create()
    .WithMetric("Coverage", 0.982, "P1")
    .WithIcon(VisualIcon.Lightning)
    .WithTrend("+2.4 pp")
    .WithStatus(VisualStatus.Positive)
    .WithTheme(VisualTheme.Report())
    .WithSize(320, 170);
AssertContains(metric.ToSvg("aot-metric"), "data-cfx-role=\"metric-label\"", "Metric card SVG render failed.");
AssertPng(metric.ToPng(), "Metric card PNG render failed.");

var topology = TopologyChart.Create()
    .WithId("aot-topology")
    .WithTitle("AOT topology")
    .WithViewport(460, 280, 24)
    .WithLegend(TopologyLegend.Default().AddNodeKind("Service", TopologyNodeKind.Service, symbol: "S"))
    .AddNode("api", "API", 80, 120, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "API")
    .AddNode("db", "DB", 280, 120, TopologyNodeKind.Database, TopologyHealthStatus.Warning, symbol: "DB")
    .AddEdge("api-db", "api", "db", "12 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning);
AssertContains(topology.ToSvg(), "data-cfx-role=\"topology\"", "Topology SVG render failed.");
AssertPng(topology.ToPng(), "Topology PNG render failed.");

var interactive = chart.ToInteractiveHtmlPage(options => {
    options.PageTitle = "AOT interactive";
    options.IdScope = "aot-chart";
    options.Localization.Language = "pl-PL";
    options.Localization[HtmlChartText.Pinned] = "Przypięte";
    options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts);
});
AssertContains(interactive, "data-cfx-export=\"png\"", "Interactive PNG export control missing.");
AssertContains(interactive, "ChartForgeXCharts", "Interactive controller runtime missing.");
AssertContains(interactive, "cfxsync", "Interactive sync protocol missing.");
AssertContains(interactive, "data-cfx-pinned-label=\"Przypięte\"", "AOT localized tooltip status missing.");

var mermaidSources = new[] {
    "flowchart LR\nA@{ shape: cloud, label: \"API\" } & B --> C & D",
    "classDiagram\nclass User {\n+string name\n+save() void\n}\nUser <|-- Admin",
    "erDiagram\nCUSTOMER ||--o{ ORDER : places",
    "gantt\nexcludes weekends\nTask :2026-01-02, 2d",
    "swimlane-beta LR\nsubgraph Client\nA\nend\nsubgraph Server\nB\nend\nA --> B",
    "usecase-beta\nactor User\nUser --> Action(Do work)",
    "cynefin-beta\ncomplex\n\"Discover\""
};
var mermaidOptions = new MermaidRenderOptions {
    Presentation = new MermaidPresentationOptions { FontFamily = portableFontFamily }
};
foreach (var source in mermaidSources) {
    var rendered = MermaidRenderer.Render(source, mermaidOptions);
    if (rendered.HasErrors || rendered.Artifact == null) throw new InvalidOperationException("Mermaid AOT render failed.");
    AssertContains(rendered.Artifact.ToSvg(), "<svg", "Mermaid SVG render failed.");
    AssertPng(rendered.Artifact.ToPng(), "Mermaid PNG render failed.");
    var json = rendered.Artifact.ToInterchangeJson();
    if (VisualArtifactInterchangeEnvelope.FromJson(json).ToJson() != json) throw new InvalidOperationException("Mermaid interchange round trip failed.");
    var markup = new MermaidVisualMarkupParser(mermaidOptions).Parse("```mermaid\n" + source + "\n```");
    if (markup.HasErrors || markup.Artifacts.Count != 1) throw new InvalidOperationException("Mermaid markup AOT render failed.");
}

PreparedPipelineSmoke.Run(portableFontFamily);
EmbeddingSmoke.Run(portableFontFamily);
var coreAnimationFrame = new ChartForgeX.Raster.RasterAnimationFrame(
    new ChartForgeX.Raster.RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 }), TimeSpan.FromSeconds(1));
foreach (var format in new[] { ChartForgeX.Raster.RasterAnimationFormat.Gif, ChartForgeX.Raster.RasterAnimationFormat.Apng }) {
    if (ChartForgeX.Raster.RasterAnimationEncoder.Encode(new[] { coreAnimationFrame }, format).Length < 32)
        throw new InvalidOperationException("Core animation encoder failed under AOT.");
}
PresentationPackageSmoke.Run();

static void AssertContains(string text, string expected, string message) {
    if (!text.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException(message);
}

static void AssertPng(byte[] bytes, string message) {
    if (bytes.Length <= 64 || bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71) {
        throw new InvalidOperationException(message);
    }
}
