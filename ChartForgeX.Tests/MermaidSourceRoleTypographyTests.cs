using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidSourceRoleTypographyTests {
    [Theory] [InlineData("flowchart")] [InlineData("agentflow")]
    public void FiniteSourceFontRolesShareTheMeasuredDirectFenceAndPortableNativeOwners(string family) {
        var source=MermaidGraphStyleSettingsRenderingTests.Fixture(family)
            .Replace("classDef framed ","classDef framed font-size:24px,font-family:"+BrowserProofFonts.Family+",font-weight:700,font-style:italic,")
            .Replace("classDef association ","classDef association font-size:18px,font-family:"+BrowserProofFonts.Family+",font-weight:400,");
        var options=new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{FontFamily=BrowserProofFonts.Family}};
        var result=MermaidRenderer.Render(source,options);Capture(result,source,family);
        var fenced=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");
        Write(family+".fence-diagnostics.json",JsonSerializer.Serialize(fenced.Diagnostics));
        var direct=result.Artifact??throw new InvalidOperationException("Typed source fonts must retain a native artifact.");
        var json=direct.ToInterchangeJson();var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();
        Write(family+".restored.svg",restored.ToSvg());Bytes(family+".restored.png",restored.ToPng());
        Assert.False(result.HasErrors);Assert.Empty(result.Diagnostics);Assert.False(fenced.HasErrors);Assert.Empty(fenced.Diagnostics);
        var chart=Assert.IsType<TopologyChart>(direct.Model);var work=Assert.Single(chart.Nodes,node=>node.Id=="work");
        Assert.Equal(24,work.TextStyle!.FontSize);Assert.Equal("700",work.TextStyle.FontWeight);Assert.True(work.TextStyle.Italic);Assert.Equal(BrowserProofFonts.Family,work.TextStyle.FontFamily);
        Assert.Equal(24,Assert.Single(chart.Groups).TextStyle!.FontSize);Assert.Equal(18,Assert.Single(chart.Edges).TextStyle!.FontSize);
        var fence=Assert.Single(fenced.Artifacts);Assert.Equal(direct.ToSvg(),fence.ToSvg());Assert.Equal(direct.ToPng(),fence.ToPng());Assert.Equal(direct.ToPng(),restored.ToPng());
        var frozen=chart.Prepare();var frozenPng=frozen.ToPng();work.TextStyle.FontSize=32;Assert.Equal(frozenPng,frozen.ToPng());Assert.Equal(source,result.Document!.SourceText);
    }

    [Theory] [InlineData("0")] [InlineData("NaN")] [InlineData("513px")] [InlineData("200%")] [InlineData("1em")]
    public void UnrepresentableFontSizesStayLocatedAndInert(string size) {
        var source="flowchart LR\nnode[Review]\nstyle node font-size:"+size;var result=MermaidRenderer.Render(source);Capture(result,source,"inert-"+size.Replace('%','p'));
        Assert.False(result.HasErrors);var warning=Assert.Single(result.Diagnostics);Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement,warning.Code);Assert.Equal(3,warning.Span.Line);Assert.True(warning.Span.Length>0);
        Assert.Null(Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes).TextStyle);
        Assert.True(MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject}).HasErrors);
    }

    [Fact]
    public void ExtractedConfigurationValueRulesKeepExistingQuotedConfigurationSemantics() {
        const string source="%%{init:{\"sequence\":{\"actorFontFamily\":\"sans-serif\",\"actorFontSize\":\"24px\",\"actorFontWeight\":700}}}%%\nsequenceDiagram\nparticipant A\nA->>A: Review";
        var result=MermaidRenderer.Render(source);Capture(result,source,"quoted-configuration-control");Assert.False(result.HasErrors);Assert.Empty(result.Diagnostics);
        var model=Assert.IsType<SequenceArtifact>(result.Artifact!.Model);Assert.Equal(24,model.Typography!.Participant!.FontSize);
        Assert.Equal("sans-serif",model.Typography.Participant.FontFamily);Assert.Equal("700",model.Typography.Participant.FontWeight);
    }

    private static void Capture(MermaidRenderResult result,string source,string name) {
        Write(name+".mmd",source);Write(name+".diagnostics.json",JsonSerializer.Serialize(result.Diagnostics));if(result.Artifact==null)return;
        Write(name+".svg",result.Artifact.ToSvg());Bytes(name+".png",result.Artifact.ToPng());Write(name+".json",result.Artifact.ToInterchangeJson());
    }
    private static string? Output(){var output=Environment.GetEnvironmentVariable("CFX_FLOWCHART_TYPOGRAPHY_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return null;Directory.CreateDirectory(output);return output;}
    private static void Write(string name,string text){var output=Output();if(output!=null)File.WriteAllText(Path.Combine(output,name),text);}
    private static void Bytes(string name,byte[] bytes){var output=Output();if(output!=null)File.WriteAllBytes(Path.Combine(output,name),bytes);}
}
