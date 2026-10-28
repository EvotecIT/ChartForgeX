using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGraphStyleSettingsRenderingTests {
    [Theory]
    [InlineData("flowchart")][InlineData("agentflow")]
    public void FamilyFixturesShareDirectFencePortableAndSourceFacts(string family) {
        var source=Fixture(family);var direct=MermaidRenderer.Render(source);MermaidGraphStyleSettingsCausalTests.Capture(direct,family+"-styles-settings-fixture");
        Assert.Empty(direct.Diagnostics);Assert.Equal(source,direct.Document!.SourceText);
        var fenced=new MermaidVisualMarkupParser().Parse("```mermaid\n"+source+"\n```");Assert.False(fenced.HasErrors);
        var artifact=direct.Artifact!;var fence=Assert.Single(fenced.Artifacts);
        Assert.Equal(artifact.ToSvg(),fence.ToSvg());Assert.Equal(artifact.ToPng(),fence.ToPng());
        var envelope=artifact.ToInterchangeEnvelope();
        Assert.Equal(envelope.ToJson(),VisualArtifactInterchangeEnvelope.FromUtf8Json(envelope.ToUtf8Json()).ToJson());
        var chart=Assert.IsType<TopologyChart>(artifact.Model);var group=Assert.Single(chart.Groups);var edge=Assert.Single(chart.Edges);
        Assert.Equal(new[]{"work","done"},chart.Nodes.Select(node=>node.Id));Assert.Equal("#e9f0fa",group.BackgroundColor);
        Assert.Equal("#345678",group.BorderColor);Assert.Equal("#234567",group.LabelColor);Assert.Equal("framed",group.Metadata["mermaid.classes"]);
        Assert.Equal("reads",edge.Id);Assert.Equal(3,edge.StrokeWidth);Assert.Equal(.6,edge.Opacity);
        Assert.Equal(new double[]{9,4,2,9,4,2},edge.DashPattern);Assert.Equal("association",edge.Metadata["mermaid.classes"]);
        Assert.Equal(44,chart.Viewport.Padding);Assert.Equal(75,chart.DefaultRenderOptions!.LayeredNodeSpacing);Assert.Equal(140,chart.DefaultRenderOptions.LayeredRankSpacing);
    }

    [Fact]
    public void ExplicitHostPaddingAndSizingRetainAuthorityOverSourceDefaults() {
        var source=MermaidGraphStyleSettingsCausalTests.SettingsSource("agentflow");
        var document=Assert.IsType<MermaidAgentflowDocument>(new MermaidParser().Parse(source).Document);
        var chart=document.ToTopologyChart(new MermaidTopologyRenderOptions {Padding=18,
            Presentation=new MermaidPresentationOptions{TopologySizing=MermaidTopologySizing.FixedViewport}});
        Assert.Equal(18,chart.Viewport.Padding);Assert.True(chart.DefaultRenderOptions!.FitContentToViewport);
    }

    [Theory]
    [InlineData("flowchart LR\nA reads@--> B\nstyle reads stroke:#123456")]
    [InlineData("agentflow-beta LR\nA reads@--> B\nstyle reads stroke:#123456")]
    public void NamedEdgeStyleIsInertAndRejectableWithoutSyntheticNode(string source) {
        var rendered=MermaidRenderer.Render(source);MermaidGraphStyleSettingsCausalTests.Capture(rendered,"named-edge-style-"+rendered.Document!.Kind);
        Assert.False(rendered.HasErrors);Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement,Assert.Single(rendered.Diagnostics).Code);
        var chart=Assert.IsType<TopologyChart>(rendered.Artifact!.Model);Assert.Equal(2,chart.Nodes.Count);var edge=Assert.Single(chart.Edges);
        Assert.Equal(ChartForgeX.Primitives.ChartColor.FromHex("#333333"),ChartForgeX.Primitives.ChartColor.Parse(edge.Color!));
        var control=MermaidRenderer.Render(source.Substring(0,source.LastIndexOf('\n')));Assert.Empty(control.Diagnostics);
        Assert.Equal(control.Artifact!.ToSvg(),rendered.Artifact.ToSvg());Assert.Equal(control.Artifact!.ToPng(),rendered.Artifact.ToPng());
        Assert.True(MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject}).HasErrors);
    }

    [Theory]
    [InlineData("agentflow-beta\nclass missing marked\na:::external-box-->b\nclassDef external-box fill:#ffeedd")]
    [InlineData("agentflow-beta\nclass missing marked\na:::external-box --> b\nclassDef external-box fill:#ffeedd")]
    public void AgentflowInlineClassesKeepTheFamilyIdentityAndUnknownClassNoop(string source) {
        var rendered=MermaidRenderer.Render(source);Assert.Empty(rendered.Diagnostics);
        var chart=Assert.IsType<TopologyChart>(rendered.Artifact!.Model);Assert.Equal(new[]{"a","b"},chart.Nodes.Select(node=>node.Id));Assert.Equal("#ffeedd",chart.Nodes[0].BackgroundColor);
    }

    internal static string Fixture(string family)=>File.ReadAllText(Path.Combine(TestRepository.Root,"tests","mermaid-conformance","fixtures",family+"-style-settings.mmd"));
}
