using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceSelfRoutesBeforeTests {
    [Theory]
    [InlineData(true,false)] [InlineData(false,false)] [InlineData(true,true)] [InlineData(false,true)]
    public void AuthoredRightAnglesAdmitsTheFiniteSelfRouteWithoutLocatedLoss(bool rightAngles,bool numbered) {
        var source="%%{init:{\"sequence\":{\"rightAngles\":"+(rightAngles?"true":"false")+"}}}%%\nsequenceDiagram\n"+(numbered?"autonumber 1000\n":"")+"participant A as Review\nA->>A: Check";
        var result=MermaidRenderer.Render(source);Capture("sequence-self-before-"+rightAngles+"-"+numbered,source,result);Assert.False(result.HasErrors);Assert.NotNull(result.Artifact);Assert.Empty(result.Diagnostics);
        var path=XDocument.Parse(result.Artifact.ToSvg()).Descendants().Single(item=>(string?)item.Attribute("data-cfx-role")=="sequence-message-line");Assert.Equal(!rightAngles,path.Attribute("d")!.Value.Contains("C",StringComparison.Ordinal));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NativeOmittedSelfRoutingKeepsItsOrdinaryPathAndInlineNumberControl(bool numbered) {
        var model=SequenceArtifact.Create("self-default").WithSize(900,700).AddParticipant("A","Review").AddMessage("A","A","Check");if(numbered)model.Metadata["mermaid.autonumber"]="true";
        var output=Output();if(output!=null){Directory.CreateDirectory(output);var stem=Path.Combine(output,"sequence-self-native-default-"+numbered);File.WriteAllText(stem+".svg",model.ToSvg());File.WriteAllBytes(stem+".png",model.ToPng());File.WriteAllText(stem+".json",model.ToVisualArtifact().ToInterchangeJson());}
        var path=XDocument.Parse(model.ToSvg()).Descendants().Single(item=>(string?)item.Attribute("data-cfx-role")=="sequence-message-line");Assert.DoesNotContain("C",path.Attribute("d")!.Value);Assert.Contains(numbered?"1. Check":"Check",model.ToSvg());
    }
    private static void Capture(string stem,string source,MermaidRenderResult result){var output=Output();if(output==null)return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".mmd"),source);File.WriteAllText(Path.Combine(output,stem+".diagnostics.json"),JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message,item.Span.Line,item.Span.Column})));if(result.Artifact is {} artifact){File.WriteAllText(Path.Combine(output,stem+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".png"),artifact.ToPng());File.WriteAllText(Path.Combine(output,stem+".json"),artifact.ToInterchangeJson());}}
    private static string? Output()=>Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY")??Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
}
