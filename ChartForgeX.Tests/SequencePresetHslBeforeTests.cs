using ChartForgeX.SvgRaster;
using Xunit;
namespace ChartForgeX.Tests;
public sealed class SequencePresetHslBeforeTests {
    [Theory] [InlineData("hsl(0 100% 50%)","#FF0000")] [InlineData("hsl(-120,100%,50%)","#0000FF")]
    [InlineData("hsla(120,100%,25%,.5)","#00800080")] [InlineData("hsl(90deg 50% 40%)","#669933")]
    public void ExistingPublicSvgHslAndLiteralHexPaintHaveIdenticalPixels(string hsl,string literal) {
        string Svg(string color)=>"<svg xmlns='http://www.w3.org/2000/svg' width='64' height='32'><rect width='64' height='32' fill='"+color+"'/></svg>";var source=Svg(hsl);var control=Svg(literal);var pixels=SvgRasterizer.ToImage(source);var expected=SvgRasterizer.ToImage(control);var root=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(!string.IsNullOrWhiteSpace(root)){Directory.CreateDirectory(root);var stem=Path.Combine(root,"preset-hsl-before-"+literal.TrimStart('#'));File.WriteAllText(stem+".svg",source);File.WriteAllBytes(stem+".png",SvgRasterizer.ToPng(source));File.WriteAllText(stem+".control.svg",control);File.WriteAllBytes(stem+".control.png",SvgRasterizer.ToPng(control));}Assert.Equal(expected.Pixels,pixels.Pixels);
    }
}
