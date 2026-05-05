using System;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyRendersDemoSvg() {
        var svg = TopologyDemoCharts.SiteTopologyDemo().ToSvg();
        Assert(!string.IsNullOrWhiteSpace(svg), "Topology SVG output should not be empty.");
        Assert(svg.Contains("<svg", StringComparison.Ordinal), "Topology renderer should emit complete SVG.");
        Assert(svg.Contains("data-cfx-role=\"topology\"", StringComparison.Ordinal), "Topology SVG should expose a topology role.");
        Assert(svg.Contains("AMER Hub", StringComparison.Ordinal), "Topology SVG should contain expected node labels.");
        Assert(svg.Contains("24 ms", StringComparison.Ordinal), "Topology SVG should contain expected edge labels.");
        Assert(svg.Contains("AMER", StringComparison.Ordinal), "Topology SVG should contain expected group labels.");
        Assert(svg.Contains("href=\"/topology/sites/amer-hub\"", StringComparison.Ordinal), "Topology SVG should emit safe href links.");
        Assert(svg.Contains("<title>AMER Hub (Healthy)</title>", StringComparison.Ordinal), "Topology SVG should emit native SVG tooltips.");
        Assert(svg.Contains("data-node-kind=\"HubSite\"", StringComparison.Ordinal), "Topology SVG should expose node kinds.");
        Assert(svg.Contains("data-cfx-status=\"Critical\"", StringComparison.Ordinal), "Topology SVG should expose health status metadata.");
        Assert(svg.Contains("data-cfx-role=\"topology-legend\"", StringComparison.Ordinal), "Topology SVG should render the legend.");
        var png = TopologyDemoCharts.SiteTopologyDemo().ToPng();
        Assert(png.Length > 64 && png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71, "Topology renderer should emit a valid PNG image.");
    }

    private static void TopologyRendersAllDemoFamilies() {
        foreach (var chart in new[] {
            TopologyDemoCharts.SiteTopologyDemo(),
            TopologyDemoCharts.ReplicationMeshDemo(),
            TopologyDemoCharts.SubnetsSiteLinksDemo(),
            TopologyDemoCharts.GeographicTopologyDemo()
        }) {
            var svg = chart.ToSvg();
            Assert(svg.Contains("data-cfx-role=\"topology-node\"", StringComparison.Ordinal), "Topology demo should render nodes: " + chart.Id + ".");
            Assert(svg.Contains("data-cfx-role=\"topology-edge\"", StringComparison.Ordinal), "Topology demo should render edges: " + chart.Id + ".");
            Assert(svg.Contains("data-cfx-role=\"topology-edge-label\"", StringComparison.Ordinal), "Topology demo should render edge labels: " + chart.Id + ".");
        }
    }

    private static void TopologyEscapesTextAndSkipsUnsafeHref() {
        var chart = new TopologyChart {
            Id = "escape-demo",
            Title = "Unsafe <Topology>",
            Viewport = new TopologyViewport { Width = 420, Height = 240, Padding = 20 }
        };
        chart.Nodes.Add(new TopologyNode { Id = "a", Label = "A < B & C", X = 40, Y = 80, Href = "javascript:alert(1)", Tooltip = "Tooltip <unsafe>" });
        chart.Nodes.Add(new TopologyNode { Id = "b", Label = "B", X = 240, Y = 80, Href = "/safe?x=1&y=2" });
        chart.Edges.Add(new TopologyEdge { Id = "a-b", SourceNodeId = "a", TargetNodeId = "b", Label = "5 < 8 & ok", Href = "data:text/html,fail" });

        var svg = chart.ToSvg();
        Assert(svg.Contains("A &lt; B &amp; C", StringComparison.Ordinal), "Topology labels should be XML-escaped.");
        Assert(svg.Contains("5 &lt; 8 &amp; ok", StringComparison.Ordinal), "Topology edge labels should be XML-escaped.");
        Assert(svg.Contains("href=\"/safe?x=1&amp;y=2\"", StringComparison.Ordinal), "Safe topology hrefs should be escaped and emitted.");
        Assert(!svg.Contains("javascript:", StringComparison.OrdinalIgnoreCase), "Unsafe javascript hrefs should not be emitted.");
        Assert(!svg.Contains("data:text/html", StringComparison.OrdinalIgnoreCase), "Unsafe data hrefs should not be emitted.");
    }

    private static void TopologyValidatorReportsActionableErrors() {
        var chart = new TopologyChart {
            Id = "bad",
            Viewport = new TopologyViewport { Width = 400, Height = 240, Padding = 12 }
        };
        chart.Groups.Add(new TopologyGroup { Id = "g", Label = "Group", X = 10, Y = 10, Width = 100, Height = 80 });
        chart.Groups.Add(new TopologyGroup { Id = "g", Label = "Duplicate", X = 130, Y = 10, Width = 100, Height = 80 });
        chart.Nodes.Add(new TopologyNode { Id = "n", Label = "Node", GroupId = "missing", X = 40, Y = 80, Width = 120, Height = 64 });
        chart.Nodes.Add(new TopologyNode { Id = "n", Label = "Duplicate", X = 220, Y = 80, Width = 120, Height = 64 });
        chart.Edges.Add(new TopologyEdge { Id = "e", SourceNodeId = "n", TargetNodeId = "missing", Label = "bad" });

        var result = new TopologyChartValidator().Validate(chart);
        Assert(!result.IsValid, "Invalid topology charts should report validation errors.");
        Assert(result.Errors.Any(error => error.Code == "duplicate-node-id"), "Topology validator should detect duplicate node ids.");
        Assert(result.Errors.Any(error => error.Code == "duplicate-group-id"), "Topology validator should detect duplicate group ids.");
        Assert(result.Errors.Any(error => error.Code == "missing-node-group"), "Topology validator should detect nodes referencing missing groups.");
        Assert(result.Errors.Any(error => error.Code == "missing-edge-target"), "Topology validator should detect edges referencing missing target nodes.");
        AssertThrows<TopologyValidationException>(() => chart.ToSvg(), "Topology renderer should throw a clear validation exception for invalid data.");
    }

    private static void TopologyValidatorRejectsInvalidDimensions() {
        var chart = TopologyDemoCharts.SiteTopologyDemo();
        chart.Nodes[0].Width = 0;
        chart.Groups[0].Height = -1;
        var result = new TopologyChartValidator().Validate(chart);
        Assert(result.Errors.Any(error => error.Code == "node-width"), "Topology validator should reject invalid node dimensions.");
        Assert(result.Errors.Any(error => error.Code == "group-height"), "Topology validator should reject invalid group dimensions.");
    }

    private static void TopologyViewsRenderFocusedSubsets() {
        var chart = TopologyDemoCharts.SiteTopologyDemo();
        var svg = chart.ToSvg(new TopologyRenderOptions {
            View = new TopologyView { Id = "emea", Title = "EMEA only", GroupIds = { "EMEA" } }
        });

        Assert(svg.Contains("data-chart-id=\"site-topology-emea\"", StringComparison.Ordinal), "Topology views should scope chart ids predictably.");
        Assert(svg.Contains("EMEA only", StringComparison.Ordinal), "Topology views should support title overrides.");
        Assert(svg.Contains("EMEA Hub", StringComparison.Ordinal), "Topology views should keep nodes in selected groups.");
        Assert(!svg.Contains("AMER Hub", StringComparison.Ordinal), "Topology views should omit nodes outside selected groups.");
        Assert(!svg.Contains("APAC Hub", StringComparison.Ordinal), "Topology views should omit unrelated selected-group nodes.");
        Assert(chart.ToPng(new TopologyRenderOptions { View = new TopologyView { GroupIds = { "EMEA" } } }).Length > 64, "Focused topology views should render as PNG.");
    }
}
