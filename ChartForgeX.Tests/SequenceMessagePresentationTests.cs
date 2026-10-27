using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceMessagePresentationTests {
    [Fact]
    public void NullAndExplicitInlineKeepExactOrdinaryNativeNumberSvgPngJson() {
        var model=SequenceMessageFixture.Basic();model.Metadata["mermaid.autonumber"]="true";var svg=model.ToSvg();var png=model.ToPng();var json=model.ToVisualArtifact().ToInterchangeJson();SequenceMessageFixture.Capture("sequence-message-native-default",model);
        model.Layout=new SequenceLayoutPolicy(numberPresentation:SequenceNumberPresentation.Inline);SequenceMessageFixture.Capture("sequence-message-native-inline",model);
        Assert.False(model.Layout.HasOverrides);Assert.Equal(svg,model.ToSvg());Assert.Equal(png,model.ToPng());Assert.Equal(json,model.ToVisualArtifact().ToInterchangeJson());Assert.Contains("1. Long request",svg);
    }
    [Theory]
    [InlineData(TextAlignment.Left)] [InlineData(TextAlignment.Center)] [InlineData(TextAlignment.Right)]
    public void MessageAlignmentReachesActualNativeLinePositionsInBothDirectionsAndSelf(TextAlignment alignment) {
        var model=SequenceMessageFixture.Basic();model.AddMessage("B","B","Long request\nNext");model.Layout=new SequenceLayoutPolicy(messageAlignment:alignment);
        var context=SequenceMessageFixture.Context();var prepared=model.Prepare(context);SequenceMessageFixture.Capture("sequence-message-align-"+alignment,prepared);
        var facts=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var elements=XDocument.Parse(prepared.ToSvg()).Descendants().Where(item=>(string?)item.Attribute("data-cfx-role")=="sequence-message").ToArray();
        var style=SequenceMessageFixture.Style(context);var shortWidth=TextLayoutEngine.Measure("Next",style).Width;
        for(var index=0;index<facts.Edges.Count;index++) {
            var bounds=facts.Edges[index].ResolvedLabelBounds!.Value;var text=elements[index].Descendants().Single(item=>item.Name.LocalName=="text"&&item.Value=="Next");
            var x=alignment==TextAlignment.Left?bounds.Left:alignment==TextAlignment.Right?bounds.Right-shortWidth:bounds.Left+(bounds.Width-shortWidth)/2;
            Assert.Equal(SequenceMessageFixture.F(x),text.Attribute("x")!.Value);
        }
    }
    [Theory]
    [InlineData(TextAlignment.Left)] [InlineData(TextAlignment.Center)] [InlineData(TextAlignment.Right)]
    public void NoteAlignmentUsesTheExistingMeasuredInteriorAndActualMultilinePaint(TextAlignment alignment) {
        var model=SequenceMessageFixture.Basic();model.AddNote(SequenceArtifactNotePlacement.Over,new[]{"A","B"},"Long note\nNext");model.Notes[0].StepIndex=1;model.Layout=new SequenceLayoutPolicy(noteAlignment:alignment);
        var context=SequenceMessageFixture.Context();var prepared=model.Prepare(context);SequenceMessageFixture.Capture("sequence-note-align-"+alignment,prepared);
        var note=prepared.Regions.Single(item=>item.Role=="sequence-note");var text=XDocument.Parse(prepared.ToSvg()).Descendants().Single(item=>item.Name.LocalName=="text"&&item.Value=="Next"&&item.Ancestors().Any(parent=>(string?)parent.Attribute("data-cfx-role")=="sequence-note"));
        var width=TextLayoutEngine.Measure("Next",SequenceMessageFixture.Style(context)).Width;var left=note.Bounds.Left+10;var inner=note.Bounds.Width-24;
        var x=alignment==TextAlignment.Left?left:alignment==TextAlignment.Right?left+inner-width:left+(inner-width)/2;Assert.Equal(SequenceMessageFixture.F(x),text.Attribute("x")!.Value);
    }
    [Theory]
    [InlineData("1",12)] [InlineData("1000",9)] [InlineData("1000000",7)] [InlineData("-1000",9)] [InlineData("1.25",9)]
    public void AutomaticBadgeFontFollowsNumericStringLengthAndItsMeasuredInkFitsTheCircle(string start,double size) {
        var model=SequenceMessageFixture.Badged(start);var prepared=model.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-number-digits-"+start.Replace('.','-'),prepared);
        var number=SequenceMessageFixture.Text(prepared,"sequence-number",start);Assert.Equal(SequenceMessageFixture.F(size),number.Attribute("font-size")!.Value);Assert.Equal("sans-serif",number.Attribute("font-family")!.Value);
        SequenceMessageFixture.AssertBadgeContainsInk(prepared);Assert.Equal(start,prepared.Regions.Single(item=>item.Role=="sequence-number").Label);
        Assert.DoesNotContain(start+". Long request",prepared.ToSvg());
    }
    [Theory]
    [InlineData("forward")] [InlineData("backward")] [InlineData("reverse-half")] [InlineData("self")] [InlineData("bidirectional")]
    public void BadgeCaptionAndTerminalGeometryHaveSeparateActualFootprintsWithoutChangingEndpoints(string pose) {
        var model=SequenceMessageFixture.Badged("1000000");var message=model.Messages[0];
        if(pose=="backward"){message.SourceId="B";message.TargetId="A";}if(pose=="self"){message.SourceId="B";message.TargetId="B";}
        if(pose=="reverse-half")message.Metadata["mermaid.operator"]="/|-";if(pose=="bidirectional")message.Metadata["mermaid.operator"]="<<->>";
        var prepared=model.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-number-pose-"+pose,prepared);
        SequenceMessageFixture.AssertBadgeContainsInk(prepared);var badge=prepared.Regions.Single(item=>item.Role=="sequence-number-badge");var facts=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var edge=Assert.Single(facts.Edges);var label=edge.ResolvedLabelBounds!.Value;
        Assert.True(label.Right<=badge.Bounds.Left||label.Left>=badge.Bounds.Right||label.Bottom<=badge.Bounds.Top||label.Top>=badge.Bounds.Bottom);
        Assert.Equal(message.SourceId,edge.SourceId);Assert.Equal(message.TargetId,edge.TargetId);Assert.Equal(pose=="self"?4:2,edge.ResolvedRoute.Count);
        var source=facts.Nodes.Single(item=>item.Id==message.SourceId);var target=facts.Nodes.Single(item=>item.Id==message.TargetId);var origin=pose=="reverse-half"?target:source;var x=origin.X!.Value+origin.Width!.Value/2;
        if(pose=="backward"||pose=="reverse-half")Assert.True(badge.Bounds.Right<x);else Assert.True(badge.Bounds.Left>x);
        // Subtracting the two translated Y coordinates can lose a few ulps of the exact eight-pixel gap.
        if(pose=="self")Assert.True(Math.Round(edge.ResolvedRoute[edge.ResolvedRoute.Count-1].Y-edge.ResolvedRoute[0].Y-badge.Bounds.Height,9)>=8);
    }
    [Fact]
    public void PartialNumberTypographyOverridesMatchingFieldsAndRemainsDynamicAfterPreparationAndRestore() {
        var model=SequenceMessageFixture.Badged("999");model.AddMessage("B","A","Ready");model.Paint.NumberText=ChartColor.FromHex("#123456");model.Paint.NumberFill=ChartColor.FromHex("#e6f0ff");
        model.Typography=new SequenceTypography(number:new TextStyleOverride{Color=ChartColor.FromHex("#a02020"),FontWeight="700"});var context=SequenceMessageFixture.Context();var prepared=model.Prepare(context);
        var portable=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var restored=VisualArtifactInterchangeEnvelope.FromJson(portable.ToJson()).ToSequenceArtifact();var again=restored.Prepare(context);
        SequenceMessageFixture.Capture("sequence-number-partial-first",prepared);SequenceMessageFixture.Capture("sequence-number-partial-second",again);
        Assert.Equal("12",SequenceMessageFixture.Text(prepared,"sequence-number","999").Attribute("font-size")!.Value);Assert.Equal("9",SequenceMessageFixture.Text(prepared,"sequence-number","1000").Attribute("font-size")!.Value);
        Assert.Equal("#A02020",SequenceMessageFixture.Text(prepared,"sequence-number","999").Attribute("fill")!.Value);Assert.Equal("700",SequenceMessageFixture.Text(prepared,"sequence-number","999").Attribute("font-weight")!.Value);
        Assert.Null(restored.Typography!.Number!.FontSize);Assert.Equal(prepared.ToSvg(),again.ToSvg());Assert.Equal(prepared.ToPng(),again.ToPng());SequenceMessageFixture.AssertBadgeContainsInk(again);
    }
    [Fact]
    public void EmptyLabelsAndPausedOrEmptyMessageCollectionsDoNotInventOrRetainBadgeOccurrences() {
        var model=SequenceMessageFixture.Badged("1");model.Messages[0].Text="";model.AddMessage("B","A","");model.Messages[1].Metadata["mermaid.autonumber.visible"]="false";
        var prepared=model.Prepare(SequenceMessageFixture.Context());var before=prepared.ToSvg();SequenceMessageFixture.Capture("sequence-number-empty-label-paused",prepared);
        Assert.Single(prepared.Regions,item=>item.Role=="sequence-number-badge");Assert.Equal(new[]{"",""},prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Edges.Select(item=>item.Label));
        var emptyModel=SequenceArtifact.Create("sequence-number").WithSize(1200,900).AddParticipant("A","Review").AddParticipant("B","Archive");emptyModel.Layout=model.Layout;var empty=emptyModel.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-number-empty-messages",empty);Assert.DoesNotContain(empty.Regions,item=>item.Role=="sequence-number-badge");Assert.Equal(before,prepared.ToSvg());
    }
    [Fact]
    public void MaximumFiniteNativeStartKeepsEveryDigitAndRejectsOnlyActualOverflow() {
        var model=SequenceMessageFixture.Badged(decimal.MaxValue.ToString(CultureInfo.InvariantCulture));var prepared=model.Prepare(SequenceMessageFixture.Context(2400));SequenceMessageFixture.Capture("sequence-number-decimal-maximum",prepared);
        Assert.Equal(decimal.MaxValue.ToString(CultureInfo.InvariantCulture),prepared.Regions.Single(item=>item.Role=="sequence-number").Label);SequenceMessageFixture.AssertBadgeContainsInk(prepared);
        model.AddMessage("B","A","Overflow");Assert.Throws<OverflowException>(()=>model.Prepare(SequenceMessageFixture.Context(2400)));
    }
    [Fact]
    public void AuthoredNumberFontAndPaintAreDetachedAndRawPreparedPoliciesRestoreWithoutReplay() {
        var role=new TextStyleOverride{FontSize=20,FontFamily="Courier New, monospace"};var model=SequenceMessageFixture.Badged("1000");model.Typography=new SequenceTypography(number:role);role.FontSize=99;
        model.Layout=new SequenceLayoutPolicy(messageAlignment:TextAlignment.Right,noteAlignment:TextAlignment.Center,numberPresentation:SequenceNumberPresentation.Badge);model.Paint.NumberText=ChartColor.FromHex("#123456");model.Paint.NumberFill=ChartColor.FromHex("#e6f0ff");
        var context=SequenceMessageFixture.Context();var prepared=model.Prepare(context);var svg=prepared.ToSvg();var png=prepared.ToPng();var envelope=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        var restored=VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToSequenceArtifact();var again=restored.Prepare(context);SequenceMessageFixture.Capture("sequence-number-authored-first",prepared);SequenceMessageFixture.Capture("sequence-number-authored-second",again);
        Assert.Equal(2,envelope.Version);Assert.Equal(svg,again.ToSvg());Assert.Equal(png,again.ToPng());Assert.Equal(20,restored.Typography!.Number!.FontSize);Assert.Equal(model.Paint.NumberFill,restored.Paint.NumberFill);
        model.Paint.NumberText=ChartColor.Black;model.Paint.NumberFill=ChartColor.White;model.Typography=new SequenceTypography();model.Messages[0].Text="Later";envelope.Extensions["chartforgex.sequence.paint.numberText"]="#ff0000";Assert.Equal(svg,prepared.ToSvg());Assert.Equal(png,prepared.ToPng());
        var fresh=restored.Prepare(SequenceMessageFixture.Context(1800));SequenceMessageFixture.Capture("sequence-number-authored-fresh",fresh);Assert.Equal("20",SequenceMessageFixture.Text(fresh,"sequence-number","1000").Attribute("font-size")!.Value);
    }
    [Fact]
    public void UnsupportedPolicyEnumsAndPortableDowngradeRejectInsteadOfDroppingPresentation() {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new SequenceLayoutPolicy(messageAlignment:(TextAlignment)99));Assert.Throws<ArgumentOutOfRangeException>(()=>new SequenceLayoutPolicy(noteAlignment:(TextAlignment)99));Assert.Throws<ArgumentOutOfRangeException>(()=>new SequenceLayoutPolicy(numberPresentation:(SequenceNumberPresentation)99));
        var model=SequenceMessageFixture.Badged("1");var json=model.ToVisualArtifact().ToInterchangeJson();Assert.Throws<NotSupportedException>(()=>VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2","\"version\":1",StringComparison.Ordinal)));
        model.Layout=null;model.Paint.NumberText=ChartColor.Black;Assert.Equal(2,model.ToVisualArtifact().ToInterchangeEnvelope().Version);
    }
}

internal static class SequenceMessageFixture {
    internal static SequenceArtifact Basic()=>SequenceArtifact.Create("sequence-message").WithSize(1200,900).AddParticipant("A","Review").AddParticipant("B","Archive").AddMessage("A","B","Long request\nNext").AddMessage("B","A","Long request\nNext");
    internal static SequenceArtifact Badged(string start) {var model=SequenceArtifact.Create("sequence-number").WithSize(1200,900).AddParticipant("A","Review").AddParticipant("B","Archive").AddMessage("A","B","Long request\nNext");model.Metadata["mermaid.autonumber"]="true";model.Metadata["mermaid.autonumber.start"]=start;model.Layout=new SequenceLayoutPolicy(numberPresentation:SequenceNumberPresentation.Badge);return model;}
    internal static VisualRenderContext Context(double width=1500)=>new(new VisualLayoutOptions(new VisualSize(width,1100),24));
    internal static TextStyle Style(VisualRenderContext context)=>new(){Font=context.Font,FontSize=context.Theme.Typography.DataLabelSize,LineHeight=1};
    internal static string F(double value)=>value.ToString("0.###",CultureInfo.InvariantCulture);
    internal static XElement Text(PreparedVisual prepared,string role,string value)=>XDocument.Parse(prepared.ToSvg()).Descendants().Single(item=>item.Name.LocalName=="text"&&item.Value==value&&item.Ancestors().Any(parent=>(string?)parent.Attribute("data-cfx-role")==role));
    internal static void AssertBadgeContainsInk(PreparedVisual prepared) {
        foreach(var badge in prepared.Regions.Where(item=>item.Role=="sequence-number-badge")) {
            var text=prepared.Regions.Single(item=>item.Role=="sequence-number"&&item.Id==badge.Id);var centerX=badge.Bounds.X+badge.Bounds.Width/2;var centerY=badge.Bounds.Y+badge.Bounds.Height/2;var radius=badge.Bounds.Width/2;
            foreach(var x in new[]{text.Bounds.Left,text.Bounds.Right})foreach(var y in new[]{text.Bounds.Top,text.Bounds.Bottom})Assert.True((x-centerX)*(x-centerX)+(y-centerY)*(y-centerY)<=radius*radius);
        }
    }
    internal static void Capture(string stem,SequenceArtifact model){var output=Output();if(output==null)return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".svg"),model.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".png"),model.ToPng());File.WriteAllText(Path.Combine(output,stem+".json"),model.ToVisualArtifact().ToInterchangeJson());}
    internal static void Capture(string stem,PreparedVisual prepared){var output=Output();if(output==null)return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".svg"),prepared.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".png"),prepared.ToPng());File.WriteAllText(Path.Combine(output,stem+".json"),prepared.ToArtifact("sequence-message",VisualArtifactKind.Sequence).ToInterchangeJson());File.WriteAllText(Path.Combine(output,stem+".regions.json"),JsonSerializer.Serialize(prepared.Regions.Select(item=>new{item.Id,item.Role,item.Label,item.Bounds.X,item.Bounds.Y,item.Bounds.Width,item.Bounds.Height})));}
    private static string? Output()=>Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY")??Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
}
