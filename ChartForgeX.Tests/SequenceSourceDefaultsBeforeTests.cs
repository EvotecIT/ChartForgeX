using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;
namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceSourceDefaultsBeforeTests {
    [Theory]
    [InlineData("views","{\"mirrorActors\":true,\"hideUnusedParticipants\":false}")]
    [InlineData("alignment","{\"messageAlign\":\"center\",\"noteAlign\":\"center\"}")]
    [InlineData("self","{\"rightAngles\":false}")]
    [InlineData("font","{\"actorFontSize\":14,\"actorFontFamily\":\"\\\"Open Sans\\\", sans-serif\",\"actorFontWeight\":400,\"messageFontSize\":16,\"messageFontFamily\":\"\\\"trebuchet ms\\\", verdana, arial, sans-serif\",\"messageFontWeight\":400,\"noteFontSize\":14,\"noteFontFamily\":\"\\\"trebuchet ms\\\", verdana, arial, sans-serif\",\"noteFontWeight\":400}")]
    [InlineData("layout","{\"activationWidth\":10,\"diagramMarginX\":50,\"diagramMarginY\":10,\"actorMargin\":50,\"width\":150,\"height\":65,\"boxMargin\":10,\"boxTextMargin\":5,\"noteMargin\":10,\"messageMargin\":35,\"wrap\":false,\"wrapPadding\":10}")]
    public void OmittedSourceDefaultsMatchTheirVerifiedExplicitSourceValues(string name,string settings) {
        var ordinary=MermaidRenderer.Render(Body());var explicitValue=MermaidRenderer.Render(Source(settings));Capture("sequence-default-before-omitted-"+name,Body(),ordinary);Capture("sequence-default-before-explicit-"+name,Source(settings),explicitValue);Assert.Empty(ordinary.Diagnostics);Assert.Empty(explicitValue.Diagnostics);
        var ordinaryArtifact = ordinary.Artifact ?? throw new InvalidOperationException("The omitted-default source did not produce an artifact.");
        var explicitArtifact = explicitValue.Artifact ?? throw new InvalidOperationException("The explicit-default source did not produce an artifact.");
        Assert.Equal(explicitArtifact.ToSvg(), ordinaryArtifact.ToSvg());
        Assert.Equal(explicitArtifact.ToPng(), ordinaryArtifact.ToPng());
    }
    [Theory] [InlineData(40,20)] [InlineData(100,35)]
    public void AuthoredFragmentLabelBoxIsAdmittedBeforeNativeTabAllocation(double width,double height) {
        var source=Source("{\"labelBoxWidth\":"+width+",\"labelBoxHeight\":"+height+"}");var result=MermaidRenderer.Render(source);Capture("sequence-default-before-tab-"+width+"-"+height,source,result);Assert.False(result.HasErrors);Assert.Empty(result.Diagnostics);Assert.NotNull(result.Artifact);
    }
    [Fact] public void AuthoredBottomAdjustmentChangesOnlyTheMirroredTrailingFrameMargin() {
        var body="sequenceDiagram\nparticipant A as Review\nparticipant B as Archive\n"+string.Join("\n",Enumerable.Range(0,20).Select(i=>"A->>B: Save "+i));var first="%%{init:{\"sequence\":{\"mirrorActors\":true,\"bottomMarginAdj\":1}}}%%\n"+body;var second=first.Replace("\"bottomMarginAdj\":1","\"bottomMarginAdj\":21",StringComparison.Ordinal);var a=MermaidRenderer.Render(first);var b=MermaidRenderer.Render(second);Capture("sequence-default-before-bottom-1",first,a);Capture("sequence-default-before-bottom-21",second,b);Assert.Empty(a.Diagnostics);Assert.Empty(b.Diagnostics);Assert.Equal(20,Height(b.Artifact!.ToSvg())-Height(a.Artifact!.ToSvg()),6);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void SourceLifelinesAreSolidHalfUnitAndKeepTheAuthoredInk(bool dark) {
        var source="%%{init:{\"themeVariables\":{\"actorLineColor\":\"#123456\"}}}%%\n"+Body();var result=MermaidRenderer.Render(source,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light}});Capture("sequence-default-before-lifeline-"+dark,source,result);Assert.Empty(result.Diagnostics);var lines=XDocument.Parse(result.Artifact!.ToSvg()).Descendants().Where(e=>(string?)e.Attribute("data-cfx-role")=="sequence-lifeline").ToArray();Assert.NotEmpty(lines);Assert.All(lines,line=>{Assert.Equal("0.5",line.Attribute("stroke-width")!.Value);Assert.Equal("#123456",line.Attribute("stroke")!.Value);Assert.Equal("none",line.Attribute("stroke-dasharray")!.Value);});
    }
    private static string Body()=>"sequenceDiagram\nparticipant A as Review\nparticipant B as Archive\nloop Work\nA->>A: Check<br/>Next\nA->>B: Save<br/>Fact\nNote over A,B: Review<br/>Note\nend";
    private static string Source(string settings)=>"%%{init:{\"sequence\":"+settings+"}}%%\n"+Body();
    private static double Height(string svg)=>double.Parse(XDocument.Parse(svg).Root!.Attribute("viewBox")!.Value.Split(' ')[3],CultureInfo.InvariantCulture);
    private static void Capture(string stem,string source,MermaidRenderResult result){var output=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".mmd"),source);File.WriteAllText(Path.Combine(output,stem+".diagnostics.json"),JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message,item.Span.Line,item.Span.Column})));if(result.Artifact is {} artifact){File.WriteAllText(Path.Combine(output,stem+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".png"),artifact.ToPng());File.WriteAllText(Path.Combine(output,stem+".json"),artifact.ToInterchangeJson());}}
}
