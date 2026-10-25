using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class FlowEffectiveHeadingObservationTests {
    [Fact]
    public void FlowPortableRestorationRetainsSourceHeadingsAndTheSeparateCallerFrame() {
        var flow=FlowArtifact.Create("flow-effective-heading").WithTitle("Model flow").WithSubtitle("Model subtitle").WithSize(640,400).AddStep("step","ChartForgeX");
        var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640,400),24),VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family),
            frame:new VisualFrame(title:"Caller flow",subtitle:"Caller subtitle",showLegend:false,transparentBackground:true));
        var before=flow.Prepare(context);var saved=before.ToArtifact().ToInterchangeEnvelope();var json=saved.ToJson();var read=VisualArtifactInterchangeEnvelope.FromJson(json);
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output) {
            Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"flow-effective-heading.svg"),before.ToSvg());File.WriteAllBytes(Path.Combine(output,"flow-effective-heading.png"),before.ToPng());File.WriteAllText(Path.Combine(output,"flow-effective-heading.json"),json);
        }
        Assert.Equal("Model flow",saved.Title);Assert.Equal("Model subtitle",saved.Subtitle);Assert.Equal(saved.Title,read.Title);Assert.Equal(saved.Subtitle,read.Subtitle);
        Assert.Equal(VisualArtifactInterchangeFamily.Flow,read.Family);Assert.Equal("Caller flow",read.Presentation!.Frame!.Title);Assert.Equal("Caller subtitle",read.Presentation.Frame.Subtitle);Assert.Equal(json,read.ToJson());
        var after=read.ToFlowArtifact().ToVisualArtifact();
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} restoredOutput){Directory.CreateDirectory(restoredOutput);File.WriteAllText(Path.Combine(restoredOutput,"flow-effective-heading-restored.svg"),after.ToSvg());File.WriteAllBytes(Path.Combine(restoredOutput,"flow-effective-heading-restored.png"),after.ToPng());}
        Assert.Equal(before.ToPng(),after.ToPng());Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()),TopologyStaticPaintObservation.Paint(after.ToSvg()));
        Assert.Equal("Model flow",flow.Title);Assert.Equal("Model subtitle",flow.Subtitle);
        Assert.Contains(before.Scene.Nodes.OfType<VisualSceneText>().SelectMany(mark=>mark.Text.Lines),line=>line.Text=="Caller flow");
    }
}
