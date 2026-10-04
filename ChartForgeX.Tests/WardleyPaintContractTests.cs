using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WardleyPaintContractTests {
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(64, 39, 36)]
    [InlineData(255, 155, 145)]
    public void StrategyAndAxisAccentsMultiplyAuthoredAlpha(byte sourceAlpha, byte accentAlpha, byte pipelineAlpha) {
        foreach (var density in new[] { 1, 2 }) {
            var (svg, png) = RendererGeometryFixture.Render("wardley-contract", sourceAlpha, density);
            var root = XDocument.Parse(svg);
            foreach (var role in new[] { "wardley-strategy", "wardley-link" }) {
                var element = Role(root, role);
                Assert.Equal(ChartColor.FromRgba(30, 80, 170, accentAlpha).ToCss(), (string?)element.Attribute("stroke"));
            }
            Assert.Equal(ChartColor.FromRgba(30, 80, 170, pipelineAlpha).ToCss(), (string?)Role(root, "wardley-pipeline").Attribute("stroke"));
            var strategy = Role(root, "wardley-strategy");
            // The top of the outer ring is isolated from the component, pipeline and axes.
            var ringInk = VerticalAlpha(png, Number(strategy, "cx"), Number(strategy, "cy") - Number(strategy, "r"), density);
            Assert.InRange(ringInk, 0, accentAlpha * density + 2);
            if (sourceAlpha == 0) Assert.All(png.Pixels.Where((_, index) => index % 4 == 3), value => Assert.Equal(0, value));
        }
    }

    [Theory]
    [InlineData("wardley-contract")]
    [InlineData("wardley-contract-narrow")]
    [InlineData("wardley-contract-wide")]
    public void PipelineDashesStartAfterTheTopLeftCorner(string family) {
        foreach (var density in new[] { 1, 2 }) {
            var (svg, png) = RendererGeometryFixture.Render(family, 255, density);
            var rect = Role(XDocument.Parse(svg), "wardley-pipeline");
            var start = Number(rect, "x") + Number(rect, "rx");
            var top = Number(rect, "y");
            // SVG's first 4-unit dash begins here, followed by a 4-unit gap, independent of perimeter length.
            Assert.True(VerticalAlpha(png, start + 1.5, top, density) > 100 * density, family + ": first dash missing");
            Assert.Equal(0, VerticalAlpha(png, start + 6, top, density));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void SvgAndNativeRoundedRectStrokesUseTheTopEdgeDashOrigin(int radius) {
        foreach (var density in new[] { 1, 2 }) {
            var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='100' height='70'><rect x='10' y='10' width='77' height='44' rx='{radius}' fill='none' stroke='#fff' stroke-width='1' stroke-dasharray='4 4'/></svg>";
            var raster = SvgRasterizer.Rasterize(svg, 100 * density, 70 * density);
            Assert.Empty(raster.Diagnostics);
            var canvas = new RgbaCanvas(100, 70, 4, null, density);
            canvas.StrokeRoundedRectDashed(9.5, 9.5, 78, 45, radius == 0 ? 0 : radius + 0.5, ChartColor.White, 1, 4, 4);
            foreach (var image in new[] { raster.Image, canvas.ToImage() }) {
                var ink = VerticalAlpha(image, 10 + radius + 1.5, 10, density);
                Assert.True(ink > 240 * density, $"radius {radius}, density {density}, SVG {image.Pixels == raster.Image.Pixels}, ink {ink}");
                Assert.Equal(0, VerticalAlpha(image, 10 + radius + 6, 10, density));
            }
        }
    }

    private static XElement Role(XDocument document, string role) => document.Descendants().Single(x => (string?)x.Attribute("data-cfx-role") == role);
    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    private static int VerticalAlpha(RgbaImage image, double x, double y, int density) {
        var column = (int)Math.Floor(x * density);
        var row = (int)Math.Floor(y * density);
        var sum = 0;
        for (var offset = -2 * density; offset <= 2 * density; offset++) sum += image.Pixels[((row + offset) * image.Width + column) * 4 + 3];
        return sum;
    }
}
