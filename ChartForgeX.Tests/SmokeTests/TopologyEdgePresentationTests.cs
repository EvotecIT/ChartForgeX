using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyMonitoringStyleCompactsHierarchyEdges() {
        var chart = TopologyChart.Create()
            .WithId("monitoring-edges")
            .WithViewport(360, 180, 20)
            .WithLegend(null)
            .AddNode("hub", "Hub", 60, 64, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 58, height: 46, symbol: "H")
            .AddNode("branch", "Branch", 230, 64, TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, width: 58, height: 46, symbol: "S")
            .AddEdge("hierarchy", "hub", "branch", null, TopologyEdgeKind.Link, TopologyHealthStatus.Unknown, VisualLinkDirection.None, TopologyEdgeRouting.Straight)
            .WithEdgeMuted("hierarchy");

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("stroke=\"#CBD5E1\"", StringComparison.Ordinal), "Monitoring style should use a lighter hierarchy edge color.");
        Assert(svg.Contains("stroke-width=\"1.05\"", StringComparison.Ordinal), "Monitoring style should render muted hierarchy edges thinner than primary paths.");
        Assert(chart.ToPng(options).Length > 64, "Compact monitoring hierarchy edges should render as PNG.");
    }

    private static void TopologySubtleEdgesPreserveStatusColor() {
        var chart = TopologyChart.Create()
            .WithId("subtle-edge")
            .WithViewport(300, 180, 20)
            .WithLegend(null)
            .AddNode("a", "A", 60, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 200, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddEdge("fan", "a", "b", null, TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.None, TopologyEdgeRouting.Straight)
            .WithEdgeLineStyle("fan", TopologyEdgeLineStyle.Dashed)
            .WithEdgeEmphasis("fan", TopologyEdgeEmphasis.Subtle);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabels = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-edge-emphasis=\"Subtle\"", StringComparison.Ordinal), "Subtle edges should expose reusable emphasis metadata.");
        Assert(svg.Contains("stroke=\"#16A34A\"", StringComparison.Ordinal), "Subtle edges should preserve health status color instead of becoming structural gray.");
        Assert(svg.Contains("stroke-width=\"1.05\"", StringComparison.Ordinal), "Subtle monitoring edges should render with a lower visual weight.");
        Assert(TopologyEdgeLine(svg, "fan").RenderedColor("stroke").A == (byte)Math.Round(255 * .48), "Subtle monitoring edges should render below normal opacity.");
        Assert(chart.ToPng(options).Length > 64, "Subtle status-preserving edges should render as PNG.");
    }

    private static void TopologyEdgesCanBeStyledByKind() {
        var chart = TopologyChart.Create()
            .WithId("kind-edge-style")
            .WithViewport(360, 180, 20)
            .WithLegend(null)
            .AddNode("hub", "Hub", 60, 64, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 58, height: 46, symbol: "H")
            .AddNode("site", "Site", 230, 64, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, width: 58, height: 46, symbol: "S")
            .AddEdge("dependency", "hub", "site", null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, VisualLinkDirection.None, TopologyEdgeRouting.Curved)
            .WithEdgesOfKind(TopologyEdgeKind.Dependency, lineStyle: TopologyEdgeLineStyle.Dashed, emphasis: TopologyEdgeEmphasis.Subtle, color: "#64748B");

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-edge-line-style=\"Dashed\"", StringComparison.Ordinal), "Bulk edge-kind styling should apply reusable line style metadata.");
        Assert(svg.Contains("data-edge-emphasis=\"Subtle\"", StringComparison.Ordinal), "Bulk edge-kind styling should apply reusable edge emphasis metadata.");
        Assert(svg.Contains("data-edge-color=\"#64748B\"", StringComparison.Ordinal), "Bulk edge-kind styling should apply reusable relationship colors.");
        Assert(TopologyEdgeLine(svg, "dependency").RenderedColor("stroke").ToHex() == "#64748B", "Bulk edge-kind colors should drive SVG route color.");
        Assert(chart.ToPng(options).Length > 64, "Bulk edge-kind styling should render as PNG.");

        chart.Edges[0].IsMuted = true;
        var mutedSvg = chart.ToSvg(options);
        Assert(TopologyEdgeLine(mutedSvg, "dependency").RenderedColor("stroke").ToHex() == "#CBD5E1", "Muted monitoring edges should render neutral even when the caller supplied an explicit edge color.");
        Assert(TopologyEntity(mutedSvg, "edge", "dependency").Attribute("data-edge-color")!.Value == "#64748B", "Authored color metadata should remain available when the visual muted state overrides its paint.");
    }

    private static void TopologyEdgesRenderByVisualPriority() {
        var chart = TopologyChart.Create()
            .WithId("edge-render-priority")
            .WithViewport(360, 180, 20)
            .WithLegend(null)
            .AddNode("a", "A", 70, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 58, height: 46, symbol: "H")
            .AddNode("b", "B", 230, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 58, height: 46, symbol: "H")
            .AddEdge("selected-healthy", "a", "b", "selected", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Curved)
            .AddEdge("dependency-context", "a", "b", null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, VisualLinkDirection.None, TopologyEdgeRouting.Curved)
            .AddEdge("critical-route", "a", "b", "critical", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.Curved)
            .WithEdgesOfKind(TopologyEdgeKind.Dependency, emphasis: TopologyEdgeEmphasis.Subtle);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle()
            .WithSelectedEdge("selected-healthy");
        var svg = chart.ToSvg(options);
        var dependencyIndex = svg.IndexOf("data-edge-id=\"dependency-context\"", StringComparison.Ordinal);
        var criticalIndex = svg.IndexOf("data-edge-id=\"critical-route\"", StringComparison.Ordinal);
        var selectedIndex = svg.IndexOf("data-edge-id=\"selected-healthy\"", StringComparison.Ordinal);

        Assert(dependencyIndex >= 0 && criticalIndex >= 0 && selectedIndex >= 0, "Priority render test should include all routes.");
        Assert(dependencyIndex < criticalIndex, "Subtle dependency context should render below critical routes.");
        Assert(criticalIndex < selectedIndex, "Selected routes should render above critical routes even when declared first.");
        Assert(svg.Contains("data-edge-render-order=\"2\"", StringComparison.Ordinal), "SVG should expose route render order metadata for host validation.");
        var criticalLabelIndex = svg.IndexOf("data-edge-id=\"critical-route\"", selectedIndex + 1, StringComparison.Ordinal);
        var selectedLabelIndex = svg.LastIndexOf("data-edge-id=\"selected-healthy\"", StringComparison.Ordinal);
        Assert(criticalLabelIndex >= 0 && selectedLabelIndex >= 0 && criticalLabelIndex < selectedLabelIndex, "Edge labels should render in the same priority order as routes.");
        Assert(svg.Contains("data-edge-label-render-order=\"2\"", StringComparison.Ordinal), "SVG should expose edge-label render order metadata for host validation.");
        Assert(chart.ToPng(options).Length > 64, "Priority-ordered edges should render as PNG.");
    }

    private static void TopologyEdgeLabelsPlaceByVisualPriority() {
        var chart = TopologyChart.Create()
            .WithId("edge-label-placement-priority")
            .WithViewport(420, 180, 20)
            .WithLegend(null)
            .AddNode("a", "A", 60, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 52, height: 42, symbol: "H")
            .AddNode("b", "B", 310, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 52, height: 42, symbol: "H")
            .AddEdge("context", "a", "b", "context", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, VisualLinkDirection.None, TopologyEdgeRouting.Straight)
            .AddEdge("critical", "a", "b", "critical", TopologyEdgeKind.Connectivity, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .WithEdgesOfKind(TopologyEdgeKind.Dependency, emphasis: TopologyEdgeEmphasis.Subtle);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = true }
            .WithMonitoringDashboardStyle();
        options.IncludeEdgeLabelBackplates = true;
        var svg = chart.ToSvg(options);
        var criticalY = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"critical\"", "data-label-y");
        var contextY = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"context\"", "data-label-y");

        Assert(Math.Abs(criticalY - contextY) > 20, "Priority-placed labels should keep critical and subtle context labels on separate readable lanes.");
        Assert(svg.IndexOf("data-edge-id=\"context\"", StringComparison.Ordinal) < svg.LastIndexOf("data-edge-id=\"critical\"", StringComparison.Ordinal), "Critical edge labels should render after subtle context labels.");
        Assert(chart.ToPng(options).Length > 64, "Priority-placed edge labels should render as PNG.");
    }

    private static void TopologyEdgeLabelsSupportReusableOffsets() {
        var chart = TopologyChart.Create()
            .WithId("label-offsets")
            .WithViewport(360, 200, 20)
            .WithLegend(null)
            .AddNode("source", "Source", 56, 80, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 62, height: 46, symbol: "H")
            .AddNode("target", "Target", 242, 80, TopologyNodeKind.Branch, TopologyHealthStatus.Warning, width: 62, height: 46, symbol: "S")
            .AddEdge("link", "source", "target", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "MPLS")
            .WithEdgeLabelOffset("link", 24, -18);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-label-offset-x=\"24\"", StringComparison.Ordinal), "Edge label offsets should be emitted for host validation.");
        Assert(svg.Contains("data-label-offset-y=\"-18\"", StringComparison.Ordinal), "Edge label offsets should support vertical placement control.");
        Assert(svg.Contains(">82 ms<", StringComparison.Ordinal), "Offset edge labels should still render their primary text.");
        Assert(chart.ToPng(options).Length > 64, "Offset edge labels should render as PNG.");
    }

    private static void TopologyEdgeLabelsAvoidGroupHeaders() {
        var chart = TopologyChart.Create()
            .WithId("label-group-headers")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddGroup("site", "HQ-NYC", 50, 48, 250, 140, TopologyHealthStatus.Healthy, "32 DCs")
            .AddNode("dc1", "DC1", 82, 72, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "site", width: 46, height: 40, symbol: "DC")
            .AddNode("dc2", "DC2", 220, 72, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "site", width: 46, height: 40, symbol: "DC")
            .AddEdge("rep", "dc1", "dc2", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "Q:312", tertiaryLabel: "7m ago");

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }.WithMonitoringDashboardStyle());
        var y = GetAttribute(svg, "data-edge-id=\"rep\"", "data-label-y");
        Assert(y > 124, "Edge labels should avoid grouped-card headers so site titles remain readable.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Header-aware edge labels should render as PNG.");
    }

    private static void TopologyMultilineEdgeLabelsAvoidOwnRoute() {
        var chart = TopologyChart.Create()
            .WithId("label-own-route")
            .WithViewport(420, 180, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 42, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("right", "Right", 320, 70, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("rep", "left", "right", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "Q:312", tertiaryLabel: "7m ago");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        var y = GetAttribute(svg, "data-edge-id=\"rep\"", "data-label-y");

        Assert(Math.Abs(y - 92) > 30, "Monitoring multi-line edge labels should reserve enough clearance from their own route instead of sitting directly on the line.");
        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-clearance\"", StringComparison.Ordinal), "No-plate stacked monitoring labels should still mask routes behind the text block.");
        Assert(chart.ToPng(options).Length > 64, "Own-route-aware multi-line labels should render as PNG.");
    }

    private static void TopologyTwoLineEdgeLabelsReserveRouteClearance() {
        var chart = TopologyChart.Create()
            .WithId("label-two-line-route")
            .WithViewport(420, 180, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 42, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "H")
            .AddNode("right", "Right", 320, 70, TopologyNodeKind.Hub, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "H")
            .AddEdge("wan", "left", "right", "142 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "MPLS");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        var y = GetAttribute(svg, "data-edge-id=\"wan\"", "data-label-y");

        Assert(Math.Abs(y - 92) > 30, "Two-line monitoring labels should not allow the route to pass between the primary and secondary text.");
        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-clearance\"", StringComparison.Ordinal), "Two-line no-plate monitoring labels should reserve a subtle route clearance mask.");
        Assert(chart.ToPng(options).Length > 64, "Two-line route-clearance labels should render as PNG.");
    }

    private static void TopologyEdgeLabelClearanceUsesGroupSurface() {
        var chart = TopologyChart.Create()
            .WithId("label-clearance-surface")
            .WithViewport(420, 220, 20)
            .WithLegend(null)
            .AddGroup("region", "Region", 50, 44, 320, 130, TopologyHealthStatus.Healthy, "2 sites", symbol: "region")
            .AddNode("left", "Left", 92, 92, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "region", width: 44, height: 44, symbol: "H")
            .AddNode("right", "Right", 278, 92, TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "region", width: 44, height: 44, symbol: "H")
            .AddEdge("wan", "left", "right", "142 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "MPLS");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-clearance\"", StringComparison.Ordinal), "Grouped stacked labels should still render route clearance.");
        Assert(svg.Contains("data-clearance-surface=\"group\"", StringComparison.Ordinal), "Route clearance masks inside a group should use the group surface instead of a white page patch.");
        Assert(svg.Contains("data-clearance-group-id=\"region\"", StringComparison.Ordinal), "Route clearance masks should expose the matched group for host validation.");
        Assert(chart.ToPng(options).Length > 64, "Group-surface route clearance should render as PNG.");
    }

    private static void TopologyMonitoringEdgeLabelBackplatesAreVisible() {
        var chart = TopologyChart.Create()
            .WithId("label-plates")
            .WithViewport(420, 180, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 42, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("right", "Right", 320, 70, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("rep", "left", "right", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "Q:312", tertiaryLabel: "7m ago");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = true }
            .WithMonitoringDashboardStyle();
        options.IncludeEdgeLabelBackplates = true;
        var svg = chart.ToSvg(options);

        var plate = System.Xml.Linq.XDocument.Parse(svg).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-label-surface");
        Assert(plate.RenderedColor("stroke").A == (byte)Math.Round(255 * TopologyRenderPrimitives.EdgeLabelBackplateMonitoringStrokeOpacity),
            "Monitoring edge label backplates should retain their visible lightweight border opacity.");
        Assert(chart.ToPng(options).Length > 64, "Visible monitoring edge label backplates should render as PNG.");
    }

    private static void TopologyMonitoringEdgeLabelsUseTextHalos() {
        var chart = TopologyChart.Create()
            .WithId("label-halos")
            .WithViewport(360, 180, 20)
            .WithLegend(null)
            .AddNode("amer", "AMER Hub", 54, 72, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 66, height: 48, symbol: "H")
            .AddNode("emea", "EMEA Hub", 240, 72, TopologyNodeKind.Hub, TopologyHealthStatus.Warning, width: 66, height: 48, symbol: "H")
            .AddEdge("wan", "amer", "emea", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight, "MPLS");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabelBackplates = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-text\"", StringComparison.Ordinal), "Monitoring edge labels should expose reusable text roles.");
        var labelText = System.Xml.Linq.XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "text" && element.AncestorsAndSelf().Any(owner => (string?)owner.Attribute("data-cfx-role") == "topology-edge-label-text")).ToArray();
        Assert(labelText.Length > 0 && labelText.All(element => element.Attribute("stroke") != null && (double?)element.Attribute("stroke-width") > 0), "Monitoring labels without backplates should render a halo for readability over links.");
        Assert(svg.Contains("paint-order=\"stroke\"", StringComparison.Ordinal), "SVG label halos should use stroke paint order instead of opaque cards.");
        Assert(chart.ToPng(options).Length > 64, "Monitoring halo edge labels should render as PNG.");
    }

    private static void TopologyMonitoringRoutesUseHalosForCrossingPaths() {
        var chart = TopologyChart.Create()
            .WithId("route-halos")
            .WithViewport(360, 220, 20)
            .WithLegend(null)
            .AddNode("a", "A", 70, 60, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 246, 60, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddNode("c", "C", 70, 150, TopologyNodeKind.Server, TopologyHealthStatus.Critical, width: 44, height: 44, symbol: "DC")
            .AddNode("d", "D", 246, 150, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddEdge("a-d", "a", "d", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("c-b", "c", "b", "238 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-edge-route-halo\"", StringComparison.Ordinal), "Monitoring topology routes should render route halos so crossing paths remain separated.");
        Assert(svg.Contains("data-cfx-role=\"topology-edge-line-ambient-halo\"", StringComparison.Ordinal) && svg.Contains("data-cfx-role=\"topology-edge-line-highlight\"", StringComparison.Ordinal), "Monitoring topology routes should render shared premium edge layers in addition to clearance halos.");
        Assert(chart.ToPng(options).Length > 64, "Monitoring route halos should render as PNG.");
    }

    private static void TopologyMonitoringEdgeLabelsReserveReadableGaps() {
        var chart = TopologyChart.Create()
            .WithId("label-gaps")
            .WithViewport(360, 240, 20)
            .WithLegend(null)
            .AddNode("a", "A", 64, 62, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 40, height: 40, symbol: "DC")
            .AddNode("b", "B", 256, 150, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 40, height: 40, symbol: "DC")
            .AddNode("c", "C", 64, 150, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 40, height: 40, symbol: "DC")
            .AddNode("d", "D", 256, 62, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 40, height: 40, symbol: "DC")
            .AddEdge("first", "a", "b", "105 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("second", "c", "d", "107 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight);

        var options = new TopologyRenderOptions { IncludeLegend = false }
            .WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);

        var firstX = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"first\"", "data-label-x");
        var firstY = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"first\"", "data-label-y");
        var secondX = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"second\"", "data-label-x");
        var secondY = GetAttribute(svg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"second\"", "data-label-y");
        var distance = Math.Sqrt(Math.Pow(firstX - secondX, 2) + Math.Pow(firstY - secondY, 2));
        Assert(distance >= 28, "Monitoring edge labels with the same natural midpoint should reserve a readable gap instead of stacking on top of each other.");
        Assert(chart.ToPng(options).Length > 64, "Monitoring edge label gap placement should render as PNG.");
    }

}
