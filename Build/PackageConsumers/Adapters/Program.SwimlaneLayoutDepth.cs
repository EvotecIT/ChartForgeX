using System;
using System.IO;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void SwimlaneLayoutDepthContracts() {
        using var stream=typeof(Program).Assembly.GetManifestResourceStream("SwimlaneLayoutDepth.mmd")??throw new InvalidOperationException("Packed adapter source missing.");
        using var reader=new StreamReader(stream);var source=reader.ReadToEnd();var options=new MermaidRenderOptions {WarningPolicy=MermaidWarningPolicy.Reject};
        var result=MermaidRenderer.Render(source,options);PackageAssertions.Require(!result.HasErrors&&result.Diagnostics.Count==0&&result.Artifact!=null,"Packed configured Swimlane was not supported.");
        var artifact=result.Artifact!;var chart=(TopologyChart)artifact.Model!;
        PackageAssertions.Require(chart.DefaultRenderOptions!.LayeredNodeSpacing==70&&chart.DefaultRenderOptions.LayeredRankSpacing==130&&chart.DefaultRenderOptions.SwimlaneIgnoreCrossLaneRankConstraints==true,"Packed source policies lost canonical native ownership.");
        PackageAssertions.Require(chart.Groups.Single(group=>group.Id=="API").LayoutDirection==TopologyLayoutDirection.TopToBottom,"Packed local direction was lost.");
        var fence=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");PackageAssertions.Require(!fence.HasErrors&&fence.Artifacts.Count==1,"Packed fenced source failed.");
        PackageAssertions.Require(artifact.ToSvg()==fence.Artifacts[0].ToSvg()&&artifact.ToPng().SequenceEqual(fence.Artifacts[0].ToPng()),"Packed direct and fenced exports differ.");
        var saved=VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());var a=saved.Nodes.Single(node=>node.Id=="A");var b=saved.Nodes.Single(node=>node.Id=="B");
        PackageAssertions.Require(b.Y>a.Y&&saved.Extensions["mermaid.source"]==source&&saved.Edges.Any(edge=>edge.SourceId=="Product"&&edge.TargetId=="Partner"),"Packed scene lost source identity, local geometry or actual cross-boundary edge.");
        PackageAssertions.Require(saved.ToTopologyChart().LayoutMode==TopologyLayoutMode.Manual,"Prepared portable geometry was misrepresented as editable host layout settings.");PackageAssertions.Png(artifact.ToPng());
    }
}
