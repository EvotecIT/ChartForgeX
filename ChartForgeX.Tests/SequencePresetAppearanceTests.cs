using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;
namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class SequencePresetAppearanceTests {
    [Theory]
    [InlineData("base","#FFF4DD")] [InlineData("default","#ECECFF")] [InlineData("dark","#1F2020")]
    [InlineData("forest","#CDE498")] [InlineData("neutral","#EEEEEE")] [InlineData("neo","#FFFFFF")]
    [InlineData("neo-dark","#2A2020")] [InlineData("redux","#FFFFFF")] [InlineData("redux-dark","#111113")]
    [InlineData("redux-color","#FDF4FF")] [InlineData("redux-dark-color","#111113")]
    public void NamedPresetsMaterializeActualSurfaceAndRetainedTypedPaint(string preset,string expected) {
        var result=Render("\"theme\":\""+preset+"\",\"look\":\"classic\"");SequencePresetFixture.Capture("preset-named-"+preset,result);Assert.Empty(result.Diagnostics);var model=Model(result);Assert.Equal(expected,(model.Participants[0].Fill??model.Paint.ParticipantFill)!.Value.ToCss());var surface=XDocument.Parse(model.ToSvg()).Descendants().First(e=>(string?)e.Attribute("data-cfx-role")=="sequence-participant-box");Assert.Equal(expected,surface.Attribute("fill")!.Value);
    }
    [Theory] [InlineData(VisualThemeMode.Light,"#FDF4FF","#FFFFFF")] [InlineData(VisualThemeMode.Dark,"#111113","#E0DFDF")]
    public void OmittedPresetTracksHostModeButAnExplicitNameStaysLiteral(VisualThemeMode mode,string omitted,string darkText) {
        var host=new MermaidPresentationOptions{ThemeMode=mode};var a=Render("\"look\":\"classic\"",host);var b=Render("\"theme\":\"redux-color\",\"look\":\"classic\"",host);SequencePresetFixture.Capture("preset-omitted-"+mode,a);SequencePresetFixture.Capture("preset-literal-"+mode,b);Assert.Empty(a.Diagnostics);Assert.Empty(b.Diagnostics);Assert.Equal(omitted,(Model(a).Participants[0].Fill??Model(a).Paint.ParticipantFill)!.Value.ToCss());Assert.Equal("#FDF4FF",Model(b).Participants[0].Fill!.Value.ToCss());if(mode==VisualThemeMode.Dark)Assert.Equal(darkText,Model(a).Paint.ParticipantText!.Value.ToCss());
    }
    [Fact] public void ExplicitHostThemeSuppressesOnlyPresetDefaultsWhileLiteralRolesAndArraysSurvive() {
        var host=new MermaidPresentationOptions{Theme=VisualTheme.Graphite().Configure(t=>{t.Light.Surface=ChartColor.FromHex("#345678");t.Dark.Surface=ChartColor.FromHex("#345678");}),FontFamily="Arial"};
        var plain=Render("\"theme\":\"redux-color\",\"look\":\"classic\"",host);var literal=Render("\"theme\":\"redux-color\",\"look\":\"classic\",\"themeVariables\":{\"actorBkg\":\"#112233\",\"signalColor\":\"#556677\",\"bkgColorArray\":[\"#445566\"],\"borderColorArray\":[\"#778899\"]} ",host);SequencePresetFixture.Capture("preset-host-theme",plain);SequencePresetFixture.Capture("preset-host-literals",literal);Assert.Empty(plain.Diagnostics);Assert.Empty(literal.Diagnostics);Assert.Null(Model(plain).Paint.ParticipantFill);Assert.Null(Model(plain).Participants[0].Fill);Assert.Equal("#345678",Model(plain).Theme.Resolve(VisualThemeMode.Light).Surface.ToCss());Assert.Equal("#112233",Model(literal).Paint.ParticipantFill!.Value.ToCss());Assert.Equal("#445566",Model(literal).Participants[0].Fill!.Value.ToCss());Assert.Equal("#778899",Model(literal).Participants[1].Border!.Value.ToCss());Assert.Equal("#556677",Model(literal).Paint.MessageLine!.Value.ToCss());
    }
    [Fact] public void FontOnlyHostRetainsPresetAndFamilyModeOverridesSharedMode() {
        var result=MermaidRenderer.Render("%%{init:{\"look\":\"classic\"}}%%\n"+SequencePresetFixture.Body,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=VisualThemeMode.Light,FontFamily="Arial"},Sequence=new MermaidSequenceRenderOptions{Presentation=new MermaidPresentationOptions{ThemeMode=VisualThemeMode.Dark}}});SequencePresetFixture.Capture("preset-family-mode",result);Assert.Empty(result.Diagnostics);Assert.Equal("#111113",Model(result).Paint.ParticipantFill!.Value.ToCss());Assert.Equal(VisualThemeMode.Dark,Model(result).ThemeMode);
    }
    [Fact] public void PaletteIndicesAreOriginalSourceOrderWithIndependentLengthsAndEmptyFallback() {
        const string source="%%{init:{\"theme\":\"redux-color\",\"look\":\"classic\",\"sequence\":{\"hideUnusedParticipants\":true},\"themeVariables\":{\"bkgColorArray\":[\"#110000\",\"#220000\"],\"borderColorArray\":[\"#001100\",\"#002200\",\"#003300\"]}}}%%\nsequenceDiagram\nparticipant U as Unused\nparticipant A as Review\nparticipant B as Archive\nA->>+B: Save\nB-->>-A: Done";var result=MermaidRenderer.Render(source);SequencePresetFixture.Capture("preset-identity-arrays",result);Assert.Empty(result.Diagnostics);var model=Model(result);Assert.Equal(new[]{"U","A","B"},model.Participants.Select(p=>p.Id));Assert.Equal("#220000",model.Participants[1].Fill!.Value.ToCss());Assert.Equal("#003300",model.Participants[2].Border!.Value.ToCss());var prepared=model.Prepare(SequencePresetFixture.Context());Assert.DoesNotContain(prepared.Regions,r=>r.Role=="sequence-participant"&&r.Id=="U");var svg=prepared.ToSvg();Assert.Contains("#220000",svg);Assert.Contains("#003300",svg);var empty=Render("\"theme\":\"redux-color\",\"look\":\"classic\",\"themeVariables\":{\"bkgColorArray\":[],\"borderColorArray\":[]}");Assert.Empty(empty.Diagnostics);Assert.All(Model(empty).Participants,p=>{Assert.Null(p.Fill);Assert.Null(p.Border);});
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void FlowAndBlockYamlPaletteInputsUseTheSameValidatedOwner(bool block) {
        var palette=block?"    bkgColorArray:\n      - '#112233'\n      - '#445566'":"    bkgColorArray: ['#112233', '#445566']";var source="---\nconfig:\n  theme: redux-color\n  look: classic\n  themeVariables:\n"+palette+"\n---\n"+SequencePresetFixture.Body;var result=MermaidRenderer.Render(source);SequencePresetFixture.Capture("preset-yaml-"+block,result);Assert.Empty(result.Diagnostics);Assert.Equal("#445566",Model(result).Participants[1].Fill!.Value.ToCss());
    }
    [Theory] [InlineData("\"bkgColorArray\":[1]")] [InlineData("\"borderColorArray\":\"red\"")] [InlineData("\"darkMode\":\"true\"")] [InlineData("\"primaryColor\":\"bogus\"")]
    public void InvalidSourceInputsFailAtTheirDeclarationBeforeNativeArtifact(string variable){var result=Render("\"look\":\"classic\",\"themeVariables\":{"+variable+"}");SequencePresetFixture.Capture("preset-invalid-"+variable.Split(':')[0].Trim('"'),result);Assert.True(result.HasErrors);Assert.Null(result.Artifact);Assert.Contains(result.Diagnostics,d=>d.Severity==MermaidDiagnosticSeverity.Error&&d.Span.Line==1);}
    [Fact] public void ExplicitRoleReplayFollowsDerivationWithoutQuantizingAnIntermediateColor() {
        var result=Render("\"theme\":\"base\",\"look\":\"classic\",\"themeVariables\":{\"primaryColor\":\"hsl(0 100% 25%)\",\"actorBorder\":\"#123456\",\"actorTextColor\":\"#abcdef\",\"loopTextColor\":\"#654321\",\"darkMode\":true}");SequencePresetFixture.Capture("preset-derivation",result);Assert.Empty(result.Diagnostics);var model=Model(result);Assert.Equal("#123456",model.Paint.ParticipantBorder!.Value.ToCss());Assert.Equal("#ABCDEF",model.Paint.ParticipantText!.Value.ToCss());Assert.Equal("#654321",model.Paint.FragmentTitleText!.Value.ToCss());// HSL(240,100%,15%) reaches the native byte boundary once; its retained double is above76.5. Quantizing the secondary first changes the result.
        Assert.Equal("#00004D",model.Paint.ActivationBorder!.Value.ToCss());var explicitCommon=Render("\"theme\":\"forest\",\"look\":\"classic\",\"themeVariables\":{\"textColor\":\"#aabbcc\"}");Assert.Equal("#AABBCC",Model(explicitCommon).Theme.Resolve(VisualThemeMode.Light).MutedForeground.ToCss());
    }
    [Fact]
    public void CompletedNeoGlyphEffectsAreRetainedWhileKnownUnresolvedPresetStylesPreserveNoPaint() {
        const string source = "sequenceDiagram\nparticipant A\nparticipant B\nA->>B: Save";
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        SequencePresetFixture.Capture("preset-genuine-omitted-glyph-effects", result);
        Assert.Empty(result.Diagnostics);
        var model = Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
        Assert.All(model.Participants, participant => Assert.NotNull(participant.SurfaceShadow));
        Assert.Contains("<image", model.ToSvg());
        var ordinary = MermaidRenderer.Render(source + "\nNote over A,B: Retain facts",
            new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.Empty(ordinary.Diagnostics);
        Assert.Null(Assert.IsType<SequenceArtifact>(ordinary.Artifact!.Model).Paint.NoteShadow);
    }
    [Theory] [InlineData("redux-dark")] [InlineData("redux-dark-color")]
    public void DarkReduxConstructorNoteRolesSurviveLaterFallbackDerivation(string preset) {
        var result=MermaidRenderer.Render("%%{init:{\"theme\":\""+preset+"\",\"look\":\"classic\"}}%%\n"+SequencePresetFixture.Body+"\nNote over A,B: Audit");SequencePresetFixture.Capture("preset-note-"+preset,result);Assert.Empty(result.Diagnostics);var model=Assert.IsType<SequenceArtifact>(result.Artifact!.Model);Assert.Equal("#FEF9C3",model.Paint.NoteFill!.Value.ToCss());Assert.Equal("#28253D",model.Paint.NoteText!.Value.ToCss());Assert.Contains("#FEF9C3",model.ToSvg());
    }
    [Fact] public void EmptyDarkBackgroundPaletteKeepsActorFallbackButActivationUsesMainBackground() {
        var result=Render("\"theme\":\"redux-dark-color\",\"look\":\"classic\" ");var model=Model(result);Assert.Null(model.Participants[0].Fill);Assert.Equal("#111113",model.Participants[0].ActivationFill!.Value.ToCss());Assert.Equal("#E879F9",model.Participants[0].ActivationBorder!.Value.ToCss());
    }
    [Fact] public void ExistingTypedDocumentThemeSetterResolvesAgainWithoutMutatingAnEarlierPreparedArtifact() {
        var result=Render("\"theme\":\"base\",\"look\":\"classic\"");var document=Assert.IsType<MermaidSequenceDocument>(result.Document);var first=document.ToSequenceArtifact();var svg=first.ToSvg();document.Theme="forest";var second=document.ToSequenceArtifact();Assert.Equal("#FFF4DD",first.Paint.ParticipantFill!.Value.ToCss());Assert.Equal("#CDE498",second.Paint.ParticipantFill!.Value.ToCss());Assert.Equal(svg,first.ToSvg());
    }
    private static MermaidRenderResult Render(string settings,MermaidPresentationOptions? host=null)=>MermaidRenderer.Render("%%{init:{"+settings+"}}%%\n"+SequencePresetFixture.Body,new MermaidRenderOptions{Presentation=host});
    private static SequenceArtifact Model(MermaidRenderResult result)=>Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
}
internal static class SequencePresetFixture {
    internal const string Body="sequenceDiagram\nparticipant A as Review\nparticipant B as Archive\nA->>B: Save";
    internal static VisualRenderContext Context(int width=1100)=>new(new VisualLayoutOptions(new VisualSize(width,1000),24));
    internal static void Capture(string stem,MermaidRenderResult result){var root=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(root))return;Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,stem+".diagnostics.txt"),string.Join("\n",result.Diagnostics.Select(d=>d.Code+":"+d.Message)));if(result.Artifact is {} artifact){File.WriteAllText(Path.Combine(root,stem+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(root,stem+".png"),artifact.ToPng());File.WriteAllText(Path.Combine(root,stem+".json"),artifact.ToInterchangeJson());}}
    internal static void Capture(string stem,PreparedVisual prepared){var root=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(root))return;Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,stem+".svg"),prepared.ToSvg());File.WriteAllBytes(Path.Combine(root,stem+".png"),prepared.ToPng());File.WriteAllText(Path.Combine(root,stem+".json"),prepared.ToArtifact("sequence-preset",VisualArtifactKind.Sequence).ToInterchangeJson());}
}
