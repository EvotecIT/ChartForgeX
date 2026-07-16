using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyAutoSizedGroupsEncloseExplicitMemberBounds() {
        var chart = TopologyChart.Create()
            .WithId("auto-sized-explicit-origin")
            .WithViewport(520, 360, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Manual)
            .AddGroup("region", "Region", 300, 200, 0, 0, TopologyHealthStatus.Healthy)
            .AddNode("site", "Site", 100, 80, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "region", width: 120, height: 64);

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var group = prepared.Groups[0];
        var node = prepared.Nodes[0];
        Assert(group.X <= node.X && group.Y <= node.Y, "Auto-sized group origins should move far enough to enclose member nodes.");
        Assert(group.X + group.Width >= node.X + node.Width && group.Y + group.Height >= node.Y + node.Height, "Auto-sized group dimensions should enclose member node bounds.");
        Assert(chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }).Contains("data-group-id=\"region\"", StringComparison.Ordinal), "Auto-sized explicit-origin groups should render as SVG.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }).Length > 64, "Auto-sized explicit-origin groups should render as PNG.");
    }
}
