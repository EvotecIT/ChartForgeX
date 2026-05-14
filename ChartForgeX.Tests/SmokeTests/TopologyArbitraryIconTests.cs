using System;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyRelationshipOverviewRendersArbitraryIconArtwork() {
        var pack = new TopologyIconPack("access-sample", "Access Sample Icons", vendor: "ChartForgeX sample")
            .AddIcon(new TopologyIconDefinition("access-sample", "client", "Client Device", TopologyNodeKind.Endpoint, TopologyIconShape.Desktop) {
                Symbol = "DEV",
                Color = "#334155",
                DisplayMode = TopologyNodeDisplayMode.Tile,
                Artwork = TopologyIconArtwork.InlineSvg("<rect x=\"8\" y=\"10\" width=\"28\" height=\"18\" rx=\"3\" fill=\"#334155\"/><rect x=\"13\" y=\"15\" width=\"18\" height=\"8\" rx=\"1.5\" fill=\"#FFFFFF\"/><rect x=\"18\" y=\"31\" width=\"8\" height=\"3\" rx=\"1\" fill=\"#334155\"/>", "0 0 44 44")
            })
            .AddIcon(new TopologyIconDefinition("access-sample", "gateway", "Gateway", TopologyNodeKind.Gateway, TopologyIconShape.LoadBalancer) {
                Symbol = "GW",
                Color = "#0E7490",
                DisplayMode = TopologyNodeDisplayMode.Pill,
                Artwork = TopologyIconArtwork.InlineSvg("<rect x=\"6\" y=\"13\" width=\"36\" height=\"18\" rx=\"9\" fill=\"#0E7490\"/><path d=\"M15 22 H31 M26 17 L32 22 L26 27\" fill=\"none\" stroke=\"#FFFFFF\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>", "0 0 48 44")
            })
            .AddIcon(new TopologyIconDefinition("access-sample", "destination", "Destination", TopologyNodeKind.Cloud, TopologyIconShape.Application) {
                Symbol = "APP",
                Color = "#6366F1",
                DisplayMode = TopologyNodeDisplayMode.Card,
                Artwork = TopologyIconArtwork.InlineSvg("<path d=\"M24 5 L40 14 V32 L24 41 L8 32 V14 Z\" fill=\"#6366F1\"/><path d=\"M24 15 L32 20 V28 L24 33 L16 28 V20 Z\" fill=\"#FFFFFF\" opacity=\"0.9\"/>", "0 0 48 48")
            });
        var catalog = TopologyIconCatalog.Default().AddPack(pack);
        var backdropArtwork = TopologyIconArtwork.InlineSvg("<path d=\"M38 102 H178 C210 102 232 82 232 56 C232 30 209 12 183 18 C171 3 146 -2 126 8 C110 16 98 29 93 45 C76 39 55 47 47 63 C27 66 12 81 12 99 C12 109 22 114 38 102 Z\" fill=\"#EAF4EE\" stroke=\"#0E7490\" stroke-width=\"3\" opacity=\"0.78\"/>", "0 0 244 124")
            .WithPreserveAspectRatio("none");
        var imageArtwork = TopologyIconArtwork.Image("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADElEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==", "0 0 1 1");

        var chart = TopologyChart.Create()
            .WithId("arbitrary-icon-topology")
            .WithViewport(640, 260, 20)
            .WithLegend(TopologyLegend.Create("Routes")
                .AddNodeKind("Client", TopologyNodeKind.Endpoint, "#334155", "DEV", iconId: "access-sample:client")
                .AddNodeKind("Gateway", TopologyNodeKind.Gateway, "#0E7490", "GW", iconId: "access-sample:gateway")
                .AddEdgeKind("Policy tunnel", TopologyEdgeKind.AuthenticationPath, "#0E7490", TopologyEdgeLineStyle.Dashed))
            .AddNode("client", "Client", 44, 94, TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy, width: 94, height: 70, symbol: "DEV", iconId: "access-sample:client")
            .AddArtworkNode("backdrop", "Cloud Backdrop", backdropArtwork, 180, 42, TopologyNodeKind.Cloud, TopologyHealthStatus.Unknown, width: 260, height: 140, symbol: "CLD")
            .AddNode("gateway", "Tunnel Gateway", 252, 106, TopologyNodeKind.Gateway, TopologyHealthStatus.Healthy, width: 168, height: 34, symbol: "GW", iconId: "access-sample:gateway")
            .AddNode("destination", "SaaS", 500, 78, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, width: 96, height: 92, symbol: "APP", iconId: "access-sample:destination")
            .AddArtworkNode("image-ref", "Image", imageArtwork, 604, 184, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 32, height: 32, symbol: "IMG")
            .AddNode("override", "Override", 604, 132, TopologyNodeKind.Application, TopologyHealthStatus.Healthy, width: 72, height: 52, symbol: "OVR", iconId: "access-sample:destination")
            .AddNode("cleared", "Cleared", 604, 40, TopologyNodeKind.Application, TopologyHealthStatus.Healthy, width: 72, height: 52, symbol: "CLR", iconId: "access-sample:destination")
            .AddNode("unsafe-direct", "Unsafe Direct", 604, 224, TopologyNodeKind.Application, TopologyHealthStatus.Warning, width: 72, height: 52, symbol: "BAD", iconId: "access-sample:destination")
            .AddNode("unsafe-standalone", "Unsafe Standalone", 520, 224, TopologyNodeKind.Application, TopologyHealthStatus.Warning, width: 72, height: 52, symbol: "BAD")
            .AddEdge("client-gateway", "client", "gateway", "policy", TopologyEdgeKind.AuthenticationPath, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .AddEdge("gateway-destination", "gateway", "destination", "route", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .WithNodeDisplay("client", TopologyNodeDisplayMode.Tile)
            .WithNodeDisplay("gateway", TopologyNodeDisplayMode.Pill)
            .WithNodeArtwork("override", imageArtwork, TopologyNodeDisplayMode.Card)
            .WithNodeArtwork("cleared", imageArtwork)
            .WithoutNodeArtwork("cleared")
            .WithEdgeLineStyle("client-gateway", TopologyEdgeLineStyle.Dashed)
            .WithEdgePorts("client-gateway", TopologyEdgePort.Right, TopologyEdgePort.Left)
            .WithEdgePorts("gateway-destination", TopologyEdgePort.Right, TopologyEdgePort.Left);
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, "unsafe-direct", StringComparison.Ordinal)) continue;
            node.Artwork = TopologyIconArtwork.InlineSvg("<script>alert(1)</script>", "0 0 10 10");
        }
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, "unsafe-standalone", StringComparison.Ordinal)) continue;
            node.Artwork = TopologyIconArtwork.InlineSvg("<script>alert(1)</script>", "0 0 10 10");
        }

        var options = TopologyRenderOptions.FromPreset(TopologyViewPreset.RelationshipOverview);
        options.IconCatalog = catalog;

        var svg = chart.ToSvg(options);
        Assert(svg.Contains("data-node-icon-artwork=\"svg\"", StringComparison.Ordinal), "Relationship overview topology should expose arbitrary SVG artwork metadata.");
        Assert(svg.Contains("data-node-icon-artwork=\"image\"", StringComparison.Ordinal), "Relationship overview topology should expose arbitrary image artwork metadata.");
        Assert(svg.Contains("data-node-artwork-source=\"node\"", StringComparison.Ordinal), "Relationship overview topology should expose node-supplied artwork source metadata.");
        Assert(svg.Contains("data-node-artwork-source=\"icon\"", StringComparison.Ordinal), "Relationship overview topology should expose catalog-supplied artwork source metadata.");
        Assert(svg.Contains("data-cfx-role=\"topology-icon-artwork\"", StringComparison.Ordinal), "Relationship overview topology should embed arbitrary icon artwork in SVG output.");
        Assert(svg.Contains("<rect x=\"8\" y=\"10\" width=\"28\"", StringComparison.Ordinal), "Relationship overview topology should render caller-supplied endpoint artwork.");
        Assert(svg.Contains("data-node-id=\"backdrop\"", StringComparison.Ordinal) && svg.Contains("data-node-display-mode=\"Artwork\"", StringComparison.Ordinal), "Relationship overview topology should support full-bounds artwork nodes.");
        Assert(svg.Contains("width=\"260\" height=\"140\" viewBox=\"0 0 244 124\"", StringComparison.Ordinal), "Artwork display nodes should scale trusted SVG artwork to the full node bounds.");
        Assert(svg.Contains("preserveAspectRatio=\"none\"", StringComparison.Ordinal), "Artwork display nodes should preserve caller-supplied preserveAspectRatio values.");
        Assert(svg.Contains("href=\"data:image/png;base64,", StringComparison.Ordinal) && svg.Contains("width=\"32\" height=\"32\"", StringComparison.Ordinal), "Artwork display nodes should embed host-managed image href artwork.");
        var overrideNodeTag = TopologyNodeStartTag(svg, "arbitrary-icon-topology", "override");
        Assert(overrideNodeTag.Contains("data-node-icon-id=\"access-sample:destination\"", StringComparison.Ordinal) && overrideNodeTag.Contains("data-node-artwork-source=\"node\"", StringComparison.Ordinal), "Node-supplied artwork should override catalog artwork while preserving icon metadata.");
        var clearedNodeTag = TopologyNodeStartTag(svg, "arbitrary-icon-topology", "cleared");
        Assert(clearedNodeTag.Contains("data-node-icon-id=\"access-sample:destination\"", StringComparison.Ordinal) && clearedNodeTag.Contains("data-node-artwork-source=\"icon\"", StringComparison.Ordinal), "Clearing node-supplied artwork should fall back to catalog artwork metadata.");
        Assert(!clearedNodeTag.Contains("data-node-artwork-source=\"node\"", StringComparison.Ordinal), "Clearing node-supplied artwork should remove the direct artwork override.");
        var unsafeNodeTag = TopologyNodeStartTag(svg, "arbitrary-icon-topology", "unsafe-direct");
        Assert(unsafeNodeTag.Contains("data-node-icon-id=\"access-sample:destination\"", StringComparison.Ordinal) && unsafeNodeTag.Contains("data-node-artwork-source=\"icon\"", StringComparison.Ordinal), "Unsafe direct artwork should not be advertised and should fall back to safe catalog artwork metadata.");
        var unsafeStandaloneNodeTag = TopologyNodeStartTag(svg, "arbitrary-icon-topology", "unsafe-standalone");
        Assert(!unsafeStandaloneNodeTag.Contains("data-node-display-mode=\"Artwork\"", StringComparison.Ordinal) && !unsafeStandaloneNodeTag.Contains("data-node-artwork-source=", StringComparison.Ordinal), "Unsafe direct artwork without a catalog fallback should not force artwork display metadata.");
        Assert(!svg.Contains("<script", StringComparison.OrdinalIgnoreCase), "Unsafe direct topology artwork should not be embedded in SVG output.");
        Assert(!svg.Contains("data-node-icon-id=\"access-sample:cloud-backdrop\"", StringComparison.Ordinal), "Node-specific artwork should not require a catalog icon id.");
        Assert(svg.Contains("data-node-display-mode=\"Pill\"", StringComparison.Ordinal), "Relationship overview topology should preserve icon-defined or caller-defined pill gateway nodes.");
        Assert(svg.Contains("stroke-dasharray", StringComparison.Ordinal), "Relationship overview topology should preserve dashed policy-tunnel styling.");
        var htmlOptions = TopologyRenderOptions.FromPreset(TopologyViewPreset.RelationshipOverview);
        htmlOptions.IconCatalog = catalog;
        htmlOptions.EnableHtmlInteractions = true;
        var html = chart.ToHtmlPage(htmlOptions);
        Assert(html.Contains("artworkSource: attr(element, 'data-node-artwork-source')", StringComparison.Ordinal), "Interactive topology payloads should include artwork source metadata.");
        Assert(html.Contains("obstacleCount: attr(element, 'data-route-obstacle-count')", StringComparison.Ordinal), "Interactive topology payloads should include route obstacle count metadata.");
        AssertThrows<ArgumentException>(() => chart.WithNodeArtwork("client", TopologyIconArtwork.SvgFile("icons/client.svg")), "Node artwork helper should reject non-embeddable sidecar SVG references.");
        AssertThrows<ArgumentException>(() => chart.WithNodeArtwork("client", TopologyIconArtwork.Image("javascript:alert(1)")), "Node artwork helper should reject unsafe image hrefs.");
        AssertThrows<ArgumentException>(() => TopologyIconArtwork.InlineSvg("<path/>").WithPreserveAspectRatio(" "), "Artwork preserveAspectRatio helpers should reject empty values.");
        AssertThrows<ArgumentException>(() => TopologyChart.Create().AddArtworkNode("bad", "Bad", TopologyIconArtwork.SvgFile("icons/bad.svg"), 0, 0), "Artwork node helper should reject non-embeddable sidecar SVG references.");
        var png = chart.ToPng(options);
        Assert(png.Length > 64 && ReadBigEndianInt32(png, 16) > 0 && ReadBigEndianInt32(png, 20) > 0, "Relationship overview topology should still render PNG with deterministic fallback glyphs.");

        var artworkFallbackPng = TopologyChart.Create()
            .WithId("png-artwork-fallback")
            .WithViewport(220, 150, 12)
            .AddArtworkNode("cloud", "Cloud", backdropArtwork, 34, 32, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, width: 150, height: 80, symbol: "CLD")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false });
        var cardFallbackPng = TopologyChart.Create()
            .WithId("png-artwork-fallback")
            .WithViewport(220, 150, 12)
            .AddNode("cloud", "Cloud", 34, 32, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, width: 150, height: 80, symbol: "CLD")
            .WithNodeDisplay("cloud", TopologyNodeDisplayMode.Card)
            .ToPng(new TopologyRenderOptions { IncludeLegend = false });
        Assert(!artworkFallbackPng.SequenceEqual(cardFallbackPng), "PNG artwork nodes should use a deterministic artwork fallback surface distinct from card nodes.");

        var autoChart = TopologyChart.Create()
            .WithId("auto-artwork-topology")
            .WithLayout(TopologyLayoutMode.Layered)
            .AddAutoArtworkNode("auto-art", "Auto Artwork", backdropArtwork, TopologyNodeKind.Cloud, TopologyHealthStatus.Healthy, width: 112, height: 72, symbol: "ART")
            .AddAutoNode("auto-target", "Target", TopologyNodeKind.Application, TopologyHealthStatus.Healthy, width: 96, height: 64, symbol: "APP")
            .AddEdge("auto-route", "auto-art", "auto-target", "auto", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, TopologyDirection.Forward);
        var autoSvg = autoChart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        Assert(autoSvg.Contains("data-layout-mode=\"Layered\"", StringComparison.Ordinal), "Auto artwork nodes should participate in deterministic layout modes.");
        Assert(autoSvg.Contains("data-node-id=\"auto-art\"", StringComparison.Ordinal) && autoSvg.Contains("data-node-artwork-source=\"node\"", StringComparison.Ordinal), "Auto artwork nodes should preserve node-supplied artwork metadata after layout.");
        Assert(autoSvg.Contains("width=\"112\" height=\"72\" viewBox=\"0 0 244 124\"", StringComparison.Ordinal), "Auto artwork nodes should render full-bounds SVG artwork after layout.");

        var inferredBackdropChart = TopologyChart.Create()
            .WithId("inferred-artwork-route")
            .WithViewport(360, 160, 20)
            .AddNode("source", "Source", 36, 62, TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy, width: 54, height: 38, symbol: "S")
            .AddNode("backdrop", "Backdrop", 112, 24, TopologyNodeKind.Cloud, TopologyHealthStatus.Unknown, width: 140, height: 112, symbol: "BG")
            .AddNode("target", "Target", 280, 62, TopologyNodeKind.Application, TopologyHealthStatus.Healthy, width: 54, height: 38, symbol: "T")
            .AddEdge("through-backdrop", "source", "target", null, TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        foreach (var node in inferredBackdropChart.Nodes) {
            if (!string.Equals(node.Id, "backdrop", StringComparison.Ordinal)) continue;
            node.Artwork = backdropArtwork;
        }
        var inferredBackdropSvg = inferredBackdropChart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        var inferredBackdropNodeTag = TopologyNodeStartTag(inferredBackdropSvg, "inferred-artwork-route", "backdrop");
        var inferredBackdropEdgeTag = TopologyEdgeStartTag(inferredBackdropSvg, "inferred-artwork-route", "through-backdrop");
        Assert(inferredBackdropNodeTag.Contains("data-node-display-mode=\"Artwork\"", StringComparison.Ordinal), "Safe direct node artwork should infer artwork display mode without requiring fluent helpers.");
        Assert(inferredBackdropEdgeTag.Contains("data-route-obstacle-count=\"0\"", StringComparison.Ordinal), "Inferred artwork backdrops should not reserve edge-route obstacles.");

        var cardArtworkObstacleChart = TopologyChart.Create()
            .WithId("card-artwork-route")
            .WithViewport(360, 160, 20)
            .AddNode("source", "Source", 36, 62, TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy, width: 54, height: 38, symbol: "S")
            .AddNode("card", "Card", 136, 38, TopologyNodeKind.Application, TopologyHealthStatus.Healthy, width: 88, height: 78, symbol: "C")
            .AddNode("target", "Target", 280, 62, TopologyNodeKind.Application, TopologyHealthStatus.Healthy, width: 54, height: 38, symbol: "T")
            .AddEdge("around-card", "source", "target", null, TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, TopologyDirection.Forward, TopologyEdgeRouting.ObstacleAvoidingOrthogonal)
            .WithNodeArtwork("card", imageArtwork, TopologyNodeDisplayMode.Card);
        var cardArtworkSvg = cardArtworkObstacleChart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        var cardArtworkEdgeTag = TopologyEdgeStartTag(cardArtworkSvg, "card-artwork-route", "around-card");
        Assert(cardArtworkEdgeTag.Contains("data-route-obstacle-count=\"1\"", StringComparison.Ordinal), "Card-mode artwork nodes should still reserve edge-route obstacles.");
    }

    private static string TopologyNodeStartTag(string svg, string chartId, string nodeId) {
        var marker = "id=\"" + chartId + "-node-" + nodeId + "\"";
        var start = svg.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        while (start > 0 && svg[start] != '<') start--;
        var end = svg.IndexOf('>', start);
        return end < 0 ? string.Empty : svg.Substring(start, end - start + 1);
    }

    private static string TopologyEdgeStartTag(string svg, string chartId, string edgeId) {
        var marker = "id=\"" + chartId + "-edge-" + edgeId + "\"";
        var start = svg.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        while (start > 0 && svg[start] != '<') start--;
        var end = svg.IndexOf('>', start);
        return end < 0 ? string.Empty : svg.Substring(start, end - start + 1);
    }
}
