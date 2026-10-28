using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceMessagePresentationBeforeTests {
    [Theory]
    [InlineData("messageAlign","left")] [InlineData("messageAlign","center")] [InlineData("messageAlign","right")]
    [InlineData("noteAlign","left")] [InlineData("noteAlign","center")] [InlineData("noteAlign","right")]
    public void AuthoredCaptionAlignmentHasNoLocatedLoss(string name,string value) {
        var source="%%{init:{\"sequence\":{\""+name+"\":\""+value+"\"}}}%%\n"+Body;var result=MermaidRenderer.Render(source);Capture("sequence-message-before-"+name+"-"+value,source,result);
        Assert.False(result.HasErrors);Assert.NotNull(result.Artifact);Assert.Empty(result.Diagnostics);
    }
    [Fact]
    public void SourceAutonumberUsesADistinctVisibleNumberOccurrence() {
        var source="sequenceDiagram\nautonumber 1000\nparticipant A as Review\nparticipant B as Archive\nA->>B: Save";
        var result=MermaidRenderer.Render(source);Capture("sequence-message-before-source-badge",source,result);Assert.Empty(result.Diagnostics);Assert.NotNull(result.Artifact);
        Assert.Contains(XDocument.Parse(result.Artifact.ToSvg()).Descendants(),item=>(string?)item.Attribute("data-cfx-role")=="sequence-number-badge");
    }
    [Fact]
    public void AuthoredNumberInkHasNoLocatedLoss() {
        var source="%%{init:{\"themeVariables\":{\"sequenceNumberColor\":\"#123456\"}}}%%\nsequenceDiagram\nautonumber\nparticipant A\nparticipant B\nA->>B: Save";
        var result=MermaidRenderer.Render(source);Capture("sequence-message-before-number-ink",source,result);Assert.False(result.HasErrors);Assert.NotNull(result.Artifact);Assert.Empty(result.Diagnostics);
    }
    [Theory]
    [InlineData(VisualThemeMode.Light)] [InlineData(VisualThemeMode.Dark)]
    public void OrdinaryUnnumberedSourceRemainsTheDefaultPixelControl(VisualThemeMode mode) {
        var result=MermaidRenderer.Render(Body,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=mode}});Capture("sequence-message-before-ordinary-"+mode,Body,result);Assert.Empty(result.Diagnostics);Assert.NotNull(result.Artifact);
    }
    [Fact]
    public void NativeMetadataNumberingRetainsItsOrdinaryInlineCaptionControl() {
        var model=SequenceArtifact.Create("native-inline-control").WithSize(900,600).AddParticipant("A","Review").AddParticipant("B","Archive").AddMessage("A","B","Save");model.Metadata["mermaid.autonumber"]="true";
        var output=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY")??Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if(!string.IsNullOrWhiteSpace(output)){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"sequence-message-before-native-inline.svg"),model.ToSvg());File.WriteAllBytes(Path.Combine(output,"sequence-message-before-native-inline.png"),model.ToPng());File.WriteAllText(Path.Combine(output,"sequence-message-before-native-inline.json"),model.ToVisualArtifact().ToInterchangeJson());}
        Assert.Contains("1. Save",model.ToSvg());
    }
    private const string Body="sequenceDiagram\nparticipant A as Review\nparticipant B as Archive\nA->>B: Save request<br/>Second line\nNote over A,B: Note first<br/>Note next\nB-->>A: Ready";
    private static void Capture(string stem,string source,MermaidRenderResult result) {
        var output=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY")??Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output,stem+".mmd"),source);File.WriteAllText(Path.Combine(output,stem+".diagnostics.json"),JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message,item.Span.Line,item.Span.Column})));
        if(result.Artifact is {} artifact){File.WriteAllText(Path.Combine(output,stem+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".png"),artifact.ToPng());File.WriteAllText(Path.Combine(output,stem+".json"),artifact.ToInterchangeJson());}
    }
}
