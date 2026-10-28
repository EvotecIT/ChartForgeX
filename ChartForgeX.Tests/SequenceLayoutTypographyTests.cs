using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceLayoutTypographyTests {
    [Fact]
    public void NullAndEmptyPoliciesKeepExactNativeSvgPngJsonAndVersion() {
        var model=SequencePolicyFixture.Basic();var svg=model.ToSvg();var png=model.ToPng();var json=model.ToVisualArtifact().ToInterchangeJson();
        SequencePolicyFixture.Capture("sequence-policy-null",model);
        model.Layout=new SequenceLayoutPolicy();model.Typography=new SequenceTypography(new TextStyleOverride(),new TextStyleOverride());
        SequencePolicyFixture.Capture("sequence-policy-empty",model);
        Assert.Equal(svg,model.ToSvg());Assert.Equal(png,model.ToPng());Assert.Equal(json,model.ToVisualArtifact().ToInterchangeJson());
        Assert.Equal(1,VisualArtifactInterchangeEnvelope.FromJson(json).Version);
    }
    [Fact]
    public void RoleStylesReachActualPaintAndMeasuredHeaderNoteFragmentAndNumberAllocation() {
        var model=SequencePolicyFixture.Styled();var prepared=model.Prepare(SequencePolicyFixture.Context());
        SequencePolicyFixture.Capture("sequence-policy-five-roles",prepared);
        AssertStyle("sequence-participant","Review",18,700);AssertStyle("sequence-message","Save",20,500);
        AssertStyle("sequence-note","Note",16,400);AssertStyle("sequence-block","alt",15,600);AssertStyle("sequence-message","1.",13,700);
        var envelope=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        Assert.All(envelope.Nodes,item=>{Assert.True(item.Width>=132);Assert.True(item.Height>=72);});
        var bars=XDocument.Parse(prepared.ToSvg()).Descendants().Where(item=>(string?)item.Attribute("data-cfx-role")=="sequence-activation-bar").ToArray();
        Assert.NotEmpty(bars);Assert.All(bars,item=>Assert.Equal(14,double.Parse(item.Attribute("width")!.Value,CultureInfo.InvariantCulture)));
        void AssertStyle(string role,string text,double size,int weight) {
            var node=SequencePolicyFixture.Text(prepared,role,text);Assert.Equal(size,double.Parse(node.Attribute("font-size")!.Value,CultureInfo.InvariantCulture));
            Assert.Equal(weight,int.Parse(node.Attribute("font-weight")!.Value,CultureInfo.InvariantCulture));
        }
    }
    [Fact]
    public void ActorStereotypeAndSymbolUseTheSameRoleAndGrowTheLifelineOrigin() {
        var model=SequencePolicyFixture.Styled();var before=model.Prepare(SequencePolicyFixture.Context());
        model.Participants[0].Kind=SequenceArtifactParticipantKind.Actor;model.Participants[0].Stereotype="human";model.Participants[1].Symbol="S";
        var after=model.Prepare(SequencePolicyFixture.Context());SequencePolicyFixture.Capture("sequence-policy-actor-caption",after);
        var first=before.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Nodes[0];
        var actor=after.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Nodes[0];Assert.True(actor.Height>first.Height);
        var stereotype=SequencePolicyFixture.Text(after,"sequence-participant","human");
        Assert.Equal(15.3,double.Parse(stereotype.Attribute("font-size")!.Value,CultureInfo.InvariantCulture));
        var lifeline=XDocument.Parse(after.ToSvg()).Descendants().First(item=>(string?)item.Attribute("data-cfx-role")=="sequence-lifeline");
        Assert.Equal((actor.Y!.Value+actor.Height!.Value).ToString("0.###",CultureInfo.InvariantCulture),lifeline.Attribute("y1")!.Value);
        Assert.Contains("S Store",after.ToSvg());
    }

    [Fact]
    public void NumberRoleInheritsAuthoredMessageInkWhileKeepingIndependentTypography() {
        var model=SequencePolicyFixture.Styled();model.Paint.MessageText=ChartColor.FromHex("#008800");
        model.Typography=new SequenceTypography(message:new TextStyleOverride{FontSize=20,Color=ChartColor.FromHex("#880000")},number:new TextStyleOverride{FontSize=13});
        var prepared=model.Prepare(SequencePolicyFixture.Context());SequencePolicyFixture.Capture("sequence-policy-number-ink",prepared);
        var number=SequencePolicyFixture.Text(prepared,"sequence-message","1.");var message=SequencePolicyFixture.Text(prepared,"sequence-message","Save");
        Assert.Equal("#880000",number.Attribute("fill")!.Value);Assert.Equal(number.Attribute("fill")!.Value,message.Attribute("fill")!.Value);
    }

    [Fact]
    public void MeasuredSpacingMinimaAndParticipantOverrideComposeWithoutChangingSourceOrder() {
        var model=SequencePolicyFixture.Basic();model.Width=120;model.Padding=24;model.Layout=new SequenceLayoutPolicy(participantMinimumWidth:110,participantMinimumHeight:64,participantSpacing:40,messageSpacing:32,noteSpacing:20);
        model.Participants[1].MinimumWidth=148;
        var natural=XDocument.Parse(model.ToSvg());var width=double.Parse(natural.Root!.Attribute("width")!.Value,CultureInfo.InvariantCulture);
        var prepared=model.Prepare(SequencePolicyFixture.Context(width:width));SequencePolicyFixture.Capture("sequence-policy-allocations",prepared);
        model.Layout=new SequenceLayoutPolicy(participantMinimumWidth:110,participantMinimumHeight:64,participantSpacing:8,messageSpacing:32,noteSpacing:20);
        var smallerWidth=double.Parse(XDocument.Parse(model.ToSvg()).Root!.Attribute("width")!.Value,CultureInfo.InvariantCulture);
        Assert.True(width>smallerWidth);
        var envelope=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var first=envelope.Nodes[0];var second=envelope.Nodes[1];
        Assert.Equal(new[]{"A","B"},envelope.Nodes.Select(item=>item.Id));Assert.True(first.Width>=110);Assert.True(second.Width>=148);
        Assert.True(second.X-(first.X+first.Width)>=40);Assert.All(envelope.Nodes,item=>Assert.True(item.Height>=64));
        var message=envelope.Edges[0].ResolvedRoute[0].Y;var next=envelope.Edges[1].ResolvedRoute[0].Y;Assert.True(next-message>=32);
        Assert.Equal(new[]{"Save","Ready"},model.Messages.Select(item=>item.Text));
    }
    [Fact]
    public void MessageAndNoteMarginsChangeActualRowsAndMeasuredNoteInterior() {
        var model=SequencePolicyFixture.Basic();model.AddNote(SequenceArtifactNotePlacement.Over,new[]{"A","B"},"Note");model.Notes[0].StepIndex=1;
        model.Layout=new SequenceLayoutPolicy(messageSpacing:8,noteSpacing:4);var small=model.Prepare(SequencePolicyFixture.Context());
        model.Layout=new SequenceLayoutPolicy(messageSpacing:32,noteSpacing:20);var large=model.Prepare(SequencePolicyFixture.Context());
        SequencePolicyFixture.Capture("sequence-policy-small-margins",small);SequencePolicyFixture.Capture("sequence-policy-large-margins",large);
        var a=small.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var b=large.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        var noteA=a.Annotations.Single(item=>item.Role==VisualArtifactInterchangeAnnotationRole.SequenceNote);
        var noteB=b.Annotations.Single(item=>item.Role==VisualArtifactInterchangeAnnotationRole.SequenceNote);
        Assert.True(double.Parse(noteB.Extensions["chartforgex.bounds.height"],CultureInfo.InvariantCulture)>double.Parse(noteA.Extensions["chartforgex.bounds.height"],CultureInfo.InvariantCulture));
        Assert.Equal(a.Edges[0].ResolvedRoute[0].Y,b.Edges[0].ResolvedRoute[0].Y);
        Assert.True(b.Edges[1].ResolvedRoute[0].Y>a.Edges[1].ResolvedRoute[0].Y+24);
    }

    [Fact]
    public void PolicyInputsGettersAndPreparedSnapshotsAreDetachedFromLaterNativeEdits() {
        var style=new TextStyleOverride{FontSize=18,FontWeight="700"};var typography=new SequenceTypography(participant:style);
        var model=SequencePolicyFixture.Basic();model.Typography=typography;style.FontSize=99;typography.Participant!.FontSize=88;
        var prepared=model.Prepare(SequencePolicyFixture.Context());var before=prepared.ToSvg();var json=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeJson();
        SequencePolicyFixture.Capture("sequence-policy-detached",prepared);
        Assert.Equal(18,model.Typography!.Participant!.FontSize);model.Participants[0].Label="Later";model.Messages[0].Text="Later message";model.Typography=new SequenceTypography();
        Assert.Equal(before,prepared.ToSvg());Assert.Equal(json,prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeJson());
        var decoded=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();decoded.Sequence!.Typography!.Participant!.FontSize=77;
        decoded.Sequence.Theme=VisualTheme.Graphite().Configure(builder=>builder.Spacing=77);decoded.Sequence.ThemeMode=VisualThemeMode.Dark;
        Assert.Equal(json,prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeJson());
    }
    [Fact]
    public void PreparedAndRawPoliciesRestoreThemeGeometryAndRoleFontsExactlyOnce() {
        var model=SequencePolicyFixture.Styled();model.Padding=24;model.Theme=VisualTheme.Graphite().Configure(builder=>{builder.Spacing=19;builder.AxisStrokeWidth=2.5;builder.SeriesStrokeWidth=3;});
        var context=SequencePolicyFixture.Context(model.Theme);var prepared=model.Prepare(context);
        var envelope=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var restored=VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToSequenceArtifact();
        var second=restored.Prepare(context);SequencePolicyFixture.Capture("sequence-policy-roundtrip-first",prepared);SequencePolicyFixture.Capture("sequence-policy-roundtrip-second",second);
        Assert.Equal(2,envelope.Version);Assert.Equal(context.Theme.ToJson(),restored.Theme.ToJson());Assert.Equal(prepared.ToSvg(),second.ToSvg());Assert.Equal(prepared.ToPng(),second.ToPng());
        Assert.Equal(model.ToSvg(),restored.ToSvg());Assert.Equal(model.ToPng(),restored.ToPng());
        var raw=VisualArtifactInterchangeEnvelope.FromJson(model.ToVisualArtifact().ToInterchangeJson()).ToSequenceArtifact();
        Assert.Equal(model.Theme.ToJson(),raw.Theme.ToJson());Assert.Equal(model.Layout!.MessageSpacing,raw.Layout!.MessageSpacing);
        var fresh=restored.Prepare(SequencePolicyFixture.Context(model.Theme,1200));SequencePolicyFixture.Capture("sequence-policy-roundtrip-fresh",fresh);
        Assert.Equal(20,double.Parse(SequencePolicyFixture.Text(fresh,"sequence-message","Save").Attribute("font-size")!.Value,CultureInfo.InvariantCulture));
        Assert.Equal(new[]{"A","B"},restored.Participants.Select(item=>item.Id));
    }
    [Fact]
    public void CapturedExplicitContextFontIsDistinctFromAndDoesNotReplayTheNativeTheme() {
        var model=SequencePolicyFixture.Styled();var baseline=SequencePolicyFixture.Context();
        var context=new VisualRenderContext(baseline.Layout,baseline.Theme,font:new FontSpec{Family="Courier New, monospace"});
        var prepared=model.Prepare(context);var restored=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().ToSequenceArtifact();
        var again=restored.Prepare(baseline);SequencePolicyFixture.Capture("sequence-policy-context-font-first",prepared);SequencePolicyFixture.Capture("sequence-policy-context-font-second",again);
        Assert.Equal(baseline.Theme.ToJson(),restored.Theme.ToJson());Assert.Equal("Courier New, monospace",restored.Typography!.Participant!.FontFamily);
        Assert.Contains("Courier New",SequencePolicyFixture.Text(again,"sequence-participant","Review").Attribute("font-family")!.Value);
        Assert.Equal(prepared.ToSvg(),again.ToSvg());Assert.Equal(prepared.ToPng(),again.ToPng());
    }

    [Fact]
    public void OuterInsetsAreModelDefaultsWhileExplicitContextOwnsItsIndependentEdges() {
        var model=SequencePolicyFixture.Basic();model.Layout=new SequenceLayoutPolicy(canvasPadding:new ChartPadding(36,28,40,32));
        SequencePolicyFixture.Capture("sequence-policy-natural-insets",model);
        var context=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1000,800),new ChartPadding(8,12,16,20)));
        var prepared=model.Prepare(context);SequencePolicyFixture.Capture("sequence-policy-explicit-insets",prepared);
        var capture=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        Assert.Equal(context.Layout.PaddingEdges,capture.Sequence!.Layout!.CanvasPadding);Assert.Equal(new ChartPadding(36,28,40,32),model.Layout.CanvasPadding);
    }
    [Fact]
    public void NoWrapPreservesWholeAuthoredLinesAndRejectsAContradictoryFixedViewport() {
        var model=SequencePolicyFixture.Basic();model.Messages[0].Text="A complete deliberately long authored request caption with a retained second line\nSecond line";
        model.Layout=new SequenceLayoutPolicy(wrapLabels:false,wrapPadding:12);
        SequencePolicyFixture.Capture("sequence-policy-unwrapped-natural",model);
        var svg=model.ToSvg();Assert.Contains("A complete deliberately long authored request caption with a retained second line",svg);Assert.Contains("Second line",svg);Assert.DoesNotContain("…",svg);
        Assert.Throws<NotSupportedException>(()=>model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(300,800),12))));
    }
    [Fact]
    public void EmptyMessageSourceCanBeAppendedWithoutChangingExistingPreparedIdentity() {
        var model=SequenceArtifact.Create("refill").WithSize(800,600);model.Layout=new SequenceLayoutPolicy(messageSpacing:24);
        Assert.Throws<InvalidOperationException>(()=>model.Prepare(SequencePolicyFixture.Context()));
        model.AddParticipant("A","Review");var empty=model.Prepare(SequencePolicyFixture.Context());var old=empty.ToSvg();
        model.AddParticipant("B","Store").AddMessage("A","B","Save");var filled=model.Prepare(SequencePolicyFixture.Context());
        SequencePolicyFixture.Capture("sequence-policy-empty-messages",empty);SequencePolicyFixture.Capture("sequence-policy-refilled",filled);
        Assert.Equal(old,empty.ToSvg());Assert.Equal("A",Assert.Single(empty.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Nodes).Id);
        Assert.Equal(new[]{"A","B"},filled.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Nodes.Select(item=>item.Id));
    }
    [Fact]
    public void PortableUnknownPoliciesDowngradeAndMalformedThemeRejectInsteadOfLosingData() {
        var model=SequencePolicyFixture.Styled();var json=model.ToVisualArtifact().ToInterchangeJson();
        Assert.Throws<NotSupportedException>(()=>VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2","\"version\":1",StringComparison.Ordinal)));
        Assert.Throws<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"messageSpacing\":24","\"mysterySpacing\":24",StringComparison.Ordinal)));
        var envelope=VisualArtifactInterchangeEnvelope.FromJson(json);envelope.Sequence!.ThemeMode=null;Assert.Throws<ArgumentException>(()=>envelope.Validate());
        using var document=JsonDocument.Parse(json);var encodedTheme=document.RootElement.GetProperty("sequence").GetProperty("themeJson").GetRawText();
        var theme=document.RootElement.GetProperty("sequence").GetProperty("themeJson").GetString()!;
        foreach(var invalid in new[]{"{invalid",theme.Replace("\"schemaVersion\":1","\"schemaVersion\":99",StringComparison.Ordinal)}) {
            var malformed=json.Replace(encodedTheme,JsonSerializer.Serialize(invalid),StringComparison.Ordinal);
            Assert.ThrowsAny<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(malformed));
        }
    }
    [Theory]
    [InlineData(false,400)]
    [InlineData(true,700)]
    public void NonDefaultContextWeightCapturesTheActualScalarOrCustomizedRoleFace(bool customized,int expectedWeight) {
        var model=SequencePolicyFixture.Basic();
        model.Paint.LifelineStroke=new ChartStrokeStyle(width:.5);
        if(customized)model.Layout=new SequenceLayoutPolicy(messageSpacing:21);
        var baseline=SequencePolicyFixture.Context();
        var authored=new FontSpec { Family=baseline.Font.Family,Weight=700 };
        var context=new VisualRenderContext(baseline.Layout,baseline.Theme,baseline.ThemeMode,baseline.Frame,authored);
        var first=model.Prepare(context);
        var saved=first.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        var restored=VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToSequenceArtifact();
        var again=restored.Prepare(context);
        SequencePolicyFixture.Capture("sequence-weight-"+customized+"-first",first);
        SequencePolicyFixture.Capture("sequence-weight-"+customized+"-restored",again);
        Assert.Equal(expectedWeight.ToString(CultureInfo.InvariantCulture),saved.Sequence!.Typography!.Participant!.FontWeight);
        Assert.All(first.Scene.Nodes.OfType<VisualSceneText>().Where(text=>text.Role=="sequence-label"),
            text=>Assert.Equal(expectedWeight,text.Text.Style.Font.Weight));
        Assert.Equal(first.ToSvg(),again.ToSvg());Assert.Equal(first.ToPng(),again.ToPng());
        authored.Weight=900;Assert.Equal(700,context.Font.Weight);
        Assert.Equal(first.ToSvg(),model.Prepare(context).ToSvg());
    }

}

internal static class SequencePolicyFixture {
    internal static SequenceArtifact Basic()=>SequenceArtifact.Create("m02-policy").WithSize(960,700).AddParticipant("A","Review").AddParticipant("B","Store")
        .AddMessage("A","B","Save").AddMessage("B","A","Ready",SequenceArtifactMessageLineStyle.Dashed,SequenceArtifactMessageKind.Return);
    internal static SequenceArtifact Styled() {
        var model=Basic();model.Layout=new SequenceLayoutPolicy(participantMinimumWidth:132,participantMinimumHeight:72,participantSpacing:32,messageSpacing:24,noteSpacing:14,fragmentMargin:10,fragmentLabelPadding:6,activationWidth:14,wrapPadding:10);
        model.Typography=new SequenceTypography(new TextStyleOverride{FontSize=18,FontWeight="700"},new TextStyleOverride{FontSize=20,FontWeight="500"},
            new TextStyleOverride{FontSize=16,FontWeight="400"},new TextStyleOverride{FontSize=15,FontWeight="600"},new TextStyleOverride{FontSize=13,FontWeight="700"});
        model.Metadata["mermaid.autonumber"]="true";model.AddNote(SequenceArtifactNotePlacement.Over,new[]{"A","B"},"Note");model.Notes[0].StepIndex=1;
        model.AddBlock(SequenceArtifactBlockKind.Alt,"Accepted",0,1);model.AddActivation("B",true,0).AddActivation("B",false,2);return model;
    }
    internal static VisualRenderContext Context(VisualTheme? theme=null,double width=1000)=>new(new VisualLayoutOptions(new VisualSize(width,900),24),theme);
    internal static XElement Text(PreparedVisual prepared,string role,string contains)=>XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(item=>item.Name.LocalName=="text"&&item.Value.Contains(contains,StringComparison.Ordinal))
        .First(item=>item.Ancestors().Any(parent=>(string?)parent.Attribute("data-cfx-role")==role));
    internal static void Capture(string stem,SequenceArtifact model) {
        var directory=DirectoryPath();if(directory==null)return;Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,stem+".svg"),model.ToSvg());File.WriteAllBytes(Path.Combine(directory,stem+".png"),model.ToPng());
        File.WriteAllText(Path.Combine(directory,stem+".json"),model.ToVisualArtifact().ToInterchangeJson());
    }
    internal static void Capture(string stem,PreparedVisual prepared) {
        var directory=DirectoryPath();if(directory==null)return;Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,stem+".svg"),prepared.ToSvg());File.WriteAllBytes(Path.Combine(directory,stem+".png"),prepared.ToPng());
        File.WriteAllText(Path.Combine(directory,stem+".json"),prepared.ToArtifact("m02-policy",VisualArtifactKind.Sequence).ToInterchangeJson());
    }
    private static string? DirectoryPath()=>Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY")??Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
}
