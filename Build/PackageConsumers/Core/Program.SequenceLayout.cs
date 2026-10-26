using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void SequenceLayoutContracts() {
        var role=new TextStyleOverride{FontSize=18,FontWeight="700"};var model=SequenceArtifact.Create("package-sequence")
            .WithSize(640,480).AddParticipant("a","Review").AddParticipant("b","Store").AddMessage("a","b","Save");
        model.Layout=new SequenceLayoutPolicy(participantMinimumWidth:100,participantSpacing:24,messageSpacing:30,canvasPadding:new ChartPadding(8,12,16,20));
        model.Typography=new SequenceTypography(participant:role,message:new TextStyleOverride{FontSize=20});role.FontSize=99;
        var theme=VisualTheme.Graphite().Configure(builder=>{builder.Spacing=19;builder.SeriesStrokeWidth=3;builder.AxisStrokeWidth=2;});
        var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800,600)),theme);
        var prepared=model.Prepare(context);var original=prepared.ToSvg();var envelope=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        var restored=VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToSequenceArtifact();
        PackageAssertions.Require(envelope.Version==2&&restored.Theme.ToJson()==theme.ToJson()&&restored.Typography?.Participant?.FontSize==18,"Sequence policy snapshot changed.");
        PackageAssertions.Require(original==restored.Prepare(context).ToSvg(),"Sequence restore reapplied presentation or changed geometry.");
        model.Messages[0].Text="Later";model.Typography!.Participant!.FontSize=77;PackageAssertions.Require(original==prepared.ToSvg(),"Sequence capture retained mutable owners.");
        PackageAssertions.Png(prepared.ToPng());
    }
}
