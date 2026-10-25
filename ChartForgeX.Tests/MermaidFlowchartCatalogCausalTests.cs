using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartCatalogCausalTests {
    [Theory]
    [InlineData("a",14)] [InlineData("b",13)] [InlineData("c",13)] [InlineData("d",13)]
    public void WholePrimaryCatalogReachesNativeSurfacesWithoutFallbackWarnings(string suffix, int count) {
        var source = Fixture(suffix);
        var rendered = MermaidRenderer.Render(source);
        if (rendered.Artifact != null) Capture(rendered.Artifact, "flowchart-catalog-causal-" + suffix);
        Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidFlowchartDocument>(rendered.Document);
        Assert.Equal(source, document.SourceText);
        Assert.Equal(count, document.Nodes.Count);
        Assert.Equal(count - 1, document.Edges.Count);
        Assert.All(document.Nodes, node => Assert.Equal(node.Properties["label"], node.Text));
        var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        Assert.Equal(document.Nodes.Select(node => node.Id), chart.Nodes.Select(node => node.Id));
        foreach (var node in document.Nodes) {
            var native = chart.Nodes.Single(item => item.Id == node.Id);
            Assert.Equal(node.Properties["shape"], native.Metadata["mermaid.property.shape"]);
            Assert.Equal(node.Properties["label"], native.Label);
        }
        if (suffix == "a") Assert.Equal("ScallopedBurst", chart.Nodes.Single(node => node.Id == "n13").Shape.ToString());
    }

    [Fact]
    public void ExistingNativeShapeBytesAndSourceArraysHaveSeparateBeforeControls() {
        const string source = "flowchart LR\na[Process] --> b(Rounded) --> c{Decision} --> d[(Store)]\nd --> e((Circle)) --> f{{Prepare}}";
        var rendered = MermaidRenderer.Render(source);
        Capture(rendered.Artifact!, "flowchart-catalog-ordinary-source");
        Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidFlowchartDocument>(rendered.Document);
        Assert.Equal(new[] { "a", "b", "c", "d", "e", "f" }, document.Nodes.Select(node => node.Id));
        Assert.Equal(5, document.Edges.Count);
        // This phase requires the composed Actor0-26 prerequisite; it does not assume a universal envelope version.
        for (var value = 0; value <= 26; value++) {
            Assert.True(Enum.IsDefined(typeof(TopologyNodeShape), value));
            var chart = TopologyChart.Create().WithTitle("").WithViewport(380,230,12).WithLegend(null);
            chart.Nodes.Add(new TopologyNode { Id="shape",Label="Native caption",X=30,Y=25,Width=180,Height=110,
                Shape=(TopologyNodeShape)value,ShowStatusBadge=false,PreserveDisplayModeSize=true,FitLabelToContent=true });
            Capture(chart.ToVisualArtifact(), "flowchart-catalog-ordinary-native-" + value);
        }
    }

    [Theory]
    [InlineData("LR",10,70)] [InlineData("RL",10,70)] [InlineData("TB",70,10)] [InlineData("BT",70,10)]
    public void ForkUsesNativeSynchronizationGeometryAndKeepsItsUnpaintedSourceLabel(string direction,int width,int height) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart " + direction + "\nbar@{ shape: fork, label: \"Retained fork\" }");
        var artifact=parsed.Document!.ToVisualArtifact(); Capture(artifact,"flowchart-catalog-fork-"+direction);
        Assert.Empty(parsed.Diagnostics);
        var node=Assert.Single(parsed.Document!.ToTopologyChart().Nodes);
        Assert.Equal(TopologyNodeShape.SynchronizationBar,node.Shape); Assert.False(node.ShowLabel); Assert.False(node.FitLabelToContent);
        Assert.Equal(width,node.Width);Assert.Equal(height,node.Height);Assert.Equal("Retained fork",node.Label);
    }

    [Fact]
    public void CollapsedAgentflowUsesTheNativeSummaryAndPreservesCrossingSourceFacts() {
        const string source="agentflow-beta LR\nflow summary[Summary]@{ view: collapsed }\na --> b\nend\nb --> outside";
        var rendered=MermaidRenderer.Render(source);
        if(rendered.Artifact!=null)Capture(rendered.Artifact,"agentflow-native-summary-causal");
        Assert.Empty(rendered.Diagnostics);
        var document=Assert.IsType<MermaidAgentflowDocument>(rendered.Document);Assert.Equal(2,document.Edges.Count);
        var chart=Assert.IsType<TopologyChart>(rendered.Artifact!.Model);var summary=chart.Nodes.Single(node=>node.Id=="summary");
        Assert.Equal("CollapsedContainer",summary.Shape.ToString());Assert.Equal("transparent",summary.BackgroundColor);
        var edge=Assert.Single(chart.Edges);Assert.Equal("summary",edge.SourceNodeId);Assert.Equal("outside",edge.TargetNodeId);
        Assert.Equal("b",edge.Metadata["mermaid.agentflow.source"]);
    }

    internal static string Fixture(string suffix)=>File.ReadAllText(Path.Combine(TestRepository.Root,"tests","mermaid-conformance","fixtures","flowchart-native-shape-catalog-"+suffix+".mmd"));
    internal static void Capture(VisualArtifact artifact,string name) {
        var output=Environment.GetEnvironmentVariable("CFX_NATIVE_CATALOG_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;
        Directory.CreateDirectory(output);var stem=Path.Combine(output,name);
        File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".png",artifact.ToPng());File.WriteAllText(stem+".json",artifact.ToInterchangeJson());
    }
}
