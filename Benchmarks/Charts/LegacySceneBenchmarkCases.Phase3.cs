using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

public static partial class LegacySceneBenchmarkCases {
#if LEGACY_CHART_API
    // The frozen API's node-label storage is internal. Retain original public authoring labels
    // outside the measured chart so even provisional SVG IDs keep their historical input.
    private static readonly ConditionalWeakTable<Chart, string[]> LegacySankeyLabels = new();
#endif
    /// <summary>Representative native families compiled against the selected product API profile.</summary>
    public static readonly string[] Phase3Fixtures = { "matrix", "calendar", "progress", "polar", "map", "treemap", "sankey", "topology" };

    private static VisualDesignTokens Phase3Tokens() {
        var tokens = VisualDesignTokens.FromJson(_tokens);
        tokens.FontFamily = "CFX Proof Carlito";
        tokens.UseGraphiteLayout = true;
        return tokens;
    }

    private static Chart CreatePhase3Chart(string fixture) {
        var theme = Phase3Tokens().ApplyTo(ChartTheme.GraphiteLight()).WithTypography(22, 13, 11, 11, 12, 11);
        var chart = Chart.Create().WithSize(Width, Height).WithTheme(theme).WithTitle(Title)
            .WithSubtitle("One prepared scene for SVG and native PNG").WithLegend().WithPadding(24, 24, 24, 24).WithPngOutputScale(1).WithPngSupersampling(2);
        switch (fixture) {
            case "matrix":
                chart.WithXLabels(Enumerable.Range(1, 12).Select(index => "Slot " + index).ToArray());
                for (var row = 0; row < 8; row++) chart.AddHeatmapRow("Lane " + (row + 1), Enumerable.Range(0, 12)
                    .Select(column => new ChartPoint(column + 1, (row * 17 + column * 7) % 101)));
                break;
            case "calendar":
                chart.AddCalendarHeatmap("Daily checks", Enumerable.Range(0, 120).Where(index => index % 7 != 0)
                    .Select(index => new ChartCalendarHeatmapItem(new DateTime(2025, 1, 1).AddDays(index), index % 9 == 0 ? 0 : (index * 13) % 37)), firstDayOfWeek: DayOfWeek.Monday);
                break;
            case "progress":
                chart.AddProgressBars("Completion", new double[] { 0, 27, 49, 73, 88, 100 }
                    .Select((value, index) => new ChartProgressItem("Group " + (index + 1), value)), 100);
                break;
            case "polar":
                for (var series = 0; series < 2; series++) chart.AddPolar("Sweep " + (series + 1), Enumerable.Range(0, 24)
                    .Select(index => new ChartPoint(index / 4d, 20 + (index * 7 + series * 13) % 47)));
                break;
            case "map":
                chart.AddDottedMap("Locations", MapItems());
                break;
            case "treemap":
#if LEGACY_CHART_API
                chart.AddTreemap("Capacity", Enumerable.Range(0, 12).Select(index => new ChartTreemapItem("Pool " + (index + 1), 10 + (index * 17) % 73)));
#else
                chart.AddTreemap("Capacity", Enumerable.Range(0, 12).Select(index => new ChartTreemapItem("pool-" + (index + 1), "Pool " + (index + 1), value: 10 + (index * 17) % 73)));
#endif
                break;
            case "sankey":
#if LEGACY_CHART_API
                var legacyLinks = SankeyLinks();
                chart.AddSankey("Requests", legacyLinks);
                LegacySankeyLabels.Add(chart, legacyLinks.SelectMany(link => new[] { link.Source, link.Target }).Distinct(StringComparer.Ordinal).ToArray());
#else
                chart.AddSankey("Requests", new[] { new ChartNode("Input A", "Input A"), new ChartNode("Queue A", "Queue A"), new ChartNode("Queue B", "Queue B"), new ChartNode("Input B", "Input B"), new ChartNode("Complete", "Complete"), new ChartNode("Retry", "Retry") }, SankeyLinks());
#endif
                break;
            default: throw new ArgumentOutOfRangeException(nameof(fixture));
        }
        return chart;
    }

    private static ChartMapPoint[] MapItems() => new[] {
        new ChartMapPoint("Warsaw", 21, 52.2, 74), new ChartMapPoint("London", -.12, 51.5, 81),
        new ChartMapPoint("New York", -74, 40.7, 63), new ChartMapPoint("Tokyo", 139.7, 35.7, 95),
        new ChartMapPoint("Sydney", 151.2, -33.9, 48), new ChartMapPoint("Cape Town", 18.4, -33.9, 56)
    };

#if LEGACY_CHART_API
    private static ChartSankeyLink[] SankeyLinks() => new[] {
        new ChartSankeyLink("Input A", "Queue A", 24), new ChartSankeyLink("Input A", "Queue B", 16),
        new ChartSankeyLink("Input B", "Queue A", 12), new ChartSankeyLink("Input B", "Queue B", 18),
        new ChartSankeyLink("Queue A", "Complete", 30), new ChartSankeyLink("Queue A", "Retry", 6),
        new ChartSankeyLink("Queue B", "Complete", 27), new ChartSankeyLink("Queue B", "Retry", 7)
    };
#else
    private static ChartFlowLink[] SankeyLinks() => new[] {
        new ChartFlowLink("flow-1", "Input A", "Queue A", 24), new ChartFlowLink("flow-2", "Input A", "Queue B", 16),
        new ChartFlowLink("flow-3", "Input B", "Queue A", 12), new ChartFlowLink("flow-4", "Input B", "Queue B", 18),
        new ChartFlowLink("flow-5", "Queue A", "Complete", 30), new ChartFlowLink("flow-6", "Queue A", "Retry", 6),
        new ChartFlowLink("flow-7", "Queue B", "Complete", 27), new ChartFlowLink("flow-8", "Queue B", "Retry", 7)
    };
#endif

    private static TopologyChart CreateTopology() {
        var chart = TopologyChart.Create().WithId("scene-topology").WithTitle(Title)
            .WithSubtitle("One prepared scene for SVG and native PNG").WithViewport(Width, Height, 24)
            .WithLayout(TopologyLayoutMode.Layered).WithDesignTokens(Phase3Tokens());
        chart.DefaultRenderOptions = new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true,
            WrapNodeLabels = true, PngOutputScale = 1, PngSupersamplingScale = 2 };
        for (var index = 0; index < 12; index++) chart.AddAutoNode("node-" + index, "Service " + (index + 1),
            status: index % 4 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, width: 112, height: 48);
        for (var index = 1; index < 12; index++) chart.AddEdge("edge-" + index, "node-" + ((index - 1) / 2), "node-" + index,
            "Link " + index, routing: TopologyEdgeRouting.Orthogonal);
        return chart;
    }

    private static object ExecuteTopology(TopologyChart chart, string operation) => operation switch {
        "Svg" => chart.ToSvg(chart.DefaultRenderOptions), "Png" => chart.ToPng(chart.DefaultRenderOptions),
        "Rgba" => chart.ToRgbaImage(chart.DefaultRenderOptions), _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    private static string Phase3SourceDigest(string fixture) {
        var source = new StringBuilder(fixture).Append('|').Append(Width).Append('|').Append(Height).Append("|padding=24|scale=1|supersampling=2|Carlito|Light");
        var model = Create(fixture);
        if (model is TopologyChart topology) {
            source.Append('|').Append(topology.Title).Append('|').Append(topology.Subtitle).Append('|').Append(topology.LayoutMode);
            foreach (var node in topology.Nodes) source.Append('|').Append(node.Id).Append(':').Append(node.Label).Append(':').Append(node.Status)
                .Append(':').Append(Numeric(node.Width)).Append(':').Append(Numeric(node.Height));
            foreach (var edge in topology.Edges) source.Append('|').Append(edge.Id).Append(':').Append(edge.SourceNodeId).Append(':')
                .Append(edge.TargetNodeId).Append(':').Append(edge.Label).Append(':').Append(edge.Routing);
        } else {
            var chart = (Chart)model;
            source.Append('|').Append(chart.Title).Append('|').Append(chart.Subtitle);
            // The legacy flat Treemap stores item labels in XAxisLabels; the current typed
            // input stores them with each item. TreemapFacts reads their common source meaning.
            if (fixture != "treemap") foreach (var label in chart.Options.XAxisLabels) source.Append('|').Append(Numeric(label.Value)).Append(':').Append(label.Text);
            foreach (var series in chart.Series) {
                source.Append('|').Append(series.Kind).Append(':').Append(series.Name);
                if (series.Kind == ChartSeriesKind.Sankey) {
                    foreach (var label in SankeyNodeLabels(chart)) source.Append("|node:").Append(label);
                    foreach (var flow in SankeyFacts(chart)) source.Append("|flow:").Append(flow.Source).Append(':').Append(flow.Target).Append(':').Append(Numeric(flow.Value));
                } else if (series.Kind == ChartSeriesKind.Treemap) {
                    foreach (var item in TreemapFacts(chart)) source.Append("|treemap:").Append(item.Label).Append(':').Append(Numeric(item.Value));
                } else foreach (var point in series.Points) source.Append('|').Append(Numeric(point.X)).Append(',').Append(Numeric(point.Y)).Append(',').Append(point.BreakBefore);
            }
            if (fixture == "map") foreach (var point in MapItems()) source.Append('|').Append(point.Label).Append(':').Append(Numeric(point.Value!.Value));
            source.Append('|').Append(Numeric(chart.Options.ProgressMaximum)).Append("|calendar-first-day=Monday");
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.ToString())));
    }

    private static IReadOnlyList<(string Label, double Value)> TreemapFacts(Chart chart) {
        var series = chart.Series[0];
#if LEGACY_CHART_API
        var labels = chart.Options.XAxisLabels.ToDictionary(label => label.Value, label => label.Text);
        return series.Points.Select(point => (labels[point.X], point.Y)).ToArray();
#else
        return series.TreemapItems.Select(item => (item.Label, item.Value!.Value)).ToArray();
#endif
    }

    // Compare actual common model facts: the historical API has no authored relationship identities.
    private static IReadOnlyList<string> SankeyNodeLabels(Chart chart) {
#if LEGACY_CHART_API
        if (!LegacySankeyLabels.TryGetValue(chart, out var labels)) throw new InvalidOperationException("Legacy Sankey source labels are missing.");
        return labels;
#else
        return chart.Series[0].Nodes.Select(node => node.Label).ToArray();
#endif
    }

    private static IEnumerable<(string Source, string Target, double Value)> SankeyFacts(Chart chart) {
        var series = chart.Series[0];
#if LEGACY_CHART_API
        var labels = SankeyNodeLabels(chart).ToArray();
        Require(series.Points.Count % 2 == 0, "Legacy Sankey source tuples changed.");
        for (var index = 0; index < series.Points.Count; index += 2) {
            var endpoints = series.Points[index]; var weight = series.Points[index + 1];
            Require(weight.X == weight.Y, "Legacy Sankey source weight changed.");
            yield return (labels[SankeyNodeIndex(chart, endpoints.X)], labels[SankeyNodeIndex(chart, endpoints.Y)], weight.X);
        }
#else
        var nodes = series.Nodes.ToDictionary(node => node.Id);
        foreach (var link in series.FlowLinks) yield return (nodes[link.SourceId].Label, nodes[link.TargetId].Label, link.Value);
#endif
    }

#if LEGACY_CHART_API
    private static int SankeyNodeIndex(Chart chart, double index) {
        Require(index >= 0 && index < SankeyNodeLabels(chart).Count && index == Math.Truncate(index), "Legacy Sankey source endpoint changed.");
        return (int)index;
    }
#endif

    private static string Numeric(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
