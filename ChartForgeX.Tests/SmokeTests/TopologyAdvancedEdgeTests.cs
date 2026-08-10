using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyAdvancedEdgesPreserveSvgPngAndDiagnosticParity() {
        var chart = TopologyChart.Create()
            .WithId("advanced-edge-contract")
            .WithViewport(720, 360, 24)
            .WithLegend(null)
            .AddNode("api", "API", 70, 120, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 170, height: 96, subtitle: "Public endpoint")
            .AddNode("db", "Database", 470, 120, TopologyNodeKind.Database, TopologyHealthStatus.Warning, width: 170, height: 96, subtitle: "Primary")
            .AddNodePort("api", "grpc", TopologyEdgePort.Right, 0.3, "gRPC")
            .AddNodePort("db", "writer", TopologyEdgePort.Left, 0.72, "Writer")
            .AddNodeDetail("api", "Region", "EU", TopologyHealthStatus.Healthy)
            .AddNodeDetail("api", "Latency", "24 ms", TopologyHealthStatus.Warning, color: "red")
            .AddEdge("api-db", "api", "db", "Queries", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgeNamedPorts("api-db", "grpc", "writer")
            .WithEdgeStroke("api-db", width: 4, opacity: 0.58, dashPattern: new[] { 11.0, 3.0, 2.0, 3.0 })
            .WithEdgeMarkers("api-db", TopologyMarkerKind.Circle, TopologyMarkerKind.Diamond)
            .WithEdgeEndpointLabels("api-db", "gRPC", "5432")
            .WithEdgeLayoutHints("api-db", preferredLength: 260, minimumRankSpan: 2, routingPriority: 25);
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeLayoutDiagnosticOverlay = true };
        chart.WithAccessibility(accessibility => accessibility.WithTextAlternative("API to database topology", "Shows the public query path.", "en"));

        var svg = chart.ToSvg(options);
        var png = chart.ToPng(options);
        var diagnostics = TopologyLayoutDiagnostics.Analyze(chart, options);
        var artifact = chart.ToVisualArtifact();
        var edge = diagnostics.Edges.Single(item => item.Id == "api-db");
        var source = diagnostics.Nodes.Single(item => item.Id == "api");

        Assert(svg.Contains("data-edge-dash-pattern=\"11 3 2 3\"", StringComparison.Ordinal), "SVG should preserve custom edge dash patterns.");
        Assert(svg.Contains("data-source-marker=\"Circle\"", StringComparison.Ordinal) && svg.Contains("data-target-marker=\"Diamond\"", StringComparison.Ordinal), "SVG should preserve explicit endpoint markers.");
        Assert(svg.Contains("data-source-port-id=\"grpc\"", StringComparison.Ordinal) && svg.Contains("data-target-port-id=\"writer\"", StringComparison.Ordinal), "SVG should preserve named edge ports.");
        Assert(svg.Contains("data-cfx-role=\"topology-edge-endpoint-label\"", StringComparison.Ordinal), "SVG should render endpoint labels.");
        Assert(svg.Contains("data-cfx-role=\"topology-node-detail-value\"", StringComparison.Ordinal), "SVG should render typed node details.");
        Assert(svg.Contains("data-cfx-role=\"topology-layout-diagnostics\"", StringComparison.Ordinal), "SVG should render the optional layout diagnostic overlay.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50, "Advanced edge styling should preserve PNG output.");
        Assert(edge.Points.Count >= 2 && edge.Strategy.Length > 0, "Machine-readable diagnostics should expose prepared routes and router strategy.");
        Assert(source.Ports.Single().Id == "grpc", "Machine-readable diagnostics should expose named port geometry.");
        Assert(!diagnostics.HasCollisions, "Separated nodes should not produce false collision diagnostics.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Office), "Topology artifacts should declare Office adapter handoff support.");
        Assert(artifact.Accessibility.Name == "API to database topology" && artifact.Accessibility.Language == "en", "Topology artifacts should preserve accessibility metadata for host adapters.");
        Assert(artifact.Regions.Any(region => region.Id == "api" && region.Kind == "topology-node"), "Topology artifacts should expose host-inspectable regions.");
        var artifactApiBounds = artifact.Regions.Single(region => region.Id == "api").Bounds!.Value;
        Assert(Math.Abs(artifactApiBounds.Left - source.Bounds.Left) < 0.001 && Math.Abs(artifactApiBounds.Top - source.Bounds.Top) < 0.001 && Math.Abs(artifactApiBounds.Width - source.Bounds.Width) < 0.001 && Math.Abs(artifactApiBounds.Height - source.Bounds.Height) < 0.001, "Topology artifact regions should use the same default prepared geometry as static renderers.");

        var autoChart = TopologyChart.Create()
            .WithId("auto-regions")
            .WithViewport(600, 300, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Layered)
            .AddAutoNode("left", "Left")
            .AddAutoNode("right", "Right")
            .AddEdge("left-right", "left", "right");
        var autoArtifact = autoChart.ToVisualArtifact();
        var leftBounds = autoArtifact.Regions.Single(region => region.Id == "left").Bounds!.Value;
        var rightBounds = autoArtifact.Regions.Single(region => region.Id == "right").Bounds!.Value;
        Assert(leftBounds.Top != rightBounds.Top || leftBounds.Left != rightBounds.Left, "Topology artifact regions should use prepared auto-layout geometry instead of overlapping at the origin.");

        autoChart.Nodes[0].Href = "javascript:alert(1)";
        autoChart.Edges[0].Href = "data:text/html,bad";
        var safeArtifact = autoChart.ToVisualArtifact();
        Assert(safeArtifact.Regions.Single(region => region.Id == "left").Href == null && safeArtifact.Regions.Single(region => region.Id == "left-right").Href == null, "Topology artifact regions should apply the same safe-link policy as SVG output.");
        autoChart.Nodes[0].Href = "java\tscript:alert(1)";
        Assert(autoChart.ToVisualArtifact().Regions.Single(region => region.Id == "left").Href == null, "Topology artifact regions should reject control-character-obfuscated script schemes.");

        var orderingChart = TopologyChart.Create()
            .WithViewport(420, 220)
            .WithLegend(null)
            .AddNode("a", "A", 30, 70)
            .AddNode("b", "B", 260, 70)
            .AddEdge("high", "a", "b", kind: TopologyEdgeKind.Dependency)
            .AddEdge("low", "a", "b", kind: TopologyEdgeKind.Connectivity)
            .WithEdgeLayoutHints("high", routingPriority: 50)
            .WithEdgeLayoutHints("low", routingPriority: -50)
            .WithEdgeStroke("high", opacity: 0.25);
        var orderingOptions = new TopologyRenderOptions { IncludeLegend = false };
        orderingOptions.SelectedEdgeIds.Add("high");
        var orderingSvg = orderingChart.ToSvg(orderingOptions);
        Assert(orderingSvg.IndexOf("data-edge-id=\"low\"", StringComparison.Ordinal) < orderingSvg.IndexOf("data-edge-id=\"high\"", StringComparison.Ordinal), "Explicit edge routing priority should be the primary render-order key.");
        byte[] selectedLowOpacity = orderingChart.ToPng(orderingOptions);
        orderingChart.Edges.Single(edgeItem => edgeItem.Id == "high").Opacity = 1D;
        byte[] selectedFullOpacity = orderingChart.ToPng(orderingOptions);
        Assert(!selectedLowOpacity.SequenceEqual(selectedFullOpacity), "Selected PNG edges should retain their explicit opacity instead of being forced fully opaque.");

        var wrapped = TopologyChart.Create()
            .WithViewport(360, 260)
            .WithLegend(null)
            .AddNode("wrapped", "Primary service\nwith a wrapped title\nthird line", 40, 40, width: 184, height: 80, subtitle: "First subtitle\nsecond subtitle")
            .AddNodeDetail("wrapped", "Status", "Ready");
        var wrappedOptions = new TopologyRenderOptions { IncludeLegend = false, WrapNodeLabels = true, MaxNodeLabelLines = 3, MaxNodeSubtitleLines = 3 };
        var wrappedDiagnostics = TopologyLayoutDiagnostics.Analyze(wrapped, wrappedOptions);
        Assert(wrappedDiagnostics.Nodes.Single().Bounds.Height > 82, "Detailed card layout should grow to reserve space for wrapped header text.");
        var wrappedSvg = System.Xml.Linq.XDocument.Parse(wrapped.ToSvg(wrappedOptions));
        var lastHeader = wrappedSvg.Descendants().Single(element => element.Value == "second subtitle");
        var firstDetail = wrappedSvg.Descendants().Single(element => string.Equals((string?)element.Attribute("data-cfx-role"), "topology-node-detail-label", StringComparison.Ordinal));
        var lastHeaderY = double.Parse(lastHeader.Attribute("y")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        var firstDetailY = double.Parse(firstDetail.Attribute("y")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert(firstDetailY - 8 > lastHeaderY + 4, "Detailed card separators should start below every rendered title and subtitle line.");
        Assert(wrapped.ToPng(wrappedOptions).Length > 64, "Wrapped detailed cards should preserve PNG output parity.");
    }

    private static void TopologyLayoutHintsAndPresetsAffectPreparedGeometry() {
        var chart = TopologyChart.Create()
            .WithId("layout-hints")
            .WithViewport(700, 300, 24)
            .WithLegend(null)
            .WithLayout(TopologyLayoutMode.Layered)
            .AddAutoNode("source", "Source", TopologyNodeKind.Service)
            .AddAutoNode("middle", "Middle", TopologyNodeKind.Service)
            .AddAutoNode("target", "Target", TopologyNodeKind.Database)
            .AddEdge("source-middle", "source", "middle")
            .AddEdge("middle-target", "middle", "target")
            .WithEdgeLayoutHints("source-middle", minimumRankSpan: 2);

        var dense = TopologyLayoutDiagnostics.Analyze(chart, new TopologyRenderOptions { IncludeLegend = false }.ApplyLayoutPreset(TopologyLayoutPreset.Dense));
        var presentation = TopologyLayoutDiagnostics.Analyze(chart, new TopologyRenderOptions { IncludeLegend = false }.ApplyLayoutPreset(TopologyLayoutPreset.Presentation));
        var denseSource = dense.Nodes.Single(item => item.Id == "source").Bounds;
        var denseMiddle = dense.Nodes.Single(item => item.Id == "middle").Bounds;
        var presentationSource = presentation.Nodes.Single(item => item.Id == "source").Bounds;
        var presentationMiddle = presentation.Nodes.Single(item => item.Id == "middle").Bounds;

        Assert(denseMiddle.Top > denseSource.Bottom, "Minimum rank span should place the target in a later prepared layer.");
        Assert(presentationMiddle.Top - presentationSource.Bottom > denseMiddle.Top - denseSource.Bottom, "Presentation layout should reserve more rank spacing than dense layout.");

        chart.Edges.Single(edge => edge.Id == "source-middle").MinimumRankSpan = 20;
        var wideSpan = TopologyLayoutDiagnostics.Analyze(chart, new TopologyRenderOptions { IncludeLegend = false }.ApplyLayoutPreset(TopologyLayoutPreset.Dense));
        var wideSource = wideSpan.Nodes.Single(item => item.Id == "source").Bounds;
        var wideMiddle = wideSpan.Nodes.Single(item => item.Id == "middle").Bounds;
        Assert(wideMiddle.Top - wideSource.Bottom > denseMiddle.Top - denseSource.Bottom + 100, "Larger minimum-rank hints should preserve empty physical ranks instead of compacting them.");

        var reused = new TopologyRenderOptions().ApplyLayoutPreset(TopologyLayoutPreset.Dense).ApplyLayoutPreset(TopologyLayoutPreset.Balanced);
        Assert(reused.NodeDisplayMode == TopologyNodeDisplayMode.Card && reused.WrapNodeLabels == false && reused.MaxNodeLabelLines == 2 && reused.MaxNodeSubtitleLines == 2, "Applying a layout preset to reused options should reset every preset-owned presentation field.");
    }

    private static void TopologyAdvancedEdgeContractsRejectInvalidValues() {
        var chart = TopologyChart.Create()
            .WithId("invalid-advanced-edge")
            .WithViewport(500, 280)
            .WithLegend(null)
            .AddNode("a", "A", 40, 80)
            .AddNode("b", "B", 300, 80)
            .AddEdge("a-b", "a", "b")
            .WithEdgeStroke("a-b", dashPattern: new[] { 6.0, 3.0, 2.0 });

        AssertThrows<TopologyValidationException>(() => chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }), "Odd custom dash patterns should fail topology validation.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Edges[0].PreferredLength = 0, "Preferred length hints should reject non-positive values.");
        AssertThrows<ArgumentOutOfRangeException>(() => chart.Edges[0].MinimumRankSpan = 0, "Minimum rank span should reject values below one.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TopologyNodePort { Id = "bad", Side = TopologyEdgePort.Auto }, "Named ports should require an explicit side.");
    }
}
