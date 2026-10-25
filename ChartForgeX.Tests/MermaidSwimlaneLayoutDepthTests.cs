using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite","Compatibility")]
public sealed partial class MermaidSwimlaneLayoutDepthTests {
    internal static string Source()=>File.ReadAllText(Path.Combine(TestRepository.Root,"Build","PackageConsumers","Adapters","swimlane-native-layout-depth.mmd"));
    internal static MermaidRenderOptions Options(bool dark,int width)=>new() {WarningPolicy=MermaidWarningPolicy.Reject,
        Presentation=MermaidNestedSwimlaneTests.Options(dark,width).Presentation,Swimlane=new MermaidFlowchartRenderOptions {Width=width,Height=850}};

    [Theory]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public void SourceSpacingLocalDirectionAndFilteredRanksUseOneNativeSceneAcrossDirectAndFencedConsumers(bool dark,int width) {
        var source=Source();var options=Options(dark,width);var result=MermaidRenderer.Render(source,options);Assert.Empty(result.Diagnostics);
        var artifact=result.Artifact!;var chart=Assert.IsType<TopologyChart>(artifact.Model);var render=chart.DefaultRenderOptions!;
        Assert.Equal(70,render.LayeredNodeSpacing);Assert.Equal(130,render.LayeredRankSpacing);Assert.True(render.SwimlaneIgnoreCrossLaneRankConstraints);
        Assert.Equal(28,chart.Viewport.Padding);Assert.Equal(TopologyLayoutDirection.TopToBottom,chart.Groups.Single(group=>group.Id=="API").LayoutDirection);
        var fence=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");Assert.False(fence.HasErrors);
        MermaidNestedSwimlaneTests.Capture(artifact,"swimlane-depth-source-"+width+"-"+dark);
        Assert.Equal(artifact.ToSvg(),Assert.Single(fence.Artifacts).ToSvg());Assert.Equal(artifact.ToPng(),fence.Artifacts[0].ToPng());
        var saved=artifact.ToInterchangeEnvelope();MermaidNestedSwimlaneTests.AssertContainment(saved);
        var a=saved.Nodes.Single(node=>node.Id=="A");var b=saved.Nodes.Single(node=>node.Id=="B");Assert.True(b.Y!.Value>a.Y!.Value);
        Assert.Equal(new[]{"P","A","B","C","D"},saved.Nodes.Select(node=>node.Id));Assert.Contains(saved.Edges,edge=>edge.SourceId=="Product"&&edge.TargetId=="Partner");
        Assert.Equal(source,saved.Extensions["mermaid.source"]);Assert.Contains("Delivery responsibilities",artifact.ToSvg());
    }

    [Fact]
    public void ExplicitHostPaddingAndSizingWinWithoutReplacingSourceSpacingOrRankPolicy() {
        var parsed=new MermaidParser().ParseFlowchart(Source());Assert.Empty(parsed.Diagnostics);
        var chart=parsed.Document!.ToTopologyChart(new MermaidFlowchartRenderOptions {Padding=41,Width=390,Height=700,
            Presentation=new MermaidPresentationOptions {TopologySizing=MermaidTopologySizing.Natural}});
        Assert.Equal(41,chart.Viewport.Padding);Assert.False(chart.DefaultRenderOptions!.FitContentToViewport);
        Assert.Equal(70,chart.DefaultRenderOptions.LayeredNodeSpacing);Assert.True(chart.DefaultRenderOptions.SwimlaneIgnoreCrossLaneRankConstraints);
    }

    [Theory]
    [InlineData("flowchart","nodeSpacing","-1")] [InlineData("flowchart","rankSpacing","\"wide\"")]
    [InlineData("flowchart","diagramPadding","[]")] [InlineData("swimlane","ignoreCrossLaneEdges","\"true\"")]
    [InlineData("swimlane","lineHops","\"cross\"")]
    public void InvalidFiniteSourceSettingsFailAtTheActualSetting(string section,string name,string value) {
        var result=MermaidRenderer.Render("---\nconfig:\n  "+section+":\n    "+name+": "+value+"\n---\nswimlane-beta LR\nA --> B");
        Assert.True(result.HasErrors);Assert.Null(result.Artifact);Assert.Contains(result.Diagnostics,item=>item.Severity==MermaidDiagnosticSeverity.Error&&item.Span.Line==4);
    }

    [Theory]
    [InlineData("lineHops","true")]
    [InlineData("lineHops","\"arc\"")] [InlineData("lineHops","\"gap\"")]
    public void SupportedHopValuesUseTheSelectedNativePolicyAndRetainTheirLocatedSourceFacts(string name,string value) {
        var source="---\nconfig:\n  swimlane:\n    "+name+": "+value+"\n---\nswimlane-beta LR\nA --> B";
        var result=MermaidRenderer.Render(source,new MermaidRenderOptions {WarningPolicy=MermaidWarningPolicy.Reject});
        if(result.Artifact!=null)MermaidNestedSwimlaneTests.Capture(result.Artifact,"swimlane-depth-supported-hop-"+value.Replace("\"",""));
        Assert.False(result.HasErrors);Assert.Empty(result.Diagnostics);Assert.NotNull(result.Artifact);
        var chart=Assert.IsType<TopologyChart>(result.Artifact!.Model);Assert.NotNull(chart.CrossingPaint);
        Assert.Equal(TopologyCrossingCurveHandling.SkipUnsupported,chart.CrossingPaint!.CurveHandling);
        Assert.Equal(value=="\"gap\""?TopologyCrossingPaintStyle.Gap:TopologyCrossingPaintStyle.Arc,chart.CrossingPaint.Style);
        Assert.Equal(source,result.Document!.SourceText);Assert.Contains(result.Document.Configuration.Settings,setting=>setting.Path=="swimlane.lineHops"&&setting.Span.Line==4);
    }

    [Fact]
    public void FalseOptimizationOrderingAndHopSettingsKeepTheIncumbentPaintAndNodeOrder() {
        const string body="swimlane-beta LR\nsubgraph Lane[Lane]\n A --> B\nend";
        var configured=MermaidRenderer.Render("---\nconfig:\n  swimlane:\n    automaticLaneOrdering: false\n    optimizeRanksByCrossings: false\n    lineHops: false\n---\n"+body);
        var original=MermaidRenderer.Render(body);MermaidNestedSwimlaneTests.Capture(original.Artifact!,"swimlane-false-policies-ordinary");MermaidNestedSwimlaneTests.Capture(configured.Artifact!,"swimlane-false-policies-authored");
        Assert.Empty(configured.Diagnostics);Assert.Equal(TopologyStaticPaintObservation.Paint(original.Artifact!.ToSvg()),TopologyStaticPaintObservation.Paint(configured.Artifact!.ToSvg()));Assert.Equal(original.Artifact!.ToPng(),configured.Artifact!.ToPng());
        Assert.Equal(body,original.Artifact!.ToInterchangeEnvelope().Extensions["mermaid.source"]);Assert.Equal(configured.Document!.SourceText,configured.Artifact!.ToInterchangeEnvelope().Extensions["mermaid.source"]);
    }

    [Fact]
    public void RealSourceBoundaryEdgesInheritGlobalDirectionWhileShellEdgesKeepLocalDirection() {
        var source=Source();var shell=new MermaidParser().ParseFlowchart(source).Document!.ToTopologyChart();
        var member=new MermaidParser().ParseFlowchart(source.Replace("Product --> Partner","B --> D",StringComparison.Ordinal)).Document!.ToTopologyChart();
        Assert.Equal(TopologyLayoutDirection.TopToBottom,shell.Groups.Single(group=>group.Id=="API").LayoutDirection);
        Assert.Equal(TopologyLayoutDirection.LeftToRight,member.Groups.Single(group=>group.Id=="API").LayoutDirection);
    }
}
