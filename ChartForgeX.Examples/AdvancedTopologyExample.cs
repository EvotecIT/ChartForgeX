using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static class AdvancedTopologyExample {
    public static void Write(string target) {
        var chart = TopologyChart.Create()
            .WithId("advanced-topology")
            .WithTitle("Application Delivery Path")
            .WithSubtitle("Named ports, endpoint labels, typed details, custom edge styling, and layout hints.")
            .WithViewport(920, 520, 30)
            .WithLayout(TopologyLayoutMode.Layered)
            .WithLegend(null)
            .AddAutoNode("gateway", "API Gateway", TopologyNodeKind.Gateway, TopologyHealthStatus.Healthy, subtitle: "Public ingress")
            .AddAutoNode("service", "Order Service", TopologyNodeKind.Service, TopologyHealthStatus.Warning, subtitle: "Application tier")
            .AddAutoNode("database", "Orders DB", TopologyNodeKind.Database, TopologyHealthStatus.Healthy, subtitle: "Primary writer")
            .AddNodePort("gateway", "https", TopologyEdgePort.Bottom, 0.34, "HTTPS")
            .AddNodePort("service", "grpc-in", TopologyEdgePort.Top, 0.28, "gRPC in")
            .AddNodePort("service", "sql-out", TopologyEdgePort.Bottom, 0.72, "SQL out")
            .AddNodePort("database", "writer", TopologyEdgePort.Top, 0.62, "Writer")
            .AddNodeDetail("service", "Region", "West Europe", TopologyHealthStatus.Healthy)
            .AddNodeDetail("service", "P95", "84 ms", TopologyHealthStatus.Warning)
            .AddNodeDetail("database", "Role", "Primary", TopologyHealthStatus.Healthy)
            .AddEdge("gateway-service", "gateway", "service", "Requests", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgeNamedPorts("gateway-service", "https", "grpc-in")
            .WithEdgeStroke("gateway-service", width: 3.4, opacity: 0.82, dashPattern: new[] { 10.0, 3.0 })
            .WithEdgeMarkers("gateway-service", TopologyMarkerKind.Circle, TopologyMarkerKind.Arrow)
            .WithEdgeEndpointLabels("gateway-service", "443", "gRPC")
            .WithEdgeLayoutHints("gateway-service", preferredLength: 190, minimumRankSpan: 1, routingPriority: 20)
            .AddEdge("service-database", "service", "database", "Queries", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Warning, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgeNamedPorts("service-database", "sql-out", "writer")
            .WithEdgeStroke("service-database", width: 3.8, opacity: 0.74, dashPattern: new[] { 7.0, 3.0, 2.0, 3.0 })
            .WithEdgeMarkers("service-database", TopologyMarkerKind.Diamond, TopologyMarkerKind.Arrow)
            .WithEdgeEndpointLabels("service-database", "SQL", "5432")
            .WithEdgeLayoutHints("service-database", preferredLength: 220, minimumRankSpan: 2, routingPriority: 30)
            .WithAccessibility(accessibility => accessibility.WithTextAlternative("Application delivery topology", "Gateway, service, and database request path with health and endpoint details.", "en"));

        var artifact = chart.ToVisualArtifact();
        var render = new VisualArtifactRenderOptions {
            Topology = new TopologyRenderOptions { IncludeLegend = false, IncludeEndpointLabels = true }.ApplyLayoutPreset(TopologyLayoutPreset.Presentation),
            Raster = new RasterImageOptions { Dpi = 144 }
        };
        var watermark = VisualWatermark.FromText("ARCHITECTURE PREVIEW");
        watermark.Anchor = VisualCanvasAnchor.Center;
        watermark.RotationDegrees = -24;
        watermark.Opacity = 0.075;
        watermark.FontSize = 34;
        watermark.Scale = 1.2;
        render.Watermarks.Add(watermark);

        artifact.SaveSvg(Path.Combine(target, "advanced-topology.svg"), render);
        artifact.SaveHtml(Path.Combine(target, "advanced-topology.html"), render);
        artifact.SavePng(Path.Combine(target, "advanced-topology.png"), render);

        var diagnostic = new VisualArtifactRenderOptions {
            Topology = render.Topology!.Clone()
        };
        diagnostic.Topology.IncludeLayoutDiagnosticOverlay = true;
        artifact.SaveSvg(Path.Combine(target, "advanced-topology-diagnostics.svg"), diagnostic);
    }
}
