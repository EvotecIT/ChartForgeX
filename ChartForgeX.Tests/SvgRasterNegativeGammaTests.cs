using System;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Independent reciprocal/constant/clamped-limit vectors for signed finite gamma exponents.</summary>
public sealed class SvgRasterNegativeGammaTests {
    [Theory]
    [InlineData(".25","-1","0","#8080c0",127)]
    [InlineData("-.25","-1","1","#8080c0",128)]
    [InlineData(".25","-1",".3","#000000",255)]
    [InlineData("-.25","-1",".3","#000000",0)]
    [InlineData("0","-1",".5","#000000",128)]
    [InlineData("0","-1e308",".5","#8080c0",128)]
    [InlineData("1e-312","-129","0","#010000",7)]
    [InlineData("-1e-312","-129",".5","#010000",120)]
    public void FiniteSignedPowersUseReciprocalConstantsAndClampedZeroLimits(
        string amplitude,string exponent,string offset,string fill,byte expected) {
        var source=ColorPrimitiveFixture.Source("<feComponentTransfer in='SourceGraphic'><feFuncR type='gamma' amplitude='"
            +amplitude+"' exponent='"+exponent+"' offset='"+offset+"'/></feComponentTransfer>",fill:fill);
        var actual=ColorPrimitiveFixture.Import(source);
        ColorPrimitiveFixture.Capture("negative-gamma-"+amplitude+"-"+exponent+"-"+offset,source,actual.Image);
        Assert.Empty(actual.Diagnostics);
        Assert.Equal(expected,ColorPrimitiveFixture.Pixel(actual.Image,3,3)[0]);
        Assert.Equal((byte)255,ColorPrimitiveFixture.Pixel(actual.Image,3,3)[3]);
    }

    [Theory]
    [InlineData("sRGB",127)] [InlineData("linearRGB",255)] [InlineData(null,255)]
    public void ColorSpaceTransformsRgbBeforeGammaAndLeavesAlphaAsCoverage(string? space,byte red) {
        var source=ColorPrimitiveFixture.Source("<feComponentTransfer in='SourceGraphic'>"
            +"<feFuncR type='gamma' amplitude='.25' exponent='-1'/>"
            +"<feFuncA type='gamma' amplitude='0' exponent='-1' offset='.5'/></feComponentTransfer>",space,fill:"#8080c0");
        var actual=ColorPrimitiveFixture.Import(source);
        ColorPrimitiveFixture.Capture("negative-gamma-space-"+(space??"default"),source,actual.Image);
        Assert.Equal(new byte[]{red,128,192,128},ColorPrimitiveFixture.Pixel(actual.Image,3,3));
        Assert.Equal(new byte[]{255,0,0,128},ColorPrimitiveFixture.Pixel(actual.Image,0,0));
    }

    [Fact]
    public void NegativeAlphaPowerCanCreateCoverageAcrossTransparentInputWithoutAHiddenRgbValue() {
        var source=ColorPrimitiveFixture.Source("<feComponentTransfer in='SourceGraphic'>"
            +"<feFuncA type='gamma' amplitude='.25' exponent='-1'/></feComponentTransfer>",fill:"none");
        var result=ColorPrimitiveFixture.Import(source);
        ColorPrimitiveFixture.Capture("negative-gamma-alpha-source",source,result.Image);
        Assert.Equal(new byte[]{0,0,0,255},ColorPrimitiveFixture.Pixel(result.Image,0,0));
        Assert.Equal(new byte[]{0,0,0,255},ColorPrimitiveFixture.Pixel(result.Image,7,7));
    }

    [Fact]
    public void ScaledOverflowPreservesTheIndependentBinaryHalfAndFiniteEndpointCancellation() {
        // (2^-1025) * (.5^-1024) = .5, although the power alone exceeds double.MaxValue.
        var scaled=new RasterComponentTransfer(RasterTransferKind.Gamma,first:Math.Pow(2,-1025),second:-1024);
        Assert.InRange(scaled.Apply(.5),.5-1e-12,.5+1e-12);
        var large=new RasterComponentTransfer(RasterTransferKind.Gamma,first:1e308,second:-1,third:-1e308);
        Assert.Equal(0,large.Apply(1));
        Assert.Equal(1,large.Apply(.5));
        Assert.Equal(1,large.Apply(0));
    }
}
