using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceParticipantCaptionInkTests {
    [Theory]
    [InlineData(SequenceArtifactParticipantKind.Participant,false,false)] [InlineData(SequenceArtifactParticipantKind.Participant,false,true)]
    [InlineData(SequenceArtifactParticipantKind.Control,false,false)] [InlineData(SequenceArtifactParticipantKind.Control,false,true)]
    [InlineData(SequenceArtifactParticipantKind.Collections,false,false)] [InlineData(SequenceArtifactParticipantKind.Collections,false,true)]
    [InlineData(SequenceArtifactParticipantKind.Participant,true,false)] [InlineData(SequenceArtifactParticipantKind.Participant,true,true)]
    [InlineData(SequenceArtifactParticipantKind.Control,true,false)] [InlineData(SequenceArtifactParticipantKind.Control,true,true)]
    [InlineData(SequenceArtifactParticipantKind.Collections,true,false)] [InlineData(SequenceArtifactParticipantKind.Collections,true,true)]
    public void AutomaticCaptionInkUsesThePaintedCaptionSurfaceRatherThanTheSeparateGlyph(SequenceArtifactParticipantKind kind,bool bands,bool dark) {
        var model=Model(kind,bands,dark);model.Participants[0].Fill=ChartColor.FromHex("#223344");
        var prepared=Prepare(model);SequenceMessageFixture.Capture("caption-surface-"+kind+"-"+bands+"-"+dark,prepared);
        var enclosed=kind==SequenceArtifactParticipantKind.Participant || bands&&kind==SequenceArtifactParticipantKind.Collections;
        var expected=enclosed?"#FFFFFF":dark?"#E2E8F0":"#172133";
        AssertInk(prepared,"Caption contrast",expected);AssertInk(prepared,"«service»",expected);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(SequenceArtifactParticipantKind.Participant,false)] [InlineData(SequenceArtifactParticipantKind.Participant,true)]
    [InlineData(SequenceArtifactParticipantKind.Control,false)] [InlineData(SequenceArtifactParticipantKind.Control,true)]
    public void SharedFillFollowsTheSameSurfaceAndExplicitParticipantInkRemainsAuthoritative(SequenceArtifactParticipantKind kind,bool bands) {
        var model=Model(kind,bands,false);model.Paint.ParticipantFill=ChartColor.FromHex("#223344");
        var first=Prepare(model);var original=first.ToSvg();
        SequenceMessageFixture.Capture("caption-shared-fill-"+kind+"-"+bands,first);
        AssertInk(first,"Caption contrast",kind==SequenceArtifactParticipantKind.Participant?"#FFFFFF":"#172133");
        model.Paint.ParticipantText=ChartColor.FromHex("#B5004B");var explicitInk=Prepare(model);
        SequenceMessageFixture.Capture("caption-explicit-ink-"+kind+"-"+bands,explicitInk);
        AssertInk(explicitInk,"Caption contrast","#B5004B");AssertInk(explicitInk,"«service»","#B5004B");
        Assert.Equal(original,first.ToSvg());
    }

    private static SequenceArtifact Model(SequenceArtifactParticipantKind kind,bool bands,bool dark) {
        var model=SequenceArtifact.Create("caption-surface").WithSize(900,600)
            .AddParticipant("A","Caption contrast",kind).AddParticipant("B","Archive").AddMessage("A","B","Save");
        model.Participants[0].Stereotype="service";
        model.Theme=VisualTheme.Graphite().Configure(theme=>{
            theme.Light.Background=ChartColor.White;theme.Light.Foreground=ChartColor.FromHex("#172133");
            theme.Dark.Background=ChartColor.FromHex("#121724");theme.Dark.Foreground=ChartColor.FromHex("#E2E8F0");
        });
        model.ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light;
        if(bands)model.Layout=new SequenceLayoutPolicy(participantBands:new SequenceParticipantBands());
        return model;
    }
    private static PreparedVisual Prepare(SequenceArtifact model)=>model.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(900,600),24),model.Theme,model.ThemeMode,new VisualFrame(showLegend:false),
        new FontSpec { Family=BrowserProofFonts.Family }));
    private static void AssertInk(PreparedVisual prepared,string caption,string expected) {
        var text=Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(),node=>node.Name.LocalName=="text"&&node.Value==caption);
        Assert.Equal(expected,(string?)text.Attribute("fill"));
    }
}
