using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;
namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidSequenceSelfRoutesTests {
    [Theory] [InlineData(true)] [InlineData(false)]
    public void DirectFenceAndTypedSourceSelectTheSameFinitePathAndPreserveRawSettings(bool rightAngles) {
        var source=SequenceSelfSourceFixture.Source(rightAngles);var options=new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject};var direct=MermaidRenderer.Render(source,options);Assert.Empty(direct.Diagnostics);var artifact=Assert.IsType<VisualArtifact>(direct.Artifact);var fence=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");Assert.Empty(fence.Diagnostics);var document=Assert.IsType<MermaidSequenceDocument>(direct.Document);var typed=document.ToSequenceArtifact();var raw=artifact.ToInterchangeEnvelope();var restored=VisualArtifactInterchangeEnvelope.FromJson(raw.ToJson()).ToSequenceArtifact();SequenceMessageFixture.Capture("sequence-self-source-"+rightAngles,restored);Assert.Equal(rightAngles?SequenceSelfMessageRouting.Orthogonal:SequenceSelfMessageRouting.Curved,restored.Layout!.SelfMessageRouting);Assert.Equal(artifact.ToSvg(),typed.ToSvg());Assert.Equal(artifact.ToSvg(),Assert.Single(fence.Artifacts).ToSvg());Assert.Equal(rightAngles ? "true" : "false",document.Configuration.Settings.Single(setting=>setting.Path=="sequence.rightAngles").Value);Assert.Contains("\"selfMessageRouting\":",raw.ToJson());
    }
    [Theory] [InlineData("compact",VisualThemeMode.Light)] [InlineData("compact",VisualThemeMode.Dark)] [InlineData("wide",VisualThemeMode.Light)] [InlineData("wide",VisualThemeMode.Dark)]
    public void OrdinaryCompleteNativeAndSourceProgramsHaveReachableOwners(string variant,VisualThemeMode mode) {
        var model=SequenceSelfRoutesRecipe.Create(variant,mode);SequenceMessageFixture.Capture("sequence-self-recipe-"+variant+"-"+mode,model);var native=ExampleRecipeSource.Emit(ExampleRecipes.Named("sequence-self-routes"),variant,"Self routes","Captured native path",mode,variant=="compact"?390:1000,820,false,"Sequence");var source=ExampleRecipeSource.EmitSequenceSelfRoutes(variant,mode);Assert.DoesNotContain("ChartForgeX.Mermaid",native);Assert.Contains("public static class SequenceSelfRoutesRecipe",native);Assert.Contains("MermaidRenderer.Render",source);Assert.DoesNotContain("{{",source);
    }
}
internal static class SequenceSelfSourceFixture {
    internal static string Canonical()=>File.ReadAllText(Path.Combine(TestRepository.Root,"Build","PackageConsumers","Adapters","sequence-self-routes.mmd"));
    internal static string Source(bool rightAngles)=>Canonical().Replace("\"rightAngles\":false","\"rightAngles\":"+(rightAngles?"true":"false"),StringComparison.Ordinal);
}
