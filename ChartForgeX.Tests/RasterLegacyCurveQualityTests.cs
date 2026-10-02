using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterLegacyCurveQualityTests {
    [Fact]
    public void CircleConvenienceMethodsUseTheSameCoverageAsEllipseShapes() {
        var outline = new RgbaCanvas(80, 80, 1);
        var ellipse = new RgbaCanvas(80, 80, 1);
        outline.DrawCircleOutline(40, 40, 20, ChartColors.Black, .25);
        ellipse.StrokeEllipse(40, 40, 20, 20, ChartColors.Black, .25);
        Assert.Equal(ellipse.Pixels, outline.Pixels);
        var circle = new RgbaCanvas(80, 80, 1);
        var filled = new RgbaCanvas(80, 80, 1);
        circle.DrawCircle(40, 40, 20, ChartColors.Black);
        filled.FillEllipse(40, 40, 20, 20, ChartColors.Black);
        Assert.Equal(filled.Pixels, circle.Pixels);
    }

    [Theory]
    [InlineData((int)RasterLineCap.Butt)]
    [InlineData((int)RasterLineCap.Round)]
    [InlineData((int)RasterLineCap.Square)]
    public void ArcConvenienceMethodsRetainFractionalWidthAndCaps(int capValue) {
        var cap = (RasterLineCap)capValue;
        var actual = new RgbaCanvas(80, 80, 1);
        var expected = new RgbaCanvas(80, 80, 1);
        actual.DrawArc(40, 40, 20, 0, Math.PI / 2, ChartColors.Black, .25, cap);
        expected.StrokeArc(40, 40, 20, 0, Math.PI / 2, ChartColors.Black, .25, cap);
        Assert.Equal(expected.Pixels, actual.Pixels);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RoundedBordersStayInsideTheirBoundsAndKeepFractionalWidths(bool dashed) {
        var canvas = new RgbaCanvas(100, 70, 1);
        if (dashed) canvas.StrokeRoundedRectDashed(10, 10, 80, 40, 10, ChartColors.Black, .25, 20, 5);
        else canvas.StrokeRoundedRect(10, 10, 80, 40, 10, ChartColors.Black, .25);
        Assert.Equal(0, canvas.Pixels[(9 * 100 + 50) * 4 + 3]);
        int coverage = Enumerable.Range(0, 20).Sum(y => canvas.Pixels[(y * 100 + 50) * 4 + 3]);
        Assert.InRange(coverage, 62, 66);
    }
}
