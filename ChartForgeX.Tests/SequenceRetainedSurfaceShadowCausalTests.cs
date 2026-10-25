using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;
namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceRetainedSurfaceShadowCausalTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void NeoBoxRetainsItsVisibleOutsideSurfaceShadowEvenWhenStylesheetShadowIsDisabled(bool disableStyle) {
        var source=(disableStyle?"%%{init:{\"themeVariables\":{\"dropShadow\":\"none\"}}}%%\n":"")+"sequenceDiagram\nparticipant A as Review\nparticipant B as Archive\nA->>B: Save";var result=MermaidRenderer.Render(source,new MermaidRenderOptions{Sequence=new MermaidSequenceRenderOptions{Width=1200,Height=800}});Assert.False(result.HasErrors, string.Join("\n", result.Diagnostics.Select(d => d.Code + ":" + d.Message)));var model=Assert.IsType<SequenceArtifact>(result.Artifact!.Model);var prepared=model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200,800),24),model.Theme,model.ThemeMode));var header=prepared.Regions.Single(r=>r.Role=="sequence-participant"&&r.Id=="A");var image=RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(1,1)));var x=(int)Math.Ceiling(header.Bounds.Right)+2;var y=(int)Math.Floor(header.Bounds.Bottom)-8;var target=Pixel(image,x,y);var exterior=Pixel(image,x+8,y);var root=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(!string.IsNullOrWhiteSpace(root)){Directory.CreateDirectory(root);var stem=Path.Combine(root,"surface-shadow-causal-"+disableStyle);File.WriteAllText(stem+".mmd",source);File.WriteAllText(stem+".svg",prepared.ToSvg());File.WriteAllBytes(stem+".png",prepared.ToPng(new VisualRenderOptions(1,1)));File.WriteAllText(stem+".json",prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeJson());File.WriteAllText(stem+".probe.txt",$"box={header.Bounds};target={x},{y}:{string.Join(",",target)};control={x+8},{y}:{string.Join(",",exterior)}");File.WriteAllText(stem+".diagnostics.txt",string.Join("\n",result.Diagnostics.Select(d=>d.Code+":"+d.Message)));}Assert.Contains("<text",prepared.ToSvg());Assert.NotEqual(exterior,target);
    }
    private static byte[] Pixel(RgbaImage image,int x,int y){Assert.InRange(x,0,image.Width-1);Assert.InRange(y,0,image.Height-1);var i=(y*image.Width+x)*4;return new[]{image.Pixels[i],image.Pixels[i+1],image.Pixels[i+2],image.Pixels[i+3]};}
}
