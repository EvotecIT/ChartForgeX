using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static class RendererGeometryFixture {
    internal static (string Svg, RgbaImage Png) Render(string family, byte alpha = 128, int density = 1) {
        var visual = Create(family, alpha, density);
        if (visual is Chart chart) return (chart.ToSvg(), chart.ToRgbaImage());
        if (visual is IVisualBlock block) return (block.ToSvg(), new PngVisualBlockRenderer().RenderImage(block));
        var topology = (TopologyChart)visual;
        var options = TopologyOptions(family, density);
        return (topology.ToSvg(options), topology.ToRgbaImage(options));
    }

    internal static object Create(string family, byte alpha = 128, int density = 1) {
        var ink = ChartColor.FromRgba(30, 80, 170, alpha);
        var theme = ChartTheme.Light();
        theme.Background = theme.CardBackground = theme.PlotBackground = ChartColor.Transparent;
        theme.Text = theme.MutedText = theme.Axis = theme.Grid = theme.PlotBorder = ChartColor.Transparent;
        theme.Palette = new[] { ink, ink, ink, ink };
        if (family.StartsWith("funnel", StringComparison.Ordinal)) {
            var chart = Chart.Create().WithSize(400, 280).WithTheme(theme).WithCard(false).WithPlotBackground(false).WithLegend(false);
            chart.Options.ShowAxes = chart.Options.ShowGrid = chart.Options.ShowDataLabels = false;
            chart.Options.PngOutputScale = density;
            chart.AddFunnel("Stages", new[] { new ChartPoint(0, 8), new ChartPoint(1, 5) }, family == "funnel-series" ? ink : null);
            chart.Series[0].ShowDataLabels = false;
            if (family == "funnel-point") chart.Series[0].WithPointColor(0, ink).WithPointColor(1, ink);
            return chart;
        }
        if (family.StartsWith("wardley", StringComparison.Ordinal)) {
            if (family != "wardley") theme = family == "wardley-dark" ? ChartTheme.Dark() : ChartTheme.Light();
            var map = WardleyMapBlock.Create().WithSize(400, 280).WithTheme(theme).WithCard(false).WithTransparentBackground().WithPngOutputScale(density);
            map.AddNode("origin", "Origin", 0.5, 0.1);
            map.AddEvolution("origin", 0.8);
            map.AddMarker("Forward", 0.8, 0.2, WardleyMapMarkerKind.Accelerator);
            map.AddMarker("Backward", 0.2, 0.2, WardleyMapMarkerKind.Deaccelerator);
            if (family != "wardley") {
                map.AddNode("user", "User", 0.8, 0.6, WardleyMapNodeKind.Anchor);
                map.AddLink("user", "origin", "Calls");
                map.AddAnnotation(1, "Review", 0.7, 0.1);
                map.Nodes[0].Strategy = "buy";
                map.AddPipeline("origin").AddComponent("Service", 0.35);
            }
            return map;
        }
        if (family is "fork" or "flame" or "droplet") {
            var icon = family == "fork" ? VisualIcon.ForkKnife : family == "flame" ? VisualIcon.Flame : VisualIcon.Droplet;
            var card = MetricCard.Create().WithMetric("Meals", 12).WithSize(200, 140).WithTheme(theme).WithCard(false).WithTransparentBackground().WithIcon(icon).WithPngOutputScale(density);
            return card;
        }
        var topologyTheme = TopologyTheme.Light();
        topologyTheme.Background = "#FFFFFF";
        var topology = TopologyChart.Create().WithId("geometry-" + family).WithViewport(400, 280, 20).WithLegend(null).WithTheme(topologyTheme);
        topology.AddNode("a", "A", 40, 100, TopologyNodeKind.Database, width: 80, height: 64);
        topology.AddNode("b", "B", 280, 100, TopologyNodeKind.Queue, width: 80, height: 64);
        topology.AddEdge("route", "a", "b", null, TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight);
        if (family == "halo" || family.StartsWith("geo-", StringComparison.Ordinal)) {
            topologyTheme.Background = "#FFFFFF80";
            topology.Edges[0].Color = "#1E50AA80";
            topology.Edges[0].Opacity = 0.5;
        }
        if (family.StartsWith("geo-", StringComparison.Ordinal)) {
            topology.WithLayout(TopologyLayoutMode.Geographic).WithNodeCoordinates("a", -80, 30).WithNodeCoordinates("b", 50, 30);
            topology.Edges[0].Routing = TopologyEdgeRouting.Curved;
            topology.Edges[0].Kind = family == "geo-link" ? TopologyEdgeKind.Link : TopologyEdgeKind.Replication;
        }
        return topology;
    }

    internal static TopologyRenderOptions TopologyOptions(string family, int density) {
        var monitoring = family.StartsWith("monitoring", StringComparison.Ordinal) || family == "halo" || family.StartsWith("geo-", StringComparison.Ordinal);
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeNodeLabels = false, IncludeStatusBadges = false, IncludeEdgeLabels = false,
            ArrowMarkerStyle = TopologyArrowMarkerStyle.Chevron, PngOutputScale = density,
            VisualStyle = monitoring ? TopologyVisualStyle.MonitoringDashboard : TopologyVisualStyle.Default };
        if (family.StartsWith("topology-", StringComparison.Ordinal) || family.StartsWith("monitoring-", StringComparison.Ordinal))
            options.ArrowMarkerStyle = Enum.Parse<TopologyArrowMarkerStyle>(family.Substring(family.IndexOf('-') + 1));
        return options;
    }
}
