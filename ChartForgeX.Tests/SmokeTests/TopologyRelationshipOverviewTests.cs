using System;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyRelationshipOverviewSupportsMultilineLabels() {
        var chart = TopologyChart.Create()
            .WithId("relationship-overview")
            .WithViewport(620, 280, 20)
            .WithLegend(TopologyLegend.Create("Links")
                .AddNodeKind("Certificate", TopologyNodeKind.Certificate, "#2563EB", "TLS")
                .AddEdgeKind("Verified", TopologyEdgeKind.CertificateChain, "#16A34A", TopologyEdgeLineStyle.Solid)
                .AddEdgeKind("Risk", TopologyEdgeKind.Mapping, "#EF4444", TopologyEdgeLineStyle.Dotted))
            .AddNode("domain", "ad.evotec.xyz\nPrimary Domain", 240, 96, TopologyNodeKind.Namespace, TopologyHealthStatus.Healthy, subtitle: "Confidence 92%\n24 linked records", width: 168, height: 86, symbol: "D", iconId: "chartforgex-identity-directory:domain")
            .AddNode("cert", "CN: ad.evotec.xyz\nLet's Encrypt R3", 40, 64, TopologyNodeKind.Certificate, TopologyHealthStatus.Healthy, subtitle: "Valid\n62 days left", width: 172, height: 88, symbol: "TLS", iconId: "chartforgex-identity-directory:certificate")
            .AddNode("finding", "Finding Bundle\n3 Critical + 4 High", 430, 64, TopologyNodeKind.Process, TopologyHealthStatus.Critical, subtitle: "TLSv1.0 observed\nEvidence linked", width: 174, height: 88, symbol: "!", color: "#EF4444")
            .AddEdge("cert-domain", "cert", "domain", "Certificate", TopologyEdgeKind.CertificateChain, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "SAN match")
            .AddEdge("domain-finding", "domain", "finding", "Observed", TopologyEdgeKind.Mapping, TopologyHealthStatus.Critical, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal, "3 sources")
            .WithEdgeLineStyle("cert-domain", TopologyEdgeLineStyle.Dashed)
            .WithEdgeLineStyle("domain-finding", TopologyEdgeLineStyle.Dotted)
            .WithEdgeColor("domain-finding", "#DC2626");

        var options = TopologyRenderOptions.FromPreset(TopologyViewPreset.RelationshipOverview).WithSelectedNode("domain");
        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-canvas-surface-style=\"PanelGrid\"", StringComparison.Ordinal), "Relationship overview topology should render a dashboard-style canvas surface.");
        Assert(svg.Contains("data-node-surface-style=\"AccentBand\"", StringComparison.Ordinal), "Relationship overview topology should render premium tinted node surfaces.");
        Assert(svg.Contains("data-cfx-role=\"topology-node-accent-band\"", StringComparison.Ordinal), "Relationship overview topology should render node accent bands.");
        Assert(svg.Contains(" Q ", StringComparison.Ordinal), "Relationship overview topology should round orthogonal edge bends.");
        Assert(svg.Contains("M 2.2 1.6 L 7.4 5 L 2.2 8.4", StringComparison.Ordinal), "Relationship overview topology should use the polished chevron marker style.");
        Assert(svg.Contains("data-edge-color=\"#DC2626\"", StringComparison.Ordinal), "Relationship overview topology should support explicit relationship colors independent from health status.");
        Assert(svg.Contains("stroke=\"#DC2626\"", StringComparison.Ordinal), "Relationship overview edge colors should be used by the route renderer.");
        Assert(svg.Contains("marker-end=\"url(#relationship-overview-arrow-dc2626)\"", StringComparison.Ordinal), "Relationship overview direction markers should use the rendered edge color instead of only health status.");
        Assert(svg.Contains(">Links<", StringComparison.Ordinal), "Relationship overview topology should preserve caller-shaped legends.");
        Assert(svg.Contains("dominant-baseline=\"central\"", StringComparison.Ordinal), "Topology legend and fallback glyph symbols should use centered text baselines.");
        Assert(svg.Contains("stroke-dasharray=\"2 5\"", StringComparison.Ordinal), "Relationship overview legends should render caller-specified dotted line styles.");
        Assert(!svg.Contains("data-legend-kind=\"status\"", StringComparison.Ordinal), "Relationship overview legends should not auto-merge every inferred status when the caller supplied a focused legend.");
        Assert(svg.Contains("data-node-icon-id=\"chartforgex-identity-directory:certificate\"", StringComparison.Ordinal), "Relationship overview topology should keep reusable icon ids in SVG metadata.");
        Assert(svg.Contains(">CN: ad.evotec.xyz<", StringComparison.Ordinal), "Topology node labels should preserve the first explicit label line.");
        Assert(svg.Contains("Encrypt R3", StringComparison.Ordinal), "Topology node labels should render explicit second label lines.");
        Assert(svg.Contains(">Confidence 92%<", StringComparison.Ordinal), "Topology node subtitles should preserve the first explicit subtitle line.");
        Assert(svg.Contains(">24 linked records<", StringComparison.Ordinal), "Topology node subtitles should render explicit second subtitle lines.");
        Assert(svg.Contains("data-edge-line-style=\"Dotted\"", StringComparison.Ordinal), "Relationship overview topology should keep typed dotted relationship links.");
        Assert(svg.Contains("cfx-topology--selected", StringComparison.Ordinal), "Relationship overview preset should still support selected record highlighting.");
        Assert(chart.ToPng(options).Length > 64, "Relationship overview topology should render multiline cards as PNG.");
    }
}
