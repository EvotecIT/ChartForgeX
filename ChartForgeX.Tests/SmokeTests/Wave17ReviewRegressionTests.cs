using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgRasterSymbolsPreserveFractionalViewportsAndOverflowClipping() {
        const string fractional = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 1 1'><defs><symbol id='fractional' viewBox='0 0 1 1' preserveAspectRatio='none'><rect width='1' height='1' fill='#16a34a'/></symbol></defs><use href='#fractional' x='.5' y='.5' width='.25' height='.25'/></svg>";
        var fractionalImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(fractional));
        Assert(IsPixelNear(fractionalImage.Pixels, 100, 60, 60, 22, 163, 74) && PixelAlpha(fractionalImage.Pixels, 100, 80, 60) == 0, "Fractional symbol viewports should retain positive scale under a larger outer viewBox.");

        const string clipped = "<svg xmlns='http://www.w3.org/2000/svg' width='80' height='40'><defs><symbol id='clipped' viewBox='0 0 20 20' preserveAspectRatio='none'><rect x='-10' width='40' height='20' fill='#ef4444'/></symbol></defs><use href='#clipped' x='20' y='10' width='20' height='20'/></svg>";
        var clippedImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(clipped));
        Assert(PixelAlpha(clippedImage.Pixels, 80, 15, 20) == 0 && IsPixelNear(clippedImage.Pixels, 80, 25, 20, 239, 68, 68) && PixelAlpha(clippedImage.Pixels, 80, 45, 20) == 0, "Instantiated symbols should clip overflowing paint to their use viewport by default.");

        const string visible = "<svg xmlns='http://www.w3.org/2000/svg' width='80' height='40'><defs><symbol id='visible' viewBox='0 0 20 20' preserveAspectRatio='none' overflow='visible'><rect x='-10' width='40' height='20' fill='#2563eb'/></symbol></defs><use href='#visible' x='20' y='10' width='20' height='20'/></svg>";
        var visibleImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(visible));
        Assert(IsPixelNear(visibleImage.Pixels, 80, 15, 20, 37, 99, 235) && IsPixelNear(visibleImage.Pixels, 80, 45, 20, 37, 99, 235), "A symbol with explicit overflow visible should expose paint beyond its use viewport.");

        const string symbolClipPath = "<svg xmlns='http://www.w3.org/2000/svg' width='80' height='40'><defs><symbol id='clip-symbol' viewBox='0 0 20 20' preserveAspectRatio='none'><rect x='-10' width='40' height='20'/></symbol><clipPath id='clip'><use href='#clip-symbol' x='20' y='10' width='20' height='20'/></clipPath></defs><rect width='80' height='40' fill='#f97316' clip-path='url(#clip)'/></svg>";
        var symbolClipPathImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(symbolClipPath));
        Assert(PixelAlpha(symbolClipPathImage.Pixels, 80, 15, 20) == 0 && IsPixelNear(symbolClipPathImage.Pixels, 80, 25, 20, 249, 115, 22) && PixelAlpha(symbolClipPathImage.Pixels, 80, 45, 20) == 0, "Symbols expanded inside clip paths should enforce the same default viewport clipping as painted uses.");

        const string transformed = "<svg xmlns='http://www.w3.org/2000/svg' width='80' height='40'><defs><symbol id='moved' viewBox='0 0 10 10' preserveAspectRatio='none' transform='translate(20 0)'><rect width='10' height='10' fill='#a855f7'/></symbol></defs><use href='#moved' x='10' y='10' width='20' height='20'/></svg>";
        var transformedImage = RasterImageDecoder.Decode(SvgRasterizer.ToPng(transformed));
        Assert(PixelAlpha(transformedImage.Pixels, 80, 15, 20) == 0 && IsPixelNear(transformedImage.Pixels, 80, 35, 20, 168, 85, 247) && PixelAlpha(transformedImage.Pixels, 80, 55, 20) == 0, "A referenced symbol transform should move its viewport clip together with its painted content.");
    }

    private static void TopologyCurvedEndpointLabelsFollowRenderedRouteTangents() {
        var chart = TopologyChart.Create()
            .WithId("curved-endpoint-tangents")
            .WithViewport(420, 220, 20)
            .WithLegend(null)
            .AddNode("source", "Source", 40, 90, width: 60, height: 40)
            .AddNode("target", "Target", 320, 90, width: 60, height: 40)
            .AddEdge("curve", "source", "target", routing: TopologyEdgeRouting.Curved)
            .WithEdgeEndpointLabels("curve", "out", "in");
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = false };
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var edge = chart.Edges.Single();
        var points = TopologyRenderPrimitives.EdgePoints(chart, edge, nodes);
        var rendered = TopologyRenderPrimitives.RenderedEdgeSamplePoints(chart, edge, nodes, points);
        var expectedSource = TopologyRenderPrimitives.EdgeEndpointLabelPoint(rendered[0], rendered[1]);
        var expectedTarget = TopologyRenderPrimitives.EdgeEndpointLabelPoint(rendered[rendered.Count - 1], rendered[rendered.Count - 2]);
        Assert(rendered.Count > 2 && rendered[rendered.Count / 2].Y < points[0].Y - 20, "Standard curved topology routes should expose sampled raster geometry instead of collapsing to their straight chord.");
        var diagnostics = TopologyLayoutDiagnostics.Analyze(chart, options).Edges.Single();
        Assert(diagnostics.Points.Count == rendered.Count && diagnostics.Points[diagnostics.Points.Count / 2].Y < diagnostics.Points[0].Y - 20, "Public topology diagnostics should expose the sampled rendered curve rather than raw endpoints or control polygons.");

        var document = XDocument.Parse(chart.ToSvg(options));
        var labels = document.Descendants().Where(element => string.Equals((string?)element.Attribute("data-cfx-role"), "topology-edge-endpoint-label", StringComparison.Ordinal)).ToDictionary(element => (string)element.Attribute("data-endpoint")!, StringComparer.Ordinal);
        var sourceX = double.Parse(labels["source"].Attribute("x")!.Value, CultureInfo.InvariantCulture);
        var sourceY = double.Parse(labels["source"].Attribute("y")!.Value, CultureInfo.InvariantCulture);
        var targetX = double.Parse(labels["target"].Attribute("x")!.Value, CultureInfo.InvariantCulture);
        var targetY = double.Parse(labels["target"].Attribute("y")!.Value, CultureInfo.InvariantCulture);
        Assert(Math.Abs(sourceX - expectedSource.X) < 0.01 && Math.Abs(sourceY - expectedSource.Y) < 0.01 && Math.Abs(targetX - expectedTarget.X) < 0.01 && Math.Abs(targetY - expectedTarget.Y) < 0.01, "SVG endpoint labels should use the same sampled curve tangents as raster routes and motion planning.");

        var png = RasterImageDecoder.Decode(chart.ToPng(options));
        var midpoint = rendered[rendered.Count / 2];
        Assert(CountAlphaInRect(png.Pixels, png.Width, (int)Math.Round(midpoint.X) - 3, (int)Math.Round(midpoint.Y) - 3, 7, 7) > 0, "PNG topology output should paint standard curved routes at the shared sampled midpoint.");
    }
}
