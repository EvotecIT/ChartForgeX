using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyDenseGroupedLayoutHandlesAdReplicationSubnetFixtures() {
        var chart = TopologyChart.Create()
            .WithId("ad-dense-fixture")
            .WithViewport(1040, 540, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("amer", "AMER", TopologyHealthStatus.Healthy, "47 sites", symbol: "region", color: "#16A34A")
            .AddAutoGroup("emea", "EMEA", TopologyHealthStatus.Warning, "56 sites", symbol: "region", color: "#2563EB")
            .AddAutoGroup("apac", "APAC", TopologyHealthStatus.Critical, "39 sites", symbol: "region", color: "#8B5CF6")
            .AddAutoNode("amer-hub", "AMER Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "amer", "10.0.0.0/16", width: 72, height: 46, symbol: "H")
            .AddAutoNode("amer-west", "NAM West", TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "amer", "10.1.0.0/24", width: 72, height: 46, symbol: "S")
            .AddAutoNode("amer-east", "NAM East", TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "amer", "10.2.0.0/24", width: 72, height: 46, symbol: "S")
            .AddAutoNode("amer-subnet", "NY Subnet", TopologyNodeKind.Network, TopologyHealthStatus.Healthy, "amer", "10.2.1.0/24", width: 72, height: 46, symbol: "NET")
            .AddAutoNode("emea-hub", "EMEA Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "emea", "10.10.0.0/16", width: 72, height: 46, symbol: "H")
            .AddAutoNode("emea-west", "EU West", TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "emea", "10.10.1.0/24", width: 72, height: 46, symbol: "S")
            .AddAutoNode("emea-east", "EU East", TopologyNodeKind.Branch, TopologyHealthStatus.Warning, "emea", "10.10.3.0/24", width: 72, height: 46, symbol: "S")
            .AddAutoNode("emea-subnet", "TR Subnet", TopologyNodeKind.Network, TopologyHealthStatus.Warning, "emea", "10.10.31.0/24", width: 72, height: 46, symbol: "NET")
            .AddAutoNode("apac-hub", "APAC Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "apac", "10.20.0.0/16", width: 72, height: 46, symbol: "H")
            .AddAutoNode("apac-anz", "ANZ", TopologyNodeKind.Branch, TopologyHealthStatus.Critical, "apac", "10.20.2.0/24", width: 72, height: 46, symbol: "S")
            .AddAutoNode("apac-in", "IN India", TopologyNodeKind.Branch, TopologyHealthStatus.Healthy, "apac", "10.20.3.0/24", width: 72, height: 46, symbol: "S")
            .AddAutoNode("apac-subnet", "SYD Subnet", TopologyNodeKind.Network, TopologyHealthStatus.Critical, "apac", "10.20.2.0/24", width: 72, height: 46, symbol: "NET")
            .AddEdge("amer-emea-site-link", "amer-hub", "emea-hub", "24 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "MPLS")
            .AddEdge("emea-apac-site-link", "emea-hub", "apac-hub", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "MPLS")
            .AddEdge("amer-branch-replication", "amer-east", "emea-west", "32 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "Q:128", tertiaryLabel: "2m ago")
            .AddEdge("apac-critical-replication", "apac-hub", "apac-anz", "142 ms", TopologyEdgeKind.Replication, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal, "Q:915", tertiaryLabel: "Just now")
            .AddEdge("subnet-overlap", "emea-subnet", "apac-subnet", "Overlap", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "10.20.2.0/24")
            .WithGroupLayout("amer", TopologyGroupLayoutPolicy.Grid)
            .WithGroupLayout("emea", TopologyGroupLayoutPolicy.Grid)
            .WithGroupLayout("apac", TopologyGroupLayoutPolicy.Grid);

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeTileSubtitles = true,
            IncludeEdgeLabelBackplates = false,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile
        }.WithMonitoringDashboardStyle();
        var svg = chart.ToSvg(options);
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var labelLayouts = TopologyRenderPrimitives.EdgeLabelLayouts(prepared, options);

        Assert(svg.Contains("data-node-id=\"apac-subnet\"", StringComparison.Ordinal), "Dense AD fixtures should include subnet nodes, not only site nodes.");
        Assert(svg.Contains("data-edge-id=\"amer-branch-replication\"", StringComparison.Ordinal), "Dense AD fixtures should include replication routes across groups.");
        Assert(svg.Contains("data-edge-id=\"emea-apac-site-link\"", StringComparison.Ordinal), "Dense AD fixtures should include inter-region site-link routes.");
        Assert(svg.Contains("data-edge-layout-inference=\"source-port target-port", StringComparison.Ordinal), "Inter-group AD fixtures should infer outside-facing route ports.");
        Assert(!LabelLayoutsOverlap(labelLayouts), "Replication, site-link, and subnet labels should be placed on readable lanes in dense monitoring layouts.");
        Assert(chart.ToPng(options).Length > 64, "Dense AD topology fixtures should render as PNG.");
    }

    private static void TopologyForceDirectedLayoutSeparatesDenseMembershipClusters() {
        var chart = TopologyChart.Create()
            .WithId("force-users")
            .WithViewport(1040, 640, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.ForceDirected)
            .AddAutoGroup("tenant", "Tenant", TopologyHealthStatus.Healthy, "3 populations", symbol: "T")
            .AddAutoGroup("segments", "User Segments", TopologyHealthStatus.Warning, "sampled cohorts", symbol: "SEG")
            .AddAutoNode("directory", "Directory", TopologyNodeKind.Namespace, TopologyHealthStatus.Healthy, "tenant", "5000 users", width: 104, height: 56, symbol: "AD");

        for (var i = 0; i < 18; i++) {
            var id = "segment-" + i.ToString("00", CultureInfo.InvariantCulture);
            chart.AddAutoNode(id, "Segment " + (i + 1).ToString(CultureInfo.InvariantCulture), TopologyNodeKind.Team, i % 7 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy, "segments", (100 + i * 270).ToString(CultureInfo.InvariantCulture) + " users", width: 86, height: 48, symbol: "G");
            chart.AddEdge("membership-" + i.ToString("00", CultureInfo.InvariantCulture), "directory", id, (100 + i * 270).ToString(CultureInfo.InvariantCulture), TopologyEdgeKind.Membership, TopologyHealthStatus.Healthy, TopologyDirection.Forward);
            if (i > 0 && i % 4 == 0) {
                chart.AddEdge("cross-segment-" + i.ToString("00", CultureInfo.InvariantCulture), id, "segment-" + (i - 3).ToString("00", CultureInfo.InvariantCulture), "shared", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional);
            }
        }

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile }
            .WithMonitoringDashboardStyle()
            .WithFitContentToViewport();
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var segmentNodes = prepared.Nodes.Where(node => node.Id.StartsWith("segment-", StringComparison.Ordinal)).ToList();
        var tenant = prepared.Groups.Single(group => group.Id == "tenant");
        var segments = prepared.Groups.Single(group => group.Id == "segments");

        Assert(prepared.LayoutMode == TopologyLayoutMode.ForceDirected, "Force-directed topology should preserve the requested layout mode.");
        Assert(segmentNodes.Count == 18, "Force-directed user overview should keep sampled cohort nodes visible.");
        Assert(segmentNodes.Select(node => Math.Round(node.X, 1)).Distinct().Count() > 8, "Force-directed layout should spread dense cohorts horizontally.");
        Assert(segmentNodes.Select(node => Math.Round(node.Y, 1)).Distinct().Count() > 6, "Force-directed layout should spread dense cohorts vertically.");
        Assert(!AnyOverlap(segmentNodes), "Force-directed layout should use node repulsion to avoid overlapping cohort tiles.");
        Assert(segments.Width > tenant.Width, "Force-directed group bounds should reflect the wider sampled cohort cluster.");
        Assert(prepared.Edges.All(edge => edge.Routing == TopologyEdgeRouting.Straight), "Force-directed layout should default relationships to straight physics-style links.");
        Assert(prepared.Edges.All(edge => edge.Metadata.TryGetValue("layout.force", out var value) && value == "true"), "Force-directed edges should expose layout diagnostics.");
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-layout-mode=\"ForceDirected\"", StringComparison.Ordinal), "SVG should expose force-directed layout mode for host adapters.");
        Assert(svg.Contains("data-cfx-meta-layout-force=\"true\"", StringComparison.Ordinal), "SVG should expose force layout edge diagnostics.");
        Assert(chart.ToPng(options).Length > 64, "Force-directed dense user overview should render as PNG.");
    }

    private static void TopologyForceDirectedLayoutAnchorsGroupHubs() {
        var chart = TopologyChart.Create()
            .WithId("force-hub-anchor")
            .WithViewport(760, 420, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.ForceDirected)
            .AddAutoGroup("population", "Population", TopologyHealthStatus.Warning, "cohorts")
            .AddAutoNode("population-root", "5000 Users", TopologyNodeKind.Team, TopologyHealthStatus.Warning, "population", "population", width: 112, height: 58, symbol: "U");

        for (var i = 0; i < 10; i++) {
            var id = "cohort-" + i.ToString("00", CultureInfo.InvariantCulture);
            chart.AddAutoNode(id, "Cohort " + i.ToString("00", CultureInfo.InvariantCulture), TopologyNodeKind.Person, TopologyHealthStatus.Healthy, "population", "500 users", width: 82, height: 46, symbol: "U")
                .AddEdge("membership-" + i.ToString("00", CultureInfo.InvariantCulture), "population-root", id, "500", TopologyEdgeKind.Membership, TopologyHealthStatus.Healthy, TopologyDirection.Forward);
        }

        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Tile }
            .WithMonitoringDashboardStyle()
            .WithFitContentToViewport();
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var group = prepared.Groups.Single(item => item.Id == "population");
        var root = prepared.Nodes.Single(node => node.Id == "population-root");
        var leaves = prepared.Nodes.Where(node => node.Id.StartsWith("cohort-", StringComparison.Ordinal)).ToList();
        var groupCenterX = group.X + group.Width / 2;
        var groupCenterY = group.Y + group.Height / 2;
        var rootDistance = Distance(root, groupCenterX, groupCenterY);
        var averageLeafDistance = leaves.Average(node => Distance(node, groupCenterX, groupCenterY));

        Assert(root.Metadata.TryGetValue("layout.force.role", out var role) && role == "hub", "Force-directed layout should mark the inferred group hub.");
        Assert(rootDistance < averageLeafDistance, "Force-directed group hubs should settle closer to the group center than their leaves.");
        Assert(!AnyOverlap(prepared.Nodes), "Force-directed hub anchoring should not reintroduce node overlaps.");
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-cfx-meta-layout-force-role=\"hub\"", StringComparison.Ordinal), "SVG should expose force hub metadata for host adapters.");
    }

    private static void TopologyForceDirectedLayoutHonorsExplicitGroupAnchors() {
        var chart = TopologyChart.Create()
            .WithId("force-explicit-anchors")
            .WithViewport(640, 360, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.ForceDirected)
            .AddGroup("left", "Left", 80, 90, 180, 120, TopologyHealthStatus.Healthy)
            .AddGroup("right", "Right", 380, 100, 180, 120, TopologyHealthStatus.Warning)
            .AddAutoNode("left-1", "Left 1", TopologyNodeKind.Team, TopologyHealthStatus.Healthy, "left")
            .AddAutoNode("left-2", "Left 2", TopologyNodeKind.Person, TopologyHealthStatus.Healthy, "left")
            .AddAutoNode("right-1", "Right 1", TopologyNodeKind.Team, TopologyHealthStatus.Warning, "right")
            .AddAutoNode("right-2", "Right 2", TopologyNodeKind.Person, TopologyHealthStatus.Warning, "right")
            .AddEdge("cross", "left-1", "right-1", null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional);

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var left = prepared.Groups.Single(group => group.Id == "left");
        var right = prepared.Groups.Single(group => group.Id == "right");

        Assert(left.Metadata["layout.force.anchor.strategy"] == "explicit", "Small force layouts should not replace explicit group anchors with weighted-row anchors.");
        Assert(right.Metadata["layout.force.anchor.strategy"] == "explicit", "Small force layouts should keep every explicit group anchor.");
        Assert(Math.Abs(double.Parse(left.Metadata["layout.force.anchor.x"], CultureInfo.InvariantCulture) - 170) < 0.001, "Explicit left group anchor should preserve the provided X and width.");
        Assert(Math.Abs(double.Parse(right.Metadata["layout.force.anchor.x"], CultureInfo.InvariantCulture) - 470) < 0.001, "Explicit right group anchor should preserve the provided X and width.");
    }

    private static void TopologyForceDirectedLayoutHandlesZeroSizeExplicitGroupAnchors() {
        var chart = TopologyChart.Create()
            .WithId("force-zero-size-anchor")
            .WithViewport(640, 360, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.ForceDirected)
            .AddGroup("left", "Left", 90, 80, 0, 0, TopologyHealthStatus.Healthy)
            .AddGroup("right", "Right", 360, 90, 0, 0, TopologyHealthStatus.Warning);

        for (var i = 0; i < 4; i++) {
            chart.AddAutoNode("left-" + i.ToString("00", CultureInfo.InvariantCulture), "Left " + i.ToString("00", CultureInfo.InvariantCulture), TopologyNodeKind.Person, TopologyHealthStatus.Healthy, "left", width: 50, height: 34)
                .AddAutoNode("right-" + i.ToString("00", CultureInfo.InvariantCulture), "Right " + i.ToString("00", CultureInfo.InvariantCulture), TopologyNodeKind.Person, TopologyHealthStatus.Warning, "right", width: 50, height: 34);
        }

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var left = prepared.Groups.Single(group => group.Id == "left");
        var leftNodes = prepared.Nodes.Where(node => node.GroupId == "left").ToList();

        Assert(double.Parse(left.Metadata["layout.force.anchor.width"], CultureInfo.InvariantCulture) > 1, "Zero-size explicit force groups should receive a usable fallback anchor width.");
        Assert(double.Parse(left.Metadata["layout.force.anchor.height"], CultureInfo.InvariantCulture) > 1, "Zero-size explicit force groups should receive a usable fallback anchor height.");
        Assert(leftNodes.Select(node => Math.Round(node.X, 1)).Distinct().Count() > 1 || leftNodes.Select(node => Math.Round(node.Y, 1)).Distinct().Count() > 1, "Zero-size explicit force anchors should not collapse their particles to one point.");
    }

    private static void TopologyForceDirectedLayoutTreatsZeroOriginGroupsAsExplicitAnchors() {
        var chart = TopologyChart.Create()
            .WithId("force-zero-origin-anchor")
            .WithViewport(640, 360, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.ForceDirected)
            .AddGroup("origin", "Origin", 0, 0, 160, 120, TopologyHealthStatus.Healthy)
            .WithGroupPosition("origin", 0, 0)
            .AddAutoGroup("auto", "Auto", TopologyHealthStatus.Warning)
            .AddAutoNode("origin-node", "Origin", TopologyNodeKind.Team, TopologyHealthStatus.Healthy, "origin")
            .AddAutoNode("auto-node", "Auto", TopologyNodeKind.Team, TopologyHealthStatus.Warning, "auto")
            .AddEdge("origin-auto", "origin-node", "auto-node", null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional);

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var origin = prepared.Groups.Single(group => group.Id == "origin");

        Assert(origin.Metadata["layout.force.anchor.strategy"] == "explicit", "Force-directed groups pinned at zero origin should still be treated as explicit anchors.");
        Assert(Math.Abs(double.Parse(origin.Metadata["layout.force.anchor.x"], CultureInfo.InvariantCulture) - 80) < 0.001, "Zero-origin force anchors should preserve the caller-provided X coordinate.");
        Assert(Math.Abs(double.Parse(origin.Metadata["layout.force.anchor.y"], CultureInfo.InvariantCulture) - 60) < 0.001, "Zero-origin force anchors should preserve the caller-provided Y coordinate.");
    }

    private static void TopologyForceDirectedLayoutKeepsSizeOnlyGroupsAutoAnchored() {
        var chart = TopologyChart.Create()
            .WithId("force-size-only-auto-anchor")
            .WithViewport(640, 360, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.ForceDirected)
            .AddGroup("left", "Left", 0, 0, 160, 120, TopologyHealthStatus.Healthy)
            .AddGroup("right", "Right", 0, 0, 160, 120, TopologyHealthStatus.Warning)
            .AddAutoNode("left-node", "Left", TopologyNodeKind.Team, TopologyHealthStatus.Healthy, "left")
            .AddAutoNode("right-node", "Right", TopologyNodeKind.Team, TopologyHealthStatus.Warning, "right")
            .AddEdge("left-right", "left-node", "right-node", null, TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional);

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var left = prepared.Groups.Single(group => group.Id == "left");
        var right = prepared.Groups.Single(group => group.Id == "right");

        Assert(left.Metadata["layout.force.anchor.strategy"] == "weighted-row", "Force-directed groups with only size overrides should still use automatic weighted anchors.");
        Assert(right.Metadata["layout.force.anchor.strategy"] == "weighted-row", "Force-directed size-only groups should not be treated as explicit origin anchors.");
        Assert(Math.Abs(double.Parse(left.Metadata["layout.force.anchor.x"], CultureInfo.InvariantCulture) - double.Parse(right.Metadata["layout.force.anchor.x"], CultureInfo.InvariantCulture)) > 50, "Force-directed size-only groups should be spread by auto anchoring.");
    }

    private static void TopologyForceDirectedLayoutKeepsWeightedAnchorsInsideNarrowViewport() {
        var chart = TopologyChart.Create()
            .WithId("force-narrow-weighted-anchors")
            .WithViewport(160, 240, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.ForceDirected)
            .AddAutoGroup("one", "One", TopologyHealthStatus.Healthy)
            .AddAutoGroup("two", "Two", TopologyHealthStatus.Warning)
            .AddAutoGroup("three", "Three", TopologyHealthStatus.Critical)
            .AddAutoNode("one-node", "One", TopologyNodeKind.Person, TopologyHealthStatus.Healthy, "one", width: 16, height: 16)
            .AddAutoNode("two-node", "Two", TopologyNodeKind.Person, TopologyHealthStatus.Warning, "two", width: 16, height: 16)
            .AddAutoNode("three-node", "Three", TopologyNodeKind.Person, TopologyHealthStatus.Critical, "three", width: 16, height: 16);

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot });
        var anchorXs = prepared.Groups
            .Select(group => double.Parse(group.Metadata["layout.force.anchor.x"], CultureInfo.InvariantCulture))
            .ToList();

        Assert(anchorXs.All(x => x >= 24 && x <= 136), "Weighted force anchors should stay inside the actual narrow viewport bounds.");
        Assert(prepared.Groups.All(group => group.Metadata["layout.force.anchor.strategy"] == "weighted-row"), "The narrow viewport fixture should exercise the weighted-row fast path.");
    }

    private static void TopologyMonitoringBundleRouteLabelsRemainReadable() {
        var chart = TopologyChart.Create()
            .WithId("dense-route-label-bundle")
            .WithViewport(1080, 520, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("source", "Source Site", TopologyHealthStatus.Healthy, "6 DCs")
            .AddAutoGroup("target", "Target Site", TopologyHealthStatus.Warning, "6 DCs")
            .WithGroupLayout("source", TopologyGroupLayoutPolicy.Grid)
            .WithGroupLayout("target", TopologyGroupLayoutPolicy.Grid);

        for (var i = 0; i < 6; i++) {
            chart
                .AddAutoNode("source-dc-" + i.ToString("00", CultureInfo.InvariantCulture), "SRC-DC-" + i.ToString("00", CultureInfo.InvariantCulture), TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "source", width: 76, height: 46, symbol: "DC")
                .AddAutoNode("target-dc-" + i.ToString("00", CultureInfo.InvariantCulture), "TGT-DC-" + i.ToString("00", CultureInfo.InvariantCulture), TopologyNodeKind.Server, i % 3 == 0 ? TopologyHealthStatus.Critical : TopologyHealthStatus.Warning, "target", width: 76, height: 46, symbol: "DC");
        }

        for (var i = 0; i < 6; i++) {
            chart.AddEdge(
                "replication-bundle-" + i.ToString("00", CultureInfo.InvariantCulture),
                "source-dc-" + i.ToString("00", CultureInfo.InvariantCulture),
                "target-dc-" + ((i * 2) % 6).ToString("00", CultureInfo.InvariantCulture),
                (95 + i * 17).ToString(CultureInfo.InvariantCulture) + " ms",
                TopologyEdgeKind.Replication,
                i % 3 == 0 ? TopologyHealthStatus.Critical : TopologyHealthStatus.Warning,
                TopologyDirection.Forward,
                TopologyEdgeRouting.ObstacleAvoidingOrthogonal,
                "Q:" + (180 + i * 41).ToString(CultureInfo.InvariantCulture),
                tertiaryLabel: (i + 2).ToString(CultureInfo.InvariantCulture) + "m ago");
        }

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeTileSubtitles = true,
            IncludeEdgeLabelBackplates = false,
            NodeDisplayMode = TopologyNodeDisplayMode.Tile
        }.WithMonitoringDashboardStyle();
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var labels = TopologyRenderPrimitives.EdgeLabelLayouts(prepared, options);
        var svg = chart.ToSvg(options);

        Assert(labels.Count == 6, "Dense replication bundles should produce one route label layout per labeled edge.");
        Assert(!LabelLayoutsOverlap(labels), "Dense replication bundle route labels should be placed without label-to-label overlap.");
        Assert(labels.All(label => Math.Abs(label.Edge.RouteLane) > 0.001 || label.Edge.Id == "replication-bundle-02" || label.Edge.Id == "replication-bundle-03"), "Dense replication bundle labels should inherit fanned route lanes.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"topology-edge-label-clearance\"") >= 6, "Dense replication bundle labels should draw clearance masks when backplates are disabled.");
        Assert(CountOccurrences(svg, "data-label-width=") >= 6 && CountOccurrences(svg, "data-label-height=") >= 6, "Dense replication bundle labels should expose final label dimensions for host diagnostics.");
        Assert(CountOccurrences(svg, "data-label-line-count=\"3\"") >= 6, "Dense replication bundle labels should expose line counts for host diagnostics.");
        Assert(CountOccurrences(svg, "data-label-clearance=\"true\"") >= 6, "Dense replication bundle labels should expose whether route clearance was applied.");
    }

    private static void TopologyDenseGroupedLayoutPreparesLargeAdSiteSubnetFixture() {
        var chart = TopologyChart.Create()
            .WithId("large-ad-site-subnet-fixture")
            .WithViewport(1400, 760, 28)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("amer", "AMER", TopologyHealthStatus.Healthy, "47 sites", symbol: "region", color: "#16A34A")
            .AddAutoGroup("emea", "EMEA", TopologyHealthStatus.Warning, "56 sites", symbol: "region", color: "#2563EB")
            .AddAutoGroup("apac", "APAC", TopologyHealthStatus.Critical, "39 sites", symbol: "region", color: "#8B5CF6")
            .WithGroupLayout("amer", TopologyGroupLayoutPolicy.CollapsedDots)
            .WithGroupLayout("emea", TopologyGroupLayoutPolicy.CollapsedDots)
            .WithGroupLayout("apac", TopologyGroupLayoutPolicy.CollapsedDots);

        var amerSites = AddDenseSitesAndSubnets(chart, "amer", "AMER", 47, 104, 10);
        var emeaSites = AddDenseSitesAndSubnets(chart, "emea", "EMEA", 56, 122, 20);
        var apacSites = AddDenseSitesAndSubnets(chart, "apac", "APAC", 39, 86, 30);
        chart
            .AddEdge("site-link-amer-emea", "amer-hub", "emea-hub", "24 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "MPLS")
            .AddEdge("site-link-emea-apac", "emea-hub", "apac-hub", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "MPLS")
            .AddEdge("site-link-amer-apac", "amer-hub", "apac-hub", "142 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Critical, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "Backup");

        AddReplicationPairs(chart, amerSites, emeaSites, "amer-emea-repl", 18, TopologyHealthStatus.Healthy);
        AddReplicationPairs(chart, emeaSites, apacSites, "emea-apac-repl", 18, TopologyHealthStatus.Warning);
        AddReplicationPairs(chart, apacSites, amerSites, "apac-amer-repl", 12, TopologyHealthStatus.Critical);

        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = false, IncludeEdgeLabels = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithMonitoringDashboardStyle()
            .WithFitContentToViewport();
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var interGroupEdges = prepared.Edges.Where(edge => edge.Kind is TopologyEdgeKind.Link or TopologyEdgeKind.Replication).ToList();
        var nodeLookup = prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);

        Assert(prepared.Nodes.Count(node => node.Kind == TopologyNodeKind.Branch) == 142, "Large AD fixtures should prepare all 142 site nodes.");
        Assert(prepared.Nodes.Count(node => node.Kind == TopologyNodeKind.Network) == 312, "Large AD fixtures should prepare all 312 subnet nodes.");
        Assert(prepared.Edges.Count(edge => edge.Kind == TopologyEdgeKind.Mapping) == 312, "Large AD fixtures should include subnet-to-site mapping edges.");
        Assert(prepared.Edges.Count(edge => edge.Kind == TopologyEdgeKind.Replication) == 48, "Large AD fixtures should include deterministic cross-region replication edges.");
        Assert(prepared.Groups.All(group => group.AppliedLayoutPolicy == TopologyGroupLayoutPolicy.CollapsedDots), "Large AD fixtures should keep region panels in collapsed-dot mode.");
        Assert(prepared.Groups.All(group => group.Width > 400 && group.Height > 300), "Large AD region panels should auto-size for dense site and subnet populations.");
        Assert(interGroupEdges.All(edge => edge.SourcePort != TopologyEdgePort.Auto && edge.TargetPort != TopologyEdgePort.Auto), "Large AD inter-region routes should infer outside-facing ports.");
        Assert(prepared.Nodes.All(node => string.IsNullOrWhiteSpace(node.GroupId) || NodeInsideGroup(node, prepared.Groups.Single(group => group.Id == node.GroupId))), "Large AD site and subnet nodes should remain inside generated region bounds.");
        AssertDenseBundleLanes(prepared, interGroupEdges.Where(edge => edge.Id == "site-link-amer-emea" || edge.Id.StartsWith("amer-emea-repl-", StringComparison.Ordinal)).ToList(), 19);
        AssertDenseBundleLanes(prepared, interGroupEdges.Where(edge => edge.Id == "site-link-emea-apac" || edge.Id.StartsWith("emea-apac-repl-", StringComparison.Ordinal)).ToList(), 19);
        AssertDenseBundleLanes(prepared, interGroupEdges.Where(edge => edge.Id == "site-link-amer-apac" || edge.Id.StartsWith("apac-amer-repl-", StringComparison.Ordinal)).ToList(), 13);
        Assert(interGroupEdges.All(edge => TopologyRenderPrimitives.EdgeRouteDiagnostics(prepared, edge, nodeLookup).CandidateCount > 1), "Large AD inter-region routes should use obstacle-aware candidate routing.");
    }

    private static void TopologyCollapsedDotPlacementUsesSizingColumnCount() {
        var chart = TopologyChart.Create()
            .WithId("collapsed-dot-column-count")
            .WithViewport(720, 420, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped)
            .AddAutoGroup("users", "Users", TopologyHealthStatus.Warning)
            .WithGroupLayout("users", TopologyGroupLayoutPolicy.CollapsedDots);

        for (var i = 0; i < 49; i++) {
            chart.AddAutoNode("user-" + i.ToString("00", CultureInfo.InvariantCulture), "User " + i.ToString("00", CultureInfo.InvariantCulture), TopologyNodeKind.Person, TopologyHealthStatus.Healthy, "users");
        }

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot });
        var users = prepared.Nodes.Where(node => node.GroupId == "users").ToList();
        var columns = users.Select(node => Math.Round(node.X, 1)).Distinct().Count();

        Assert(columns == 8, "Collapsed-dot placement should use the same dense column count that sizing used for the group bounds.");
    }

    private static void TopologyMonitoringSingleLineRouteLabelsUseClearance() {
        var chart = TopologyChart.Create()
            .WithId("single-line-route-label-clearance")
            .WithViewport(520, 220, 24)
            .WithLegend(null)
            .AddNode("amer", "AMER Hub", 70, 86, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 96, height: 56)
            .AddNode("emea", "EMEA Hub", 354, 86, TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, width: 96, height: 56)
            .AddEdge("amer-emea", "amer", "emea", "24 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal);

        var options = new TopologyRenderOptions {
            IncludeLegend = false,
            IncludeEdgeLabelBackplates = false
        }.WithMonitoringDashboardStyle();
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var nodes = prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var edge = prepared.Edges.Single(item => item.Id == "amer-emea");
        var points = TopologyRenderPrimitives.EdgePoints(prepared, edge, nodes);
        var routePoint = TopologyRenderPrimitives.EdgeLabelPoint(points);
        var label = TopologyRenderPrimitives.EdgeLabelLayouts(prepared, options).Single(item => item.Edge.Id == "amer-emea");
        var svg = chart.ToSvg(options);

        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-clearance\"", StringComparison.Ordinal), "Monitoring-style one-line route labels should draw a clearance mask when backplates are disabled.");
        Assert(Math.Abs(label.CenterY - routePoint.Y) > label.Height / 2 || Math.Abs(label.CenterX - routePoint.X) > label.Width / 2, "Monitoring-style one-line route labels should be offset from the route centerline instead of sitting directly on the edge.");
    }

    private static void TopologyLayeredHierarchyRoutesUseTieredBusesForWrappedChildren() {
        var chart = TopologyChart.Create()
            .WithId("tiered-hierarchy-buses")
            .WithViewport(620, 520, 24)
            .WithLegend(null)
            .AddHierarchy(ManyChildHierarchyItems(), new TopologyHierarchyOptions { NodeDisplayMode = TopologyNodeDisplayMode.Tile, NodeWidth = 70, NodeHeight = 46 });

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var children = prepared.Nodes.Where(node => node.Metadata.TryGetValue("hierarchy.parentId", out var parent) && parent == "root").ToList();
        var edges = prepared.Edges.Where(edge => edge.SourceNodeId == "root").ToList();
        var tiers = edges.Select(edge => edge.Metadata["hierarchy.route.tier"]).Distinct(StringComparer.Ordinal).Count();
        var busYs = edges.Select(edge => double.Parse(edge.Metadata["hierarchy.route.busY"], CultureInfo.InvariantCulture)).Distinct().OrderBy(value => value).ToList();

        Assert(children.Select(node => node.Metadata["layout.row"]).Distinct(StringComparer.Ordinal).Count() > 1, "The fixture should wrap many hierarchy children into multiple rows.");
        Assert(tiers > 1, "Wrapped hierarchy fan-outs should allocate separate bus tiers instead of using one cramped junction lane.");
        Assert(busYs.Count > 1, "Wrapped hierarchy fan-outs should expose distinct bus coordinates for host diagnostics.");
        Assert(edges.All(edge => Math.Abs(double.Parse(edge.Metadata["hierarchy.route.busY"], CultureInfo.InvariantCulture) - edge.Waypoints[0].Y) < 0.001), "Hierarchy busY diagnostics should match the final normalized waypoint geometry.");
        Assert(edges.All(edge => Math.Abs(edge.Waypoints[0].X - TopologyRenderPrimitives.CenterX(prepared.Nodes.Single(node => node.Id == edge.SourceNodeId))) < 0.001), "Hierarchy bus waypoints should stay aligned to the final normalized parent center.");
        Assert(edges.All(edge => Math.Abs(edge.Waypoints[1].X - TopologyRenderPrimitives.CenterX(prepared.Nodes.Single(node => node.Id == edge.TargetNodeId))) < 0.001), "Hierarchy bus waypoints should stay aligned to the final normalized child center.");
        Assert(busYs.All(busY => children.All(child => busY < child.Y || busY > child.Y + child.Height)), "Tiered hierarchy buses should sit between child rows instead of cutting through node cards.");
        Assert(chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle()).Length > 64, "Tiered hierarchy fan-outs should render as PNG.");
    }

    private static void TopologyLayeredHierarchySupportsReverseDirections() {
        var bottomToTop = TopologyLayoutEngine.Prepare(TopologyChart.Create()
            .WithId("bottom-to-top-hierarchy")
            .WithViewport(620, 520, 24)
            .WithLegend(null)
            .AddHierarchy(ManyChildHierarchyItems(), new TopologyHierarchyOptions { LayoutDirection = TopologyLayoutDirection.BottomToTop, NodeDisplayMode = TopologyNodeDisplayMode.Tile, NodeWidth = 70, NodeHeight = 46 }), options: new TopologyRenderOptions { IncludeLegend = false });
        var bottomRoot = bottomToTop.Nodes.Single(node => node.Id == "root");
        var bottomChildren = bottomToTop.Nodes.Where(node => node.Metadata.TryGetValue("hierarchy.parentId", out var parent) && parent == "root").ToList();
        var bottomEdges = bottomToTop.Edges.Where(edge => edge.SourceNodeId == "root").ToList();
        var bottomBusYs = bottomEdges.Select(edge => double.Parse(edge.Metadata["hierarchy.route.busY"], CultureInfo.InvariantCulture)).Distinct().ToList();

        Assert(bottomRoot.Y > bottomChildren.Max(node => node.Y), "Bottom-to-top hierarchy should place parents below their children.");
        Assert(bottomEdges.All(edge => edge.TargetPort == TopologyEdgePort.Bottom), "Bottom-to-top hierarchy routes should enter children from below after mirroring.");
        Assert(bottomBusYs.Count > 1, "Bottom-to-top wrapped fan-outs should preserve tiered bus diagnostics.");

        var rightToLeft = TopologyLayoutEngine.Prepare(TopologyChart.Create()
            .WithId("right-to-left-hierarchy")
            .WithViewport(720, 440, 24)
            .WithLegend(null)
            .AddHierarchy(ManyChildHierarchyItems(), new TopologyHierarchyOptions { LayoutDirection = TopologyLayoutDirection.RightToLeft, NodeDisplayMode = TopologyNodeDisplayMode.Tile, NodeWidth = 70, NodeHeight = 46 }), options: new TopologyRenderOptions { IncludeLegend = false });
        var rightRoot = rightToLeft.Nodes.Single(node => node.Id == "root");
        var rightChildren = rightToLeft.Nodes.Where(node => node.Metadata.TryGetValue("hierarchy.parentId", out var parent) && parent == "root").ToList();
        var rightEdges = rightToLeft.Edges.Where(edge => edge.SourceNodeId == "root").ToList();
        var rightBusXs = rightEdges.Select(edge => double.Parse(edge.Metadata["hierarchy.route.busX"], CultureInfo.InvariantCulture)).Distinct().ToList();

        Assert(rightRoot.X > rightChildren.Max(node => node.X), "Right-to-left hierarchy should place parents to the right of their children.");
        Assert(rightEdges.All(edge => edge.TargetPort == TopologyEdgePort.Right), "Right-to-left hierarchy routes should enter children from the right after mirroring.");
        Assert(rightBusXs.Count > 1, "Right-to-left wrapped fan-outs should preserve tiered bus diagnostics.");
        Assert(rightEdges.All(edge => Math.Abs(double.Parse(edge.Metadata["hierarchy.route.busX"], CultureInfo.InvariantCulture) - edge.Waypoints[0].X) < 0.001), "Right-to-left hierarchy busX diagnostics should match final normalized waypoint geometry.");
        Assert(rightEdges.All(edge => Math.Abs(edge.Waypoints[0].Y - TopologyRenderPrimitives.CenterY(rightToLeft.Nodes.Single(node => node.Id == edge.SourceNodeId))) < 0.001), "Right-to-left hierarchy bus waypoints should stay aligned to the final normalized parent center.");
        Assert(rightEdges.All(edge => Math.Abs(edge.Waypoints[1].Y - TopologyRenderPrimitives.CenterY(rightToLeft.Nodes.Single(node => node.Id == edge.TargetNodeId))) < 0.001), "Right-to-left hierarchy bus waypoints should stay aligned to the final normalized child center.");
    }

    private static void TopologyLayeredHierarchyMirrorsSingleChildPorts() {
        var items = new List<TopologyHierarchyItem> {
            new("root", "Root") { Kind = TopologyNodeKind.Team },
            new("child", "Child", "root") { Kind = TopologyNodeKind.Person }
        };
        var bottomToTop = TopologyLayoutEngine.Prepare(TopologyChart.Create()
            .WithId("single-bottom-to-top-hierarchy")
            .WithViewport(320, 280, 24)
            .WithLegend(null)
            .AddHierarchy(items, new TopologyHierarchyOptions { LayoutDirection = TopologyLayoutDirection.BottomToTop, NodeDisplayMode = TopologyNodeDisplayMode.Tile, NodeWidth = 70, NodeHeight = 46 }), options: new TopologyRenderOptions { IncludeLegend = false });
        var bottomEdge = bottomToTop.Edges.Single();

        Assert(bottomEdge.SourcePort == TopologyEdgePort.Top, "Bottom-to-top single-child hierarchy routes should leave parents upward after mirroring.");
        Assert(bottomEdge.TargetPort == TopologyEdgePort.Bottom, "Bottom-to-top single-child hierarchy routes should enter children from below after mirroring.");

        var rightToLeft = TopologyLayoutEngine.Prepare(TopologyChart.Create()
            .WithId("single-right-to-left-hierarchy")
            .WithViewport(360, 240, 24)
            .WithLegend(null)
            .AddHierarchy(items, new TopologyHierarchyOptions { LayoutDirection = TopologyLayoutDirection.RightToLeft, NodeDisplayMode = TopologyNodeDisplayMode.Tile, NodeWidth = 70, NodeHeight = 46 }), options: new TopologyRenderOptions { IncludeLegend = false });
        var rightEdge = rightToLeft.Edges.Single();

        Assert(rightEdge.SourcePort == TopologyEdgePort.Left, "Right-to-left single-child hierarchy routes should leave parents toward the left after mirroring.");
        Assert(rightEdge.TargetPort == TopologyEdgePort.Right, "Right-to-left single-child hierarchy routes should enter children from the right after mirroring.");
    }

    private static void TopologyHierarchyWithoutLayeredLayoutUsesFinalDirectionPorts() {
        var items = new List<TopologyHierarchyItem> {
            new("root", "Root") { Kind = TopologyNodeKind.Team },
            new("child", "Child", "root") { Kind = TopologyNodeKind.Person }
        };
        var bottomToTop = TopologyChart.Create()
            .AddHierarchy(items, new TopologyHierarchyOptions { ApplyLayeredLayout = false, LayoutDirection = TopologyLayoutDirection.BottomToTop });
        var rightToLeft = TopologyChart.Create()
            .AddHierarchy(items, new TopologyHierarchyOptions { ApplyLayeredLayout = false, LayoutDirection = TopologyLayoutDirection.RightToLeft });
        var bottomEdge = bottomToTop.Edges.Single();
        var rightEdge = rightToLeft.Edges.Single();

        Assert(bottomEdge.SourcePort == TopologyEdgePort.Top && bottomEdge.TargetPort == TopologyEdgePort.Bottom, "Non-layered bottom-to-top hierarchy edges should use final bottom-to-top ports without relying on mirroring.");
        Assert(rightEdge.SourcePort == TopologyEdgePort.Left && rightEdge.TargetPort == TopologyEdgePort.Right, "Non-layered right-to-left hierarchy edges should use final right-to-left ports without relying on mirroring.");
    }

    private static void TopologyLayoutModeNumericValuesRemainCompatible() {
        Assert((int)TopologyLayoutMode.Manual == 0, "Topology layout mode numeric values should remain stable for persisted payloads.");
        Assert((int)TopologyLayoutMode.DenseGrouped == 5, "DenseGrouped layout mode numeric value should remain stable.");
        Assert((int)TopologyLayoutMode.Geographic == 6, "Geographic layout mode numeric value should remain stable after adding force-directed layout.");
        Assert((int)TopologyLayoutMode.ForceDirected == 7, "New topology layout modes should append numeric values.");
    }

    private static void TopologyDenseGroupedLayoutSupportsRightToLeftFlow() {
        var chart = TopologyChart.Create()
            .WithId("dense-right-left")
            .WithViewport(920, 360, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.RightToLeft)
            .AddAutoGroup("amer", "AMER", TopologyHealthStatus.Healthy)
            .AddAutoGroup("emea", "EMEA", TopologyHealthStatus.Healthy)
            .AddAutoGroup("apac", "APAC", TopologyHealthStatus.Healthy)
            .AddAutoNode("amer-hub", "AMER Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "amer")
            .AddAutoNode("emea-hub", "EMEA Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "emea")
            .AddAutoNode("apac-hub", "APAC Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "apac")
            .AddEdge("amer-emea", "amer-hub", "emea-hub", "24 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .AddEdge("emea-apac", "emea-hub", "apac-hub", "82 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle());
        var amer = prepared.Groups.Single(group => group.Id == "amer");
        var emea = prepared.Groups.Single(group => group.Id == "emea");
        var apac = prepared.Groups.Single(group => group.Id == "apac");
        var link = prepared.Edges.Single(edge => edge.Id == "amer-emea");
        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle());

        Assert(amer.X > emea.X && emea.X > apac.X, "Right-to-left dense grouped layout should mirror group ordering.");
        Assert(link.SourcePort == TopologyEdgePort.Left && link.TargetPort == TopologyEdgePort.Right, "Right-to-left dense grouped edge defaults should mirror outside-facing ports.");
        Assert(svg.Contains("data-layout-direction=\"RightToLeft\"", StringComparison.Ordinal), "Right-to-left dense grouped SVG should expose the layout direction.");
    }

    private static void TopologyDenseReverseLayoutsPreserveExplicitGroupCoordinates() {
        var rightToLeft = TopologyLayoutEngine.Prepare(TopologyChart.Create()
            .WithId("dense-explicit-rtl")
            .WithViewport(760, 420, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.RightToLeft)
            .AddGroup("pinned", "Pinned", 80, 90, 180, 150, TopologyHealthStatus.Healthy)
            .AddAutoGroup("auto", "Auto", TopologyHealthStatus.Warning)
            .AddAutoNode("pinned-node", "Pinned", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "pinned")
            .AddAutoNode("auto-node", "Auto", TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "auto")
            .AddEdge("link", "pinned-node", "auto-node", null, TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional), options: new TopologyRenderOptions { IncludeLegend = false });
        var bottomToTop = TopologyLayoutEngine.Prepare(TopologyChart.Create()
            .WithId("dense-explicit-btt")
            .WithViewport(760, 520, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.BottomToTop)
            .AddGroup("pinned", "Pinned", 90, 120, 180, 150, TopologyHealthStatus.Healthy)
            .AddAutoGroup("auto", "Auto", TopologyHealthStatus.Warning)
            .AddAutoNode("pinned-node", "Pinned", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "pinned")
            .AddAutoNode("auto-node", "Auto", TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "auto")
            .AddEdge("link", "pinned-node", "auto-node", null, TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional), options: new TopologyRenderOptions { IncludeLegend = false });
        var rtlPinned = rightToLeft.Groups.Single(group => group.Id == "pinned");
        var bttPinned = bottomToTop.Groups.Single(group => group.Id == "pinned");

        Assert(Math.Abs(rtlPinned.X - 80) < 0.001 && Math.Abs(rtlPinned.Y - 90) < 0.001, "Right-to-left dense grouped layouts should preserve caller-pinned group coordinates.");
        Assert(Math.Abs(bttPinned.X - 90) < 0.001 && Math.Abs(bttPinned.Y - 120) < 0.001, "Bottom-to-top dense grouped layouts should preserve caller-pinned group coordinates.");
        Assert(NodeInsideGroup(rightToLeft.Nodes.Single(node => node.Id == "pinned-node"), rtlPinned), "Right-to-left dense grouped layouts should translate pinned group nodes back with the group.");
        Assert(NodeInsideGroup(bottomToTop.Nodes.Single(node => node.Id == "pinned-node"), bttPinned), "Bottom-to-top dense grouped layouts should translate pinned group nodes back with the group.");
    }

    private static void TopologyDenseReverseLayoutsKeepSizeOnlyGroupsAutoPacked() {
        var prepared = TopologyLayoutEngine.Prepare(TopologyChart.Create()
            .WithId("dense-size-only-rtl")
            .WithViewport(760, 420, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.RightToLeft)
            .AddGroup("left", "Left", 0, 0, 180, 150, TopologyHealthStatus.Healthy)
            .AddGroup("right", "Right", 0, 0, 180, 150, TopologyHealthStatus.Warning)
            .AddAutoNode("left-node", "Left", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "left")
            .AddAutoNode("right-node", "Right", TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "right")
            .AddEdge("link", "left-node", "right-node", null, TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional), options: new TopologyRenderOptions { IncludeLegend = false });
        var left = prepared.Groups.Single(group => group.Id == "left");
        var right = prepared.Groups.Single(group => group.Id == "right");

        Assert(Math.Abs(left.X - right.X) > 100, "Right-to-left dense grouped layouts should still auto-pack groups that only override size.");
        Assert(NodeInsideGroup(prepared.Nodes.Single(node => node.Id == "left-node"), left), "Size-only dense group nodes should remain inside their auto-packed group.");
        Assert(NodeInsideGroup(prepared.Nodes.Single(node => node.Id == "right-node"), right), "Size-only dense group nodes should remain inside their auto-packed group.");
    }

    private static void TopologyDenseReverseLayoutsRefreshRestoredEdgeGeometry() {
        var prepared = TopologyLayoutEngine.Prepare(TopologyChart.Create()
            .WithId("dense-restored-manual-route")
            .WithViewport(760, 420, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.RightToLeft)
            .AddGroup("pinned", "Pinned", 80, 90, 180, 150, TopologyHealthStatus.Healthy)
            .AddAutoGroup("auto", "Auto", TopologyHealthStatus.Warning)
            .AddAutoNode("pinned-node", "Pinned", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "pinned")
            .AddAutoNode("auto-node", "Auto", TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "auto")
            .AddEdge("link", "pinned-node", "auto-node", null, TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal)
            .WithEdgeWaypoints("link", new ChartForgeX.Primitives.ChartPoint(260, 180)), options: new TopologyRenderOptions { IncludeLegend = false });
        var edge = prepared.Edges.Single(item => item.Id == "link");
        var pinned = prepared.Groups.Single(group => group.Id == "pinned");
        var auto = prepared.Groups.Single(group => group.Id == "auto");
        var dx = auto.X + auto.Width / 2 - (pinned.X + pinned.Width / 2);

        Assert(edge.Waypoints.Single().X < 260, "Manual dense waypoints should be translated when pinned groups are restored after mirroring.");
        Assert(edge.SourcePort == (dx >= 0 ? TopologyEdgePort.Right : TopologyEdgePort.Left), "Dense edge source port inference should match final restored group geometry.");
        Assert(edge.TargetPort == (dx >= 0 ? TopologyEdgePort.Left : TopologyEdgePort.Right), "Dense edge target port inference should match final restored group geometry.");
    }

    private static void TopologyMirroringInvertsRouteLanes() {
        var rightToLeft = TopologyLayoutEngine.Prepare(TopologyChart.Create()
            .WithId("rtl-route-lane")
            .WithViewport(720, 360, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.RightToLeft)
            .AddAutoGroup("left", "Left", TopologyHealthStatus.Healthy)
            .AddAutoGroup("right", "Right", TopologyHealthStatus.Warning)
            .AddAutoNode("left-node", "Left", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "left")
            .AddAutoNode("right-node", "Right", TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "right")
            .AddEdge("link", "left-node", "right-node", null, TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal)
            .WithEdgeRouteLane("link", 24), options: new TopologyRenderOptions { IncludeLegend = false });
        var bottomToTop = TopologyLayoutEngine.Prepare(TopologyChart.Create()
            .WithId("btt-route-lane")
            .WithViewport(360, 620, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.BottomToTop)
            .AddAutoGroup("top", "Top", TopologyHealthStatus.Healthy)
            .AddAutoGroup("bottom", "Bottom", TopologyHealthStatus.Warning)
            .AddAutoNode("top-node", "Top", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "top")
            .AddAutoNode("bottom-node", "Bottom", TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "bottom")
            .AddEdge("link", "top-node", "bottom-node", null, TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.Orthogonal)
            .WithEdgeRouteLane("link", 18), options: new TopologyRenderOptions { IncludeLegend = false });

        Assert(Math.Abs(rightToLeft.Edges.Single().RouteLane + 24) < 0.001, "Right-to-left mirroring should invert the route-lane sign for regenerated edge geometry.");
        Assert(Math.Abs(bottomToTop.Edges.Single().RouteLane + 18) < 0.001, "Bottom-to-top mirroring should invert the route-lane sign for regenerated edge geometry.");
    }

    private static void TopologyDenseGroupedLayoutSupportsBottomToTopFlow() {
        var chart = TopologyChart.Create()
            .WithId("dense-bottom-top")
            .WithViewport(520, 720, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.BottomToTop)
            .AddAutoGroup("core", "Core", TopologyHealthStatus.Healthy)
            .AddAutoGroup("middle", "Middle", TopologyHealthStatus.Warning)
            .AddAutoGroup("edge", "Edge", TopologyHealthStatus.Critical)
            .AddAutoNode("core-hub", "Core Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, "core")
            .AddAutoNode("middle-hub", "Middle Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Warning, "middle")
            .AddAutoNode("edge-hub", "Edge Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Critical, "edge")
            .AddEdge("core-middle", "core-hub", "middle-hub", "32 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Warning, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .AddEdge("middle-edge", "middle-hub", "edge-hub", "58 ms", TopologyEdgeKind.Link, TopologyHealthStatus.Critical, TopologyDirection.Bidirectional, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle());
        var core = prepared.Groups.Single(group => group.Id == "core");
        var middle = prepared.Groups.Single(group => group.Id == "middle");
        var edge = prepared.Groups.Single(group => group.Id == "edge");
        var link = prepared.Edges.Single(edgeItem => edgeItem.Id == "core-middle");
        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }.WithMonitoringDashboardStyle());

        Assert(core.Y > middle.Y && middle.Y > edge.Y, "Bottom-to-top dense grouped layout should mirror group ordering vertically.");
        Assert(link.SourcePort == TopologyEdgePort.Top && link.TargetPort == TopologyEdgePort.Bottom, "Bottom-to-top dense grouped edge defaults should mirror outside-facing ports.");
        Assert(svg.Contains("data-layout-direction=\"BottomToTop\"", StringComparison.Ordinal), "Bottom-to-top dense grouped SVG should expose the layout direction.");
    }

    private static void TopologyDenseGroupedBottomToTopKeepsMultiColumnPacking() {
        var chart = TopologyChart.Create()
            .WithId("dense-bottom-top-columns")
            .WithViewport(900, 720, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.BottomToTop);

        for (var i = 0; i < 6; i++) {
            var id = "group-" + i.ToString("00", CultureInfo.InvariantCulture);
            chart.AddAutoGroup(id, "Group " + i.ToString("00", CultureInfo.InvariantCulture), TopologyHealthStatus.Healthy)
                .AddAutoNode(id + "-hub", "Hub " + i.ToString("00", CultureInfo.InvariantCulture), TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, id);
        }

        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var columns = prepared.Groups.Select(group => Math.Round(group.X, 1)).Distinct().Count();

        Assert(columns > 1, "Bottom-to-top dense grouped layouts should keep multi-column packing before the final vertical mirror.");
    }

    private static List<TopologyHierarchyItem> ManyChildHierarchyItems() {
        var items = new List<TopologyHierarchyItem> { new("root", "Directory") { Kind = TopologyNodeKind.Team, IconId = "team" } };
        for (var i = 0; i < 32; i++) {
            items.Add(new TopologyHierarchyItem("user-" + i.ToString("000", CultureInfo.InvariantCulture), "User " + i.ToString("000", CultureInfo.InvariantCulture), "root") { Kind = TopologyNodeKind.Person, IconId = "person" });
        }

        return items;
    }

    private static List<string> AddDenseSitesAndSubnets(TopologyChart chart, string groupId, string labelPrefix, int siteCount, int subnetCount, int cidrOctet) {
        var siteIds = new List<string>(siteCount);
        chart.AddAutoNode(groupId + "-hub", labelPrefix + " Hub", TopologyNodeKind.Hub, TopologyHealthStatus.Healthy, groupId, cidrOctet.ToString(CultureInfo.InvariantCulture) + ".0.0.0/16", width: 48, height: 36, symbol: "H");
        for (var i = 0; i < siteCount; i++) {
            var id = groupId + "-site-" + i.ToString("000", CultureInfo.InvariantCulture);
            var status = i % 31 == 0 ? TopologyHealthStatus.Critical : i % 11 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy;
            siteIds.Add(id);
            chart.AddAutoNode(id, labelPrefix + " Site " + i.ToString("000", CultureInfo.InvariantCulture), TopologyNodeKind.Branch, status, groupId, cidrOctet.ToString(CultureInfo.InvariantCulture) + "." + i.ToString(CultureInfo.InvariantCulture) + ".0.0/24", width: 48, height: 36, symbol: "S");
        }

        for (var i = 0; i < subnetCount; i++) {
            var site = siteIds[i % siteIds.Count];
            var subnet = groupId + "-subnet-" + i.ToString("000", CultureInfo.InvariantCulture);
            var status = i % 43 == 0 ? TopologyHealthStatus.Critical : i % 17 == 0 ? TopologyHealthStatus.Warning : TopologyHealthStatus.Healthy;
            chart
                .AddAutoNode(subnet, labelPrefix + " Subnet " + i.ToString("000", CultureInfo.InvariantCulture), TopologyNodeKind.Network, status, groupId, cidrOctet.ToString(CultureInfo.InvariantCulture) + "." + (i / 255).ToString(CultureInfo.InvariantCulture) + "." + (i % 255).ToString(CultureInfo.InvariantCulture) + ".0/24", width: 48, height: 36, symbol: "N")
                .AddEdge("map-" + subnet, subnet, site, null, TopologyEdgeKind.Mapping, status, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal);
        }

        return siteIds;
    }

    private static void AddReplicationPairs(TopologyChart chart, IReadOnlyList<string> sourceSites, IReadOnlyList<string> targetSites, string prefix, int count, TopologyHealthStatus status) {
        for (var i = 0; i < count; i++) {
            chart.AddEdge(prefix + "-" + i.ToString("000", CultureInfo.InvariantCulture), sourceSites[(i * 5) % sourceSites.Count], targetSites[(i * 7) % targetSites.Count], (32 + i * 3).ToString(CultureInfo.InvariantCulture) + " ms", TopologyEdgeKind.Replication, status, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "Q:" + (100 + i * 13).ToString(CultureInfo.InvariantCulture), tertiaryLabel: (i + 1).ToString(CultureInfo.InvariantCulture) + "m ago");
        }
    }

    private static void AssertDenseBundleLanes(TopologyChart prepared, IReadOnlyList<TopologyEdge> edges, int expectedCount) {
        Assert(edges.Count == expectedCount, "Dense inter-region bundles should contain the expected site-link and replication routes.");
        var lanes = edges.Select(edge => Math.Round(edge.RouteLane, 3)).OrderBy(value => value).ToList();
        Assert(lanes.Distinct().Count() == expectedCount, "Dense inter-region bundles should assign each route a distinct lane.");
        Assert(lanes.First() < 0 && lanes[lanes.Count - 1] > 0, "Dense inter-region bundle lanes should fan out around the center route.");
        Assert(Math.Abs(lanes.Sum()) < 0.001, "Dense inter-region bundle lanes should remain centered around zero.");
        Assert(edges.All(edge => (edge.LayoutInference & TopologyEdgeLayoutInference.RouteLane) == TopologyEdgeLayoutInference.RouteLane), "Dense inter-region bundle lanes should be marked as inferred.");
        Assert(edges.All(edge => TopologyRenderPrimitives.EdgePoints(prepared, edge, prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal)).Count >= 2), "Dense inter-region bundle routes should resolve to drawable edge geometry.");
    }

    private static bool NodeInsideGroup(TopologyNode node, TopologyGroup group) {
        return node.X >= group.X
            && node.X + node.Width <= group.X + group.Width
            && node.Y >= group.Y
            && node.Y + node.Height <= group.Y + group.Height;
    }

    private static double Distance(TopologyNode node, double x, double y) {
        var dx = node.X + node.Width / 2 - x;
        var dy = node.Y + node.Height / 2 - y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool LabelLayoutsOverlap(IReadOnlyList<TopologyEdgeLabelLayout> labels) {
        for (var i = 0; i < labels.Count; i++) {
            for (var j = i + 1; j < labels.Count; j++) {
                if (Math.Abs(labels[i].CenterX - labels[j].CenterX) * 2 < labels[i].Width + labels[j].Width + 8 &&
                    Math.Abs(labels[i].CenterY - labels[j].CenterY) * 2 < labels[i].Height + labels[j].Height + 8) {
                    return true;
                }
            }
        }

        return false;
    }

}
