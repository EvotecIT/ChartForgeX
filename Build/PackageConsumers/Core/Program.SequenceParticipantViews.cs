using System.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void SequenceParticipantViewContracts() {
        var model=SequenceArtifact.Create("package-views").WithSize(900,800).AddParticipant("a","Review").AddParticipant("unused","Retained").AddParticipant("b","Archive").AddMessage("a","b","Save");
        model.Layout=new SequenceLayoutPolicy(mirrorParticipants:true,hideUnusedParticipants:true);var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1000,900),24));var prepared=model.Prepare(context);
        var portable=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var restored=VisualArtifactInterchangeEnvelope.FromJson(portable.ToJson()).ToSequenceArtifact();
        PackageAssertions.Require(portable.Nodes.Count==3&&prepared.Regions.Count(item=>item.Role=="sequence-participant")==2&&prepared.Regions.Count(item=>item.Role=="sequence-participant-footer")==2,"Sequence view deleted source facts or lost footer occurrences.");
        PackageAssertions.Require(prepared.ToSvg()==restored.Prepare(context).ToSvg(),"Sequence view portable preparation changed.");PackageAssertions.Png(prepared.ToPng());
        restored.Layout=new SequenceLayoutPolicy();PackageAssertions.Require(restored.Prepare(context).Regions.Count(item=>item.Role=="sequence-participant")==3,"Retained unused source could not be revealed.");
    }
}
