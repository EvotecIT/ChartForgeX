using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterStrokePaintTests {
    [Theory]
    [InlineData(8, 0, 0, 8, 8)]
    [InlineData(0, 8, -8, 0, 8)]
    [InlineData(8, 0, 0, 2, 8)]
    [InlineData(1, 0, 1, 1, 1.618033988749895)]
    [InlineData(1, 2, 2, 1, 3)]
    [InlineData(1e300, 0, 0, 1e300, 1e300)]
    [InlineData(1e-300, 0, 0, 1e-300, 1e-300)]
    public void StrokeResolutionUsesTheGreatestAffineStretch(double a, double b, double c, double d, double expected) {
        double actual = new SvgRasterMatrix(a, b, c, d, 100, -100).MaximumScale;
        Assert.InRange(actual / expected, 1 - 1e-12, 1 + 1e-12);
    }

    [Fact]
    public void DashedGradientKeepsGlobalPaintAndPhaseAcrossRasterTileBoundaries() {
        const string body = "<defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' x2='4200'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs><path d='M-5.13 6 L4210 6 M-5.13 6 L4210 6' fill='none' stroke='url(#g)' stroke-width='2.5' stroke-opacity='.5' stroke-linecap='round' stroke-dasharray='3.3 4.7' stroke-dashoffset='1.2'/>";
        var actual = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='4200' height='12'>" + body + "</svg>");
        foreach (var boundary in new[] { 2048, 4096 }) {
            var left = boundary - 20;
            var expected = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='40' height='12' viewBox='" + left + " 0 40 12'>" + body + "</svg>");
            for (var y = 0; y < 12; y++) for (var x = 0; x < 40; x++) for (var channel = 0; channel < 4; channel++)
                Assert.InRange(System.Math.Abs(actual.Pixels[(y * 4200 + left + x) * 4 + channel] - expected.Pixels[(y * 40 + x) * 4 + channel]), 0, 1);
        }
    }

    [Fact]
    public void GradientStrokeKeepsItsColorsAcrossOneTranslucentPath() {
        var image=SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='80' height='40'><defs><linearGradient id='g'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs><path d='M10 20 L40 20 L70 20' fill='none' stroke='url(#g)' stroke-width='8' stroke-opacity='.5'/></svg>");
        var pixels=image.Pixels;
        int left=(20*80+15)*4,right=(20*80+65)*4,middle=(20*80+40)*4;
        Assert.True(pixels[left]>pixels[left+2]); Assert.True(pixels[right+2]>pixels[right]);
        Assert.InRange(pixels[middle+3],(byte)127,(byte)128);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TiledSvgStrokeRetainsItsTransformedGradientFrame(bool userSpace) {
        const int width = 300000;
        var coordinates = userSpace ? "gradientUnits='userSpaceOnUse' x1='0' x2='150000'" : "x1='0' x2='1'";
        var body = "<defs><linearGradient id='g' " + coordinates + "><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs>"
            + "<path transform='matrix(2 .00001 .2 1.5 .125 -3)' d='M-5.13 5 L150010 5 M-5.13 7 L150010 7' fill='none' stroke='url(#g)' stroke-width='1.25' stroke-opacity='.5' stroke-linecap='round' stroke-dasharray='.625 .75' stroke-dashoffset='-.375'/>";
        var actual = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='300000' height='16'>" + body + "</svg>");
        foreach (var boundary in new[] { 2048, 65536, 299008 }) {
            var left = boundary - 20;
            var expected = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='40' height='16' viewBox='" + left + " 0 40 16'>" + body + "</svg>");
            for (var y = 0; y < 16; y++) for (var x = 0; x < 40; x++) for (var channel = 0; channel < 4; channel++)
                Assert.InRange(System.Math.Abs(actual.Pixels[(y * width + left + x) * 4 + channel] - expected.Pixels[(y * 40 + x) * 4 + channel]), 0, 1);
        }
    }

    [Fact]
    public void NonUniformStrokeTransformPreservesTheLocalOutline() {
        var image=SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='80' height='40'><path transform='scale(2 1)' d='M10 10 L10 30' fill='none' stroke='black' stroke-width='2' stroke-linecap='butt'/></svg>");
        Assert.Equal((byte)255,image.Pixels[(20*80+18)*4+3]);
        Assert.Equal((byte)255,image.Pixels[(20*80+21)*4+3]);
        Assert.Equal((byte)0,image.Pixels[(20*80+17)*4+3]);
    }

    [Fact]
    public void InheritedDashOffsetMovesGapsWithoutChangingThePattern() {
        var actual=SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='80' height='40'><g stroke-dasharray='4 4' stroke-dashoffset='2'><path d='M10 20 L70 20' fill='none' stroke='black' stroke-width='2'/></g></svg>");
        Assert.Equal((byte)255,actual.Pixels[(20*80+10)*4+3]);
        Assert.Equal((byte)0,actual.Pixels[(20*80+13)*4+3]);
        Assert.Equal((byte)255,actual.Pixels[(20*80+17)*4+3]);
    }
}
