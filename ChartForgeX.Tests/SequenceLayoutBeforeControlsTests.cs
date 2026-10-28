using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceLayoutBeforeControlsTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OrdinaryUnconfiguredSequenceRetainsActualNativeSvgPngAndPortableControl(bool longCaption) {
        var source="sequenceDiagram\nparticipant A as Review\nparticipant B as Store\nA->>B: "+(longCaption?"An authored request keeps its entire source caption across native wrapping":"Save")+"\nNote over A,B: Note\nB-->>A: Ready";
        var result=MermaidRenderer.Render(source);var artifact=result.Artifact;
        var directory=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY")??Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if(!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);var stem=Path.Combine(directory,"sequence-layout-default-before-"+longCaption);
            File.WriteAllText(stem+".mmd",source);File.WriteAllText(stem+".diagnostics.json",JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message})));
            if(artifact!=null){File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".png",artifact.ToPng());File.WriteAllText(stem+".json",artifact.ToInterchangeJson());}
        }
        Assert.False(result.HasErrors);Assert.NotNull(artifact);Assert.Empty(result.Diagnostics);
        var model=Assert.IsType<SequenceArtifact>(artifact.Model);Assert.Equal(new[]{"A","B"},model.Participants.Select(item=>item.Id));Assert.Equal(2,model.Messages.Count);
    }
}
