using ChartForgeX.VisualArtifacts;
using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGraphStyleSettingsCausalTests {
    [Fact]
    public void ExistingNamedEdgeClassesReachTheirNativeAssociationWithoutCreatingNodes() {
        const string source="flowchart LR\nclassDef marked stroke:#123456,color:#654321\nA reads@-->|Reads| B\nclass reads marked";
        var rendered=MermaidRenderer.Render(source);Capture(rendered,"flowchart-named-edge-class");
        Assert.Empty(rendered.Diagnostics);var chart=Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        Assert.Equal(new[]{"A","B"},chart.Nodes.Select(node=>node.Id));var edge=Assert.Single(chart.Edges);
        Assert.Equal("reads",edge.Id);Assert.Equal("#123456",edge.Color);Assert.Equal("#654321",edge.LabelColor);
        Assert.Equal("marked",edge.Metadata["mermaid.classes"]);
    }

    [Fact]
    public void NamedEdgeStyleRemainsLocatedAndInertWithoutAnIdentityCollision() {
        const string source="flowchart LR\nA reads@--> B\nstyle reads stroke:#123456";
        var rendered=MermaidRenderer.Render(source);Capture(rendered,"flowchart-named-edge-style-noop");
        Assert.False(rendered.HasErrors);var warning=Assert.Single(rendered.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement,warning.Code);Assert.Equal(3,warning.Span.Line);
        var chart=Assert.IsType<TopologyChart>(rendered.Artifact!.Model);Assert.Equal(2,chart.Nodes.Count);var edge=Assert.Single(chart.Edges);
        // The implicit source preset owns the edge paint; the unsupported statement must not change it.
        Assert.Equal(ChartForgeX.Primitives.ChartColor.FromHex("#333333"),ChartForgeX.Primitives.ChartColor.Parse(edge.Color!));
        var control=MermaidRenderer.Render(source.Substring(0,source.LastIndexOf('\n')));Assert.Empty(control.Diagnostics);
        Assert.Equal(control.Artifact!.ToSvg(),rendered.Artifact.ToSvg());Assert.Equal(control.Artifact!.ToPng(),rendered.Artifact.ToPng());
        Assert.True(MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject}).HasErrors);
    }

    [Fact]
    public void CanonicalSubgraphKeepsClassAndDirectPaintsAfterPlaceholderRemoval() {
        const string source="flowchart LR\nsubgraph scope[Scope]\nA --> B\nend\nclassDef framed fill:#ffeedd,stroke:#123456,color:#654321\nclass scope framed\nstyle scope fill:#aabbcc";
        var rendered=MermaidRenderer.Render(source);Capture(rendered,"flowchart-group-paints");
        Assert.Empty(rendered.Diagnostics);var chart=Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        Assert.Equal(new[]{"A","B"},chart.Nodes.Select(node=>node.Id));var group=Assert.Single(chart.Groups);
        Assert.Equal("#aabbcc",group.BackgroundColor);Assert.Equal("#123456",group.BorderColor);Assert.Equal("#654321",group.LabelColor);
        Assert.Equal("framed",group.Metadata["mermaid.classes"]);
    }

    [Theory]
    [InlineData("flowchart")][InlineData("agentflow")]
    public void FamilyGraphConfigurationReachesExistingNativeSpacingAndTextFields(string family) {
        var source=SettingsSource(family);var rendered=MermaidRenderer.Render(source);Capture(rendered,family+"-graph-settings");
        Assert.Empty(rendered.Diagnostics);var chart=Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        Assert.Equal(44,chart.Viewport.Padding);Assert.Equal(75,chart.DefaultRenderOptions!.LayeredNodeSpacing);Assert.Equal(140,chart.DefaultRenderOptions.LayeredRankSpacing);
        Assert.False(chart.DefaultRenderOptions.FitContentToViewport);var node=chart.Nodes.Single(item=>item.Id=="work");
        Assert.Equal(160,node.MaximumTextWidth);Assert.Equal(14,node.ContentPadding);
    }

    [Fact]
    public void IndexedEdgeStyleUsesExistingExactDashWidthAndOpacityOwners() {
        const string source="flowchart LR\nA -->|Reads| B\nlinkStyle 0 stroke:#123456,color:#654321,stroke-width:3px,opacity:0.6,stroke-dasharray:9 4 2";
        var rendered=MermaidRenderer.Render(source);Capture(rendered,"flowchart-exact-edge-style");
        Assert.Empty(rendered.Diagnostics);var edge=Assert.Single(Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Edges);
        Assert.Equal(3,edge.StrokeWidth);Assert.Equal(.6,edge.Opacity);Assert.Equal("#654321",edge.LabelColor);
        Assert.Equal(new double[]{9,4,2,9,4,2},edge.DashPattern);
    }

    [Fact]
    public void AgentflowClassFlowAndLinkStyleUseItsOwnGrammarAndSharedNativePaints() {
        const string source="agentflow-beta LR\nflow review[Review]\na[Work] --> b[Check]\nend\nclassDef marked fill:#ffeedd,stroke:#123456,color:#654321\nclass a,review marked\nlinkStyle 0 stroke:#234567,stroke-width:3px";
        var rendered=MermaidRenderer.Render(source);Capture(rendered,"agentflow-style-cascade");
        Assert.Empty(rendered.Diagnostics);var chart=Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        Assert.Equal("#ffeedd",chart.Nodes.Single(node=>node.Id=="a").BackgroundColor);Assert.Equal("#ffeedd",Assert.Single(chart.Groups).BackgroundColor);
        Assert.Equal("#234567",Assert.Single(chart.Edges).Color);Assert.Equal(3,chart.Edges[0].StrokeWidth);
    }

    [Fact]
    public void OrdinaryClassAndUnconfiguredSourceArraysHaveExactByteControls() {
        const string flow="flowchart LR\nclassDef ready fill:#d6e7fa,stroke:#3d68a0\nA[Plan]:::ready --> B[Deliver]";
        const string agent="agentflow-beta LR\na[Plan]@{shape:task} --> b[Deliver]@{shape:tool}";
        foreach(var item in new[]{("flowchart",flow),("agentflow",agent)}) {
            var rendered=MermaidRenderer.Render(item.Item2);Capture(rendered,item.Item1+"-style-settings-ordinary");Assert.Empty(rendered.Diagnostics);
            Assert.Equal(2,Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Nodes.Count);
        }
    }

    internal static string SettingsSource(string family)=>"---\nconfig:\n  "+family+":\n    diagramPadding: 44\n    nodeSpacing: 75\n    rankSpacing: 140\n    wrappingWidth: 160\n    minNodeWidth: 70\n    padding: 14\n    useMaxWidth: false\n---\n"+(family=="flowchart"?"flowchart":"agentflow-beta")+" LR\nwork[Work] --> done[Done]";
    internal static void Capture(MermaidRenderResult result,string name) {
        var output=Environment.GetEnvironmentVariable("CFX_GRAPH_STYLE_SETTINGS_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;
        Directory.CreateDirectory(output);var stem=Path.Combine(output,name);
        File.WriteAllText(stem+".diagnostics.json",JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message,item.Severity,item.Span.Line,item.Span.Column,item.Span.Length})));
        if(result.Artifact==null)return;
        File.WriteAllText(stem+".svg",result.Artifact.ToSvg());File.WriteAllBytes(stem+".png",result.Artifact.ToPng());File.WriteAllText(stem+".json",result.Artifact.ToInterchangeJson());
    }
}
