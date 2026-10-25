using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidSequenceMessagePresentationTests {
    [Fact]
    public void DirectTypedAndFencedSourceRetainsAlignmentNumberIdentitySpansAndRawValues() {
        var source=SequenceMessageSourceFixture.Canonical();var direct=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject});Assert.Empty(direct.Diagnostics);
        var document=Assert.IsType<MermaidSequenceDocument>(direct.Document);var typed=document.ToSequenceArtifact();var model=Assert.IsType<SequenceArtifact>(direct.Artifact!.Model);
        var fenced=new MermaidVisualMarkupParser(new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject}).Parse("```mermaid\n"+source+"\n```");Assert.Empty(fenced.Diagnostics);
        foreach(var item in new[]{model,typed,Assert.IsType<SequenceArtifact>(Assert.Single(fenced.Artifacts).Model)}) {
            Assert.Equal(TextAlignment.Right,item.Layout!.MessageAlignment);Assert.Equal(TextAlignment.Center,item.Layout.NoteAlignment);Assert.Equal(SequenceNumberPresentation.Badge,item.Layout.NumberPresentation);
            Assert.Equal(new[]{"1000","1001","1002"},item.Messages.Select(message=>message.Metadata["mermaid.autonumber.value"]));Assert.Equal(new[]{"A","B"},item.Participants.Select(participant=>participant.Id));
            var prepared=item.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-source-message-"+(ReferenceEquals(item,model)?"direct":ReferenceEquals(item,typed)?"typed":"fence"),prepared);
            Assert.Equal(new[]{"1000","1001","1002"},prepared.Regions.Where(region=>region.Role=="sequence-number").Select(region=>region.Label));SequenceMessageFixture.AssertBadgeContainsInk(prepared);
        }
        Assert.Equal(model.ToSvg(),typed.ToSvg());var setting=document.Configuration.Settings.Last(item=>item.Path=="sequence.messageAlign");Assert.Equal("right",setting.Value);Assert.True(setting.IsString);Assert.Equal(1,setting.Span.Line);
    }
    [Fact]
    public void SourceFalseAndNoneKeepUnnumberedOrdinaryOutputWhileAutonumberOccurrencesWinFalse() {
        const string body="sequenceDiagram\nA->>B: Save";var ordinary=MermaidRenderer.Render(body);var disabled=MermaidRenderer.Render("%%{init:{\"sequence\":{\"showSequenceNumbers\":false}}}%%\n"+body);
        Assert.Empty(ordinary.Diagnostics);Assert.Empty(disabled.Diagnostics);
        var ordinaryArtifact=Assert.IsType<VisualArtifact>(ordinary.Artifact);var disabledArtifact=Assert.IsType<VisualArtifact>(disabled.Artifact);
        Assert.Equal(ordinaryArtifact.ToSvg(),disabledArtifact.ToSvg());Assert.Equal(ordinaryArtifact.ToPng(),disabledArtifact.ToPng());
        SequenceMessageFixture.Capture("sequence-source-message-none",Assert.IsType<SequenceArtifact>(ordinaryArtifact.Model));SequenceMessageFixture.Capture("sequence-source-message-false",Assert.IsType<SequenceArtifact>(disabledArtifact.Model));
        var configured=MermaidRenderer.Render("%%{init:{\"sequence\":{\"showSequenceNumbers\":false}}}%%\nsequenceDiagram\nautonumber 5\nA->>B: Save");Assert.Empty(configured.Diagnostics);
        var prepared=Assert.IsType<SequenceArtifact>(configured.Artifact!.Model).Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-source-message-false-autonumber",prepared);
        Assert.Equal("5",Assert.Single(prepared.Regions,item=>item.Role=="sequence-number").Label);
    }
    [Fact]
    public void AuthoredNumberAndSignalInkAreExactAndNativePartialOverridesRemainIndependent() {
        var result=MermaidRenderer.Render(SequenceMessageSourceFixture.Canonical());Assert.Empty(result.Diagnostics);var model=Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
        Assert.Equal(ChartColor.White,model.Paint.NumberText);Assert.Equal(ChartColor.FromHex("#3b5ccc"),model.Paint.MessageLine);Assert.Null(model.Paint.NumberFill);
        var prepared=model.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-source-message-authored-paint",prepared);
        Assert.Equal("#FFFFFF",SequenceMessageFixture.Text(prepared,"sequence-number","1000").Attribute("fill")!.Value);
        model.Typography=new SequenceTypography(number:new TextStyleOverride{FontSize=20,Color=ChartColor.FromHex("#a02020")});model.Paint.NumberFill=ChartColor.FromHex("#e6f0ff");
        var overridden=model.Prepare(SequenceMessageFixture.Context());SequenceMessageFixture.Capture("sequence-source-message-native-override",overridden);
        Assert.Equal("20",SequenceMessageFixture.Text(overridden,"sequence-number","1000").Attribute("font-size")!.Value);Assert.Equal("#A02020",SequenceMessageFixture.Text(overridden,"sequence-number","1000").Attribute("fill")!.Value);SequenceMessageFixture.AssertBadgeContainsInk(overridden);
    }
    [Theory]
    [InlineData("\"sequence\":{\"messageAlign\":\"justify\"}","messageAlign")] [InlineData("\"sequence\":{\"noteAlign\":1}","noteAlign")]
    [InlineData("\"themeVariables\":{\"sequenceNumberColor\":\"not-a-color\"}","sequenceNumberColor")]
    public void UnsupportedSourceValuesStayLocatedBeforeAnyArtifact(string setting,string name) {
        var result=MermaidRenderer.Render("%%{init:{"+setting+"}}%%\nsequenceDiagram\nA->>B: Save");SequenceSourcePolicyFixture.CaptureDiagnostics("sequence-source-message-invalid-"+name,result);
        Assert.True(result.HasErrors);Assert.Null(result.Artifact);Assert.Contains(result.Diagnostics,item=>item.Message.Contains(name,StringComparison.Ordinal)&&item.Span.Line==1);
    }
    [Theory]
    [InlineData("compact",VisualThemeMode.Light)] [InlineData("compact",VisualThemeMode.Dark)] [InlineData("wide",VisualThemeMode.Light)] [InlineData("wide",VisualThemeMode.Dark)]
    public void CompleteNativeAndAdapterProgramsRemainInTheirActualCompileOwners(string variant,VisualThemeMode mode) {
        var model=SequenceMessagePresentationRecipe.Create(variant,mode);var width=variant=="compact"?390:1000;var prepared=model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width,820)),themeMode:mode));SequenceMessageFixture.Capture("sequence-message-recipe-"+variant+"-"+mode,prepared);SequenceMessageFixture.AssertBadgeContainsInk(prepared);
        var native=ExampleRecipeSource.Emit(ExampleRecipes.Named("sequence-message-presentation"),variant,"Message presentation","Aligned captions",mode,width,820,false,"Sequence");var adapter=ExampleRecipeSource.EmitSequenceMessagePresentation(variant,mode);
        Assert.DoesNotContain("ChartForgeX.Mermaid",native);Assert.Contains("public static class SequenceMessagePresentationRecipe",native);Assert.DoesNotContain("{{",native);
        Assert.Contains("ChartForgeX.Mermaid",adapter);Assert.Contains("public static class SequenceMessagePresentationSourceRecipe",adapter);Assert.DoesNotContain("{{",adapter);
    }
}

internal static class SequenceMessageSourceFixture {
    internal static string Canonical()=>File.ReadAllText(Path.Combine(TestRepository.Root,"Build","PackageConsumers","Adapters","sequence-message-presentation.mmd"));
}
