using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class FlowPortablePresentationRecipeTests {
    [Theory] [InlineData("compact",false)] [InlineData("compact",true)] [InlineData("wide",false)] [InlineData("wide",true)]
    public void CoreOnlyCompiledAndEmittedWorkflowRetainTheSeparateFrameAndEditableFlow(string variant,bool dark) {
        var mode=dark?VisualThemeMode.Dark:VisualThemeMode.Light;var artifact=FlowPortablePresentationRecipe.Create(variant,mode);
        var json=artifact.ToInterchangeJson();var flow=VisualArtifactInterchangeEnvelope.FromJson(json).ToFlowArtifact();var restored=flow.ToVisualArtifact();
        var program=ExampleRecipeSource.EmitFlowPortablePresentation(variant,mode);var svg=artifact.ToSvg();var png=artifact.ToPng();var html=artifact.ToHtmlPage();
        FlowchartAssetRecipeTests.Capture("flow-portable-program-"+variant+"-"+dark,svg,png,json,html,program);
        FlowchartAssetRecipeTests.Capture("flow-portable-program-restored-"+variant+"-"+dark,restored.ToSvg(),restored.ToPng(),restored.ToInterchangeJson(),"","");
        Assert.Equal("Request review",flow.Title);Assert.Equal("One editable workflow",flow.Subtitle);Assert.Equal(png,restored.ToPng());
        var saved=VisualArtifactInterchangeEnvelope.FromJson(json);Assert.Equal("Review workflow",saved.Presentation!.Frame!.Title);Assert.Equal(mode,saved.Presentation.Theme!.Mode??VisualThemeMode.Light);
        Assert.Contains("ToFlowArtifact()",program,StringComparison.Ordinal);Assert.Contains("FlowPortablePresentationRecipe.Create",program,StringComparison.Ordinal);Assert.DoesNotContain("ChartForgeX.Examples",program,StringComparison.Ordinal);
        Assert.DoesNotContain("<script",html,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("foreignObject",svg,StringComparison.Ordinal);
    }
}
