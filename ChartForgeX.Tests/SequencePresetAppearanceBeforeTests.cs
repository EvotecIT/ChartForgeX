using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;
namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class SequencePresetAppearanceBeforeTests {
    [Theory]
    [InlineData("base","#FFF4DD")] [InlineData("default","#ECECFF")] [InlineData("dark","#1F2020")]
    [InlineData("forest","#CDE498")] [InlineData("neutral","#EEEEEE")] [InlineData("neo","#FFFFFF")]
    [InlineData("neo-dark","#2A2020")] [InlineData("redux","#FFFFFF")] [InlineData("redux-dark","#111113")]
    [InlineData("redux-color","#FDF4FF")] [InlineData("redux-dark-color","#111113")]
    public void NamedPresetReachesActualParticipantSurfaceInsteadOfNativeFallback(string name,string expected) {
        var source="%%{init:{\"theme\":\""+name+"\",\"look\":\"classic\"}}%%\n"+Body;var result=MermaidRenderer.Render(source);Capture("sequence-preset-before-"+name,source,result);Assert.False(result.HasErrors);Assert.NotNull(result.Artifact);var surface=XDocument.Parse(result.Artifact!.ToSvg()).Descendants().First(e=>(string?)e.Attribute("data-cfx-role")=="sequence-participant-box");Assert.Equal(expected,surface.Attribute("fill")!.Value);
    }
    [Theory] [InlineData("base")] [InlineData("redux-dark-color")]
    public void ExplicitRoleOverrideRetainsItsLiteralInkAndSurfaceAcrossNamedPresetSelection(string name) {
        var source="%%{init:{\"theme\":\""+name+"\",\"look\":\"classic\",\"themeVariables\":{\"actorBkg\":\"#123456\",\"actorTextColor\":\"#fedcba\",\"signalColor\":\"#102030\"}}}%%\n"+Body;var result=MermaidRenderer.Render(source);Capture("sequence-preset-before-explicit-"+name,source,result);Assert.False(result.HasErrors);var model=Assert.IsType<SequenceArtifact>(result.Artifact!.Model);Assert.Equal("#123456",model.Paint.ParticipantFill!.Value.ToCss());Assert.Equal("#FEDCBA",model.Paint.ParticipantText!.Value.ToCss());Assert.Equal("#102030",model.Paint.MessageLine!.Value.ToCss());
    }
    [Fact] public void NeoMultilineIconLabelsGrowUpwardToOneHeaderDatum() {
        const string source="%%{init:{\"look\":\"neo\"}}%%\nsequenceDiagram\nactor A as First<br/>Second\nparticipant B@{type: control, alias: Worker}\nA->>B: Save";var result=MermaidRenderer.Render(source);Capture("sequence-preset-before-neo-bands",source,result);Assert.False(result.HasErrors);var xml=XDocument.Parse(result.Artifact!.ToSvg());var groups=xml.Descendants().Where(e=>(string?)e.Attribute("data-cfx-role")=="sequence-participant").ToArray();var first=groups.Single(e=>((string?)e.Attribute("data-label"))?.StartsWith("First",StringComparison.Ordinal)==true);var second=groups.Single(e=>(string?)e.Attribute("data-label")=="Worker");var a=first.Descendants().Single(e=>e.Name.LocalName=="text"&&e.Value=="Second");var b=second.Descendants().Single(e=>e.Name.LocalName=="text"&&e.Value=="Worker");Assert.Equal(b.Attribute("y")!.Value,a.Attribute("y")!.Value);
    }
    [Fact] public void NeoKnownLoopTabExtraAddsFifteenToAnOtherwiseIdenticalLargeAuthoredHeader() {
        var body="sequenceDiagram\nparticipant A\nparticipant B\nloop Work\n"+string.Join("\n",Enumerable.Range(0,20).Select(i=>"A->>B: Save "+i))+"\nend";var first="%%{init:{\"look\":\"classic\",\"sequence\":{\"mirrorActors\":true,\"height\":200,\"labelBoxHeight\":100}}}%%\n"+body;var second=first.Replace("\"look\":\"classic\"","\"look\":\"neo\"",StringComparison.Ordinal);var a=MermaidRenderer.Render(first);var b=MermaidRenderer.Render(second);Capture("sequence-preset-before-tab-classic",first,a);Capture("sequence-preset-before-tab-neo",second,b);Assert.Empty(a.Diagnostics);Assert.Empty(b.Diagnostics);Assert.Equal(15,Height(b.Artifact!.ToSvg())-Height(a.Artifact!.ToSvg()),6);
    }
    [Theory] [InlineData(SequenceArtifactParticipantKind.Participant)] [InlineData(SequenceArtifactParticipantKind.Actor)] [InlineData(SequenceArtifactParticipantKind.Control)]
    public void NativeOrdinaryKindRemainsAnIncumbentScriptFreeControl(SequenceArtifactParticipantKind kind) {
        var model=SequenceArtifact.Create("native-preset-control").WithSize(1100,700).AddParticipant("A","Review",kind).AddParticipant("B","Archive").AddMessage("A","B","Save");var output=Output();if(output!=null){Directory.CreateDirectory(output);var stem=Path.Combine(output,"sequence-preset-native-control-"+kind);File.WriteAllText(stem+".svg",model.ToSvg());File.WriteAllBytes(stem+".png",model.ToPng());File.WriteAllText(stem+".json",model.ToVisualArtifact().ToInterchangeJson());}Assert.DoesNotContain("<script",model.ToSvg());Assert.True(model.ToPng().Length>64);Assert.Contains("Review",model.ToSvg());Assert.Equal(kind,model.Participants[0].Kind);
    }
    private const string Body="sequenceDiagram\nparticipant A as Review\nparticipant B as Archive\nA->>B: Save";
    private static double Height(string svg)=>double.Parse(XDocument.Parse(svg).Root!.Attribute("viewBox")!.Value.Split(' ')[3],CultureInfo.InvariantCulture);
    private static void Capture(string stem,string source,MermaidRenderResult result){var output=Output();if(output==null)return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".mmd"),source);File.WriteAllText(Path.Combine(output,stem+".diagnostics.json"),JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message,item.Span.Line,item.Span.Column})));if(result.Artifact is {} artifact){File.WriteAllText(Path.Combine(output,stem+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".png"),artifact.ToPng());File.WriteAllText(Path.Combine(output,stem+".json"),artifact.ToInterchangeJson());}}
    private static string? Output(){var value=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");return string.IsNullOrWhiteSpace(value)?null:value;}
}
