using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;
namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceSelfRoutesTests {
    [Theory] [InlineData(SequenceSelfMessageRouting.Orthogonal,25)] [InlineData(SequenceSelfMessageRouting.Curved,20)]
    public void ExplicitRouteUsesOneActualPathExtremaAndPortableSamples(SequenceSelfMessageRouting routing,double drop) {
        var model=Basic();model.Layout=new SequenceLayoutPolicy(selfMessageRouting:routing);var prepared=model.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-self-route-"+routing,prepared);
        var edge=Assert.Single(prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Edges);var first=edge.ResolvedRoute[0];var last=edge.ResolvedRoute[edge.ResolvedRoute.Count-1];Assert.Equal(drop,last.Y-first.Y,8);
        var path=Path(prepared);Assert.Equal(routing==SequenceSelfMessageRouting.Curved,path.Attribute("d")!.Value.Contains("C",StringComparison.Ordinal));Assert.Equal("Check",edge.Label);
        if(routing==SequenceSelfMessageRouting.Curved){Assert.True(edge.ResolvedRoute.Count>4);Assert.True(edge.ResolvedRoute.Min(p=>p.Y)<first.Y);Assert.True(edge.ResolvedRoute.Max(p=>p.Y)>last.Y);Assert.True(edge.ResolvedRoute.Max(p=>p.X)>first.X+40);}
        else Assert.Equal(4,edge.ResolvedRoute.Count);
        var region=prepared.Regions.Single(r=>r.Role=="sequence-message");Assert.All(edge.ResolvedRoute,p=>{Assert.InRange(p.X,region.Bounds.Left,region.Bounds.Right);Assert.InRange(p.Y,region.Bounds.Top,region.Bounds.Bottom);});
    }
    [Fact] public void NullAndEmptyPolicyPreserveCompleteOrdinarySelfAndOtherMessageBytes() {
        var model=Basic();model.AddParticipant("B","Archive");model.AddMessage("A","B","Save");model.Metadata["mermaid.autonumber"]="true";var svg=model.ToSvg();var png=model.ToPng();var json=model.ToVisualArtifact().ToInterchangeJson();SequenceMessageFixture.Capture("sequence-self-null",model);
        model.Layout=new SequenceLayoutPolicy();SequenceMessageFixture.Capture("sequence-self-empty",model);Assert.Equal(svg,model.ToSvg());Assert.Equal(png,model.ToPng());Assert.Equal(json,model.ToVisualArtifact().ToInterchangeJson());
        var edge=model.ToVisualArtifact().ToInterchangeEnvelope().Edges[0];Assert.DoesNotContain("C",Path(model.Prepare(SequenceMessageFixture.Context())).Attribute("d")!.Value);
    }
    [Theory] [InlineData(SequenceSelfMessageRouting.Orthogonal)] [InlineData(SequenceSelfMessageRouting.Curved)]
    public void RawAndPreparedRoutesRestoreWithoutPromotingSamplesAndRemainDetached(SequenceSelfMessageRouting routing) {
        var model=Basic();model.Layout=new SequenceLayoutPolicy(selfMessageRouting:routing);var raw=VisualArtifactInterchangeEnvelope.FromJson(model.ToVisualArtifact().ToInterchangeJson()).ToSequenceArtifact();Assert.Equal(routing,raw.Layout!.SelfMessageRouting);
        var context=SequenceMessageFixture.Context();var prepared=raw.Prepare(context);var artifact=prepared.ToArtifact(raw.Id,VisualArtifactKind.Sequence);var facts=artifact.ToInterchangeEnvelope();var restored=VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToSequenceArtifact();var again=restored.Prepare(context);
        SequenceMessageFixture.Capture("sequence-self-restored-"+routing,again);Assert.Equal(prepared.ToSvg(),again.ToSvg());Assert.Equal(prepared.ToPng(),again.ToPng());Assert.Equal(2,facts.Version);model.Layout=null;model.Messages[0].Text="Later";facts.Edges[0].ResolvedRoute.Clear();Assert.Equal(prepared.ToSvg(),again.ToSvg());Assert.Equal("Check",restored.Messages[0].Text);
        var fresh=restored.Prepare(SequenceMessageFixture.Context(1800));SequenceMessageFixture.Capture("sequence-self-fresh-"+routing,fresh);Assert.Equal(routing==SequenceSelfMessageRouting.Curved,Path(fresh).Attribute("d")!.Value.Contains("C",StringComparison.Ordinal));
    }
    [Theory] [InlineData("->>")] [InlineData("-->>")] [InlineData("-)")] [InlineData("-x")] [InlineData("/|-")]
    public void CurvedTerminalUsesActualTangentAndSameNotationPainter(string notation) {
        var model=Basic();model.Layout=new SequenceLayoutPolicy(selfMessageRouting:SequenceSelfMessageRouting.Curved);model.Messages[0].Metadata["mermaid.operator"]=notation;var prepared=model.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-self-terminal-"+notation.Replace('/','s').Replace('|','p').Replace('>','r').Replace(')','c'),prepared);
        var svg=XDocument.Parse(prepared.ToSvg());var message=svg.Descendants().Single(e=>(string?)e.Attribute("data-cfx-role")=="sequence-message");Assert.Contains(message.Descendants(),e=>e.Name.LocalName=="g"&&((string?)e.Attribute("transform"))?.Contains("rotate(",StringComparison.Ordinal)==true);Assert.Contains("C",Path(prepared).Attribute("d")!.Value);
    }
    [Theory] [InlineData(SequenceSelfMessageRouting.Orthogonal)] [InlineData(SequenceSelfMessageRouting.Curved)]
    public void BadgeActivationAndMirroredDeathConsumeActualReturnAndExtrema(SequenceSelfMessageRouting routing) {
        var model=Basic();model.Metadata["mermaid.autonumber"]="true";model.Metadata["mermaid.autonumber.start"]="1000000";model.Layout=new SequenceLayoutPolicy(numberPresentation:SequenceNumberPresentation.Badge,mirrorParticipants:true,selfMessageRouting:routing);model.AddMessage("A","A","Close");model.AddActivation("A",true,0);model.Participants[0].DestroyedAtMessageIndex=1;
        var prepared=model.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-self-lifetime-"+routing,prepared);SequenceMessageFixture.AssertBadgeContainsInk(prepared);var facts=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();var edge=facts.Edges[1];var footer=prepared.Regions.Single(r=>r.Role=="sequence-participant-footer");Assert.Equal(edge.ResolvedRoute[edge.ResolvedRoute.Count-1].Y,footer.Bounds.Top+footer.Bounds.Height/2,8);
        Assert.Single(prepared.Regions,region=>region.Role=="sequence-activation");Assert.DoesNotContain("sequence-destruction-cross",prepared.ToSvg());Assert.True(edge.ResolvedRoute.Min(p=>p.Y)>=0);Assert.True(edge.ResolvedRoute.Max(p=>p.Y)<=prepared.Size.Height);
    }
    [Fact] public void EmptyAndRefilledCollectionsKeepSourceIdentityAndRejectUnknownPolicy() {
        var model=Basic();model.Layout=new SequenceLayoutPolicy(selfMessageRouting:SequenceSelfMessageRouting.Curved);var original=model.Prepare(SequenceMessageFixture.Context());var svg=original.ToSvg();var emptyModel=SequenceArtifact.Create("sequence-self").WithSize(1000,820).AddParticipant("A","Review");emptyModel.Layout=model.Layout;var empty=emptyModel.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-self-empty-messages",empty);Assert.Empty(empty.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Edges);emptyModel.AddMessage("A","A","Again");var refill=emptyModel.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-self-refill",refill);Assert.Equal("A",Assert.Single(refill.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope().Edges).SourceId);Assert.Equal(svg,original.ToSvg());Assert.Throws<ArgumentOutOfRangeException>(()=>new SequenceLayoutPolicy(selfMessageRouting:(SequenceSelfMessageRouting)99));
    }
    internal static SequenceArtifact Basic()=>SequenceArtifact.Create("sequence-self").WithSize(1000,820).AddParticipant("A","Review").AddMessage("A","A","Check");
    internal static XElement Path(PreparedVisual prepared)=>XDocument.Parse(prepared.ToSvg()).Descendants().First(e=>(string?)e.Attribute("data-cfx-role")=="sequence-message-line");
}
