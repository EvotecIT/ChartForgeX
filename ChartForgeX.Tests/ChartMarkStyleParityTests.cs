using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartMarkStyleParityTests {
    [Theory]
    [InlineData("error")]
    [InlineData("box")]
    [InlineData("candle")]
    [InlineData("ohlc")]
    [InlineData("dumbbell")]
    [InlineData("band")]
    [InlineData("range")]
    [InlineData("waterfall")]
    [InlineData("bullet")]
    public void FinancialAndIntervalMarksKeepSvgPaintCoverage(string family) {
        foreach (var density in new[] { 1, 2 }) foreach (var alpha in new byte[] { 255, 128 }) {
            var chart = ChartMarkParityFixture.Create(family, alpha, density);
            var actual = chart.ToRgbaImage();
            var expected = SvgRasterizer.Rasterize(chart.ToSvg(), actual.Width, actual.Height);
            Assert.Empty(expected.Diagnostics);
            Assert.True(RelativeAlphaError(actual, expected.Image) < 0.02, $"{family}, alpha {alpha}, density {density} lost its SVG fill/stroke policy.");
        }
    }

    [Theory]
    [InlineData(128, 1)]
    [InlineData(128, 2)]
    [InlineData(255, 1)]
    [InlineData(255, 2)]
    public void ElementOpacityIsAppliedOnceToFillStrokeOverlap(int alpha, int density) {
        var canvas = new RgbaCanvas(20, 20, 2, null, density);
        var color = ChartColor.FromRgba(30, 80, 170, (byte)alpha);
        canvas.FillAndStrokeRoundedRect(4, 4, 10, 10, 0, color, color, 4, 0.22);
        var image = canvas.ToImage();
        var single = (int)Math.Round(alpha * 0.22);
        var overlap = (int)Math.Round((alpha + alpha * (1 - alpha / 255d)) * 0.22);
        Assert.InRange(Alpha(image, 3 * density, 8 * density), single - 1, single + 1);
        Assert.InRange(Alpha(image, 9 * density, 8 * density), single - 1, single + 1);
        Assert.InRange(Alpha(image, 4 * density, 8 * density), overlap - 1, overlap + 1);
        Assert.Equal(0, Alpha(image, density, 8 * density));
    }

    [Fact]
    public void ClippedElementLayerRetainsItsCenteredStroke() {
        var canvas = new RgbaCanvas(10, 10, 2);
        var color = ChartColor.FromRgba(30, 80, 170, 128);
        canvas.FillAndStrokeRoundedRect(-4, -4, 12, 12, 0, color, color, 4, 0.22);
        var image = canvas.ToImage();
        Assert.InRange(Alpha(image, 0, 0), 27, 29);
        Assert.InRange(Alpha(image, 7, 4), 41, 43);
        Assert.InRange(Alpha(image, 8, 4), 27, 29);
    }

    [Theory]
    [InlineData("pie-full")]
    [InlineData("pie-offset")]
    [InlineData("donut")]
    [InlineData("polar")]
    [InlineData("polar-zero")]
    public void ClosedSliceOutlinesAndGradientsFollowSvg(string family) {
        foreach (var density in new[] { 1, 2 }) {
            var chart = ChartMarkParityFixture.Create(family, 128, density);
            var actual = chart.ToRgbaImage();
            var expected = SvgRasterizer.Rasterize(chart.ToSvg(), actual.Width, actual.Height);
            Assert.Empty(expected.Diagnostics);
            // Arc flattening and subpixel coverage differ; solid fills or missing closed borders exceed this bound.
            Assert.True(RelativeAlphaError(actual, expected.Image) < 0.05);
        }
    }

    [Fact]
    public void ExplicitSliceColorRemainsSolidAndPreservesAuthoredAlpha() {
        var chart = ChartMarkParityFixture.Create("pie-full", 128);
        chart.Series[0].WithPointColor(0, ChartColor.FromRgba(30, 80, 170, 128));
        var svg = XDocument.Parse(chart.ToSvg());
        var slice = svg.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "pie-slice");
        var coordinates = ((string)slice.Attribute("d")!).Split(' ');
        var cx = double.Parse(coordinates[1], CultureInfo.InvariantCulture);
        var cy = double.Parse(coordinates[2], CultureInfo.InvariantCulture);
        var image = chart.ToRgbaImage();
        Assert.InRange(Alpha(image, (int)cx, (int)cy - 15), 127, 129);
        Assert.InRange(Alpha(image, (int)cx, (int)cy + 15), 127, 129);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PaletteSliceGradientChangesAcrossBothObjectAxes(int density) {
        var chart = ChartMarkParityFixture.Create("pie-full", 255, density);
        var svg = XDocument.Parse(chart.ToSvg());
        var slice = svg.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "pie-slice");
        var coordinates = ((string)slice.Attribute("d")!).Split(' ');
        var cx = double.Parse(coordinates[1], CultureInfo.InvariantCulture);
        var cy = double.Parse(coordinates[2], CultureInfo.InvariantCulture);
        var image = chart.ToRgbaImage();
        var reference = SvgRasterizer.Rasterize(chart.ToSvg(), image.Width, image.Height).Image;
        foreach (var offset in new[] { (-30, 0), (30, 0), (0, -30), (0, 30) }) {
            var x = (int)(cx * density) + offset.Item1 * density;
            var y = (int)(cy * density) + offset.Item2 * density;
            Assert.InRange(Math.Abs(Alpha(image, x, y) - Alpha(reference, x, y)), 0, 2);
        }
        Assert.True(Alpha(image, (int)(cx * density) - 30 * density, (int)(cy * density)) >
            Alpha(image, (int)(cx * density) + 30 * density, (int)(cy * density)) + 15);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void DiagonalGradientPreservesNormalizedObjectCoordinates(int transform) {
        var attributes = transform switch {
            1 => "gradientUnits='userSpaceOnUse' x2='1' y2='1' gradientTransform='matrix(80 0 0 20 10 10)'",
            2 => "gradientUnits='userSpaceOnUse' x2='1' y2='1' gradientTransform='matrix(80 0 20 20 10 10)'",
            3 => "gradientUnits='userSpaceOnUse' x2='1' y2='1' gradientTransform='matrix(-80 0 0 20 90 10)'",
            _ => "x2='1' y2='1'"
        };
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'><defs><linearGradient id='g' {attributes}><stop stop-color='#1e50aa'/><stop offset='1' stop-color='#1e50aa' stop-opacity='0'/></linearGradient></defs><rect x='10' y='10' width='80' height='20' fill='url(#g)'/></svg>";
        var result = SvgRasterizer.Rasterize(svg, 100, 40);
        Assert.Empty(result.Diagnostics);
        // At (.25,.25) in gradient space the alpha is .75, even when its physical axes differ.
        Assert.InRange(Alpha(result.Image, transform == 3 ? 69 : transform == 2 ? 34 : 29, 14), 193, 199);
        Assert.InRange(Alpha(result.Image, transform == 3 ? 29 : transform == 2 ? 74 : 69, 14), 129, 135);
    }

    [Fact]
    public void SunburstFillKeepsItsSvgOpacity() {
        var chart = ChartMarkParityFixture.Create("sunburst");
        var actual = chart.ToRgbaImage();
        var expected = SvgRasterizer.Rasterize(chart.ToSvg(), actual.Width, actual.Height);
        Assert.True(RelativeAlphaError(actual, expected.Image) < 0.015);
    }

    [Theory]
    [InlineData(ChartFillPattern.DiagonalForward)]
    [InlineData(ChartFillPattern.DiagonalBackward)]
    [InlineData(ChartFillPattern.Crosshatch)]
    public void HatchCoverageKeepsSvgTileSpacingAndOpacity(ChartFillPattern pattern) {
        var chart = ChartMarkParityFixture.Create("hatch", 128, 2);
        chart.Series[0].WithFillPattern(pattern);
        var actual = chart.ToRgbaImage();
        var expected = SvgRasterizer.Rasterize(chart.ToSvg(), actual.Width, actual.Height);
        Assert.Empty(expected.Diagnostics);
        Assert.True(RelativeAlphaError(actual, expected.Image) < 0.06);
    }

    private static int Alpha(RgbaImage image, int x, int y) => image.Pixels[(y * image.Width + x) * 4 + 3];

    private static double RelativeAlphaError(RgbaImage actual, RgbaImage expected) {
        long error = 0, ink = 0;
        for (var i = 3; i < actual.Pixels.Length; i += 4) { error += Math.Abs(actual.Pixels[i] - expected.Pixels[i]); ink += expected.Pixels[i]; }
        return error / (double)Math.Max(1, ink);
    }
}
