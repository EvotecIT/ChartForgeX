using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartCatalogRenderingTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AllFourCatalogProgramsShareDirectFenceNativeAndPortableFacts(bool dark) {
        foreach(var suffix in new[]{"a","b","c","d"}) {
            var source=MermaidFlowchartCatalogCausalTests.Fixture(suffix);
            if(dark)source="%%{init: {'theme':'dark'}}%%\n"+source;
            var direct=MermaidRenderer.Render(source);Assert.Empty(direct.Diagnostics);
            var fence=new MermaidVisualMarkupParser().Parse("```mermaid\n"+source+"\n```");Assert.False(fence.HasErrors);
            var artifact=direct.Artifact!;var fenced=Assert.Single(fence.Artifacts);
            Assert.Equal(artifact.ToSvg(),fenced.ToSvg());Assert.Equal(artifact.ToPng(),fenced.ToPng());
            var facts=artifact.ToInterchangeEnvelope();Assert.Equal(2,facts.Version);
            Assert.Equal(facts.ToJson(),VisualArtifactInterchangeEnvelope.FromUtf8Json(facts.ToUtf8Json()).ToJson());
            Assert.DoesNotContain(XDocument.Parse(artifact.ToSvg()).Descendants(),element=>element.Name.LocalName is "script" or "image" or "foreignObject");
            var chart=Assert.IsType<TopologyChart>(artifact.Model);
            foreach(var node in chart.Nodes.Where(node=>!node.ShowLabel))Assert.False(string.IsNullOrWhiteSpace(node.Label));
            MermaidFlowchartCatalogCausalTests.Capture(artifact,"flowchart-catalog-"+suffix+(dark?"-dark":"-light"));
        }
    }
}
