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
            .AddNodeDetail("api", "Latency", "24 ms", TopologyHealthStatus.Warning)
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
