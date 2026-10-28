using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceParticipantViewsBeforeTests {
    [Theory]
    [InlineData("mirrorActors", true)] [InlineData("mirrorActors", false)]
    [InlineData("hideUnusedParticipants", true)] [InlineData("hideUnusedParticipants", false)]
    public void AuthoredParticipantViewSettingsHaveNoLocatedLoss(string name, bool value) {
        var source="%%{init:{\"sequence\":{\""+name+"\":"+(value?"true":"false")+"}}}%%\n"+Body;
        var result=MermaidRenderer.Render(source);Capture("sequence-view-causal-"+name+"-"+value,source,result);
        Assert.False(result.HasErrors);Assert.NotNull(result.Artifact);Assert.Empty(result.Diagnostics);
    }
    [Theory]
    [InlineData(VisualThemeMode.Light)] [InlineData(VisualThemeMode.Dark)]
    public void UnconfiguredParticipantHeadersLifelinesAndDefaultPaintRemainAControl(VisualThemeMode mode) {
        var result=MermaidRenderer.Render(Body,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=mode}});
        Capture("sequence-view-ordinary-"+mode,Body,result);
        Assert.Empty(result.Diagnostics);Assert.NotNull(result.Artifact);
        var model=Assert.IsType<SequenceArtifact>(result.Artifact.Model);Assert.Equal(new[]{"A","Unused","B"},model.Participants.Select(item=>item.Id));
        Assert.Equal(3,model.Prepare(new ChartForgeX.Rendering.VisualRenderContext(new ChartForgeX.Rendering.VisualLayoutOptions(new ChartForgeX.Rendering.VisualSize(1000,700)),themeMode:mode)).Regions.Count(item=>item.Role=="sequence-participant"));
    }
    private const string Body="sequenceDiagram\nactor A as Review\nparticipant Unused as Unused caption\nparticipant B as Archive\nA->>B: Save\nB-->>A: Ready";
    private static void Capture(string stem,string source,MermaidRenderResult result) {
        var output=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY")??Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;
        Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".mmd"),source);
        File.WriteAllText(Path.Combine(output,stem+".diagnostics.json"),JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message,item.Span.Line,item.Span.Column})));
        if(result.Artifact is {} artifact){File.WriteAllText(Path.Combine(output,stem+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".png"),artifact.ToPng());File.WriteAllText(Path.Combine(output,stem+".json"),artifact.ToInterchangeJson());}
    }
}
