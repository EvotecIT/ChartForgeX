using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidC4BoundaryStyleRegressionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BorderOnlyBoundaryStylePaintsTheBorderWithoutRetintingTheSurface(bool dark) {
        const string source = "C4Context\nSystem_Boundary(scope, \"Scope\") {\nPerson(user, \"User\")\n}";
        var options = new MermaidTopologyRenderOptions { Presentation = new MermaidPresentationOptions {
            ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family } };
        var parser = new MermaidParser();
        var baseline = parser.ParseC4(source).Document!.ToTopologyChart(options);
        var styled = parser.ParseC4(source + "\nUpdateElementStyle(scope, $borderColor=\"#ff0000\")").Document!.ToTopologyChart(options);
        var originalGroup = Assert.Single(baseline.Groups); var group = Assert.Single(styled.Groups);
        Assert.Equal(originalGroup.Color, group.Color);
        Assert.Equal(originalGroup.BackgroundColor, group.BackgroundColor);
        Assert.Equal("#ff0000", group.BorderColor);
        var original = baseline.Prepare(); var prepared = styled.Prepare();
        var originalSvg = original.ToSvg(); var svg = prepared.ToSvg();
        var originalSurface = Surface(originalSvg); var surface = Surface(svg);
        Assert.Equal((string?)originalSurface.Attribute("fill"), (string?)surface.Attribute("fill"));
        Assert.Equal("#ff0000", ((string?)surface.Attribute("stroke"))?.ToLowerInvariant());
        Assert.Equal("#ff0000", Assert.Single(prepared.ToInterchangeEnvelope().Groups).BorderColor);
        Assert.Equal((original.Width, original.Height), (prepared.Width, prepared.Height));
        foreach (var attribute in new[] { "x", "y", "width", "height", "rx", "stroke-width" })
            Assert.Equal((string?)originalSurface.Attribute(attribute), (string?)surface.Attribute(attribute));
        var originalNative = Assert.Single(original.Visual.Scene.Nodes.OfType<VisualSceneRectangle>(), item => item.Role == "topology-group-surface");
        var native = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneRectangle>(), item => item.Role == "topology-group-surface");
        Assert.Equal(originalNative.Fill, native.Fill);
        Assert.Equal(ChartColor.Parse("#ff0000"), native.Stroke!.Value);

        var baselinePng = original.ToPng(); var png = prepared.ToPng();
        Capture(dark, "baseline", originalSvg, baselinePng);
        Capture(dark, "authored", svg, png);
        AssertBorderPaintDelta(RasterImageDecoder.Decode(baselinePng), RasterImageDecoder.Decode(png), surface, "native PNG");

        // SVG rasterization separately checks the same authored border through the exported artifact.
        var baselineSvgRaster = SvgRasterizer.Rasterize(originalSvg, strict: true).Image;
        var svgRaster = SvgRasterizer.Rasterize(svg, strict: true).Image;
        Capture(dark, "baseline-strict-svg", originalSvg, PngWriter.WriteRgba(baselineSvgRaster));
        Capture(dark, "authored-strict-svg", svg, PngWriter.WriteRgba(svgRaster));
        AssertBorderPaintDelta(baselineSvgRaster, svgRaster, surface, "strict SVG rasterization");
    }

    private static XElement Surface(string svg) => Assert.Single(XDocument.Parse(svg).Descendants(),
        item => (string?)item.Attribute("data-cfx-role") == "topology-group-surface");

    private static void AssertBorderPaintDelta(RgbaImage baseline, RgbaImage styled, XElement surface, string backend) {
        Assert.Equal((baseline.Width, baseline.Height), (styled.Width, styled.Height));
        double Number(string name) => double.Parse(surface.Attribute(name)!.Value, CultureInfo.InvariantCulture);
        var left = Number("x"); var top = Number("y");
        var right = left + Number("width"); var bottom = top + Number("height");
        var radius = Number("rx"); var strokeWidth = Number("stroke-width");
        var margin = Math.Max(radius, strokeWidth) + 2;
        var visibleRedChanges = 0; var interiorChanges = 0;
        // The bottom straight edge is clear of the fixture's header, corners, and contained node.
        // Coverage is allowed to blend the authored opaque red with the unchanged underlying paint.
        for (var y = Math.Max(0, (int)Math.Floor(bottom - strokeWidth / 2 - 1)); y < Math.Min(styled.Height, (int)Math.Ceiling(bottom + strokeWidth / 2 + 1)); y++)
            for (var x = Math.Max(0, (int)Math.Ceiling(left + margin)); x < Math.Min(styled.Width, (int)Math.Floor(right - margin)); x++) {
                var index = (y * styled.Width + x) * 4;
                var before = baseline.Pixels[index] - Math.Max(baseline.Pixels[index + 1], baseline.Pixels[index + 2]);
                var after = styled.Pixels[index] - Math.Max(styled.Pixels[index + 1], styled.Pixels[index + 2]);
                if (styled.Pixels[index + 3] > 0 && after >= 32 && after - before >= 32) visibleRedChanges++;
            }
        for (var y = Math.Max(0, (int)Math.Ceiling(top + margin)); y < Math.Min(styled.Height, (int)Math.Floor(bottom - margin)); y++)
            for (var x = Math.Max(0, (int)Math.Ceiling(left + margin)); x < Math.Min(styled.Width, (int)Math.Floor(right - margin)); x++) {
                var index = (y * styled.Width + x) * 4;
                if (baseline.Pixels[index] != styled.Pixels[index] || baseline.Pixels[index + 1] != styled.Pixels[index + 1]
                    || baseline.Pixels[index + 2] != styled.Pixels[index + 2] || baseline.Pixels[index + 3] != styled.Pixels[index + 3]) interiorChanges++;
            }
        Assert.True(visibleRedChanges > 10, $"The authored border must visibly redden the unchanged bottom border in {backend}; observed {visibleRedChanges} pixels.");
        Assert.True(interiorChanges == 0, $"A border-only change must preserve the inset surface and its contents in {backend}; observed {interiorChanges} changed interior pixels.");
    }

    private static void Capture(bool dark, string variant, string svg, byte[] png) {
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var path = Path.Combine(captures, "c4-border-only-" + (dark ? "dark" : "light") + "-" + variant);
        File.WriteAllText(path + ".svg", svg);
        File.WriteAllBytes(path + ".png", png);
    }
}
