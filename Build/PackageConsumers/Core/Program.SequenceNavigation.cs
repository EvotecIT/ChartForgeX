using System;
using ChartForgeX.VisualArtifacts;
internal static partial class Program {
    private static void SequenceNavigationContracts() {
        var model=SequenceArtifact.Create("navigation-consumer").WithSize(1000,820).AddParticipant("A","Review").AddParticipant("B","Archive").AddMessage("A","B","Save");var links=new[]{new VisualNavigationLink("Guide","#guide"),new VisualNavigationLink("Status","../status")};model.Participants[0].NavigationLinks=links;links[0]=new VisualNavigationLink("Changed","#changed");var artifact=model.ToVisualArtifact();var json=artifact.ToInterchangeJson();var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToSequenceArtifact();if(restored.Participants[0].NavigationLinks.Count!=2||restored.Participants[0].NavigationLinks[0].Href!="#guide"||restored.ToPng().Length==0||restored.ToSvg().Contains("<details"))throw new InvalidOperationException("Detached native navigation was lost.");
    }
}
