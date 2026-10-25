using System.Globalization;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidSequenceLayoutTypographyTests {
    [Fact]
    public void CanonicalDirectTypedAndFencePathsRetainPoliciesSourceSpansAndIdentity() {
        var source=SequenceSourcePolicyFixture.Canonical();var direct=MermaidRenderer.Render(source);
        Assert.False(direct.HasErrors);Assert.Empty(direct.Diagnostics);var model=Assert.IsType<SequenceArtifact>(direct.Artifact!.Model);
        var document=Assert.IsType<MermaidSequenceDocument>(direct.Document);var typed=document.ToSequenceArtifact();
        var fenced=new MermaidVisualMarkupParser().Parse("```mermaid\n"+source+"\n```");Assert.Empty(fenced.Diagnostics);
        var fenceModel=Assert.IsType<SequenceArtifact>(Assert.Single(fenced.Artifacts).Model);
        foreach(var item in new[]{model,typed,fenceModel}) {
            SequencePolicyFixture.Capture("sequence-source-policy-"+(ReferenceEquals(item,model)?"direct":ReferenceEquals(item,typed)?"typed":"fence"),item);
            Assert.Equal(20,item.Typography!.Message!.FontSize);Assert.Equal(18,item.Typography.Participant!.FontSize);Assert.Equal(16,item.Typography.Note!.FontSize);
            Assert.Equal(32,item.Layout!.ParticipantSpacing);Assert.Equal(14,item.Layout.ActivationWidth);Assert.Equal(new[]{"A","B"},item.Participants.Select(participant=>participant.Id));
            Assert.Equal(new[]{"Save","Ready"},item.Messages.Select(message=>message.Text));
        }
        Assert.Equal(direct.Artifact.ToSvg(),typed.ToSvg());
        var setting=document.Configuration.Settings.Last(item=>item.Path=="sequence.messageFontSize");Assert.Equal("20px",setting.Value);Assert.True(setting.IsString);Assert.True(setting.Span.Line>0);
    }
    [Fact]
    public void FamilyAndSharedHostFieldsOverrideOnlyMatchingSourceFontDimensions() {
        var source=SequenceSourcePolicyFixture.With("{\"actorFontSize\":18,\"actorFontFamily\":\"Georgia, serif\",\"actorFontWeight\":700,\"messageFontSize\":20,\"noteFontSize\":16}");
        var result=MermaidRenderer.Render(source,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{FontFamily="Arial, sans-serif",FontSize=22},
            Sequence=new MermaidSequenceRenderOptions{Presentation=new MermaidPresentationOptions{FontSize=24}}});
        Assert.Empty(result.Diagnostics);var model=Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
        Assert.Equal(24,model.Typography!.Participant!.FontSize);Assert.Equal("Arial, sans-serif",model.Typography!.Participant!.FontFamily);Assert.Equal("700",model.Typography!.Participant!.FontWeight);
        Assert.Equal(24,model.Typography!.Message!.FontSize);Assert.Equal(24,model.Typography!.Note!.FontSize);
        var prepared=model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200,900)),font:new FontSpec{Family="Courier New, monospace"}));
        SequencePolicyFixture.Capture("sequence-source-host-precedence",prepared);
        var actor=SequencePolicyFixture.Text(prepared,"sequence-participant","Review");Assert.Equal("24",actor.Attribute("font-size")!.Value);Assert.Contains("Arial",actor.Attribute("font-family")!.Value);
        Assert.Equal("18",result.Document!.Configuration.Settings.Last(item=>item.Path=="sequence.actorFontSize").Value);
    }
    [Fact]
    public void ExplicitHostPaddingOverridesSourceInsetsWhileOmissionKeepsBothAxes() {
        var source=SequenceSourcePolicyFixture.With("{\"diagramMarginX\":36,\"diagramMarginY\":28}");
        var ordinary=MermaidRenderer.Render(source);var host=MermaidRenderer.Render(source,new MermaidRenderOptions{Sequence=new MermaidSequenceRenderOptions{Padding=32}});
        var a=Assert.IsType<SequenceArtifact>(ordinary.Artifact!.Model);var b=Assert.IsType<SequenceArtifact>(host.Artifact!.Model);
        SequencePolicyFixture.Capture("sequence-source-insets",a);SequencePolicyFixture.Capture("sequence-source-host-insets",b);
        Assert.Equal(new ChartForgeX.Primitives.ChartPadding(36,28,36,28),a.Layout!.CanvasPadding);Assert.Equal(ChartForgeX.Primitives.ChartPadding.All(32),b.Layout!.CanvasPadding);
    }
    [Theory]
    [InlineData("{\"actorMargin\":-1}","actorMargin")]
    [InlineData("{\"messageFontWeight\":\"heavy\"}","messageFontWeight")]
    [InlineData("{\"noteFontFamily\":\"red; color:red\"}","noteFontFamily")]
    [InlineData("{\"wrap\":\"false\"}","wrap")]
    public void InvalidSourceValuesKeepLocatedErrorsWithoutProducingAnArtifact(string settings,string name) {
        var result=MermaidRenderer.Render(SequenceSourcePolicyFixture.With(settings));
        SequenceSourcePolicyFixture.CaptureDiagnostics("sequence-source-invalid-"+name,result);
        Assert.True(result.HasErrors);Assert.Null(result.Artifact);Assert.Contains(result.Diagnostics,item=>item.Message.Contains(name,StringComparison.Ordinal)&&item.Span.Line>0);
    }
    [Theory]
    [InlineData("compact",ChartForgeX.Themes.VisualThemeMode.Light)] [InlineData("compact",ChartForgeX.Themes.VisualThemeMode.Dark)]
    [InlineData("wide",ChartForgeX.Themes.VisualThemeMode.Light)] [InlineData("wide",ChartForgeX.Themes.VisualThemeMode.Dark)]
    public void CompleteNativeAndAdapterProgramsUseTheirActualCompileOwners(string variant,ChartForgeX.Themes.VisualThemeMode mode) {
        var model=SequenceLayoutTypographyRecipe.Create(variant,mode);var width=variant=="compact"?390:1000;
        var prepared=model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width,620)),themeMode:mode));
        SequencePolicyFixture.Capture("sequence-recipe-"+variant+"-"+mode,prepared);
        var native=ExampleRecipeSource.Emit(ExampleRecipes.Named("sequence-layout-typography"),variant,"Measured review sequence","Independent roles",mode,width,620,false,"Sequence");
        var adapter=ExampleRecipeSource.EmitSequenceLayoutSource(variant,mode);
        Assert.DoesNotContain("ChartForgeX.Mermaid",native);Assert.Contains("public static class SequenceLayoutTypographyRecipe",native);Assert.DoesNotContain("{{",native);
        Assert.Contains("ChartForgeX.Mermaid",adapter);Assert.Contains("public static class SequenceLayoutTypographySourceRecipe",adapter);Assert.DoesNotContain("{{",adapter);
    }
}

internal static class SequenceSourcePolicyFixture {
    internal static string Canonical()=>File.ReadAllText(Path.Combine(TestRepository.Root,"Build","PackageConsumers","Adapters","sequence-layout-typography.mmd"));
    internal static string With(string settings)=>"%%{init:{\"sequence\":"+settings+"}}%%\nsequenceDiagram\nparticipant A as Review\nparticipant B as Store\nA->>B: Save\nNote over A,B: Note\nB-->>A: Ready";
    internal static void CaptureDiagnostics(string stem,MermaidRenderResult result) {
        var directory=Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY")??Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if(string.IsNullOrWhiteSpace(directory))return;Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,stem+".json"),System.Text.Json.JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message,item.Span.Line,item.Span.Column})));
    }
}
