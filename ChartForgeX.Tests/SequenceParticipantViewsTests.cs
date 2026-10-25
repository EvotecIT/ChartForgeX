using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceParticipantViewsTests {
    [Fact]
    public void NullAndFalseViewPoliciesKeepExactOrdinarySvgPngJsonAndLifetimes() {
        var model=SequenceViewFixture.Basic();model.Participants[2].DestroyedAtMessageIndex=1;
        var svg=model.ToSvg();var png=model.ToPng();var json=model.ToVisualArtifact().ToInterchangeJson();SequenceViewFixture.Capture("sequence-view-native-null",model);
        foreach(var policy in new[]{new SequenceLayoutPolicy(mirrorParticipants:false),new SequenceLayoutPolicy(hideUnusedParticipants:false),new SequenceLayoutPolicy(mirrorParticipants:false,hideUnusedParticipants:false)}) {
            var name=policy.MirrorParticipants.HasValue?(policy.HideUnusedParticipants.HasValue?"both":"mirror"):"hide";
            model.Layout=policy;SequenceViewFixture.Capture("sequence-view-native-false-"+name,model);
            Assert.False(policy.HasOverrides);Assert.Equal(svg,model.ToSvg());Assert.Equal(png,model.ToPng());Assert.Equal(json,model.ToVisualArtifact().ToInterchangeJson());
        }
    }
    [Theory]
    [InlineData(SequenceArtifactParticipantKind.Participant)] [InlineData(SequenceArtifactParticipantKind.Actor)]
    [InlineData(SequenceArtifactParticipantKind.Boundary)] [InlineData(SequenceArtifactParticipantKind.Control)]
    [InlineData(SequenceArtifactParticipantKind.Entity)] [InlineData(SequenceArtifactParticipantKind.Database)]
    [InlineData(SequenceArtifactParticipantKind.Collections)] [InlineData(SequenceArtifactParticipantKind.Queue)]
    public void MirroredOccurrencesReuseEveryExistingParticipantPainterAndSameSourceIdentity(SequenceArtifactParticipantKind kind) {
        var model=SequenceViewFixture.Basic();model.Participants[0].Kind=kind;model.Participants[0].Stereotype="worker";model.Participants[0].Symbol="R";
        model.Layout=new SequenceLayoutPolicy(mirrorParticipants:true);var prepared=model.Prepare(SequenceViewFixture.Context());SequenceViewFixture.Capture("sequence-view-kind-"+kind,prepared);
        var headers=prepared.Regions.Where(item=>item.Role=="sequence-participant").ToArray();var footers=prepared.Regions.Where(item=>item.Role=="sequence-participant-footer").ToArray();
        Assert.Equal(3,headers.Length);Assert.Equal(3,footers.Length);
        foreach(var header in headers) {
            var footer=Assert.Single(footers,item=>item.Id==header.Id);Assert.Equal(header.Bounds.Width,footer.Bounds.Width);Assert.Equal(header.Bounds.Height,footer.Bounds.Height);Assert.Equal(header.Bounds.X,footer.Bounds.X);
            Assert.True(footer.Bounds.Y>header.Bounds.Bottom);var line=SequenceViewFixture.Lifeline(prepared,header.Bounds.X+header.Bounds.Width/2);
            Assert.Equal(footer.Bounds.Y.ToString("0.###",CultureInfo.InvariantCulture),line.Attribute("y2")!.Value);
        }
        var svg=XDocument.Parse(prepared.ToSvg());Assert.Equal(2,svg.Descendants().Count(item=>item.Name.LocalName=="text"&&item.Value.Contains("worker",StringComparison.Ordinal)));
        var ids=svg.Descendants().Attributes("id").Select(item=>item.Value).ToArray();Assert.Equal(ids.Length,ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(3,prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Nodes.Count);
    }
    [Fact]
    public void HiddenColumnsKeepRawIdsOrderMetadataAndMessageEndpointsWhileCompressingOnlyTheView() {
        var model=SequenceViewFixture.Basic();model.Participants[1].Metadata["caller"]="retained";var full=model.Prepare(SequenceViewFixture.Context());
        model.Layout=new SequenceLayoutPolicy(hideUnusedParticipants:true);var hidden=model.Prepare(SequenceViewFixture.Context());SequenceViewFixture.Capture("sequence-view-hidden-column",hidden);
        var facts=hidden.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();Assert.Equal(new[]{"A","Unused","B"},facts.Nodes.Select(item=>item.Id));
        Assert.Null(facts.Nodes[1].X);Assert.Equal("retained",facts.Nodes[1].Extensions["caller"]);Assert.Equal(new[]{"A","B"},hidden.Regions.Where(item=>item.Role=="sequence-participant").Select(item=>item.Id));
        Assert.Equal(2,hidden.Regions.Count(item=>item.Role=="sequence-participant"));Assert.True(hidden.Regions.First(item=>item.Id=="B").Bounds.X<full.Regions.First(item=>item.Id=="B").Bounds.X);
        Assert.Equal("A",facts.Edges[0].SourceId);Assert.Equal("B",facts.Edges[0].TargetId);Assert.Equal(3,model.Participants.Count);
    }
    [Fact]
    public void ParticipantGroupsUseOnlyVisibleMembersWithoutChangingPortableMembership() {
        var model=SequenceViewFixture.Basic();model.AddParticipant("Empty","Empty group member");model.AddParticipantGroup("live","Handlers",0,2);model.AddParticipantGroup("empty","Empty",3,3);
        model.Layout=new SequenceLayoutPolicy(hideUnusedParticipants:true,mirrorParticipants:true);var prepared=model.Prepare(SequenceViewFixture.Context());SequenceViewFixture.Capture("sequence-view-compressed-groups",prepared);
        var group=Assert.Single(prepared.Regions,item=>item.Role=="sequence-participant-group");Assert.Equal("live",group.Id);
        foreach(var member in prepared.Regions.Where(item=>(item.Role=="sequence-participant"||item.Role=="sequence-participant-footer")&&(item.Id=="A"||item.Id=="B"))) {
            Assert.True(member.Bounds.Left>=group.Bounds.Left);Assert.True(member.Bounds.Right<=group.Bounds.Right);Assert.True(member.Bounds.Bottom<=group.Bounds.Bottom);
        }
        var facts=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();Assert.Equal(2,facts.Groups.Count);Assert.Null(facts.Groups[1].X);
        Assert.Equal(new[]{"live","live","live","empty"},facts.Nodes.Select(item=>item.GroupId));
    }
    [Fact]
    public void EmptyVisibleViewRetainsSourceAndCanBeRefilledWithoutMutatingAnEarlierCapture() {
        var model=SequenceArtifact.Create("view-empty").WithSize(700,600).AddParticipant("A","Review").AddParticipant("B","Archive");model.AddParticipantGroup("empty","Empty",0,1);
        model.Layout=new SequenceLayoutPolicy(hideUnusedParticipants:true,mirrorParticipants:true);var empty=model.Prepare(SequenceViewFixture.Context());var svg=empty.ToSvg();SequenceViewFixture.Capture("sequence-view-empty",empty);
        Assert.DoesNotContain(empty.Regions,item=>item.Role.StartsWith("sequence-participant",StringComparison.Ordinal));Assert.Equal(2,empty.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Nodes.Count);
        model.AddMessage("A","B","Save");var filled=model.Prepare(SequenceViewFixture.Context());SequenceViewFixture.Capture("sequence-view-refilled",filled);
        Assert.Equal(2,filled.Regions.Count(item=>item.Role=="sequence-participant"));Assert.Equal(svg,empty.ToSvg());Assert.Equal(new[]{"A","B"},model.Participants.Select(item=>item.Id));
    }
    [Theory]
    [InlineData("note")] [InlineData("activation")] [InlineData("reference")]
    public void NonArrowReferencesCountAsActualParticipantUse(string usage) {
        var model=SequenceArtifact.Create("view-"+usage).WithSize(700,700).AddParticipant("Unused","Unused").AddParticipant("B","Archive");
        if(usage=="note")model.AddNote(SequenceArtifactNotePlacement.Over,new[]{"B"},"Note-only use");
        if(usage=="activation")model.AddActivation("B",true,0);
        if(usage=="reference"){model.AddBlock(SequenceArtifactBlockKind.Reference,"Audit",0,0,true);model.Blocks[0].ParticipantIds.Add("B");}
        model.Layout=new SequenceLayoutPolicy(hideUnusedParticipants:true,mirrorParticipants:true);var prepared=model.Prepare(SequenceViewFixture.Context());SequenceViewFixture.Capture("sequence-view-only-"+usage,prepared);
        Assert.Equal("B",Assert.Single(prepared.Regions,item=>item.Role=="sequence-participant").Id);Assert.Single(prepared.Regions,item=>item.Role=="sequence-participant-footer");
        Assert.Equal(new[]{"Unused","B"},prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Nodes.Select(item=>item.Id));
    }
    [Theory]
    [InlineData("source")] [InlineData("target")] [InlineData("self")]
    public void DestroyedFooterAndLifelineEndAtTheSameMeasuredTerminalOccurrence(string endpoint) {
        var model=SequenceArtifact.Create("view-death").WithSize(1200,800).AddParticipant("A","Review").AddParticipant("B","Archive");
        model.AddMessage(endpoint=="target"?"A":"B",endpoint=="source"?"A":"B","Close");model.Participants[1].DestroyedAtMessageIndex=0;
        model.Layout=new SequenceLayoutPolicy(mirrorParticipants:true);var prepared=model.Prepare(SequenceViewFixture.Context(1500));SequenceViewFixture.Capture("sequence-view-terminal-"+endpoint,prepared);
        var footer=prepared.Regions.Single(item=>item.Role=="sequence-participant-footer"&&item.Id=="B");var facts=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var route=facts.Edges[0].ResolvedRoute;
        var terminal=endpoint=="source"?route[0]:route[route.Count-1];Assert.Equal(footer.Bounds.Y+footer.Bounds.Height/2,terminal.Y);Assert.Equal(endpoint=="source"?footer.Bounds.Left: endpoint=="self"?footer.Bounds.Right:footer.Bounds.Left,terminal.X);
        var line=SequenceViewFixture.Lifeline(prepared,footer.Bounds.X+footer.Bounds.Width/2);Assert.Equal(footer.Bounds.Y.ToString("0.###",CultureInfo.InvariantCulture),line.Attribute("y2")!.Value);
        var label=facts.Edges[0].ResolvedLabelBounds!.Value;
        Assert.True(label.X+label.Width<=footer.Bounds.Left||label.X>=footer.Bounds.Right||label.Y+label.Height<=footer.Bounds.Top||label.Y>=footer.Bounds.Bottom);
        Assert.DoesNotContain("sequence-destruction-cross",prepared.ToSvg());Assert.Equal(0,facts.Nodes.Single(item=>item.Id=="B").Sequence!.DestroyedAtMessageIndex);
        foreach(var region in prepared.Regions)Assert.True(region.Bounds.Right<=prepared.Size.Width&&region.Bounds.Bottom<=prepared.Size.Height);
    }
    [Fact]
    public void TerminalActivationClosesAtTheFooterTopInsteadOfPaintingThroughItsCaption() {
        var model=SequenceViewFixture.Basic();model.Participants[2].DestroyedAtMessageIndex=1;model.AddActivation("B",true,0);
        model.Layout=new SequenceLayoutPolicy(mirrorParticipants:true);var prepared=model.Prepare(SequenceViewFixture.Context());SequenceViewFixture.Capture("sequence-view-terminal-activation",prepared);
        var footer=prepared.Regions.Single(item=>item.Role=="sequence-participant-footer"&&item.Id=="B");
        var bar=Assert.Single(prepared.Regions,item=>item.Role=="sequence-activation");Assert.Equal(footer.Bounds.Top,bar.Bounds.Bottom);
    }
    [Fact]
    public void CreatedParticipantStartsItsLifelineAtItsMeasuredHeaderAndGetsOneFooter() {
        var model=SequenceViewFixture.Basic();model.Participants[2].CreatedAtMessageIndex=0;model.Layout=new SequenceLayoutPolicy(mirrorParticipants:true,hideUnusedParticipants:true);
        var prepared=model.Prepare(SequenceViewFixture.Context());SequenceViewFixture.Capture("sequence-view-created",prepared);
        var header=prepared.Regions.Single(item=>item.Role=="sequence-participant"&&item.Id=="B");var footer=prepared.Regions.Single(item=>item.Role=="sequence-participant-footer"&&item.Id=="B");
        var line=SequenceViewFixture.Lifeline(prepared,header.Bounds.X+header.Bounds.Width/2);Assert.Equal(header.Bounds.Bottom.ToString("0.###",CultureInfo.InvariantCulture),line.Attribute("y1")!.Value);Assert.Equal(footer.Bounds.Y.ToString("0.###",CultureInfo.InvariantCulture),line.Attribute("y2")!.Value);
    }
    [Fact]
    public void ViewPoliciesRoundTripAndPreparedSourceFactsStayDetachedFromCallerMutation() {
        var model=SequenceViewFixture.Basic();model.Layout=new SequenceLayoutPolicy(mirrorParticipants:true,hideUnusedParticipants:true);var prepared=model.Prepare(SequenceViewFixture.Context());var svg=prepared.ToSvg();var png=prepared.ToPng();
        var envelope=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var restored=VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToSequenceArtifact();
        SequenceViewFixture.Capture("sequence-view-portable-first",prepared);var again=restored.Prepare(SequenceViewFixture.Context());SequenceViewFixture.Capture("sequence-view-portable-second",again);
        Assert.Equal(2,envelope.Version);Assert.True(restored.Layout!.MirrorParticipants);Assert.True(restored.Layout.HideUnusedParticipants);Assert.Equal(svg,again.ToSvg());Assert.Equal(png,again.ToPng());
        model.Participants[0].Label="Later";model.Messages[0].Text="Changed caller message";model.Participants[1].Label="Changed unused participant";model.Layout=new SequenceLayoutPolicy();envelope.Nodes[1].Label="Other";
        Assert.Equal(svg,prepared.ToSvg());Assert.Equal(3,restored.Participants.Count);Assert.Equal("Unused",restored.Participants[1].Label);
        restored.Layout=new SequenceLayoutPolicy(mirrorParticipants:false,hideUnusedParticipants:false);Assert.Equal(3,restored.Prepare(SequenceViewFixture.Context()).Regions.Count(item=>item.Role=="sequence-participant"));
    }
    [Fact]
    public void ActivationStartingAtTheDestroyingOccurrenceRejectsTheContradictoryFooterPresentation() {
        var model=SequenceViewFixture.Basic();model.Participants[2].DestroyedAtMessageIndex=1;model.AddActivation("B",true,1);model.Layout=new SequenceLayoutPolicy(mirrorParticipants:true);
        Assert.Throws<NotSupportedException>(()=>model.Prepare(SequenceViewFixture.Context()));
    }
    [Fact]
    public void ContradictoryFixedHeightRejectsTheFooterBeforeReturningAnIncompleteCapture() {
        var model=SequenceViewFixture.Basic();var ordinary=model.Prepare(SequenceViewFixture.Context());
        var bottom=XDocument.Parse(ordinary.ToSvg()).Descendants().Where(item=>(string?)item.Attribute("data-cfx-role")=="sequence-lifeline")
            .Max(item=>double.Parse(item.Attribute("y2")!.Value,CultureInfo.InvariantCulture));
        var bounded=new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1000,Math.Ceiling(bottom)+28),24));
        var control=model.Prepare(bounded);SequenceViewFixture.Capture("sequence-view-fixed-height-control",control);
        model.Layout=new SequenceLayoutPolicy(mirrorParticipants:true);Assert.Throws<NotSupportedException>(()=>model.Prepare(bounded));
    }
}

internal static class SequenceViewFixture {
    internal static SequenceArtifact Basic()=>SequenceArtifact.Create("sequence-view").WithSize(1000,800).AddParticipant("A","Review").AddParticipant("Unused","Unused").AddParticipant("B","Archive")
        .AddMessage("A","B","Save").AddMessage("B","A","Ready",SequenceArtifactMessageLineStyle.Dashed,SequenceArtifactMessageKind.Return);
    internal static VisualRenderContext Context(double width=1000)=>new(new VisualLayoutOptions(new VisualSize(width,1000),24));
    internal static XElement Lifeline(PreparedVisual prepared,double x)=>XDocument.Parse(prepared.ToSvg()).Descendants().First(item=>(string?)item.Attribute("data-cfx-role")=="sequence-lifeline"&&item.Attribute("x1")!.Value==x.ToString("0.###",CultureInfo.InvariantCulture));
    internal static void Capture(string stem,SequenceArtifact model) {var output=Output();if(output==null)return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".svg"),model.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".png"),model.ToPng());File.WriteAllText(Path.Combine(output,stem+".json"),model.ToVisualArtifact().ToInterchangeJson());}
    internal static void Capture(string stem,PreparedVisual prepared) {var output=Output();if(output==null)return;Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,stem+".svg"),prepared.ToSvg());File.WriteAllBytes(Path.Combine(output,stem+".png"),prepared.ToPng());File.WriteAllText(Path.Combine(output,stem+".json"),prepared.ToArtifact("sequence-view",VisualArtifactKind.Sequence).ToInterchangeJson());File.WriteAllText(Path.Combine(output,stem+".regions.json"),JsonSerializer.Serialize(prepared.Regions.Select(item=>new{item.Id,item.Role,item.Label,item.Bounds.X,item.Bounds.Y,item.Bounds.Width,item.Bounds.Height})));}
    private static string? Output()=>Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY")??Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
}
