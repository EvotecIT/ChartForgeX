using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RendererGeometryTests {
    [Theory]
    [InlineData("funnel-palette")]
    [InlineData("funnel-series")]
    [InlineData("funnel-point")]
    [InlineData("wardley")]
    public void PublicMarksRetainSvgPaintAndOpacity(string family) {
        foreach (var alpha in new byte[] { 128, 255 }) foreach (var density in new[] { 1, 2 }) {
            var (svg, actual) = RendererGeometryFixture.Render(family, alpha, density);
            var expected = SvgRasterizer.Rasterize(svg, actual.Width, actual.Height);
            Assert.Empty(expected.Diagnostics);
            var error = PremultipliedError(actual, expected.Image, 0, 0, actual.Width, actual.Height);
            Assert.True(error < 0.02, $"{family}, alpha {alpha}, density {density}: {error:P2}");
        }
    }

    [Theory]
    [InlineData("fork")]
    [InlineData("flame")]
    [InlineData("droplet")]
    public void CurvedIconsRetainTheirSvgOutlines(string family) {
        foreach (var density in new[] { 1, 2 }) {
            var (svg, actual) = RendererGeometryFixture.Render(family, 128, density);
            var expected = SvgRasterizer.Rasterize(svg, actual.Width, actual.Height);
            Assert.Empty(expected.Diagnostics);
            var error = PremultipliedError(actual, expected.Image, 40 * density, 44 * density, 50 * density, 48 * density);
            Assert.True(error < 0.06, $"{family}, density {density}: {error:P2}");
        }
    }

    [Theory]
    [InlineData("topology")]
    [InlineData("monitoring")]
    public void DatabaseAndQueueIconsKeepSvgFootprints(string family) {
        foreach (var density in new[] { 1, 2 }) {
            var (svg, actual) = RendererGeometryFixture.Render(family, 255, density);
            var expected = SvgRasterizer.Rasterize(svg, actual.Width, actual.Height);
            foreach (var center in new[] { 62, 302 }) {
                // Compare contrast against the white surface: counting the surface itself hides a wrong glyph.
                // The text badge permits font coverage differences; replacing it with bars exceeds 100%.
                var error = ContrastError(actual, expected.Image, (center - 11) * density, 120 * density, 22 * density, 24 * density);
                Assert.True(error < 0.2, $"{family}, center {center}, density {density}: {error:P2}");
            }
        }
    }

    [Theory]
    [InlineData("topology-Triangle")]
    [InlineData("topology-Chevron")]
    [InlineData("topology-Diamond")]
    [InlineData("topology-Circle")]
    [InlineData("monitoring-Triangle")]
    [InlineData("monitoring-Chevron")]
    [InlineData("monitoring-Diamond")]
    [InlineData("monitoring-Circle")]
    public void DirectionMarkersRetainSvgSizeAndAnchor(string family) {
        foreach (var density in new[] { 1, 2 }) {
            var (svg, actual) = RendererGeometryFixture.Render(family, 255, density);
            var expected = SvgRasterizer.Rasterize(svg, actual.Width, actual.Height);
            var error = ContrastError(actual, expected.Image, 256 * density, 122 * density, 23 * density, 20 * density);
            Assert.True(error < 0.2, $"{family}, density {density}: {error:P2}");
        }
    }

    [Fact]
    public void MonitoringHaloRetainsAuthoredAlphaAndRouteOpacity() {
        foreach (var density in new[] { 1, 2 }) {
            var (svg, actual) = RendererGeometryFixture.Render("halo", 255, density);
            var expected = SvgRasterizer.Rasterize(svg, actual.Width, actual.Height);
            var error = PremultipliedError(actual, expected.Image, 180 * density, 125 * density, 30 * density, 14 * density);
            Assert.True(error < 0.01, $"density {density}: {error:P2}");
        }
    }

    [Theory]
    [InlineData("accent-explicit")]
    [InlineData("accent-theme")]
    [InlineData("accent-catalog")]
    [InlineData("accent-explicit-monitoring")]
    [InlineData("accent-theme-monitoring")]
    [InlineData("accent-catalog-monitoring")]
    public void TranslucentNodeAccentsRetainIconSurfaceTints(string family) {
        foreach (var density in new[] { 1, 2 }) {
            var (svg, actual) = RendererGeometryFixture.Render(family, 128, density);
            var expected = SvgRasterizer.Rasterize(svg, actual.Width, actual.Height);
            foreach (var point in new[] { (62, 132), (296, 132), (65, 222) }) {
                var offset = (point.Item2 * density * actual.Width + point.Item1 * density) * 4;
                // Interior pixels test the tint, independently of font or curved-edge coverage.
                Assert.Equal(new byte[] { 230, 230, 255, 255 }, expected.Image.Pixels.Skip(offset).Take(4).ToArray());
                Assert.Equal(expected.Image.Pixels.Skip(offset).Take(4).ToArray(), actual.Pixels.Skip(offset).Take(4).ToArray());
            }
        }
    }

    private static double ContrastError(RgbaImage actual, RgbaImage expected, int x, int y, int width, int height) {
        double error = 0, contrast = 0;
        for (var row = y; row < y + height; row++) for (var col = x; col < x + width; col++) {
            var offset = (row * actual.Width + col) * 4;
            for (var channel = 0; channel < 3; channel++) {
                error += Math.Abs(actual.Pixels[offset + channel] - expected.Pixels[offset + channel]);
                contrast += 255 - expected.Pixels[offset + channel];
            }
            error += Math.Abs(actual.Pixels[offset + 3] - expected.Pixels[offset + 3]);
        }
        return error / Math.Max(1, contrast);
    }

    private static double PremultipliedError(RgbaImage actual, RgbaImage expected, int x, int y, int width, int height) {
        double error = 0, ink = 0;
        for (var row = y; row < y + height; row++) for (var col = x; col < x + width; col++) {
            var offset = (row * actual.Width + col) * 4;
            var a = actual.Pixels[offset + 3] / 255d;
            var b = expected.Pixels[offset + 3] / 255d;
            for (var channel = 0; channel < 3; channel++) {
                error += Math.Abs(actual.Pixels[offset + channel] * a - expected.Pixels[offset + channel] * b);
                ink += expected.Pixels[offset + channel] * b;
            }
            error += Math.Abs(a - b) * 255;
            ink += b * 255;
        }
        return error / Math.Max(1, ink);
    }
}
