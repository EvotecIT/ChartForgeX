using System;
using System.IO;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void SequenceParticipantViewSourceContracts() {
        using var stream=typeof(Program).Assembly.GetManifestResourceStream("PackageProbe.sequence-participant-views.mmd")??throw new InvalidOperationException("Missing participant-view source.");using var reader=new StreamReader(stream);var source=reader.ReadToEnd();
        var result=RenderFixture(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject});PackageAssertions.Require(result.Diagnostics.Count==0&&result.Artifact!=null,"Sequence view source did not lower.");
        var model=(SequenceArtifact)result.Artifact!.Model!;var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200,1000),24));var prepared=model.Prepare(context);var portable=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        PackageAssertions.Require(model.Layout?.MirrorParticipants==true&&model.Layout.HideUnusedParticipants==true&&portable.Nodes.Count==5&&prepared.Regions.Count(item=>item.Role=="sequence-participant")==3,"Source view lost retained participant facts.");
        var fence=CreateFixtureParser().Parse("```mermaid\n"+source+"\n```");PackageAssertions.Require(fence.Diagnostics.Count==0&&fence.Artifacts.Count==1,"Fenced view source changed.");PackageAssertions.Png(prepared.ToPng());
    }
}
