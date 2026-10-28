using System;
using ChartForgeX.VisualArtifacts;
internal static partial class Program {
    private static void SequenceSelfRoutesContracts() {
        var model=SequenceArtifact.Create("self-consumer").WithSize(1000,800).AddParticipant("A","Review").AddMessage("A","A","Check");model.Layout=new SequenceLayoutPolicy(selfMessageRouting:SequenceSelfMessageRouting.Curved);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(model.ToVisualArtifact().ToInterchangeJson()).ToSequenceArtifact();if(restored.Layout?.SelfMessageRouting!=SequenceSelfMessageRouting.Curved||!restored.ToSvg().Contains("C")||restored.ToPng().Length==0)throw new InvalidOperationException("Native self route did not survive portable ownership.");
    }
}
