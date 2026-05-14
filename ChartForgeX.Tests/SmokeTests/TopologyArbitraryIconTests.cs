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
        Assert(png.Length > 64 && ReadBigEndianInt32(png, 16) > 0 && ReadBigEndianInt32(png, 20) > 0, "Relationship overview topology should render PNG with inline SVG artwork.");

        var rasterizedArtwork = TopologyIconArtwork.InlineSvg("<rect x=\"2\" y=\"2\" width=\"18\" height=\"20\" fill=\"#FF0000\"/><circle cx=\"34\" cy=\"13\" r=\"10\" fill=\"#0000FF\"/><path d=\"M4 34 H40\" fill=\"none\" stroke=\"#00AA00\" stroke-width=\"4\"/>", "0 0 44 44");
        var rasterizedPng = TopologyChart.Create()
            .WithId("png-svg-raster-artwork")
            .WithViewport(160, 120, 10)
            .AddArtworkNode("art", "Art", rasterizedArtwork, 36, 18, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 88, height: 88, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var rasterPixels = ReadPngRgba(rasterizedPng, out _, out _);
        Assert(CountPixelsNear(rasterPixels, 255, 0, 0) > 450, "PNG topology artwork should rasterize caller-supplied inline SVG fills instead of falling back to a generic card.");
        Assert(CountPixelsNear(rasterPixels, 0, 0, 255) > 280, "PNG topology artwork should rasterize non-rectangular inline SVG nodes.");
        Assert(CountPixelsNear(rasterPixels, 0, 170, 0) > 120, "PNG topology artwork should rasterize inline SVG strokes.");

        var clippedArtwork = TopologyIconArtwork.InlineSvg("<defs><clipPath id=\"left\"><rect x=\"0\" y=\"0\" width=\"22\" height=\"44\" fill=\"#00FF00\"/></clipPath></defs><rect x=\"0\" y=\"0\" width=\"44\" height=\"44\" fill=\"#FF0000\" clip-path=\"url(#left)\"/><rect x=\"22\" y=\"0\" width=\"22\" height=\"44\" fill=\"#0000FF\"/>", "0 0 44 44");
        var clippedPng = TopologyChart.Create()
            .WithId("png-svg-raster-clipped-artwork")
            .WithViewport(160, 120, 10)
            .AddArtworkNode("art", "Art", clippedArtwork, 36, 18, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 88, height: 88, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var clippedPixels = ReadPngRgba(clippedPng, out var clippedWidth, out _);
        Assert(CountPixelsNear(clippedPixels, clippedWidth, 36, 18, 44, 88, 255, 0, 0) > 900, "PNG topology artwork should render clipped inline SVG content inside the clip-path region.");
        Assert(CountPixelsNear(clippedPixels, clippedWidth, 80, 18, 44, 88, 255, 0, 0) < 80, "PNG topology artwork should not leak clipped content outside the clip-path region.");
        Assert(CountPixelsNear(clippedPixels, 0, 255, 0) < 80, "PNG topology artwork should not paint clipPath geometry from defs as visible artwork.");
        Assert(CountPixelsNear(clippedPixels, clippedWidth, 80, 18, 44, 88, 0, 0, 255) > 900, "PNG topology artwork should continue rendering unclipped siblings after a clipped element.");

        var maskedArtwork = TopologyIconArtwork.InlineSvg("<defs><mask id=\"left-mask\"><rect x=\"0\" y=\"0\" width=\"44\" height=\"44\" fill=\"#000000\"/><rect x=\"0\" y=\"0\" width=\"22\" height=\"44\" fill=\"#FFFFFF\"/></mask></defs><rect x=\"0\" y=\"0\" width=\"44\" height=\"44\" fill=\"#FF0000\" mask=\"url(#left-mask)\"/><rect x=\"22\" y=\"0\" width=\"22\" height=\"44\" fill=\"#0000FF\"/>", "0 0 44 44");
        var maskedPng = TopologyChart.Create()
            .WithId("png-svg-raster-masked-artwork")
            .WithViewport(160, 120, 10)
            .AddArtworkNode("art", "Art", maskedArtwork, 36, 18, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 88, height: 88, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var maskedPixels = ReadPngRgba(maskedPng, out var maskedWidth, out _);
        Assert(CountPixelsNear(maskedPixels, maskedWidth, 36, 18, 44, 88, 255, 0, 0) > 900, "PNG topology artwork should reveal masked SVG content through white mask regions.");
        Assert(CountPixelsNear(maskedPixels, maskedWidth, 80, 18, 44, 88, 255, 0, 0) < 80, "PNG topology artwork should hide masked SVG content through black mask regions.");
        Assert(CountPixelsNear(maskedPixels, maskedWidth, 36, 18, 88, 88, 255, 255, 255) < 80, "PNG topology artwork should not paint mask geometry from defs as visible artwork.");
        Assert(CountPixelsNear(maskedPixels, maskedWidth, 80, 18, 44, 88, 0, 0, 255) > 900, "PNG topology artwork should continue rendering siblings after a masked element.");

        var useArtwork = TopologyIconArtwork.InlineSvg("<defs><symbol id=\"left-symbol\" viewBox=\"0 0 10 10\"><rect x=\"0\" y=\"0\" width=\"10\" height=\"10\" fill=\"#FF0000\"/></symbol><symbol id=\"right-symbol\" viewBox=\"0 0 10 10\"><circle cx=\"5\" cy=\"5\" r=\"5\" fill=\"#0000FF\"/></symbol></defs><use href=\"#left-symbol\" x=\"0\" y=\"0\" width=\"22\" height=\"44\"/><use href=\"#right-symbol\" x=\"22\" y=\"0\" width=\"22\" height=\"44\"/>", "0 0 44 44");
        var usePng = TopologyChart.Create()
            .WithId("png-svg-raster-use-symbol-artwork")
            .WithViewport(160, 120, 10)
            .AddArtworkNode("art", "Art", useArtwork, 36, 18, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 88, height: 88, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var usePixels = ReadPngRgba(usePng, out var useWidth, out _);
        Assert(CountPixelsNear(usePixels, useWidth, 36, 18, 44, 88, 255, 0, 0) > 900, "PNG topology artwork should expand symbol references through SVG use elements.");
        Assert(CountPixelsNear(usePixels, useWidth, 80, 18, 44, 88, 0, 0, 255) > 520, "PNG topology artwork should place multiple use references with their own viewport.");

        var patternArtwork = TopologyIconArtwork.InlineSvg("<defs><pattern id=\"stripe-source\"><rect x=\"0\" y=\"0\" width=\"11\" height=\"11\" fill=\"#FF0000\"/><rect x=\"5.5\" y=\"0\" width=\"5.5\" height=\"11\" fill=\"#0000FF\"/></pattern><pattern id=\"stripe\" href=\"#stripe-source\" patternUnits=\"userSpaceOnUse\" x=\"0\" y=\"0\" width=\"11\" height=\"11\"/></defs><rect x=\"0\" y=\"0\" width=\"44\" height=\"44\" fill=\"url(#stripe)\"/>", "0 0 44 44");
        var patternPng = TopologyChart.Create()
            .WithId("png-svg-raster-pattern-artwork")
            .WithViewport(160, 120, 10)
            .AddArtworkNode("art", "Art", patternArtwork, 36, 18, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 88, height: 88, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var patternPixels = ReadPngRgba(patternPng, out _, out _);
        Assert(CountPixelsNear(patternPixels, 255, 0, 0) > 1500, "PNG topology artwork should rasterize SVG pattern fills instead of replacing them with a solid fallback.");
        Assert(CountPixelsNear(patternPixels, 0, 0, 255) > 1500, "PNG topology artwork should repeat SVG pattern tiles across filled contours.");

        var textSpanArtwork = TopologyIconArtwork.InlineSvg("<text x=\"8\" y=\"14\" font-size=\"10\" font-weight=\"700\" dominant-baseline=\"middle\"><tspan fill=\"#FF0000\">A</tspan><tspan dx=\"12\" fill=\"#0000FF\">B</tspan><tspan x=\"8\" dy=\"16\" fill=\"#00AA00\">C</tspan></text>", "0 0 44 44");
        var textSpanPng = TopologyChart.Create()
            .WithId("png-svg-raster-text-span-artwork")
            .WithViewport(160, 120, 10)
            .AddArtworkNode("art", "Art", textSpanArtwork, 36, 18, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 88, height: 88, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var textSpanPixels = ReadPngRgba(textSpanPng, out _, out _);
        Assert(CountPixelsNear(textSpanPixels, 255, 0, 0) > 20, "PNG topology artwork should render SVG text spans with inherited positioning.");
        Assert(CountPixelsNear(textSpanPixels, 0, 0, 255) > 20, "PNG topology artwork should apply tspan dx positioning and fill style.");
        Assert(CountPixelsNear(textSpanPixels, 0, 170, 0) > 20, "PNG topology artwork should apply tspan x/dy positioning for multiline labels.");

        var gradientArtwork = TopologyIconArtwork.InlineSvg("<defs><linearGradient id=\"brand-stops\"><stop offset=\"0%\" stop-color=\"#FF0000\"/><stop offset=\"100%\" stop-color=\"#0000FF\"/></linearGradient><linearGradient id=\"brand\" href=\"#brand-stops\" x1=\"0%\" y1=\"0%\" x2=\"100%\" y2=\"0%\"/></defs><rect x=\"0\" y=\"0\" width=\"44\" height=\"44\" fill=\"url(#brand)\"/><svg x=\"11\" y=\"11\" width=\"22\" height=\"22\" viewBox=\"0 0 10 10\"><circle cx=\"5\" cy=\"5\" r=\"5\" fill=\"#00FF00\"/></svg>", "0 0 44 44");
        var gradientPng = TopologyChart.Create()
            .WithId("png-svg-raster-gradient-artwork")
            .WithViewport(160, 120, 10)
            .AddArtworkNode("art", "Art", gradientArtwork, 36, 18, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 88, height: 88, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var gradientPixels = ReadPngRgba(gradientPng, out _, out _);
        Assert(CountPixelsNear(gradientPixels, 255, 0, 0) > 160, "PNG topology artwork should rasterize SVG linearGradient fills from defs.");
        Assert(CountPixelsNear(gradientPixels, 0, 0, 255) > 160, "PNG topology artwork should inherit stops from referenced linearGradient definitions.");
        Assert(CountPixelsNear(gradientPixels, 0, 255, 0) > 520, "PNG topology artwork should apply nested SVG viewBox scaling instead of treating nested SVG as an unscaled group.");

        var transformedGradientArtwork = TopologyIconArtwork.InlineSvg("<defs><linearGradient id=\"turn\" x1=\"0%\" y1=\"0%\" x2=\"100%\" y2=\"0%\" gradientTransform=\"rotate(90 .5 .5)\"><stop offset=\"0%\" stop-color=\"#FF0000\"/><stop offset=\"100%\" stop-color=\"#0000FF\"/></linearGradient></defs><rect x=\"0\" y=\"0\" width=\"44\" height=\"44\" fill=\"url(#turn)\"/>", "0 0 44 44");
        var transformedGradientPng = TopologyChart.Create()
            .WithId("png-svg-raster-transformed-gradient-artwork")
            .WithViewport(160, 120, 10)
            .AddArtworkNode("art", "Art", transformedGradientArtwork, 36, 18, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 88, height: 88, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var transformedGradientPixels = ReadPngRgba(transformedGradientPng, out var transformedGradientWidth, out _);
        Assert(CountPixelsNear(transformedGradientPixels, transformedGradientWidth, 36, 18, 88, 36, 255, 0, 0) > 280, "PNG topology artwork should apply linearGradient gradientTransform before rasterizing.");
        Assert(CountPixelsNear(transformedGradientPixels, transformedGradientWidth, 36, 70, 88, 36, 0, 0, 255) > 280, "PNG topology artwork should preserve transformed linearGradient direction.");

        var repeatedGradientArtwork = TopologyIconArtwork.InlineSvg("<defs><linearGradient id=\"repeat\" x1=\"0%\" y1=\"0%\" x2=\"25%\" y2=\"0%\" spreadMethod=\"repeat\"><stop offset=\"0%\" stop-color=\"#FF0000\"/><stop offset=\"100%\" stop-color=\"#0000FF\"/></linearGradient></defs><rect x=\"0\" y=\"0\" width=\"44\" height=\"44\" fill=\"url(#repeat)\"/>", "0 0 44 44");
        var repeatedGradientPng = TopologyChart.Create()
            .WithId("png-svg-raster-repeated-gradient-artwork")
            .WithViewport(160, 120, 10)
            .AddArtworkNode("art", "Art", repeatedGradientArtwork, 36, 18, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 88, height: 88, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var repeatedGradientPixels = ReadPngRgba(repeatedGradientPng, out _, out _);
        Assert(CountPixelsNear(repeatedGradientPixels, 255, 0, 0) > 520, "PNG topology artwork should support repeated SVG gradient spread.");
        Assert(CountPixelsNear(repeatedGradientPixels, 0, 0, 255) > 520, "PNG topology artwork should keep repeated gradient bands instead of padding only the final stop.");

        var radialArtwork = TopologyIconArtwork.InlineSvg("<defs><radialGradient id=\"spot-stops\"><stop offset=\"0%\" stop-color=\"#FFFFFF\"/><stop offset=\"100%\" stop-color=\"#FF00FF\"/></radialGradient><radialGradient id=\"spot\" href=\"#spot-stops\" cx=\"50%\" cy=\"50%\" r=\"50%\"/></defs><rect x=\"0\" y=\"0\" width=\"44\" height=\"44\" fill=\"url(#spot)\"/>", "0 0 44 44");
        var radialPng = TopologyChart.Create()
            .WithId("png-svg-raster-radial-gradient-artwork")
            .WithViewport(160, 120, 10)
            .AddArtworkNode("art", "Art", radialArtwork, 36, 18, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 88, height: 88, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var radialPixels = ReadPngRgba(radialPng, out _, out _);
        Assert(CountPixelsNear(radialPixels, 255, 255, 255) > 90, "PNG topology artwork should rasterize the center of radialGradient fills.");
        Assert(CountPixelsNear(radialPixels, 255, 0, 255) > 260, "PNG topology artwork should inherit stops from referenced radialGradient definitions.");

        var radialAxisArtwork = TopologyIconArtwork.InlineSvg("<defs><radialGradient id=\"ellipse\" cx=\"50%\" cy=\"50%\" r=\"50%\"><stop offset=\"0%\" stop-color=\"#FFFFFF\"/><stop offset=\"100%\" stop-color=\"#FF00FF\"/></radialGradient></defs><rect x=\"0\" y=\"0\" width=\"80\" height=\"40\" fill=\"url(#ellipse)\"/>", "0 0 80 40");
        var radialAxisPng = TopologyChart.Create()
            .WithId("png-svg-raster-radial-axis-gradient-artwork")
            .WithViewport(180, 120, 10)
            .AddArtworkNode("art", "Art", radialAxisArtwork, 30, 30, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, width: 120, height: 60, symbol: "ART")
            .ToPng(new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        var radialAxisPixels = ReadPngRgba(radialAxisPng, out var radialAxisWidth, out _);
        Assert(CountPixelsNear(radialAxisPixels, radialAxisWidth, 66, 48, 48, 24, 255, 255, 255) > 24, "PNG topology artwork should preserve the center of radial gradients on non-square artwork.");
        Assert(CountPixelsNear(radialAxisPixels, radialAxisWidth, 45, 30, 90, 10, 255, 0, 255) > 120, "PNG topology artwork should rasterize objectBoundingBox radial gradients as ellipses on non-square artwork.");

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

    private static int CountPixelsNear(byte[] rgba, int red, int green, int blue) {
        var count = 0;
        for (var i = 0; i + 3 < rgba.Length; i += 4) {
            if (rgba[i + 3] < 180) continue;
            if (Math.Abs(rgba[i] - red) <= 24 && Math.Abs(rgba[i + 1] - green) <= 24 && Math.Abs(rgba[i + 2] - blue) <= 24) count++;
        }

        return count;
    }

    private static int CountPixelsNear(byte[] rgba, int width, int x, int y, int regionWidth, int regionHeight, int red, int green, int blue) {
        var count = 0;
        for (var yy = Math.Max(0, y); yy < y + regionHeight; yy++) {
            for (var xx = Math.Max(0, x); xx < x + regionWidth; xx++) {
                var index = (yy * width + xx) * 4;
                if (index < 0 || index + 3 >= rgba.Length || rgba[index + 3] < 180) continue;
                if (Math.Abs(rgba[index] - red) <= 24 && Math.Abs(rgba[index + 1] - green) <= 24 && Math.Abs(rgba[index + 2] - blue) <= 24) count++;
            }
        }

        return count;
    }
}
