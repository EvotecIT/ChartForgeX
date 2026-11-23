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
        var theme = VisualTheme.Graphite();
        theme = theme.Configure(builder => { builder.Light.Background = builder.Dark.Background = builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = builder.Light.Surface = builder.Dark.Surface = ChartColor.Transparent; });
        theme = theme.Configure(builder => { builder.Light.Foreground = builder.Dark.Foreground = ChartColor.Transparent; builder.Light.Muted = builder.Dark.Muted = builder.Light.Axis = builder.Dark.Axis = builder.Light.Grid = builder.Dark.Grid = builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.Transparent; });
        theme = theme.Configure(builder => { builder.Light.Palette = builder.Dark.Palette = new[] { ink, ink, ink, ink }; });
        if (family.StartsWith("funnel", StringComparison.Ordinal)) {
            var chart = Chart.Create().Size(400, 280).Theme(theme).ShowCard(false).ShowPlotBackground(false).Legend(false);
            chart.Options.ShowAxes = chart.Options.ShowGrid = false; chart.Options.DataLabels.Visible = false;
            chart.Options.Png.OutputScale = density;
            chart.AddFunnel("Stages", new[] { new ChartPoint(0, 8), new ChartPoint(1, 5) }, family == "funnel-series" ? ink : null);
            chart.Series[0].ShowDataLabels = false;
            if (family == "funnel-point") chart.Series[0].PointColor(0, ink).PointColor(1, ink);
            return chart;
        }
        if (family.StartsWith("wardley-contract", StringComparison.Ordinal)) {
            theme = theme.Configure(builder => { builder.Light.Axis = builder.Dark.Axis = ink; });
            var map = WardleyMapBlock.Create().Size(400, 280).Theme(theme).ShowCard(false).TransparentBackground().PngOutputScale(density);
            map.AddNode("origin", "Origin", 0.5, 0.1).Strategy = "buy";
            map.AddNode("user", "User", 0.8, 0.6, WardleyMapNodeKind.Anchor);
            map.AddLink("user", "origin", flow: VisualLinkDirection.Bidirectional);
            var evolution = family.EndsWith("narrow", StringComparison.Ordinal) ? 0.1 : family.EndsWith("wide", StringComparison.Ordinal) ? 0.9 : 0.35;
            map.AddPipeline("origin").AddComponent("Service", evolution);
            return map;
        }
        if (family.StartsWith("wardley", StringComparison.Ordinal)) {
            if (family != "wardley") theme = VisualTheme.Graphite();
            var mode = family == "wardley-dark" ? ThemeMode.Dark : ThemeMode.Light;
            var map = WardleyMapBlock.Create().Size(400, 280).Theme(theme).ThemeMode(mode).ShowCard(false).TransparentBackground().PngOutputScale(density);
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
            var icon = family == "fork" ? MetricCardIcons.ForkKnife : family == "flame" ? MetricCardIcons.Flame : MetricCardIcons.Droplet;
            var card = MetricCard.Create().Metric("Meals", 12).Size(200, 140).Theme(theme).ShowCard(false).TransparentBackground().Icon(icon).PngOutputScale(density);
            return card;
        }
        var topologyTheme = VisualTheme.Graphite();
        topologyTheme = topologyTheme.Configure(builder => {
            // Geometry and alpha comparisons use an explicit white backing in both modes.
            builder.Light.Background = builder.Dark.Background = ChartColor.White;
            builder.Light.Surface = builder.Dark.Surface = ChartColor.White;
        });
        var topology = TopologyChart.Create().Id("geometry-" + family).Viewport(new TopologyViewport(400, 280, 20)).Legend((TopologyLegend?)null).Theme(topologyTheme);
        topology.AddNode("a", "A", 40, 100, TopologyNodeKind.Database, width: 80, height: 64);
        topology.AddNode("b", "B", 280, 100, TopologyNodeKind.Queue, width: 80, height: 64);
        topology.AddEdge("route", "a", "b", null, TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight);
        if (family == "halo" || family.StartsWith("geo-", StringComparison.Ordinal)) {
            topologyTheme = topologyTheme.Configure(builder => { builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#FFFFFF80"); });
            topology.Edges[0].Color = ChartColor.FromHex("#1E50AA80");
            topology.Edges[0].Opacity = 0.5;
        }
        if (family.StartsWith("geo-", StringComparison.Ordinal)) {
            topology.LayoutMode(TopologyLayoutMode.Geographic).UpdateNode("a", n => n.Longitude(-80).Latitude(30)).UpdateNode("b", n => n.Longitude(50).Latitude(30));
            topology.Edges[0].Routing = TopologyEdgeRouting.Curved;
            topology.Edges[0].Kind = family == "geo-link" ? TopologyEdgeKind.Link : TopologyEdgeKind.Replication;
        }
        if (family.StartsWith("accent-", StringComparison.Ordinal)) {
            topology.AddNode("c", "Cloud", 40, 190, TopologyNodeKind.Cloud, width: 80, height: 64);
            if (family.Contains("theme")) topologyTheme = topologyTheme.Configure(builder => { var status = builder.Light.Status; status.Neutral = new VisualTokenColor(ChartColor.FromHex("#0000FF80"), status.Neutral.Ink); });
            else foreach (var id in new[] { "a", "b", "c" }) {
                if (family.Contains("catalog")) topology.NodeIcon(id, "sample:" + id, TopologyOptions(family, density).IconCatalog);
                else topology.UpdateNode(id, n => n.Color(ChartColor.FromHex("#0000FF80")));
            }
        }
        return topology.Theme(topologyTheme);
    }

    internal static TopologyRenderOptions TopologyOptions(string family, int density) {
        var monitoring = family.StartsWith("monitoring", StringComparison.Ordinal) || family.EndsWith("-monitoring", StringComparison.Ordinal) || family == "halo" || family.StartsWith("geo-", StringComparison.Ordinal);
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeNodeLabels = false, IncludeStatusBadges = false, IncludeEdgeLabels = false,
            ArrowMarkerStyle = TopologyArrowMarkerStyle.Chevron, PngOutputScale = density,
            VisualStyle = monitoring ? TopologyVisualStyle.MonitoringDashboard : TopologyVisualStyle.Default };
        if (family.StartsWith("topology-", StringComparison.Ordinal) || family.StartsWith("monitoring-", StringComparison.Ordinal))
            options.ArrowMarkerStyle = Enum.Parse<TopologyArrowMarkerStyle>(family.Substring(family.IndexOf('-') + 1));
        if (family.Contains("catalog")) {
            var pack = new TopologyIconPack("sample", "Sample")
                .AddIcon("a", "Database", TopologyNodeKind.Database, TopologyIconShape.Database, "DB", ChartColor.FromHex("#0000FF80"))
                .AddIcon("b", "Queue", TopologyNodeKind.Queue, TopologyIconShape.Badge, "Q", ChartColor.FromHex("#0000FF80"))
                .AddIcon("c", "Cloud", TopologyNodeKind.Cloud, TopologyIconShape.Cloud, "CL", ChartColor.FromHex("#0000FF80"));
            options.IconCatalog = TopologyIconCatalog.Default().AddPack(pack);
        }
        return options;
    }
}
