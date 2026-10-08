using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyTextFitsIconRowsAndCards() {
        var chart = TopologyChart.Create()
            .WithId("text-fit")
            .WithViewport(420, 240, 20)
            .WithLegend(null)
            .AddGroup("app", "Application Tier", 48, 70, 190, 120, TopologyHealthStatus.Warning, "Services", symbol: "service")
            .AddNode("team", "Payments Team With Extra Label", 92, 126, TopologyNodeKind.Team, TopologyHealthStatus.Healthy, "app", "Owner", width: 120, height: 58, symbol: "TM");

        var options = new TopologyRenderOptions { IncludeLegend = false };
        var svg = chart.ToSvg(options);
        var groupTitle = TopologyRoleTexts(svg, "topology-group-label").Single();
        Assert(((string?)groupTitle.Attribute("text-anchor") ?? "start") == "start", "Group icon/title rows should use left-aligned measured text.");
        Assert(!svg.Contains(">Payments Team With Extra Label<", StringComparison.Ordinal), "Node card titles should trim to the available card width.");
        Assert(svg.Contains("Pay", StringComparison.Ordinal), "Trimmed node labels should keep their useful prefix.");
        Assert(chart.ToPng(options).Length > 64, "Icon row and fitted-card text should render as PNG.");

        var wrapped = TopologyChart.Create()
            .WithId("wrapped-text-fit")
            .WithViewport(320, 200, 20)
            .WithLegend(null)
            .AddNode("wrapped", "Authentication Relationship Service", 84, 96, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 180, height: 64, symbol: "S");
        var wrappedSvg = wrapped.ToSvg(new TopologyRenderOptions { IncludeLegend = false, WrapNodeLabels = true, MaxNodeLabelLines = 2 });
        var wrappedLabels = TopologyRoleTexts(wrappedSvg, "topology-node-label");
        Assert(wrappedLabels.Length == 2 && wrappedLabels.All(label => (double?)label.Attribute("font-size") == ChartForgeX.Themes.VisualTheme.Graphite().Typography.DataLabelSize),
            "Wrapped node labels should use the common typography size and fit across two measured lines.");
    }

    private static void TopologyIconLabelsRemainReusableAndFitted() {
        var chart = TopologyChart.Create()
            .WithId("icon-labels")
            .WithViewport(220, 150, 20)
            .WithLegend(null)
            .AddNode("dc1", "NYC-DC1", 60, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 46, height: 42, symbol: "DC")
            .AddNode("dc2", "LON-DC2-VeryLongName", 140, 70, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 46, height: 42, symbol: "DC")
            .AddEdge("rep", "dc1", "dc2", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight);

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeIconLabels = true,
            NodeDisplayMode = TopologyNodeDisplayMode.Icon
        };
        var svg = chart.ToSvg(options);
        Assert(svg.Contains(">NYC-DC1<", StringComparison.Ordinal), "Icon-mode labels should render when enabled.");
        Assert(svg.Contains("data-cfx-role=\"topology-node-icon-label\"", StringComparison.Ordinal), "Icon-mode labels should render with a readable label plate.");
        Assert(!svg.Contains(">LON-DC2-VeryLongName<", StringComparison.Ordinal), "Icon-mode labels should fit to their compact visual width.");
        Assert(chart.ToPng(options).Length > 64, "Icon-mode labels should render as PNG.");
    }

    private static void TopologyMonitoringStyleSupportsNeutralGroupSurfaces() {
        var chart = TopologyChart.Create()
            .WithId("neutral-groups")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddGroup("site", "HQ-NYC (32 DCs)", 64, 72, 220, 92, TopologyHealthStatus.Healthy, symbol: "globe")
            .AddNode("dc1", "DC 1", 96, 118, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "site", width: 14, height: 14)
            .WithNodeDisplay("dc1", TopologyNodeDisplayMode.Dot);

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithMonitoringDashboardStyle()
            .WithNeutralGroupSurfaces();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-visual-style=\"MonitoringDashboard\"", StringComparison.Ordinal), "Neutral group surfaces should compose with monitoring style.");
        Assert(svg.Contains("data-group-symbol=\"globe\"", StringComparison.Ordinal), "Neutral group surfaces should keep reusable group symbols.");
        Assert(svg.Contains("data-cfx-role=\"topology-group-status\"", StringComparison.Ordinal), "Monitoring group headers should expose compact reusable status dots.");
        var surface = System.Xml.Linq.XDocument.Parse(svg).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-group-surface");
        Assert(surface.RenderedColor("fill").ToHex() == "#FFFFFF" && surface.RenderedColor("stroke").ToHex() == "#16A34A",
            "Neutral group surfaces should render white cards with status-colored borders.");
        Assert(chart.ToPng(options).Length > 64, "Neutral group surfaces should render as PNG.");
    }

    private static void TopologyGroupStatusDotsUseHealthStatusColor() {
        var chart = TopologyChart.Create()
            .WithId("group-status-color")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddGroup("site", "Blue Warning Site", 64, 72, 220, 92, TopologyHealthStatus.Warning, symbol: "globe", color: "#2563EB")
            .AddNode("dc1", "DC 1", 96, 118, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "site", width: 14, height: 14)
            .WithNodeDisplay("dc1", TopologyNodeDisplayMode.Dot);

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithMonitoringDashboardStyle()
            .WithNeutralGroupSurfaces();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-group-status\"", StringComparison.Ordinal), "Monitoring group headers should expose a reusable status dot.");
        Assert(svg.Contains("data-cfx-status=\"Warning\"", StringComparison.Ordinal), "The compact group status dot should preserve health state metadata.");
        Assert(svg.Contains("fill=\"#F97316\"", StringComparison.Ordinal), "Group status dots should use the health status color, not the group accent color.");
        Assert(chart.ToPng(options).Length > 64, "Group status dots should render as PNG.");
    }

    private static void TopologyGroupHeadersReserveIconAndStatusSpace() {
        const string longTitle = "Application Tier With Extremely Long Service Owner Title";
        var chart = TopologyChart.Create()
            .WithId("group-header-fit")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddGroup("app", longTitle, 52, 72, 260, 92, TopologyHealthStatus.Critical, subtitle: "Services", symbol: "application", color: "#F97316")
            .AddNode("svc", "Payments Team", 112, 126, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, "app", width: 76, height: 44, symbol: "TM");

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle()
            .WithNeutralGroupSurfaces();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-group-status\"", StringComparison.Ordinal), "Monitoring group headers should keep the right-side status marker.");
        Assert(!svg.Contains($">{longTitle}<", StringComparison.Ordinal), "Group header labels should be fitted before they can collide with the symbol or status marker.");
        Assert(svg.Contains("Application", StringComparison.Ordinal), "Fitted group headers should keep the meaningful start of the label.");
        Assert(chart.ToPng(options).Length > 64, "Fitted group headers should render as PNG.");
    }

    private static void TopologyGroupHeadersFitMediumTitlesBeforeTrimming() {
        const string mediumTitle = "SFO-SanFrancisco (11 DCs)";
        var chart = TopologyChart.Create()
            .WithId("group-header-medium-fit")
            .WithViewport(520, 220, 20)
            .WithLegend(null)
            .AddGroup("sfo", mediumTitle, 72, 72, 330, 100, TopologyHealthStatus.Healthy, symbol: "globe")
            .AddNode("dc1", "DC 1", 112, 124, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "sfo", width: 18, height: 18)
            .WithNodeDisplay("dc1", TopologyNodeDisplayMode.Dot);

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithMonitoringDashboardStyle()
            .WithNeutralGroupSurfaces();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains($">{mediumTitle}<", StringComparison.Ordinal), "Medium-length monitoring group titles should fit by reducing font size before trimming.");
        Assert(chart.ToPng(options).Length > 64, "Medium fitted group headers should render as PNG.");
    }

    private static void TopologyMonitoringDotNodesRenderCompactSymbols() {
        var chart = TopologyChart.Create()
            .WithId("dot-symbols")
            .WithViewport(240, 160, 20)
            .WithLegend(null)
            .AddNode("dc1", "DC 1", 90, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 18, height: 18, symbol: "DC")
            .WithNodeDisplay("dc1", TopologyNodeDisplayMode.Dot);

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-node-symbol\"", StringComparison.Ordinal), "Monitoring dot nodes should render compact reusable symbols when a node has a symbol.");
        Assert(chart.ToPng(options).Length > 64, "Monitoring dot node symbols should render as PNG.");
    }

    private static void TopologyHiddenNodesWorkAsReusableEdgeAnchors() {
        var chart = TopologyChart.Create()
            .WithId("hidden-anchors")
            .WithViewport(320, 160, 20)
            .WithLegend(null)
            .AddNode("source", "Source", 42, 64, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "CL")
            .AddNode("anchor", "Group Edge Anchor", 238, 84, TopologyNodeKind.Generic, TopologyHealthStatus.Unknown, width: 1, height: 1)
            .AddEdge("source-anchor", "source", "anchor", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .WithNodeDisplay("source", TopologyNodeDisplayMode.Icon)
            .WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-edge-id=\"source-anchor\"", StringComparison.Ordinal), "Hidden anchors should still be usable by edges.");
        Assert(!svg.Contains("data-node-id=\"anchor\"", StringComparison.Ordinal), "Hidden anchors should not render visible node markup.");
        Assert(chart.ToPng(options).Length > 64, "Hidden edge anchors should render as PNG without visible node artifacts.");
    }

}
