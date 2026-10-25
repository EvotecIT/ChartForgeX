using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSurfacePaintCausalTests {
    [Theory]
    [InlineData("flowchart")][InlineData("agentflow")]
    public void SourceContourWidthAndDashReachNodesAndCanonicalContainers(string family) {
        var source=Source(family);var rendered=MermaidRenderer.Render(source);Capture(rendered,family+"-surface-width-dash");
        Assert.Empty(rendered.Diagnostics);
        using var json=JsonDocument.Parse(rendered.Artifact!.ToInterchangeJson());
        foreach(var item in new[]{json.RootElement.GetProperty("nodes")[0],json.RootElement.GetProperty("groups")[0]}) {
            var native=item.GetProperty("topology");Assert.Equal(3,native.GetProperty("strokeWidth").GetDouble());
            Assert.Equal(new double[]{9,4,2,9,4,2},native.GetProperty("dashPattern").EnumerateArray().Select(value=>value.GetDouble()));
        }
    }

    [Fact]
    public void SourceOpacityKeepsItsLocatedWarningUntilSurfaceReferenceProofExists() {
        const string source="flowchart LR\na[Work]\nstyle a opacity:0.5";
        var rendered=MermaidRenderer.Render(source);Capture(rendered,"source-opacity-pending-reference");
        Assert.False(rendered.HasErrors);var warning=Assert.Single(rendered.Diagnostics);Assert.Equal(3,warning.Span.Line);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement,warning.Code);
    }

    [Fact]
    public void OrdinarySourceHasSeparateUnmodifiedByteControls() {
        const string source="flowchart LR\nsubgraph review[Review]\na[Work] --> b[Done]\nend\nclassDef marked fill:#ffeedd,stroke:#345678,color:#234567\nclass a,review marked";
        var rendered=MermaidRenderer.Render(source);Capture(rendered,"ordinary-surface-paints");Assert.Empty(rendered.Diagnostics);
        var document=Assert.IsType<MermaidFlowchartDocument>(rendered.Document);Assert.Equal(new[]{"a","b"},document.Nodes.Select(node=>node.Id));Assert.Equal("review",Assert.Single(document.Subgraphs).Id);Assert.Single(document.Edges);
        foreach(var shape in new[]{TopologyNodeShape.Rectangle,TopologyNodeShape.DoubleCircle}) {
            var chart=TopologyChart.Create().WithTitle("").WithViewport(300,250,0).WithLegend(null);
            chart.Nodes.Add(new TopologyNode{Id="node",Label="Node",X=60,Y=60,Width=100,Height=80,Shape=shape,BackgroundColor="#ff0000",Color="#ff0000",ShowStatusBadge=false,PreserveDisplayModeSize=true});
            CaptureArtifact(chart.ToVisualArtifact(),"ordinary-native-paint-"+shape);
        }
    }

    internal static string Source(string family)=>(family=="flowchart"?"flowchart LR\nsubgraph review[Review]":"agentflow-beta LR\nflow review[Review]")+"\na[Work] --> b[Done]\nend\nclassDef marked fill:#ffeedd,stroke:#345678,color:#234567,stroke-width:3px,stroke-dasharray:9 4 2\nclass a,review marked";
    internal static void Capture(MermaidRenderResult result,string name) {
        var output=Environment.GetEnvironmentVariable("CFX_SURFACE_PAINT_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;
        Directory.CreateDirectory(output);var stem=Path.Combine(output,name);
        File.WriteAllText(stem+".diagnostics.json",JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message,item.Severity,item.Span.Line,item.Span.Column,item.Span.Length})));
        if(result.Artifact!=null)CaptureArtifact(result.Artifact,name);
    }
    internal static void CaptureArtifact(VisualArtifact artifact,string name) {
        var output=Environment.GetEnvironmentVariable("CFX_SURFACE_PAINT_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;Directory.CreateDirectory(output);var stem=Path.Combine(output,name);
        File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".png",artifact.ToPng());File.WriteAllText(stem+".json",artifact.ToInterchangeJson());
    }
}
