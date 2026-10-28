using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidSequenceParticipantViewsTests {
    [Fact]
    public void DirectTypedAndFenceViewsRetainLocatedSourceAndEveryRawParticipantIdentity() {
        var source=SequenceViewSourceFixture.Canonical();var direct=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject});Assert.Empty(direct.Diagnostics);
        var document=Assert.IsType<MermaidSequenceDocument>(direct.Document);var typed=document.ToSequenceArtifact();var model=Assert.IsType<SequenceArtifact>(direct.Artifact!.Model);
        var fence=new MermaidVisualMarkupParser(new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject}).Parse("```mermaid\n"+source+"\n```");Assert.Empty(fence.Diagnostics);var fenced=Assert.IsType<SequenceArtifact>(Assert.Single(fence.Artifacts).Model);
        foreach(var item in new[]{model,typed,fenced}) {
            Assert.Equal(new[]{"A","Unused","B","Empty","N"},item.Participants.Select(participant=>participant.Id));Assert.True(item.Layout!.MirrorParticipants);Assert.True(item.Layout.HideUnusedParticipants);
            var prepared=item.Prepare(SequenceViewFixture.Context(1300));SequenceViewFixture.Capture("sequence-source-view-"+(ReferenceEquals(item,model)?"direct":ReferenceEquals(item,typed)?"typed":"fence"),prepared);
            Assert.Equal(new[]{"A","B","N"},prepared.Regions.Where(region=>region.Role=="sequence-participant").Select(region=>region.Id));Assert.Equal(3,prepared.Regions.Count(region=>region.Role=="sequence-participant-footer"));
        }
        Assert.Equal(model.ToSvg(),typed.ToSvg());Assert.Equal(model.ToPng(),typed.ToPng());
        var setting=document.Configuration.Settings.Last(item=>item.Path=="sequence.mirrorActors");Assert.False(setting.IsString);Assert.Equal("true",setting.Value);Assert.Equal(1,setting.Span.Line);
    }
    [Fact]
    public void AuthoredFalseFieldsOverridePinnedOmittedMirrorWhileRetainingEverySourceIdentity() {
        var body=string.Join("\n",SequenceViewSourceFixture.Canonical().Split('\n').Skip(1));var ordinary=MermaidRenderer.Render(body);
        var disabled=MermaidRenderer.Render("%%{init:{\"sequence\":{\"mirrorActors\":false,\"hideUnusedParticipants\":false}}}%%\n"+body);
        Assert.Empty(ordinary.Diagnostics);Assert.Empty(disabled.Diagnostics);SequenceViewFixture.Capture("sequence-source-view-absent",Assert.IsType<SequenceArtifact>(ordinary.Artifact!.Model));SequenceViewFixture.Capture("sequence-source-view-false",Assert.IsType<SequenceArtifact>(disabled.Artifact!.Model));
        var included=Assert.IsType<SequenceArtifact>(ordinary.Artifact.Model);var excluded=Assert.IsType<SequenceArtifact>(disabled.Artifact.Model);Assert.True(included.Layout!.MirrorParticipants);Assert.False(excluded.Layout!.MirrorParticipants);Assert.Equal(included.Participants.Select(item=>item.Id),excluded.Participants.Select(item=>item.Id));Assert.Equal(included.Participants.Count,SequencePreparedCompiler.PrepareDefault(included).Regions.Count(item=>item.Role=="sequence-participant-footer"));Assert.DoesNotContain(SequencePreparedCompiler.PrepareDefault(excluded).Regions,item=>item.Role=="sequence-participant-footer");Assert.NotEqual(ordinary.Artifact.ToSvg(),disabled.Artifact.ToSvg());
    }
    [Fact]
    public void SourceDestroyedFooterPreservesLifetimeFactsAndPortableMeasuredGeometry() {
        var result=MermaidRenderer.Render(SequenceViewSourceFixture.Canonical());Assert.Empty(result.Diagnostics);var model=Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
        var context=SequenceViewFixture.Context(1300);var prepared=model.Prepare(context);var envelope=prepared.ToArtifact(model.Id,VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        var restored=VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToSequenceArtifact();var second=restored.Prepare(context);
        SequenceViewFixture.Capture("sequence-source-view-portable-first",prepared);SequenceViewFixture.Capture("sequence-source-view-portable-second",second);
        Assert.Equal(prepared.ToSvg(),second.ToSvg());Assert.Equal(prepared.ToPng(),second.ToPng());Assert.Equal(2,restored.Participants.Single(item=>item.Id=="B").DestroyedAtMessageIndex);
        var footer=prepared.Regions.Single(item=>item.Role=="sequence-participant-footer"&&item.Id=="B");var terminal=envelope.Edges[2].ResolvedRoute.Last();Assert.Equal(footer.Bounds.Top+footer.Bounds.Height/2,terminal.Y);Assert.Equal(footer.Bounds.Left,terminal.X);
    }
    [Theory]
    [InlineData("mirrorActors")] [InlineData("hideUnusedParticipants")]
    public void StringViewFlagsRejectAtTheirActualSourceSpan(string name) {
        var result=MermaidRenderer.Render("%%{init:{\"sequence\":{\""+name+"\":'true'}}}%%\nsequenceDiagram\nparticipant A");
        SequenceSourcePolicyFixture.CaptureDiagnostics("sequence-source-view-invalid-"+name,result);Assert.True(result.HasErrors);Assert.Null(result.Artifact);Assert.Contains(result.Diagnostics,item=>item.Message.Contains(name,StringComparison.Ordinal)&&item.Span.Line==1);
    }
    [Theory]
    [InlineData("compact",VisualThemeMode.Light)] [InlineData("compact",VisualThemeMode.Dark)] [InlineData("wide",VisualThemeMode.Light)] [InlineData("wide",VisualThemeMode.Dark)]
    public void CompleteNativeAndSourceProgramsUseExistingCoreAndAdapterCompileBoundaries(string variant,VisualThemeMode mode) {
        var model=SequenceParticipantViewsRecipe.Create(variant,mode);var width=variant=="compact"?390:1000;
        var prepared=model.Prepare(new ChartForgeX.Rendering.VisualRenderContext(new ChartForgeX.Rendering.VisualLayoutOptions(new ChartForgeX.Rendering.VisualSize(width,820)),themeMode:mode));SequenceViewFixture.Capture("sequence-view-recipe-"+variant+"-"+mode,prepared);
        var native=ExampleRecipeSource.Emit(ExampleRecipes.Named("sequence-participant-views"),variant,"Participant views","Source identities",mode,width,820,false,"Sequence");var source=ExampleRecipeSource.EmitSequenceParticipantViews(variant,mode);
        Assert.DoesNotContain("ChartForgeX.Mermaid",native);Assert.Contains("public static class SequenceParticipantViewsRecipe",native);Assert.DoesNotContain("{{",native);
        Assert.Contains("ChartForgeX.Mermaid",source);Assert.Contains("public static class SequenceParticipantViewsSourceRecipe",source);Assert.DoesNotContain("{{",source);
    }
}

internal static class SequenceViewSourceFixture {
    internal static string Canonical()=>File.ReadAllText(Path.Combine(TestRepository.Root,"Build","PackageConsumers","Adapters","sequence-participant-views.mmd"));
}
