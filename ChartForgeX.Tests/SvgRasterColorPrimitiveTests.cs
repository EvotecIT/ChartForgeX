using System;
using System.IO;
using System.Linq;
using System.Threading;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Independent straight-channel vectors and public import boundaries; browser surfaces are observed separately.</summary>
public sealed class SvgRasterColorPrimitiveTests {
    [Theory]
    [InlineData("matrix", "0 0 1 0 0 0 1 0 0 0 1 0 0 0 0 0 0 0 .5 0", 192,128,64,128)]
    [InlineData("saturate", "0",119,119,119,255)]
    [InlineData("hueRotate", "180",174,110,46,255)]
    [InlineData("luminanceToAlpha", "",0,0,0,119)]
    [InlineData("matrix", null,64,128,192,255)]
    public void MatrixConvenienceTypesUseTheSameStraightChannelLaw(string type,string? values,int r,int g,int b,int a) {
        var primitive="<feColorMatrix in='SourceGraphic' type='"+type+"'"+(values==null?"":" values='"+values+"'")+"/>";
        var source=ColorPrimitiveFixture.Source(primitive);
        var actual=ColorPrimitiveFixture.Import(source);
        ColorPrimitiveFixture.Capture("color-matrix-"+type,source,actual.Image);
        Assert.Empty(actual.Diagnostics);
        Assert.Equal(new byte[]{(byte)r,(byte)g,(byte)b,(byte)a},ColorPrimitiveFixture.Pixel(actual.Image,3,3));
    }

    [Theory]
    [InlineData("sRGB",128)] [InlineData("linearRGB",188)] [InlineData(null,188)]
    public void FilterSpaceIncludesItsLinearDefaultAndDoesNotTransferAlpha(string? space,byte red) {
        const string primitive="<feColorMatrix in='SourceGraphic' values='0 0 0 0 .5 0 0 0 0 0 0 0 0 0 0 0 0 0 0 .5'/>";
        var source=ColorPrimitiveFixture.Source(primitive,space);
        var actual=ColorPrimitiveFixture.Import(source);
        ColorPrimitiveFixture.Capture("color-space-"+(space??"default"),source,actual.Image);
        Assert.Equal(new byte[]{red,0,0,128},ColorPrimitiveFixture.Pixel(actual.Image,0,0));
        Assert.Equal(new byte[]{red,0,0,128},ColorPrimitiveFixture.Pixel(actual.Image,7,7));
        if(space=="sRGB") {
            var inherited=source.Replace("<defs>","<defs color-interpolation-filters='sRGB'>",StringComparison.Ordinal)
                .Replace(" color-interpolation-filters='sRGB' x="," x=",StringComparison.Ordinal);
            Assert.Equal(actual.Image.Pixels,ColorPrimitiveFixture.Import(inherited).Image.Pixels);
        }
    }

    [Theory]
    [InlineData("identity","",64)]
    [InlineData("table","tableValues='1 0'",191)]
    [InlineData("table","tableValues=''",64)]
    [InlineData("discrete","tableValues='0 .25 .75 1'",64)]
    [InlineData("linear","slope='2'",128)]
    [InlineData("gamma","exponent='2'",16)]
    public void TransferTypesHaveIndependentVectorsWithMissingChannelsIdentity(string type,string attributes,byte red) {
        var primitive="<feComponentTransfer in='SourceGraphic'><feFuncR type='"+type+"' "+attributes+"/></feComponentTransfer>";
        var source=ColorPrimitiveFixture.Source(primitive);
        var actual=ColorPrimitiveFixture.Import(source);
        ColorPrimitiveFixture.Capture("color-transfer-"+type+"-"+red,source,actual.Image);
        Assert.Empty(actual.Diagnostics);
        Assert.Equal(new byte[]{red,128,192,255},ColorPrimitiveFixture.Pixel(actual.Image,3,3));
    }

    [Fact]
    public void TransferEndpointsLastOccurrenceAndTransparentInputConstantsAreRealSourceSemantics() {
        var endpoint=ColorPrimitiveFixture.Import(ColorPrimitiveFixture.Source("<feComponentTransfer in='SourceGraphic'>"
            +"<feFuncR type='table' tableValues='.25 .75'/><feFuncG type='discrete' tableValues='.1 .9'/>"
            +"<feFuncB type='linear' slope='0'/><feFuncB type='identity'/></feComponentTransfer>",fill:"#ffffff"));
        Assert.Equal(new byte[]{191,230,255,255},ColorPrimitiveFixture.Pixel(endpoint.Image,3,3));
        var emptySource=ColorPrimitiveFixture.Source("<feComponentTransfer in='SourceGraphic'>"
            +"<feFuncR type='linear' slope='0' intercept='.5'/><feFuncA type='linear' slope='0' intercept='.5'/></feComponentTransfer>",fill:"none");
        var result=ColorPrimitiveFixture.Import(emptySource);
        ColorPrimitiveFixture.Capture("color-transfer-empty-source",emptySource,result.Image);
        Assert.Equal(new byte[]{128,0,0,128},ColorPrimitiveFixture.Pixel(result.Image,0,0));
        Assert.Equal(new byte[]{128,0,0,128},ColorPrimitiveFixture.Pixel(result.Image,7,7));
    }

    [Fact]
    public void SourceCoverageAndOwnOpacityAreAppliedOnceAfterStraightAlphaRemapping() {
        var primitive="<feComponentTransfer in='SourceGraphic'><feFuncA type='linear' slope='.5'/></feComponentTransfer>";
        var source=ColorPrimitiveFixture.Source(primitive,body:"<g opacity='.5' filter='url(#colors)'><rect x='2' y='2' width='4' height='4' fill='#4080c0' fill-opacity='.5'/></g>");
        var actual=ColorPrimitiveFixture.Import(source);
        ColorPrimitiveFixture.Capture("color-transfer-scoped-opacity",source,actual.Image);
        Assert.Equal((byte)32,ColorPrimitiveFixture.Pixel(actual.Image,3,3)[3]);
        Assert.Equal(new byte[4],ColorPrimitiveFixture.Pixel(actual.Image,0,0));
    }

    [Theory]
    [InlineData("<feColorMatrix in='BackgroundAlpha'/>")]
    [InlineData("<feColorMatrix in='SourceGraphic' values='1 0'/>")]
    [InlineData("<feColorMatrix in='SourceGraphic' x='.5'/>")]
    [InlineData("<feComponentTransfer in='SourceGraphic'><feFuncR type='gamma' exponent='NaN'/></feComponentTransfer>")]
    [InlineData("<feComponentTransfer in='SourceGraphic'><feFuncR type='unknown'/></feComponentTransfer>")]
    [InlineData("<feColorMatrix in='SourceGraphic'/><feOffset in='BackgroundImage'/>")]
    public void UnselectedGraphsAndMalformedColorFactsKeepLocatedStrictLoss(string primitive) {
        var source=ColorPrimitiveFixture.Source(primitive);
        var loose=SvgRasterizer.Rasterize(source,filters:new SvgRasterFilterOptions());
        Assert.Contains(loose.Diagnostics,item=>item.Code=="SFR002");
        Assert.Throws<NotSupportedException>(()=>ColorPrimitiveFixture.Import(source));
    }

    [Fact]
    public void NativeSourceBudgetCancellationAndNullOptionBoundariesStayInTheExistingOwners() {
        var source=ColorPrimitiveFixture.Source("<feColorMatrix in='SourceGraphic' type='saturate' values='0'/>");
        Assert.Throws<NotSupportedException>(()=>SvgRasterizer.Rasterize(source,strict:true));
        Assert.Contains(SvgRasterizer.Rasterize(source).Diagnostics,item=>item.Code=="SFR002");
        Assert.Throws<NotSupportedException>(()=>SvgRasterizer.Rasterize(source,strict:true,filters:new SvgRasterFilterOptions(maximumLiveBytes:1)));
        Assert.Throws<NotSupportedException>(()=>SvgRasterizer.Rasterize(source,strict:true,filters:new SvgRasterFilterOptions(maximumWorkSamples:1)));
        Assert.Throws<OperationCanceledException>(()=>SvgRasterizer.Rasterize(source,strict:true,
            filters:new SvgRasterFilterOptions(cancellationToken:new CancellationToken(true))));
        var policy=new RasterColorTransform(new double[]{1,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,1,0},false);
        var plan=RasterEffectPlan.TransformColor(new RasterEffectRegion(0,0,8,6),policy);
        var input=new RgbaImage(8,6,new byte[8*6*4]);
        Assert.Throws<ArgumentOutOfRangeException>(()=>RasterEffectPreflight.Prepare(new[]{plan},new[]{input},maximumLiveBytes:383));
        var exact=RasterEffectPreflight.Prepare(new[]{plan},new[]{input},maximumLiveBytes:384);
        Assert.Equal(384L,exact.ConservativeLiveBytes);
        Assert.Equal(input.Pixels,RasterEffects.Execute(exact,0,false,CancellationToken.None).Image.Pixels);
    }
}

internal static class ColorPrimitiveFixture {
    internal static string Source(string primitive,string? space="sRGB",string fill="#4080c0",string? body=null) =>
        "<svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'><defs><filter id='colors' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse'"
        +(space==null?"":" color-interpolation-filters='"+space+"'")+" x='0' y='0' width='8' height='8'>"+primitive+"</filter></defs>"
        +(body??"<rect x='2' y='2' width='4' height='4' fill='"+fill+"' filter='url(#colors)'/>")+"</svg>";
    internal static SvgRasterizationResult Import(string source)=>SvgRasterizer.Rasterize(source,strict:true,filters:new SvgRasterFilterOptions());
    internal static byte[] Pixel(RgbaImage image,int x,int y)=>image.Pixels.Skip((y*image.Width+x)*4).Take(4).ToArray();
    internal static void Capture(string stem,string source,RgbaImage image) {
        var directory=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(directory))return;
        Directory.CreateDirectory(directory);File.WriteAllText(Path.Combine(directory,stem+".source.svg"),source);
        File.WriteAllBytes(Path.Combine(directory,stem+".native.png"),PngWriter.WriteRgba(image));
    }
}
