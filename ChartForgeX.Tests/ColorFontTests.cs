using System.Buffers.Binary;
using System.Reflection;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Observable palette, paint, strike and layout contracts using original portable fonts.</summary>
public sealed class ColorFontTests {
    private static byte[] Bytes(string name) {
        using var stream = typeof(ColorFontTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + name + ".ttf")!;
        using var buffer = new MemoryStream(); stream.CopyTo(buffer); return buffer.ToArray();
    }
    private static TrueTypeFont Font(string name) => TrueTypeFont.TryLoad(Bytes(name))!;
    private static RgbaCanvas Draw(TrueTypeFont face, string text, int size = 100, ChartColor? color = null, int outputScale = 1) {
        var canvas = new RgbaCanvas(220,220,1,face,outputScale,useDefaultOutlineFont:false);
        Assert.True(face.Draw(canvas,40,40,text,color ?? ChartColor.Black,size)); return canvas;
    }
    private static byte[] Pixel(RgbaCanvas canvas, int x, int y) => canvas.ToOutputPixels().Skip((y*canvas.OutputWidth+x)*4).Take(4).ToArray();
    [Theory]
    [InlineData("color-colr0")]
    [InlineData("color-colr1")]
    public void PaletteAndForegroundLayersPaintInOrderWithoutChangingAdvance(string name) {
        var face=Font(name);var canvas=Draw(face,"😀",color:ChartColor.FromRgb(0,255,0));
        Assert.Equal(100,face.Measure("😀",100),6);
        Assert.Equal(new byte[]{255,0,0,255},Pixel(canvas,30,100));
        Assert.Equal(new byte[]{0,255,0,255},Pixel(canvas,45,75));
        Assert.True(Pixel(canvas,80,90)[2]>100);
        var ink=face.MeasureGlyphInk(TextShaper.Shape(face,"😀"),100,false)!.Value;
        Assert.Equal(-20,ink.X,4);Assert.Equal(120,ink.Width,4);
    }
    [Fact]
    public void ColrOpacityIsAppliedOnceToTheComposedGlyph() {
        var canvas=Draw(Font("color-colr1"),"😀",color:new ChartColor(0,255,0,128));
        Assert.Equal(new byte[]{0,255,0,128},Pixel(canvas,45,75));
        Assert.Equal((byte)128,Pixel(canvas,80,90)[3]);
    }
    [Theory]
    [InlineData("😁",new byte[]{188,0,187,255})]
    [InlineData("😂",new byte[]{188,0,187,255})]
    public void GradientsInterpolateInLinearLight(string text,byte[] expected) {
        var canvas=Draw(Font("color-colr1"),text);
        var pixel=Pixel(canvas,text=="😁"?60:80,90);
        Assert.InRange(pixel[0],expected[0]-4,expected[0]+4);Assert.InRange(pixel[2],expected[2]-4,expected[2]+4);Assert.Equal(255,pixel[3]);
    }
    [Theory]
    [InlineData("😃")] [InlineData("😈")]
    public void SweepAndBaseInstanceVariableSweepUseTheEncodedAngleBias(string text) {
        var canvas=Draw(Font("color-colr1"),text);
        var right=Pixel(canvas,80,70);var left=Pixel(canvas,40,70);
        Assert.True(right[0]>right[2]+50);Assert.True(left[2]>left[0]+50);
        Assert.Equal(new byte[]{0,0,255,255},Pixel(canvas,40,100));
    }
    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void DetailedVectorGlyphsKeepColourAndOpacityAtHighSamplingDensity(int dpr) {
        var face=Font("color-detailed");var canvas=new RgbaCanvas(220,220,4,face,dpr,useDefaultOutlineFont:false);
        Assert.True(face.Draw(canvas,40,40,"😀",new ChartColor(0,255,0,128),100));
        Assert.Equal(new byte[]{255,0,0,128},Pixel(canvas,50*dpr,100*dpr));
        Assert.Equal(100,face.Measure("😀",100),6);
    }
    [Fact]
    public void SharedColourLinesDoNotAmplifyRetainedStopArrays() {
        var face=Font("color-shared-stops");var run=TextShaper.Shape(face,"😀");
        var before=GC.GetAllocatedBytesForCurrentThread();var ink=face.MeasureGlyphInk(run,100,false)!.Value;
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(-20,ink.X,4);Assert.Equal(80,ink.Width,4);
        Assert.InRange(allocated,0,8*1024*1024);
    }
    [Fact]
    public void ExcessiveDistinctGradientStopsFallBackToUsableOutlines() {
        var face=Font("color-stop-budget");var canvas=Draw(face,"😀",color:ChartColor.FromRgb(0,255,0));
        Assert.Equal(new byte[]{0,255,0,255},Pixel(canvas,50,100));
        Assert.Equal(2,face.MeasureGlyphInk(TextShaper.Shape(face,"😀"),100,false)!.Value.X,4);
    }
    [Fact]
    public void ClipBoxesBoundUnboundedPaintsAndReferencedGlyphs() {
        var face=Font("color-colr1");var glyphs=TextShaper.Shape(face,"😆");var ink=face.MeasureGlyphInk(glyphs,100,false)!.Value;
        Assert.Equal(-20,ink.X,4);Assert.Equal(120,ink.Width,4);Assert.Equal(-20,ink.Y,4);Assert.Equal(105,ink.Height,4);
        var canvas=Draw(face,"😆");Assert.Equal(new byte[]{255,0,0,255},Pixel(canvas,30,60));Assert.Equal(new byte[4],Pixel(canvas,15,60));
    }
    [Fact]
    public void PaintGraphCyclesFallBackToTheUsableOutline() {
        var canvas=Draw(Font("color-cycle"),"😈",color:ChartColor.FromRgb(0,255,0));
        Assert.Equal(new byte[]{0,255,0,255},Pixel(canvas,50,100));Assert.Equal(new byte[4],Pixel(canvas,30,100));
    }
    [Theory]
    [InlineData("color-colr0")]
    [InlineData("color-colr1")]
    public void FontProvidedZwjLigaturesReachTheSameColourOwner(string name) {
        var face=Font(name);var glyphs=TextShaper.Shape(face,"👩‍💻");Assert.Single(glyphs);
        var canvas=Draw(face,"👩‍💻");Assert.Equal(new byte[]{255,0,0,255},Pixel(canvas,30,100));
    }
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void CblcIndexFormatsLocatePngMetricsAndBitmapOnlyFaces(int format) {
        var face=Font("color-cbdt"+format);Assert.NotNull(face);var canvas=Draw(face,"😀",32);
        Assert.Equal(new byte[]{0,255,0,255},Pixel(canvas,40,62));Assert.Equal(12,face.Measure("😀",32),4);
        var box=face.MeasureGlyphInk(TextShaper.Shape(face,"😀"),32,false)!.Value;
        Assert.Equal(-2,box.X,4);Assert.Equal(8,box.Width,4);Assert.Equal(19.6,box.Y,4);
    }
    [Theory]
    [InlineData(32,1,255,0)] [InlineData(32,2,0,255)] [InlineData(48,1,0,255)]
    public void SbixUsesNearestOutputPixelStrikeAndLargerStrikeForTies(int size,int dpr,int red,int blue) {
        var face=Font("color-sbix");var canvas=Draw(face,"😀",size,outputScale:dpr);
        var pixels=canvas.ToOutputPixels();Assert.Contains(pixels.Chunk(4),p=>p[3]>200&&p[0]==red&&p[2]==blue);
    }
    [Fact]
    public void SbixDuplicateUsesItsOwnOriginAndOutlineFlagOverlaysTheBitmap() {
        var face=Font("color-sbix");var first=face.MeasureGlyphInk(TextShaper.Shape(face,"😀"),32,false)!.Value;
        var duplicate=face.MeasureGlyphInk(TextShaper.Shape(face,"😇"),32,false)!.Value;
        Assert.True(duplicate.X>first.X);Assert.True(duplicate.Y<first.Y);
        var plain=Draw(face,"😀",32,color:ChartColor.FromRgb(0,255,0));
        var outlined=Draw(Font("color-sbix-outlines"),"😀",32,color:ChartColor.FromRgb(0,255,0));
        Assert.NotEqual(plain.ToOutputPixels(),outlined.ToOutputPixels());
        Assert.Contains(outlined.ToOutputPixels().Chunk(4),p=>p[1]==255&&p[3]==255);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FittedAndRotatedBitmapTextRetainsFinalOutputPixelDensity(bool rotated) {
        var face=Font("color-sbix");var canvas=new RgbaCanvas(220,220,1,face,2,useDefaultOutlineFont:false);
        if(rotated)canvas.DrawTextRotated(140,40,"😀",ChartColor.Black,32,90,0,0);
        else canvas.DrawTextFitted(40,40,"😀",ChartColor.Black,32,20,face);
        Assert.Contains(canvas.ToOutputPixels().Chunk(4),p=>p[0]==0&&p[2]==255&&p[3]>200);
    }
    [Fact]
    public void InvalidOptionalPalettePreservesTheMonochromeFace() {
        var data=Bytes("color-colr0");var offset=Table(data,"CPAL");BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset+12),65535);
        var face=TrueTypeFont.TryLoad(data)!;var canvas=Draw(face,"😀",color:ChartColor.FromRgb(0,255,0));
        Assert.Equal(new byte[]{0,255,0,255},Pixel(canvas,50,100));
    }
    [Fact]
    public void ColourOnlyBitmapFacesCanBeRegisteredForSvgAndRetainTheirEmojiClusters() {
        var path=Path.Combine(Path.GetTempPath(),"CFX-colour-only-"+Guid.NewGuid().ToString("N")+".ttf");
        try {
            File.WriteAllBytes(path,Bytes("color-cbdt1"));FontRegistry.Register("CFX Colour Only",path);
            var face=TypographyFontResolver.ResolveFace("CFX Colour Only",400,false).Font!;
            Assert.False(face.HasGlyph('C'));Assert.Single(TextShaper.Shape(face,"😀"));
            Assert.Same(face,TextShaper.Shape(face,"😀")[0].Face);
            var image=SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><text x='40' y='65.6' font-family='CFX Colour Only' font-size='32'>😀</text></svg>");
            Assert.Contains(image.Pixels.Chunk(4),p=>p.SequenceEqual(new byte[]{0,255,0,255}));
        }finally{FontRegistry.Clear();File.Delete(path);}
    }
    [Fact]
    public void TruncatedStrikeIndexCannotReadOutsideItsDeclaredExtent() {
        var data=Bytes("color-cbdt2");var offset=Table(data,"CBLC");
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset+12),16);
        var face=TrueTypeFont.TryLoad(data)!;var canvas=Draw(face,"😀",32,color:ChartColor.FromRgb(0,0,255));
        Assert.Equal(new byte[]{0,0,255,255},Pixel(canvas,44,60));
    }
    [Fact]
    public void CyclicBitmapDuplicatesFallBackWithoutInvalidatingOtherGlyphs() {
        var data=Bytes("color-sbix");var offset=Table(data,"sbix");
        for(var strike=0;strike<2;strike++){
            var start=offset+(int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset+8+strike*4));
            var record=start+(int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(start+4+12*4));
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(record+8),12);
        }
        var face=TrueTypeFont.TryLoad(data)!;var canvas=Draw(face,"😇",color:ChartColor.FromRgb(0,255,0));
        Assert.Equal(new byte[]{0,255,0,255},Pixel(canvas,50,100));
        Assert.Contains(Draw(face,"😀",32).ToOutputPixels().Chunk(4),p=>p[0]==255&&p[2]==0&&p[3]==255);
    }
    [Theory]
    [InlineData(0,0,0,0,0)] [InlineData(1,0,0,255,128)] [InlineData(2,255,0,0,191)]
    [InlineData(3,175,0,199,223)] [InlineData(5,0,0,255,96)] [InlineData(6,255,0,0,96)]
    [InlineData(7,0,0,255,32)] [InlineData(8,255,0,0,96)]
    public void CompositesPreserveTheSpecifiedPremultipliedLinearLightResult(int mode,int r,int g,int b,int a) {
        var canvas=Draw(Font("color-colr1"),char.ConvertFromUtf32(0xe000+mode));var pixel=Pixel(canvas,80,90);
        var expected=new[]{r,g,b,a};for(var i=0;i<4;i++)Assert.InRange((int)pixel[i],expected[i]-1,expected[i]+1);
    }
    [Fact]
    public void SvgRasterAndDirectDrawingConsumeIdenticalColourRuns() {
        var path=Path.Combine(Path.GetTempPath(),"CFX-colour-svg-"+Guid.NewGuid().ToString("N")+".ttf");
        var face=Font("color-colr1");
        try {
            File.WriteAllBytes(path,Bytes("color-colr1"));FontRegistry.Register("CFXTestColour",path);
            var direct=new RgbaCanvas(200,150,1,face,1,useDefaultOutlineFont:false);face.Draw(direct,40,20,"😀😁",ChartColor.Black,64);
            var svg="<svg xmlns='http://www.w3.org/2000/svg' width='200' height='150'><text x='40' y='71.2' font-family='CFXTestColour' font-size='64'>😀😁</text></svg>";
            var raster=SvgRasterizer.ToImage(svg);
            Assert.All(direct.ToOutputPixels().Zip(raster.Pixels),p=>Assert.InRange(Math.Abs(p.First-p.Second),0,1));
        }finally{FontRegistry.Clear();File.Delete(path);}
    }
    [Theory]
    [InlineData("linearGradient")] [InlineData("radialGradient")]
    public void SvgPaintReferencesPreserveFontPalettesAndApplyOpacityOnce(string paint) {
        var path=Path.Combine(Path.GetTempPath(),"CFX-colour-paint-"+Guid.NewGuid().ToString("N")+".ttf");
        try {
            File.WriteAllBytes(path,Bytes("color-colr0"));FontRegistry.Register("CFX Palette",path);
            var svg="<svg xmlns='http://www.w3.org/2000/svg' width='240' height='180'><defs><"+paint+" id='fg'><stop stop-color='lime'/><stop offset='1' stop-color='yellow'/></"+paint+"></defs><text x='40' y='120' color='blue' fill='url(#fg)' fill-opacity='.5' font-family='CFX Palette' font-size='100'>A C</text></svg>";
            var rgba=SvgRasterizer.ToImage(svg).Pixels;
            byte[] At(int x,int y)=>rgba.Skip((y*240+x)*4).Take(4).ToArray();
            Assert.Equal(new byte[]{255,0,0,128},At(30,100));
            Assert.Equal(new byte[]{0,0,255,128},At(45,75));
            Assert.Equal(new byte[]{0,0,255,128},At(80,90));
        }finally{FontRegistry.Clear();File.Delete(path);}
    }
    private static int Table(byte[] data,string tag) {
        var count=BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(4));
        for(var i=0;i<count;i++){var at=12+i*16;if(System.Text.Encoding.ASCII.GetString(data,at,4)==tag)return (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(at+8));}
        throw new InvalidOperationException(tag);
    }
}
