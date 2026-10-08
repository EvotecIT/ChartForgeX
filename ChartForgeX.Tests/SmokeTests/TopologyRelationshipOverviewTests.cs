using System;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyRelationshipOverviewSupportsMultilineLabels() {
        var chart = TopologyChart.Create()
            .WithId("relationship-overview")
            .WithViewport(1000, 300, 20)
            .WithLegend(TopologyLegend.Create("Links")
                .AddNodeKind("Certificate", TopologyNodeKind.Certificate, "#2563EB", "TLS")
                .AddEdgeKind("Verified", TopologyEdgeKind.CertificateChain, "#16A34A", TopologyEdgeLineStyle.Solid)
                .AddEdgeKind("Risk", TopologyEdgeKind.Mapping, "#EF4444", TopologyEdgeLineStyle.Dotted))
            .AddNode("domain", "ad.evotec.xyz\nPrimary Domain", 380, 96, TopologyNodeKind.Namespace, TopologyHealthStatus.Healthy, subtitle: "Confidence 92%\n24 linked records", width: 240, height: 86, symbol: "D", iconId: "chartforgex-identity-directory:domain")
            .AddNode("cert", "CN: ad.evotec.xyz\nLet's Encrypt R3", 40, 64, TopologyNodeKind.Certificate, TopologyHealthStatus.Healthy, subtitle: "Valid\n62 days left", width: 260, height: 88, symbol: "TLS", iconId: "chartforgex-identity-directory:certificate")
            .AddNode("finding", "Finding Bundle\n3 Critical + 4 High", 730, 64, TopologyNodeKind.Process, TopologyHealthStatus.Critical, subtitle: "TLSv1.0 observed\nEvidence linked", width: 230, height: 88, symbol: "!", color: "#EF4444")
            .AddEdge("cert-domain", "cert", "domain", "Certificate", TopologyEdgeKind.CertificateChain, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "SAN match")
            .AddEdge("domain-finding", "domain", "finding", "Observed", TopologyEdgeKind.Mapping, TopologyHealthStatus.Critical, VisualLinkDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "3 sources")
            .WithEdgeLineStyle("cert-domain", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("domain-finding", TopologyEdgeLineStyle.Dotted)
            .WithEdgeColor("domain-finding", "#DC2626");

        var options = TopologyRenderOptions.FromPreset(TopologyViewPreset.RelationshipOverview).WithSelectedNode("domain");
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-canvas-surface-style=\"PanelGrid\"", StringComparison.Ordinal), "Relationship overview topology should render a dashboard-style canvas surface.");
        Assert(svg.Contains("data-node-surface-style=\"AccentBand\"", StringComparison.Ordinal), "Relationship overview topology should render premium tinted node surfaces.");
        Assert(svg.Contains("data-cfx-role=\"topology-node-accent\"", StringComparison.Ordinal), "Relationship overview topology should render native node accent bands.");
        Assert(chart.Prepare(options).ToInterchangeEnvelope().Edges.Any(edge => edge.ResolvedRoute.Count > 4), "Relationship overview topology should retain sampled rounded orthogonal bends in the common scene and semantic geometry.");
        var markers = TopologyEntity(svg, "edge", "domain-finding").Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-marker").ToArray();
        Assert(markers.Length == 1 && (string?)markers[0].Attribute("fill") == "none" && markers[0].Attribute("d")!.Value.Count(character => character == 'L') == 2, "Relationship overview topology should use a native open chevron with two stroked arms.");
        Assert(svg.Contains("data-edge-color=\"#DC2626\"", StringComparison.Ordinal), "Relationship overview topology should support explicit relationship colors independent from health status.");
        Assert(TopologyEdgeLine(svg, "domain-finding").RenderedColor("stroke").ToHex() == "#DC2626", "Relationship overview edge colors should be used by the route renderer while retaining highlight opacity.");
        Assert(markers[0].RenderedColor("stroke").ToHex() == "#DC2626", "Native direction markers should use the explicit relationship color.");
        Assert(svg.Contains(">Links<", StringComparison.Ordinal), "Relationship overview topology should preserve caller-shaped legends.");
        Assert(TopologyRoleTexts(svg, "legend-label").Length == 3, "The common legend should render all caller-shaped relationship entries.");
        Assert(svg.Contains("stroke-dasharray=\"2 5\"", StringComparison.Ordinal), "Relationship overview legends should render caller-specified dotted line styles.");
        Assert(!svg.Contains("data-legend-kind=\"status\"", StringComparison.Ordinal), "Relationship overview legends should not auto-merge every inferred status when the caller supplied a focused legend.");
        Assert(svg.Contains("data-node-icon-id=\"chartforgex-identity-directory:certificate\"", StringComparison.Ordinal), "Relationship overview topology should keep reusable icon ids in SVG metadata.");
        var certificateLabels = TopologyRoleTexts(TopologyEntity(svg, "node", "cert").ToString(), "topology-node-label").Select(text => text.Value).ToArray();
        Assert(certificateLabels.SequenceEqual(new[] { "CN: ad.evotec.xyz", "Let's Encrypt R3" }), "Roomy topology cards should paint both explicit label lines.");
        var domainSubtitles = TopologyRoleTexts(TopologyEntity(svg, "node", "domain").ToString(), "topology-node-subtitle").Select(text => text.Value).ToArray();
        Assert(domainSubtitles.SequenceEqual(new[] { "Confidence 92%", "24 linked records" }), "Roomy topology cards should paint both explicit subtitle lines.");
        Assert(svg.Contains("data-edge-line-style=\"Dotted\"", StringComparison.Ordinal), "Relationship overview topology should keep typed dotted relationship links.");
        var selectedDomain = TopologyEntity(svg, "node", "domain");
        Assert((string?)selectedDomain.Attribute("data-cfx-selected") == "true"
            && selectedDomain.Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "topology-node-surface" && (double?)element.Attribute("stroke-width") > 2),
            "Relationship overview preset should expose selected records and paint a stronger selection outline.");
        Assert(chart.ToPng(options).Length > 64, "Relationship overview topology should render multiline cards as PNG.");
    }

    private static void TopologyRelationshipOverviewLabelLeadersPointBackToEdges() {
        var chart = TopologyChart.Create()
            .WithId("relationship-label-leaders")
            .WithViewport(460, 240, 20)
            .AddNode("source", "Source", 52, 92, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 112, height: 62)
            .AddNode("target", "Target", 292, 92, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 112, height: 62)
            .AddEdge("source-target", "source", "target", "publishes", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal, "exports")
            .WithEdgeLabelOffset("source-target", 0, -78)
            .WithEdgeColor("source-target", "#2563EB");

        var options = TopologyRenderOptions.FromPreset(TopologyViewPreset.RelationshipOverview);
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-cfx-role=\"topology-edge-label-leader\"", StringComparison.Ordinal), "Displaced relationship overview edge labels should point back to their edge route.");
        Assert(svg.Contains("data-label-leader=\"true\"", StringComparison.Ordinal), "SVG edge label metadata should expose when a leader was rendered.");
        Assert(svg.Contains("data-label-anchor-x=\"", StringComparison.Ordinal) && svg.Contains("data-label-anchor-y=\"", StringComparison.Ordinal), "SVG edge label metadata should expose the edge anchor used by label leaders.");
        Assert(!chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }).Contains("topology-edge-label-leader", StringComparison.Ordinal), "Default topology render options should not add extra label leaders unless requested by a preset or caller.");
        Assert(chart.ToPng(options).Length > 64, "PNG topology output should render displaced label leaders.");
    }

    private static void TopologyRelationshipOverviewLabelLeadersSupportAnnotationAnchors() {
        var chart = TopologyChart.Create()
            .WithId("relationship-label-anchor-node")
            .WithViewport(520, 260, 20)
            .AddNode("source", "Source", 54, 92, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 122, height: 64)
            .AddNode("target", "Target", 330, 92, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 122, height: 64)
            .AddEdge("source-target", "source", "target", "publishes", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal, "exports")
            .WithEdgeLabelOffset("source-target", 0, -86)
            .WithEdgeLabelAnchorNode("source-target", "target");

        var options = TopologyRenderOptions.FromPreset(TopologyViewPreset.RelationshipOverview);
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-label-anchor-node-id=\"target\"", StringComparison.Ordinal), "Displaced edge labels should be able to anchor leader lines to a node boundary.");
        Assert(svg.Contains("data-label-leader=\"true\"", StringComparison.Ordinal), "Node-anchored displaced labels should render leader lines in relationship overview mode.");
        Assert(chart.ToPng(options).Length > 64, "PNG topology output should render node-anchored label leaders.");

        chart.WithEdgeLabelAnchor("source-target", 270, 120);
        svg = chart.ToSvg(options);
        Assert(svg.Contains("data-label-anchor-override=\"true\"", StringComparison.Ordinal), "Displaced edge labels should expose explicit point anchors for annotation-style placement.");
        Assert(svg.Contains("data-label-anchor-x=\"270\"", StringComparison.Ordinal), "Explicit point anchors should drive SVG label-leader metadata.");

        chart.ClearEdgeLabelAnchor("source-target");
        svg = chart.ToSvg(options);
        Assert(svg.Contains("data-label-anchor-override=\"false\"", StringComparison.Ordinal), "Clearing a label anchor should restore route-based leader placement.");
        Assert(!svg.Contains("data-label-anchor-node-id=\"target\"", StringComparison.Ordinal), "Clearing a label anchor should remove the node anchor metadata.");
    }

    private static void TopologyRelationshipOverviewLabelAnchorsFollowLayoutTransforms() {
        var shifted = TopologyChart.Create()
            .WithId("relationship-label-anchor-shift")
            .WithTitle("Shifted Anchor")
            .WithViewport(360, 230, 20)
            .AddNode("source", "Source", 0, 0, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 92, height: 54)
            .AddNode("target", "Target", 180, 0, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, width: 92, height: 54)
            .AddEdge("source-target", "source", "target", "observes", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Straight)
            .WithEdgeLabelOffset("source-target", 0, -64)
            .WithEdgeLabelAnchor("source-target", 100, 30);

        var options = TopologyRenderOptions.FromPreset(TopologyViewPreset.RelationshipOverview);
        var shiftedSvg = shifted.ToSvg(options);
        var shiftedAnchorY = GetAttribute(shiftedSvg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"source-target\"", "data-label-anchor-y");
        Assert(shiftedAnchorY > 90, "Explicit edge-label anchors should shift with normalized chart content.");

        var mirrored = TopologyChart.Create()
            .WithId("relationship-label-anchor-mirror")
            .WithViewport(420, 260, 20)
            .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.RightToLeft)
            .AddAutoNode("source", "Source", TopologyNodeKind.Namespace, TopologyHealthStatus.Healthy, width: 92, height: 54)
            .AddAutoNode("target", "Target", TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy, width: 92, height: 54)
            .AddEdge("source-target", "source", "target", "observes", TopologyEdgeKind.Link, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward, TopologyEdgeRouting.Orthogonal)
            .WithEdgeLabelOffset("source-target", 0, -64)
            .WithEdgeLabelAnchor("source-target", 90, 120);
        mirrored.Nodes[0].Metadata["layer"] = "0";
        mirrored.Nodes[1].Metadata["layer"] = "1";

        var mirroredSvg = mirrored.ToSvg(options);
        var mirroredAnchorX = GetAttribute(mirroredSvg, "data-cfx-role=\"topology-edge-label\" data-edge-id=\"source-target\"", "data-label-anchor-x");
        Assert(mirroredAnchorX > 250, "Explicit edge-label anchors should mirror with right-to-left prepared layouts.");
    }
}
