using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ZenUmlPresentationDepthTests {
    [Fact]
    public void DetachedEditableCommonJsonRestoresAllTypedNativeSequenceRecords() {
        var source=SequenceArtifact.Create("typed-roundtrip").WithTitle("Typed editable sequence").WithSubtitle("Every supported typed record").WithSize(1800,1800);
        foreach(var kind in Enum.GetValues<SequenceArtifactParticipantKind>())source.AddParticipant("p"+(int)kind,kind.ToString(),kind);
        var first=source.Participants[0];first.Stereotype="Service";first.Symbol="🚀";first.MinimumWidth=180;first.Fill=ChartColor.FromHex("#336699");first.Href="https://example.invalid/details";first.Metadata["opaque"]="kept";
        foreach(var kind in Enum.GetValues<SequenceArtifactMessageKind>())source.AddMessage("p0","p1",kind.ToString(),kind==SequenceArtifactMessageKind.Return?SequenceArtifactMessageLineStyle.Dashed:SequenceArtifactMessageLineStyle.Solid,kind);
        source.Messages[0].ActivatesTarget=true;source.Messages[1].Deactivates=true;source.Messages[1].SourceId="p1";source.Messages[1].TargetId="p0";
        source.AddActivation("p2",true,0).AddActivation("p2",false,3);source.Activations[0].Metadata["opaque"]="activation";
        foreach(var placement in Enum.GetValues<SequenceArtifactNotePlacement>()) {source.AddNote(placement,new[]{"p3"},placement.ToString());source.Notes.Last().StepIndex=1;}
        foreach(var kind in Enum.GetValues<SequenceArtifactBlockKind>()) {
            var instant=kind is SequenceArtifactBlockKind.Reference or SequenceArtifactBlockKind.Divider;
            source.AddBlock(kind,kind.ToString(),0,instant?0:3,instant);
            if(kind==SequenceArtifactBlockKind.Reference)source.Blocks.Last().ParticipantIds.AddRange(new[]{"p0","p1"});
            if(kind==SequenceArtifactBlockKind.Rect)source.Blocks.Last().BackgroundColor=ChartColor.FromHex("#ddeeff");
            if(!instant)source.AddBranch(kind,"Primary",kind.ToString(),0,3);
        }
        source.AddParticipantGroup("group","Participants",0,1,ChartColor.FromHex("#dceaf8"));source.Paint.NoteBorder=ChartColor.FromHex("#008844");
        var saved=VisualArtifactInterchangeEnvelope.FromJson(source.ToVisualArtifact().ToInterchangeJson());var restored=saved.ToSequenceArtifact();
        Assert.Equal(saved.Nodes.Select(item=>item.Id),restored.Participants.Select(item=>item.Id));
        Assert.Equal(source.Participants.Select(item=>(item.Kind,item.Label,item.Stereotype,item.Symbol,item.MinimumWidth,item.Fill,item.Href)),restored.Participants.Select(item=>(item.Kind,item.Label,item.Stereotype,item.Symbol,item.MinimumWidth,item.Fill,item.Href)));
        Assert.Equal(source.Messages.Select(item=>(item.Text,item.Kind,item.LineStyle,item.ActivatesTarget,item.Deactivates)),restored.Messages.Select(item=>(item.Text,item.Kind,item.LineStyle,item.ActivatesTarget,item.Deactivates)));
        Assert.Equal(source.Notes.Select(item=>(item.Placement,item.StepIndex,item.Text)),restored.Notes.Select(item=>(item.Placement,item.StepIndex,item.Text)));
        Assert.Equal(source.Blocks.Select(item=>(item.Kind,item.StartStepIndex,item.EndStepIndex,item.IsEmpty,item.Depth,item.BackgroundColor)),restored.Blocks.Select(item=>(item.Kind,item.StartStepIndex,item.EndStepIndex,item.IsEmpty,item.Depth,item.BackgroundColor)));
        Assert.Equal(source.Branches.Select(item=>(item.ParentKind,item.Kind,item.Text,item.IsEmpty,item.Depth)),restored.Branches.Select(item=>(item.ParentKind,item.Kind,item.Text,item.IsEmpty,item.Depth)));
        Assert.Equal(source.Activations.Select(item=>(item.ParticipantId,item.Active,item.StepIndex)),restored.Activations.Select(item=>(item.ParticipantId,item.Active,item.StepIndex)));
        Assert.Equal("activation",restored.Activations[0].Metadata["opaque"]);Assert.Equal(source.Paint.NoteBorder,restored.Paint.NoteBorder);
        saved.Nodes[0].Label="edited input";source.Participants[0].Stereotype="edited producer";restored.Participants[0].Label="edited restored model";
        Assert.Equal("Service",restored.Participants[0].Stereotype);Assert.NotEqual(saved.Nodes[0].Label,restored.Participants[0].Label);
    }
    [Fact]
    public void PreparedHostContextCaptureAndReaderMutationsCannotChangeTheNativePresentationSnapshot() {
        var model=Model("zenuml-native-presentation");var family=new FontSpec{Family=BrowserProofFonts.Family};
        var theme=VisualTheme.Graphite().WithTypography(new VisualTypography(BrowserProofFonts.Family,dataLabelSize:17));
        var prepared=model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1400,1800),24),theme,VisualThemeMode.Dark,font:family));
        var artifact=prepared.ToArtifact();var clone=artifact.Clone();var svg=prepared.ToSvg();var png=prepared.ToPng();var json=artifact.ToInterchangeJson();
        var saved=artifact.ToInterchangeEnvelope();var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToSequenceArtifact();
        Assert.Equal(24,restored.Padding);Assert.Equal(17,restored.Theme.Typography.DataLabelSize);Assert.Equal(BrowserProofFonts.Family,restored.Theme.Typography.Family);
        var api=model.Participants.Single(item=>item.Id=="API");api.Symbol="changed";api.Stereotype="changed";api.Fill=ChartColor.FromHex("#000000");api.MinimumWidth=900;
        model.Blocks.Single(item=>item.Kind==SequenceArtifactBlockKind.Reference).ParticipantIds.Clear();model.ThemeMode=VisualThemeMode.Light;family.Family="changed";
        saved.Nodes.Single(item=>item.Extensions.TryGetValue("mermaid.id",out var id)&&id=="API").Sequence!.Symbol="reader mutation";saved.Annotations[0].TargetIds.Clear();clone.Metadata["new"]="host";
        Assert.Equal(svg,prepared.ToSvg());Assert.Equal(png,prepared.ToPng());Assert.Equal(json,artifact.ToInterchangeJson());
        Assert.Contains(restored.Participants,item=>item.Symbol=="🚀");Assert.Equal(2,restored.Blocks.Single(item=>item.Kind==SequenceArtifactBlockKind.Reference).ParticipantIds.Count);
    }
    [Theory] [InlineData("divider-target")] [InlineData("reference-duplicate")] [InlineData("branch-reference")]
    [InlineData("instant-span")] [InlineData("source-order")] [InlineData("unknown-color")]
    public void PortableContradictionsRejectBeforeReconstructionOrPaint(string variant) {
        var saved=Model("zenuml-native-presentation").ToVisualArtifact().ToInterchangeEnvelope();
        var reference=saved.Annotations.Single(item=>item.Sequence?.BlockKind==SequenceArtifactBlockKind.Reference);
        var divider=saved.Annotations.Single(item=>item.Sequence?.BlockKind==SequenceArtifactBlockKind.Divider);
        switch(variant) {
            case "divider-target":divider.TargetIds.Add(saved.Nodes[0].Id);break;
            case "reference-duplicate":reference.TargetIds.Add(reference.TargetIds[0]);break;
            case "branch-reference":saved.Annotations.Add(new VisualArtifactInterchangeAnnotation{Id="contradiction",Role=VisualArtifactInterchangeAnnotationRole.SequenceBranch,Kind="SequenceBranch:Primary",StartIndex=0,Sequence=new(){ParentBlockKind=SequenceArtifactBlockKind.Reference,BranchKind="Primary",IsEmpty=true}});break;
            case "instant-span":reference.Sequence!.IsEmpty=false;reference.EndIndex=reference.StartIndex+1;break;
            case "source-order":reference.Extensions["chartforgex.sequence.endSourceOrder"]="0";reference.Extensions["chartforgex.sequence.closeStep"]="100001";break;
            case "unknown-color":saved.Nodes[0].Sequence!.Fill="url(https://example.invalid/paint)";break;
        }
        Assert.ThrowsAny<ArgumentException>(()=>saved.Validate());Assert.ThrowsAny<ArgumentException>(()=>saved.ToSequenceArtifact());
    }
    [Theory] [InlineData("edited-message-id")] [InlineData("common-field")] [InlineData("unknown-annotation-bag")]
    public void UnrepresentableCarriedFieldsRejectRatherThanDisappearDuringEditableRestoration(string variant) {
        var saved=Model("zenuml-native-presentation").ToVisualArtifact().ToInterchangeEnvelope();
        if(variant=="edited-message-id")saved.Edges[0].Id="edited-message";
        else if(variant=="common-field")saved.Nodes[0].Tooltip="unrepresentable";
        else saved.Annotations.Single(item=>item.Sequence?.BlockKind==SequenceArtifactBlockKind.Reference).Extensions["unrepresentable"]="extra";
        saved.Validate();Assert.Throws<NotSupportedException>(()=>saved.ToSequenceArtifact());
    }
    [Fact]
    public void NativeInstantaneousTargetSpanAndBranchContradictionsRejectAtTheSameOwner() {
        var model=SequenceArtifact.Create("bad").AddParticipant("a").AddBlock(SequenceArtifactBlockKind.Divider,"bad",0,0,true);
        model.Blocks[0].ParticipantIds.Add("a");Assert.Throws<ArgumentException>(()=>model.ToSvg());model.Blocks[0].ParticipantIds.Clear();
        model.Blocks[0].EndStepIndex=1;Assert.Throws<ArgumentException>(()=>model.ToSvg());model.Blocks[0].EndStepIndex=0;
        model.AddBranch(SequenceArtifactBlockKind.Reference,"Primary","bad",0,0,isEmpty:true);Assert.Throws<ArgumentException>(()=>model.ToSvg());
    }
}
