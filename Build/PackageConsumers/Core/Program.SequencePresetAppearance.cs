using System;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;
internal static partial class Program {
    private static void SequencePresetAppearanceNativeContracts() {
        var model=SequenceArtifact.Create("package-bands").WithSize(1100,700).AddParticipant("A","Review\nOwner",SequenceArtifactParticipantKind.Actor).AddParticipant("B","Archive",SequenceArtifactParticipantKind.Control).AddMessage("A","B","Save");model.Layout=new SequenceLayoutPolicy(participantBands:new SequenceParticipantBands(),mirrorParticipants:true);model.Participants[0].Border=ChartColor.FromHex("#123456");model.Participants[0].ActivationFill=ChartColor.FromHex("#abcdef");model.Paint.FragmentTitleText=ChartColor.FromHex("#654321");var json=model.ToVisualArtifact().ToInterchangeJson();var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToSequenceArtifact();if(restored.Layout?.ParticipantBands?.GlyphHeight!=44||restored.Participants[0].Border?.ToCss()!="#123456"||restored.Participants[0].ActivationFill?.ToCss()!="#ABCDEF"||restored.Paint.FragmentTitleText?.ToCss()!="#654321"||restored.ToSvg()!=model.ToSvg()||restored.ToPng().Length==0)throw new InvalidOperationException("Native participant band or paint facts did not survive public save/load.");
    }
}
