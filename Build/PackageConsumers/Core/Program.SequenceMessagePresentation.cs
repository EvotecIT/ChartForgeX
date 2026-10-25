using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void SequenceMessagePresentationContracts() {
        var model=SequenceArtifact.Create("package-number").WithSize(1000,800).AddParticipant("a","Review").AddParticipant("b","Archive").AddMessage("a","b","Save\nFact").AddMessage("b","b","Check\nSelf");
        model.Metadata["mermaid.autonumber"]="true";model.Metadata["mermaid.autonumber.start"]="1000";model.Layout=new SequenceLayoutPolicy(messageAlignment:TextAlignment.Right,noteAlignment:TextAlignment.Center,numberPresentation:SequenceNumberPresentation.Badge);model.Paint.NumberText=ChartColor.White;
        var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1400,1000),24));var prepared=model.Prepare(context);var envelope=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var restored=VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToSequenceArtifact();
        PackageAssertions.Require(envelope.Version==2&&prepared.Regions.Count(item=>item.Role=="sequence-number-badge")==2&&restored.Layout!.MessageAlignment==TextAlignment.Right,"Sequence number/alignment snapshot changed.");
        PackageAssertions.Require(prepared.ToSvg()==restored.Prepare(context).ToSvg(),"Sequence number font or paint replay changed.");PackageAssertions.Png(prepared.ToPng());
    }
}
