using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyIconLabelFittingUsesRenderedPlateWidth() {
        var chart = TopologyChart.Create()
            .WithId("icon-label-fit")
            .WithViewport(160, 110, 12)
            .WithLegend(null)
            .AddNode("edge", "OK", 0, 28, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 24, height: 24, symbol: "S")
            .WithNodeDisplay("edge", TopologyNodeDisplayMode.Icon);

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false, IncludeIconLabels = true });
        var plateX = GetAttribute(svg, "data-cfx-role=\"topology-node-icon-label\"", "x");

        Assert(plateX >= 0, "Viewport fitting should reserve the rendered minimum icon-label plate width.");
    }

    private static void TopologyIconLabelsStackBelowIconBadges() {
        var chart = TopologyChart.Create()
            .WithId("icon-label-badge")
            .WithViewport(220, 150, 12)
            .WithLegend(null)
            .AddNode("edge", "Payments Team", 72, 36, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 48, height: 42, symbol: "TM")
            .WithNodeDisplay("edge", TopologyNodeDisplayMode.Icon)
            .WithNodeBadge("edge", "OK");

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeIconLabels = true };
        var svg = chart.ToSvg(options);
        var badgeY = GetAttribute(svg, "data-cfx-role=\"topology-node-badge\"", "y");
        var plateY = GetAttribute(svg, "data-cfx-role=\"topology-node-icon-label\"", "y");

        Assert(plateY >= badgeY + 19, "Icon label plates should stack below icon badges instead of sharing the badge slot.");
        Assert(chart.ToPng(options).Length > 64, "Stacked icon labels and badges should render as PNG.");
    }

    private static void TopologyRoundedRoutesTolerateDuplicateWaypoints() {
        var chart = TopologyChart.Create()
            .WithId("duplicate-waypoint-routes")
            .WithViewport(560, 300, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 40, 40, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("right", "Right", 360, 40, TopologyNodeKind.Database, TopologyHealthStatus.Warning)
            .AddEdge("left-right", "left", "right", "via route", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgeWaypoints("left-right", new ChartPoint(200, 180), new ChartPoint(200, 180), new ChartPoint(300, 180));

        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle());

        Assert(!svg.Contains("NaN", StringComparison.Ordinal) && !svg.Contains("Infinity", StringComparison.Ordinal), "Rounded topology routes should tolerate duplicate consecutive waypoints.");
    }

    private static void TopologyEdgeLabelsTolerateDuplicateEdgeIds() {
        var chart = TopologyChart.Create()
            .WithId("duplicate-edge-labels")
            .WithViewport(360, 180, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 40, 72, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 48, height: 42)
            .AddNode("right", "Right", 260, 72, TopologyNodeKind.Database, TopologyHealthStatus.Warning, width: 48, height: 42)
            .AddEdge("dup", "left", "right", "primary", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .AddEdge("dup", "right", "left", "backup", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var options = new TopologyRenderOptions { IncludeLegend = false };
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-edge-id=\"dup\"", StringComparison.Ordinal), "Duplicate edge ids should not crash edge-label ordering.");
        Assert(chart.ToPng(options).Length > 64, "Duplicate edge ids should not crash PNG edge-label ordering.");
    }

    private static void TopologyDuplicatePortPeersUseEdgeInstanceIdentity() {
        var chart = TopologyChart.Create()
            .WithId("duplicate-port-peers")
            .WithViewport(340, 220, 20)
            .WithLegend(null)
            .AddNode("hub", "Hub", 70, 82, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 52, height: 52)
            .AddNode("a", "A", 230, 48, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 42, height: 38)
            .AddNode("b", "B", 230, 122, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 42, height: 38)
            .AddEdge("dup", "hub", "a", "A", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("dup", "hub", "b", "B", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal);
        foreach (var edge in chart.Edges) {
            edge.SourcePort = TopologyEdgePort.Right;
            edge.TargetPort = TopologyEdgePort.Left;
        }

        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var first = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges[0], nodes);
        var second = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges[1], nodes);

        Assert(Math.Abs(first[0].Y - second[0].Y) > 0.01, "Duplicate edge ids sharing one explicit port should still receive distinct fan slots.");
    }

    private static void TopologyOrthogonalDuplicateEdgesUseInferredRouteLanes() {
        var chart = TopologyChart.Create()
            .WithId("orthogonal-duplicates")
            .WithViewport(420, 260, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 70, 64, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 52, height: 42)
            .AddNode("right", "Right", 286, 160, TopologyNodeKind.Database, TopologyHealthStatus.Warning, width: 52, height: 42)
            .AddEdge("primary", "left", "right", "primary", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("backup", "left", "right", "backup", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal);

        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var primary = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges[0], nodes);
        var backup = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges[1], nodes);

        Assert(Math.Abs(primary[1].X - backup[1].X) > 0.01, "Default duplicate orthogonal edges should infer distinct route lanes instead of collapsing.");
    }

    private static void TopologyExplicitZeroRouteLaneStaysCentered() {
        var chart = TopologyChart.Create()
            .WithId("explicit-zero-route-lane")
            .WithViewport(420, 260, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 70, 64, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 52, height: 42)
            .AddNode("right", "Right", 286, 160, TopologyNodeKind.Database, TopologyHealthStatus.Warning, width: 52, height: 42)
            .AddEdge("center", "left", "right", "center", TopologyEdgeKind.Dependency, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("backup", "left", "right", "backup", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgeRouteLane("center", 0);

        Assert(Math.Abs(TopologyRenderPrimitives.EdgeRouteLane(chart, chart.Edges[0])) < 0.0001, "Explicit zero route lanes should stay centered instead of being replaced by inferred parallel offsets.");
        Assert(Math.Abs(TopologyRenderPrimitives.EdgeRouteLane(chart, chart.Edges[1])) > 0.0001, "Unset duplicate route lanes should still infer a parallel offset.");
    }

    private static void TopologyEdgeLabelObstaclesFollowRenderedGroupOptions() {
        var headerChart = TopologyChart.Create()
            .WithId("headerless-label")
            .WithViewport(420, 260, 20)
            .WithLegend(null)
            .AddGroup("middle", "Hidden Header", 150, 118, 100, 80, TopologyHealthStatus.Warning)
            .AddNode("left", "Left", 40, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 40, height: 40)
            .AddNode("right", "Right", 320, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 40, height: 40)
            .AddEdge("link", "left", "right", "64 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var headerless = TopologyRenderPrimitives.EdgeLabelLayouts(headerChart, new TopologyRenderOptions { IncludeLegend = false, IncludeGroupLabels = false }).Single();
        Assert(Math.Abs(headerless.CenterY - 150) < 0.01, "Hidden group headers should not reserve phantom edge-label obstacles.");
        var hiddenGroups = TopologyRenderPrimitives.EdgeLabelLayouts(headerChart, new TopologyRenderOptions { IncludeLegend = false, IncludeGroups = false, IncludeGroupLabels = true }).Single();
        Assert(Math.Abs(hiddenGroups.CenterY - 150) < 0.01, "Hidden group cards should not reserve phantom group-header obstacles.");

        var groupChart = TopologyChart.Create()
            .WithId("groupless-label")
            .WithViewport(420, 260, 20)
            .WithLegend(null)
            .AddGroup("left-group", "Left Group", 95, 110, 90, 90, TopologyHealthStatus.Healthy)
            .AddGroup("right-group", "Right Group", 205, 110, 90, 90, TopologyHealthStatus.Warning)
            .AddNode("left", "Left", 40, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "left-group", width: 40, height: 40)
            .AddNode("right", "Right", 320, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "right-group", width: 40, height: 40)
            .AddEdge("link", "left", "right", "64 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight);

        var groupless = TopologyRenderPrimitives.EdgeLabelLayouts(groupChart, new TopologyRenderOptions { IncludeLegend = false, IncludeGroups = false, IncludeGroupLabels = false }).Single();
        Assert(Math.Abs(groupless.CenterY - 150) < 0.01, "Hidden group surfaces should not reserve phantom edge-label obstacles.");
    }

    private static void TopologyEdgeLabelObstaclesReserveIconLabelPlates() {
        var chart = TopologyChart.Create()
            .WithId("icon-label-obstacle")
            .WithViewport(420, 260, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 40, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 40, height: 40)
            .AddNode("right", "Right", 320, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 40, height: 40)
            .AddNode("owner", "Payments Platform Owner", 176, 88, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 48, height: 42)
            .AddEdge("link", "left", "right", "64 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .WithNodeDisplay("owner", TopologyNodeDisplayMode.Icon);

        var hiddenIconLabel = TopologyRenderPrimitives.EdgeLabelLayouts(chart, new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = false, IncludeIconLabels = true }).Single();
        var visibleIconLabel = TopologyRenderPrimitives.EdgeLabelLayouts(chart, new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = true, IncludeIconLabels = true }).Single();

        Assert(Math.Abs(hiddenIconLabel.CenterY - 150) < 0.01, "Hidden icon labels should not reserve edge-label obstacles.");
        Assert(Math.Abs(visibleIconLabel.CenterY - 150) > 0.01, "Rendered icon-label plates should reserve edge-label obstacles.");
    }

    private static void TopologyHiddenNodesDoNotAffectViewportFitOrEdgeLabels() {
        var viewportChart = TopologyChart.Create()
            .WithId("hidden-fit")
            .WithViewport(220, 160, 18)
            .WithLegend(null)
            .AddNode("visible", "Visible", 34, 58, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 52, height: 40)
            .AddNode("anchor", "Anchor", 1200, 740, TopologyNodeKind.Location, TopologyHealthStatus.Critical, width: 60, height: 44)
            .WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);

        var fitSvg = viewportChart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(GetAttribute(fitSvg, string.Empty, "width") < 400, "Hidden anchor nodes should not expand normalized viewport bounds.");

        var labelChart = TopologyChart.Create()
            .WithId("hidden-label-obstacle")
            .WithViewport(420, 260, 20)
            .WithLegend(null)
            .AddNode("left", "Left", 40, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 40, height: 40)
            .AddNode("right", "Right", 320, 130, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 40, height: 40)
            .AddNode("anchor", "Anchor", 176, 139, TopologyNodeKind.Location, TopologyHealthStatus.Critical, width: 48, height: 22)
            .AddEdge("link", "left", "right", "64 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight)
            .WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);

        var label = TopologyRenderPrimitives.EdgeLabelLayouts(labelChart, new TopologyRenderOptions { IncludeLegend = false }).Single();
        Assert(Math.Abs(label.CenterY - 150) < 0.01, "Hidden anchor nodes should not reserve phantom edge-label obstacles.");
    }

    private static void TopologyEdgeLabelClearanceUsesBackgroundWhenGroupsAreHidden() {
        var theme = TopologyTheme.Light();
        var group = new TopologyGroup { Id = "g", Label = "Group", X = 40, Y = 40, Width = 160, Height = 120, Status = TopologyHealthStatus.Warning };
        var fill = TopologyRenderPrimitives.EdgeLabelClearanceFill(group, theme, new TopologyRenderOptions { IncludeGroups = false }.WithMonitoringDashboardStyle());

        Assert(string.Equals(fill, theme.Background, StringComparison.Ordinal), "Monitoring edge-label clearance should use the actual background when group cards are hidden.");
    }

    private static void TopologyGeographicCalloutsAvoidUnlabeledLinkRoutes() {
        var link = new TopologyEdge { Kind = TopologyEdgeKind.Link };
        var dependency = new TopologyEdge { Kind = TopologyEdgeKind.Dependency };

        Assert(TopologyRenderPrimitives.ShouldReserveGeographicCalloutRouteObstacle(link), "Visible unlabeled geographic site links should reserve callout route obstacles.");
        Assert(!TopologyRenderPrimitives.ShouldReserveGeographicCalloutRouteObstacle(dependency), "Unlabeled non-route edges should not add geographic callout obstacles.");

        var routeSamples = TopologyGeographicCallouts.SampleRouteObstaclePoints(new[] { new ChartPoint(0, 0), new ChartPoint(126, 0) });
        Assert(routeSamples.Count >= 4 && routeSamples.Any(point => point.X > 40 && point.X < 90), "Straight route obstacles should sample interior route geometry instead of only endpoints.");
    }

    private static void TopologyExplicitCalloutPlacementAddsMapEdgeCandidates() {
        var chart = TopologyChart.Create()
            .WithId("explicit-callout-placement")
            .WithViewport(640, 340, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("apac", "APAC", 0, 0, 0, 0, TopologyHealthStatus.Warning, "1 site", symbol: "region")
            .AddNode("sin", "Singapore", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Warning, "apac", width: 48, height: 36)
            .WithGroupCoordinates("apac", 103.8198, 1.3521)
            .WithNodeCoordinates("sin", 103.8198, 1.3521);
        chart.Groups[0].Metadata["calloutPlacement"] = "left-corner";

        var callout = TopologyGeographicCallouts.Build(chart, new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeGroups = false,
            IncludeGeographicCallouts = true,
            PreferGeographicCalloutMapEdges = false
        }.WithMonitoringDashboardStyle(), TopologyTheme.Light()).Single();

        Assert(string.Equals(callout.Placement, "left-corner", StringComparison.Ordinal), "Explicit calloutPlacement metadata should be honored even when global map-edge preference is disabled.");
    }

    private static void TopologySingleGeographicCalloutUsesNearestMapCorner() {
        var chart = TopologyChart.Create()
            .WithId("single-callout-corner")
            .WithViewport(640, 340, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("apac", "APAC", 0, 0, 0, 0, TopologyHealthStatus.Warning, "1 site", symbol: "region")
            .AddNode("sin", "Singapore", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Warning, "apac", width: 48, height: 36)
            .WithGroupCoordinates("apac", 103.8198, 1.3521)
            .WithNodeCoordinates("sin", 103.8198, 1.3521);

        var callout = TopologyGeographicCallouts.Build(chart, new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeGroups = false,
            IncludeGeographicCallouts = true,
            PreferGeographicCalloutMapEdges = true
        }.WithMonitoringDashboardStyle(), TopologyTheme.Light()).Single();

        Assert(string.Equals(callout.Placement, "right-corner", StringComparison.Ordinal), "A single eastern callout should prefer the right map corner instead of defaulting to the left.");
    }

    private static void TopologyPngHalosUseHighlightAlpha() {
        var chart = TopologyChart.Create()
            .WithId("highlight-alpha")
            .WithViewport(300, 180, 20)
            .WithLegend(null)
            .AddGroup("g", "Group", 40, 40, 160, 100, TopologyHealthStatus.Healthy)
            .AddNode("a", "A", 60, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "g", width: 42, height: 36)
            .AddNode("b", "B", 200, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "g", width: 42, height: 36)
            .AddEdge("a-b", "a", "b", null, TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Straight);
        var activeHighlight = TopologyHighlightState.From(chart, new TopologyRenderOptions { HighlightStatuses = { TopologyHealthStatus.Critical }, DimmedOpacity = 0.25 });
        var inactiveHighlight = TopologyHighlightState.From(chart, new TopologyRenderOptions { DimmedOpacity = 0.25 });

        Assert(TopologyRenderPrimitives.HighlightAlpha(224, false, activeHighlight) == 56, "PNG route halos should use the same dimmed alpha factor as highlighted edge strokes.");
        Assert(TopologyRenderPrimitives.HighlightAlpha(194, false, activeHighlight) == 48, "PNG callout leader halos should dim with the same highlight state as callout leaders.");
        Assert(TopologyRenderPrimitives.HighlightAlpha(224, true, activeHighlight) == 224, "Highlighted halos should keep their full base alpha.");
        Assert(TopologyRenderPrimitives.HighlightAlpha(224, false, inactiveHighlight) == 224, "Inactive highlight state should not dim halos.");
    }

    private static void TopologyGeographicCalloutIconObstaclesUseRenderedLabelWidth() {
        var node = new TopologyNode {
            Id = "owner",
            Label = "Payments Platform Ownership Team",
            X = 80,
            Y = 60,
            Width = 42,
            Height = 36,
            Kind = TopologyNodeKind.Service,
            DisplayMode = TopologyNodeDisplayMode.Icon
        };
        var options = new TopologyRenderOptions { IncludeIconLabels = true };
        var obstacle = TopologyGeographicCallouts.CalloutBox.FromNodeVisual(node, options);
        var expectedWidth = TopologyRenderPrimitives.IconLabelPlateWidth(node) + 16;

        Assert(obstacle.Width >= expectedWidth, "Geographic callout icon obstacles should reserve the rendered icon-label plate width.");
    }

    private static void TopologyGeographicCalloutIconObstaclesFollowRenderedLabels() {
        var node = new TopologyNode {
            Id = "owner",
            Label = "Payments Platform Ownership Team",
            X = 80,
            Y = 60,
            Width = 42,
            Height = 36,
            Kind = TopologyNodeKind.Service,
            DisplayMode = TopologyNodeDisplayMode.Icon
        };
        var hiddenLabelObstacle = TopologyGeographicCallouts.CalloutBox.FromNodeVisual(node, new TopologyRenderOptions { IncludeNodeLabels = false, IncludeIconLabels = true });
        var visibleLabelObstacle = TopologyGeographicCallouts.CalloutBox.FromNodeVisual(node, new TopologyRenderOptions { IncludeNodeLabels = true, IncludeIconLabels = true });

        Assert(visibleLabelObstacle.Width > hiddenLabelObstacle.Width, "Geographic callout icon obstacles should only reserve icon-label plates when node labels are rendered.");
    }

    private static void TopologyReciprocalBundleMarksAssignedLanesExplicit() {
        var chart = TopologyChart.Create()
            .WithId("reciprocal-center-lane")
            .WithViewport(340, 220, 20)
            .WithLegend(null)
            .AddNode("a", "A", 82, 86, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, width: 44, height: 44, symbol: "DC")
            .AddNode("b", "B", 228, 86, TopologyNodeKind.Server, TopologyHealthStatus.Warning, width: 44, height: 44, symbol: "DC")
            .AddEdge("z-center", "a", "b", "center", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("a-left", "a", "b", "left", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .AddEdge("m-right", "b", "a", "right", TopologyEdgeKind.Replication, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithReciprocalEdgeRouteBundles(20);

        var center = chart.Edges.Single(edge => edge.Id == "z-center");

        Assert(Math.Abs(center.RouteLane) < 0.0001, "Reciprocal route bundles should assign the sorted middle edge to the center lane.");
        Assert(Math.Abs(TopologyRenderPrimitives.EdgeRouteLane(chart, center)) < 0.0001, "Reciprocal route bundle lanes should be treated as explicit so inferred duplicate offsets do not move the center lane.");
    }

    private static void TopologyGeographicRegionHullsIgnoreHiddenNodes() {
        var chart = TopologyChart.Create()
            .WithId("hidden-hull")
            .WithViewport(640, 340, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Geographic)
            .WithMapViewport(ChartMapViewport.World())
            .AddGroup("amer", "AMER", 0, 0, 0, 0, TopologyHealthStatus.Healthy, "1 site", symbol: "region")
            .AddNode("visible", "Visible", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Healthy, "amer", width: 48, height: 36)
            .AddNode("anchor", "Anchor", 0, 0, TopologyNodeKind.Location, TopologyHealthStatus.Critical, "amer", width: 24, height: 24)
            .WithGroupCoordinates("amer", -98.5795, 39.8283)
            .WithNodeCoordinates("visible", -98.5795, 39.8283)
            .WithNodeCoordinates("anchor", -10, 52)
            .WithNodeDisplay("anchor", TopologyNodeDisplayMode.Hidden);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeGroups = false, IncludeGeographicRegionHulls = true };
        var svg = chart.ToSvg(options);
        var radius = GetAttribute(svg, "data-cfx-role=\"topology-geographic-region-hulls\"", "r");

        Assert(Math.Abs(radius - options.GeographicRegionHullMinRadius) < 0.01, "Hidden geographic anchor nodes should not enlarge rendered region hulls.");
        Assert(chart.ToPng(options).Length > 64, "Hidden geographic anchor nodes should not break PNG region hull rendering.");
    }
}
