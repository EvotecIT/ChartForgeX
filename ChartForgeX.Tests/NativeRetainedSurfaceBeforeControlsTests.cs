using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;
namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRetainedSurfaceBeforeControlsTests {
    [Theory] [InlineData(1,1,false)] [InlineData(1,2,true)] [InlineData(2,1,false)] [InlineData(2,2,true)]
    public void OrdinaryVectorsLinksFactsAndZeroBlurCaptureStayExactAcrossDensityCoverageAndMode(int density,int coverage,bool dark) {
        var model=Model();model.ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light;var prepared=model.Prepare(Context(model.Theme,model.ThemeMode));var options=new VisualRenderOptions(density,coverage);var svg=prepared.ToSvg();var png=prepared.ToPng(options);var capture=prepared.CaptureLayer(new VisualLayerBlur(0),options);var pixels=capture.ToImage();var decoded=RasterImageDecoder.Decode(png);Capture("surface-before-"+density+"-"+coverage+"-"+dark,prepared,options,capture);Assert.Same(prepared,capture.Source);Assert.Equal(decoded.Width,pixels.Width);Assert.Equal(decoded.Height,pixels.Height);Assert.Equal(decoded.Pixels,pixels.Pixels);Assert.Equal(svg,prepared.ToSvg(new VisualSvgOptions()));Assert.Contains("<text",svg);Assert.Contains("href=\"#guide\"",svg);Assert.DoesNotContain("<image",svg);Assert.DoesNotContain("<script",svg);Assert.Contains(prepared.Regions,r=>r.Role=="sequence-participant"&&r.Id=="A");Assert.Equal(new[]{"A","B"},prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Nodes.Select(n=>n.Id));Assert.Equal(svg,capture.Source.ToSvg());
    }
    [Fact] public void CurrentPrecancelAndPixelBudgetFailuresLeavePreparedNativeStateUnchanged() {
        var prepared=Model().Prepare(Context(VisualTheme.Graphite(),VisualThemeMode.Light));var svg=prepared.ToSvg();var json=prepared.ToArtifact("surface-before",VisualArtifactKind.Sequence).ToInterchangeJson();Capture("surface-before-failures",prepared,new VisualRenderOptions(1,1),null);Assert.Throws<OperationCanceledException>(()=>prepared.CaptureLayer(new VisualLayerBlur(1),cancellationToken:new CancellationToken(true)));Assert.Throws<ArgumentOutOfRangeException>(()=>prepared.CaptureLayer(new VisualLayerBlur(1),new VisualRenderOptions(pixelBudget:1)));Assert.Equal(svg,prepared.ToSvg());Assert.Equal(json,prepared.ToArtifact("surface-before",VisualArtifactKind.Sequence).ToInterchangeJson());
    }
    [Fact] public void CurrentPreparedSurfaceAndCaptureRemainDetachedFromProducerAndCopiedPixels() {
        var model=Model();var prepared=model.Prepare(Context(model.Theme,model.ThemeMode));var capture=prepared.CaptureLayer(new VisualLayerBlur(0),new VisualRenderOptions(1,1));var svg=prepared.ToSvg();var before=capture.ToImage().Pixels.ToArray();Capture("surface-before-detachment",prepared,new VisualRenderOptions(1,1),capture);var copy=capture.ToImage();Array.Clear(copy.Pixels,0,copy.Pixels.Length);model.Participants[0].Label="Changed";model.Messages[0].Text="Changed";model.Participants[0].Href="#changed";Assert.Equal(svg,prepared.ToSvg());Assert.Equal(before,capture.ToImage().Pixels);Assert.Equal(svg,capture.Source.ToSvg());
    }
    private static SequenceArtifact Model(){var model=SequenceArtifact.Create("surface-before").WithSize(800,620).AddParticipant("A","Review").AddParticipant("B","Archive").AddMessage("A","B","Save").AddMessage("B","A","Stored");model.Participants[0].Href="#guide";model.AddNote(SequenceArtifactNotePlacement.Over,new[]{"A","B"},"Retain facts");model.Notes[0].StepIndex=1;model.AddBlock(SequenceArtifactBlockKind.Loop,"Audit",0,1);return model;}
    private static VisualRenderContext Context(VisualTheme theme,VisualThemeMode mode)=>new(new VisualLayoutOptions(new VisualSize(800,900),24),theme,mode);
    private static void Capture(string stem,PreparedVisual prepared,VisualRenderOptions options,CapturedVisualLayer? capture){var root=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(root))return;Directory.CreateDirectory(root);var path=Path.Combine(root,stem);File.WriteAllText(path+".svg",prepared.ToSvg());File.WriteAllText(path+".fixed.svg",prepared.ToSvg("surface-before-fixed"));File.WriteAllBytes(path+".png",prepared.ToPng(options));File.WriteAllText(path+".json",prepared.ToArtifact("surface-before",VisualArtifactKind.Sequence).ToInterchangeJson());if(capture!=null){File.WriteAllText(path+".capture.svg",capture.Visual.ToSvg("surface-before-capture"));File.WriteAllBytes(path+".capture.rgba",capture.ToImage().Pixels);}}
}
