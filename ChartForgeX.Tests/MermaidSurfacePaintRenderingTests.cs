using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSurfacePaintRenderingTests {
    [Theory]
    [InlineData("flowchart")][InlineData("agentflow")]
    public void FiniteSurfaceStylesShareNativeFenceAndPortablePaintFacts(string family) {
        var source=Fixture(family);var direct=MermaidRenderer.Render(source);MermaidSurfacePaintCausalTests.Capture(direct,family+"-surface-fixture");Assert.Empty(direct.Diagnostics);
        var fence=new MermaidVisualMarkupParser().Parse("```mermaid\n"+source+"\n```");Assert.False(fence.HasErrors);var fenced=Assert.Single(fence.Artifacts);
        var artifact=Assert.IsType<VisualArtifact>(direct.Artifact);
        Assert.Equal(artifact.ToSvg(),fenced.ToSvg());Assert.Equal(artifact.ToPng(),fenced.ToPng());
        var facts=artifact.ToInterchangeEnvelope();Assert.Equal(2,facts.Version);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToTopologyChart();Assert.Equal(3,restored.Nodes[0].StrokeWidth);Assert.Equal(3,restored.Groups[0].StrokeWidth);
        Assert.Equal(new double[]{9,4,2,9,4,2},restored.Nodes[0].DashPattern);Assert.Equal(restored.Nodes[0].DashPattern,restored.Groups[0].DashPattern);
        Assert.Null(restored.Nodes[0].Opacity);Assert.Null(restored.Nodes[0].SurfaceOpacity);Assert.Equal(source,direct.Document!.SourceText);
    }
    internal static string Fixture(string family)=>File.ReadAllText(Path.Combine(TestRepository.Root,"tests","mermaid-conformance","fixtures",family+"-native-surface-paints.mmd"));
}
