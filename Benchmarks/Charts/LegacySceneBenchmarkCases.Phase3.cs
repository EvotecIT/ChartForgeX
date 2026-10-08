using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public static partial class LegacySceneBenchmarkCases {
    /// <summary>Representative additional native families; all factories use APIs present in the frozen legacy binary.</summary>
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
                chart.AddTreemap("Capacity", Enumerable.Range(0, 12).Select(index => new ChartTreemapItem("Pool " + (index + 1), 10 + (index * 17) % 73)));
                break;
            case "sankey":
                chart.AddSankey("Requests", SankeyLinks());
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

    private static ChartSankeyLink[] SankeyLinks() => new[] {
        new ChartSankeyLink("Input A", "Queue A", 24), new ChartSankeyLink("Input A", "Queue B", 16),
        new ChartSankeyLink("Input B", "Queue A", 12), new ChartSankeyLink("Input B", "Queue B", 18),
        new ChartSankeyLink("Queue A", "Complete", 30), new ChartSankeyLink("Queue A", "Retry", 6),
        new ChartSankeyLink("Queue B", "Complete", 27), new ChartSankeyLink("Queue B", "Retry", 7)
    };

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
            foreach (var label in chart.Options.XAxisLabels) source.Append('|').Append(Numeric(label.Value)).Append(':').Append(label.Text);
            foreach (var series in chart.Series) {
                source.Append('|').Append(series.Kind).Append(':').Append(series.Name);
                foreach (var point in series.Points) source.Append('|').Append(Numeric(point.X)).Append(',').Append(Numeric(point.Y)).Append(',').Append(point.BreakBefore);
            }
            if (fixture == "map") foreach (var point in MapItems()) source.Append('|').Append(point.Label).Append(':').Append(Numeric(point.Value!.Value));
            if (fixture == "sankey") foreach (var link in SankeyLinks()) source.Append('|').Append(link.Source).Append(':').Append(link.Target).Append(':').Append(Numeric(link.Value));
            source.Append('|').Append(Numeric(chart.Options.ProgressMaximum)).Append("|calendar-first-day=Monday");
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.ToString())));
    }

    private static string Numeric(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
