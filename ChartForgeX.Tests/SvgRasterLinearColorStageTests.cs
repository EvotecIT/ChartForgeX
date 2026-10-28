using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Flat ordered-stage contracts; these do not qualify general multi-input SVG graphs.</summary>
public sealed class SvgRasterLinearColorStageTests {
    [Theory]
    [InlineData(false,72,128,64)] [InlineData(true,192,128,8)]
    public void ThreeStagesRetainTheAuthoredOrderWithoutPixelIntermediates(bool reverse,byte r,byte g,byte b) {
        var swap="<feColorMatrix {0} values='0 0 1 0 0 0 1 0 0 0 1 0 0 0 0 0 0 0 1 0'/>";
        var square="<feComponentTransfer {0}><feFuncR type='gamma' exponent='2'/></feComponentTransfer>";
        var half="<feComponentTransfer {0}><feFuncR type='linear' slope='.5'/></feComponentTransfer>";
        var first=reverse?square:swap;var middle=reverse?half:square;var last=reverse?swap:half;
        var nodes=string.Format(first,"in='SourceGraphic' result='a'")+string.Format(middle,"in='a' result='b'")+string.Format(last,"in='b'");
        var source=ColorPrimitiveFixture.Source(nodes);var actual=ColorPrimitiveFixture.Import(source);
        ColorPrimitiveFixture.Capture("linear-color-order-"+reverse,source,actual.Image);
        Assert.Empty(actual.Diagnostics);Assert.Equal(new byte[]{r,g,b,255},ColorPrimitiveFixture.Pixel(actual.Image,3,3));
    }

    [Theory]
    [InlineData("sRGB","linearRGB",42)] [InlineData("linearRGB","sRGB",69)]
    public void EachOfThreeStagesResolvesItsActualWorkingSpace(string firstSpace,string nextSpace,byte red) {
        var nodes="<feColorMatrix in='SourceGraphic' result='a' color-interpolation-filters='"+firstSpace+"' values='0 0 0 0 .5 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1'/>"
            +"<feComponentTransfer in='a' result='b' color-interpolation-filters='"+nextSpace+"'><feFuncR type='gamma' exponent='2'/></feComponentTransfer>"
            +"<feComponentTransfer in='b' color-interpolation-filters='"+nextSpace+"'><feFuncR type='linear' slope='.5'/></feComponentTransfer>";
        var source=ColorPrimitiveFixture.Source(nodes);var result=ColorPrimitiveFixture.Import(source);
        ColorPrimitiveFixture.Capture("linear-color-spaces-"+firstSpace,source,result.Image);
        Assert.Equal(new byte[]{red,0,0,255},ColorPrimitiveFixture.Pixel(result.Image,3,3));
    }

    [Fact]
    public void PerStageClampAndZeroAlphaNormalizationPrecedeLaterColorOrCoverageCreation() {
        var clamped="<feColorMatrix in='SourceGraphic' result='a' values='0 0 0 0 2 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1'/>"
            +"<feComponentTransfer in='a' result='b'><feFuncR type='linear' slope='.5'/></feComponentTransfer>"
            +"<feComponentTransfer in='b'><feFuncR type='gamma' exponent='2'/></feComponentTransfer>";
        Assert.Equal(new byte[]{64,0,0,255},ColorPrimitiveFixture.Pixel(ColorPrimitiveFixture.Import(ColorPrimitiveFixture.Source(clamped)).Image,3,3));
        var cleared="<feColorMatrix in='SourceGraphic' result='a' values='0 0 0 0 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0'/>"
            +"<feComponentTransfer in='a' result='b'><feFuncA type='linear' slope='0' intercept='1'/></feComponentTransfer>"
            +"<feComponentTransfer in='b' result='c'><feFuncR type='linear' slope='.5' intercept='.25'/></feComponentTransfer>"
            +"<feColorMatrix in='c'/>";
        var source=ColorPrimitiveFixture.Source(cleared);var result=ColorPrimitiveFixture.Import(source);
        ColorPrimitiveFixture.Capture("linear-color-clear-alpha",source,result.Image);
        Assert.Equal(new byte[]{64,0,0,255},ColorPrimitiveFixture.Pixel(result.Image,3,3));
    }

    [Fact]
    public void SeventeenFlatStagesUseOneApplicationAndTheExistingCountedWorkAndPixelBudgets() {
        var stages=Enumerable.Range(0,17).Select(_=>new RasterColorTransform(Identity(),false)).ToArray();
        var sequence=new RasterColorTransform(stages);var plan=RasterEffectPlan.TransformColor(new RasterEffectRegion(0,0,8,6),sequence);
        var input=new RgbaImage(8,6,new byte[8*6*4]);
        Assert.Equal(17,sequence.StageCount);Assert.Equal(48L*160*17,plan.WorkSamples);
        Assert.Throws<ArgumentOutOfRangeException>(()=>RasterEffectPreflight.Prepare(new[]{plan},new[]{input},maximumWorkSamples:plan.WorkSamples-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>RasterEffectPreflight.Prepare(new[]{plan},new[]{input},maximumLiveBytes:383));
        var permit=RasterEffectPreflight.Prepare(new[]{plan},new[]{input},maximumWorkSamples:plan.WorkSamples,maximumLiveBytes:384);
        Assert.Equal(input.Pixels,RasterEffects.Execute(permit,0,false,CancellationToken.None).Image.Pixels);
        Assert.Throws<OperationCanceledException>(()=>sequence.Apply(0,0,0,0,out _,out _,out _,out _,new CancellationToken(true)));
        var source=ColorPrimitiveFixture.Source(IdentitySequence(17));
        var actual=SvgRasterizer.Rasterize(source,strict:true,filters:new SvgRasterFilterOptions(maximumApplications:1));
        ColorPrimitiveFixture.Capture("linear-color-seventeen",source,actual.Image);
        Assert.Equal(new byte[]{64,128,192,255},ColorPrimitiveFixture.Pixel(actual.Image,3,3));
        Assert.Throws<NotSupportedException>(()=>SvgRasterizer.Rasterize(source,strict:true,filters:new SvgRasterFilterOptions(maximumWorkSamples:20000)));
        Assert.Empty(SvgRasterizer.Rasterize(ColorPrimitiveFixture.Source(IdentitySequence(1)),strict:true,
            filters:new SvgRasterFilterOptions(maximumWorkSamples:20000)).Diagnostics);
        Assert.Throws<OperationCanceledException>(()=>SvgRasterizer.Rasterize(source,strict:true,filters:new SvgRasterFilterOptions(cancellationToken:new CancellationToken(true))));
    }

    [Theory]
    [InlineData("<feColorMatrix in='SourceGraphic'/><feColorMatrix in='BackgroundImage'/>")]
    [InlineData("<feColorMatrix in='SourceGraphic' result='a'/><feColorMatrix in='StrokePaint'/>")]
    [InlineData("<feColorMatrix in='SourceGraphic' result='a'/><feColorMatrix in='a' result='SourceGraphic'/><feColorMatrix in='a'/>")]
    [InlineData("<feColorMatrix in='SourceGraphic' result='a'/><feColorMatrix in='a' in2='SourceGraphic'/>")]
    [InlineData("<feColorMatrix in='SourceGraphic' result='a'/><feGaussianBlur in='a' stdDeviation='129'/>")]
    public void UncapturedInputsReservedNamesAndUnsupportedParametersRemainLocatedLoss(string nodes) {
        var source=ColorPrimitiveFixture.Source(nodes);
        Assert.Contains(SvgRasterizer.Rasterize(source,filters:new SvgRasterFilterOptions()).Diagnostics,item=>item.Code=="SFR002");
        Assert.Throws<NotSupportedException>(()=>ColorPrimitiveFixture.Import(source));
    }

    private static double[] Identity()=>new double[]{1,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,1,0};
    private static string IdentitySequence(int count)=>string.Concat(Enumerable.Range(0,count).Select(index=>
        "<feColorMatrix in='"+(index==0?"SourceGraphic":"stage"+(index-1))+"'"+(index+1==count?"":" result='stage"+index+"'")+"/>"));
}
