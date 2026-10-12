using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterDashTileCompositionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TiledTranslucentDashesMatchDirectWindowsAcrossTileBoundaries(bool round) {
        const int width = 300000;
        var cap = round ? RasterLineCap.Round : RasterLineCap.Butt;
        var pattern = round ? new[] { 0d, .5, .25 } : new[] { 1.25, 1.5 };
        RgbaCanvas Render(int canvasWidth, int shift) {
            var canvas = new RgbaCanvas(canvasWidth, 8, 1);
            canvas.StrokePolylines(new[] { new[] { new ChartPoint(.25 - shift, 4), new ChartPoint(width - .25 - shift, 4) } },
                new ChartColor(30, 80, 140, 128), 1, cap, RasterLineJoin.Miter, pattern, dashOffset: -.375);
            return canvas;
        }
        var actual = Render(width, 0);
        foreach (var boundary in new[] { 2048, 65536, 299008 }) {
            var expected = Render(16, boundary - 8);
            for (var y = 0; y < 8; y++) for (var x = 0; x < 16; x++) for (var channel = 0; channel < 4; channel++) {
                var first = actual.Pixels[(y * width + boundary - 8 + x) * 4 + channel];
                var second = expected.Pixels[(y * 16 + x) * 4 + channel];
                Assert.InRange(Math.Abs(first - second), 0, 1);
            }
        }
    }
}
