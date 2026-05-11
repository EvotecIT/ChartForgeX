using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

var chart = Chart.Create()
    .WithTitle("AOT smoke")
    .WithSubtitle("Static report output")
    .WithSize(420, 240)
    .WithTheme(ChartTheme.ReportLight())
    .WithXAxis("Run")
    .WithYAxis("Value")
    .AddSmoothLine("Values", new[] { new ChartPoint(1, 2), new ChartPoint(2, 5), new ChartPoint(3, 4) })
    .AddBar("Warnings", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2), new ChartPoint(3, 1) }, ChartColor.FromRgb(249, 115, 22));

AssertContains(chart.ToSvg(), "<svg", "SVG render failed.");
AssertContains(chart.ToHtmlPage(), "<html", "HTML page render failed.");
AssertPng(chart.ToPng(), "PNG render failed.");

var grid = ChartGrid.Create()
    .WithTitle("AOT grid")
    .WithPanelSize(260, 180)
    .Add(chart)
    .Add(Chart.Create().WithSize(260, 180).WithXLabels("Ready", "Risk").AddDonut("Share", new[] { new ChartPoint(1, 72), new ChartPoint(2, 28) }));
AssertContains(grid.ToSvg("aot-grid"), "data-cfx-role=\"grid-panel\"", "Grid SVG render failed.");
AssertPng(grid.ToPng(), "Grid PNG render failed.");

var metric = MetricCard.Create()
    .WithMetric("Coverage", 0.982, "P1")
    .WithIcon(VisualIcon.Lightning)
    .WithTrend("+2.4 pp")
    .WithStatus(VisualStatus.Positive)
    .WithTheme(ChartTheme.ReportLight())
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
    options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts);
});
AssertContains(interactive, "data-cfx-export=\"png\"", "Interactive PNG export control missing.");
AssertContains(interactive, "new CustomEvent('cfxsync'", "Interactive sync runtime missing.");

static void AssertContains(string text, string expected, string message) {
    if (!text.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException(message);
}

static void AssertPng(byte[] bytes, string message) {
    if (bytes.Length <= 64 || bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71) {
        throw new InvalidOperationException(message);
    }
}
