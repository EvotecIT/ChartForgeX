using System;
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;
internal static partial class Program {
    private static void SequenceSourceDefaultsContracts() {var model=SequenceArtifact.Create("sequence-layout").WithSize(1100,820).AddParticipant("A","Review").AddParticipant("B","Archive").AddMessage("A","B","Save");model.AddBlock(SequenceArtifactBlockKind.Loop,"Review work",0,0);model.Layout=new SequenceLayoutPolicy(mirrorParticipants:true,footerBottomMargin:1,fragmentTabMinimumWidth:80,fragmentTabMinimumHeight:40);model.Paint.LifelineStroke=new ChartStrokeStyle(width:.5,dash:Array.Empty<double>());var restored=VisualArtifactInterchangeEnvelope.FromJson(model.ToVisualArtifact().ToInterchangeJson()).ToSequenceArtifact();if(restored.Layout?.FragmentTabMinimumWidth!=80||restored.Paint.LifelineStroke?.Width!=.5||restored.ToPng().Length==0||!restored.ToSvg().Contains("sequence-fragment-tab"))throw new InvalidOperationException("Sequence native allocations were lost.");}
}
