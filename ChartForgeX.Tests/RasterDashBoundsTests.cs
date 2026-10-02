using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterDashBoundsTests {
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
