using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterDashBoundsTests {
    [Fact]
    public void AcceptedQuarterPixelDashesRenderOnALargeValidCanvas() {
        var canvas = new RgbaCanvas(30000, 4, 1);
        canvas.StrokePolylines(new[] { new[] { new ChartPoint(0, 2), new ChartPoint(30000, 2) } },
            ChartColors.Black, 1, RasterLineCap.Butt, RasterLineJoin.Miter, new[] { .25, .25 });
        Assert.Contains(canvas.Pixels, value => value > 0);
    }

    [Fact]
    public void TileBoundariesPreserveDashPhaseAndTranslucentStrokeUnions() {
        var actual = new RgbaCanvas(4200, 12, 1);
        var color = new ChartColor(11, 40, 90, 128);
        var line = new[] { new ChartPoint(-5.13, 6), new ChartPoint(4210, 6) };
        actual.StrokePolylines(new[] { line, line }, color, 2.5, RasterLineCap.Round, RasterLineJoin.Miter, new[] { 3.3, 4.7 });
        foreach (var boundary in new[] { 2048, 4096 }) {
            var left = boundary - 20;
            var expected = new RgbaCanvas(40, 12, 1);
            var translated = new[] { new ChartPoint(-5.13 - left, 6), new ChartPoint(4210 - left, 6) };
            expected.StrokePolylines(new[] { translated, translated }, color, 2.5, RasterLineCap.Round, RasterLineJoin.Miter, new[] { 3.3, 4.7 });
            for (var y = 0; y < 12; y++) for (var x = 0; x < 40; x++) {
                var alpha = actual.Pixels[(y * 4200 + left + x) * 4 + 3];
                Assert.InRange(alpha, 0, 128);
                Assert.InRange(System.Math.Abs(alpha - expected.Pixels[(y * 40 + x) * 4 + 3]), 0, 1);
            }
        }
    }

    [Fact]
    public void UntrustedSubPixelDashExplosionStillHasABoundedWorkGuard() {
        var canvas = new RgbaCanvas(120, 4, 1);
        Assert.Throws<System.InvalidOperationException>(() => canvas.StrokePolylines(
            new[] { new[] { new ChartPoint(0, 2), new ChartPoint(120, 2) } }, ChartColors.Black, 1,
            RasterLineCap.Butt, RasterLineJoin.Miter, new[] { .00001, .00001 }));
    }

    [Fact]
    public void FractionalDashLengthsFinishRoundedBoundaries() {
        var canvas = new RgbaCanvas(120, 40, 1);
        canvas.StrokePolylines(new[] { new[] { new ChartPoint(3.7, 10), new ChartPoint(53.8, 10), new ChartPoint(116.3, 10) } },
            ChartColors.Black, 1.2, RasterLineCap.Round, RasterLineJoin.Miter, new[] { 1.3, 2.7 });
        Assert.Contains(canvas.Pixels, value => value > 0);
        Assert.Contains(Enumerable.Range(10, 90), x => canvas.Pixels[(10 * 120 + x) * 4 + 3] == 0);
    }

    [Fact]
    public void LongInvisibleLineKeepsTheVisibleDashPhase() {
        var actual = new RgbaCanvas(80, 20, 1);
        var expected = new RgbaCanvas(80, 20, 1);
        actual.StrokePolylines(new[] { new[] { new ChartPoint(-10000000, 10), new ChartPoint(100, 10) } }, ChartColors.Black, 2, RasterLineCap.Butt, RasterLineJoin.Miter, new[] { 4D, 4D });
        expected.StrokePolylines(new[] { new[] { new ChartPoint(0, 10), new ChartPoint(100, 10) } }, ChartColors.Black, 2, RasterLineCap.Butt, RasterLineJoin.Miter, new[] { 4D, 4D });
        Assert.Equal(expected.Pixels, actual.Pixels);
    }

    [Fact]
    public void HugeFiniteBezierRetainsTheBoundedMaximumDetail() {
        int count = ChartCurveFlattening.QuadraticSegments(new ChartPoint(0, 0), new ChartPoint(1E30, 1E30), new ChartPoint(1, 1), 1);
        Assert.Equal(2048, count);
    }
}
