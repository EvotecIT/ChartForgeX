using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterStrokePaintTests {
    [Fact]
    public void GradientStrokeKeepsItsColorsAcrossOneTranslucentPath() {
        var image=SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='80' height='40'><defs><linearGradient id='g'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs><path d='M10 20 L40 20 L70 20' fill='none' stroke='url(#g)' stroke-width='8' stroke-opacity='.5'/></svg>");
        var pixels=image.Pixels;
        int left=(20*80+15)*4,right=(20*80+65)*4,middle=(20*80+40)*4;
        Assert.True(pixels[left]>pixels[left+2]); Assert.True(pixels[right+2]>pixels[right]);
        Assert.InRange(pixels[middle+3],(byte)127,(byte)128);
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
