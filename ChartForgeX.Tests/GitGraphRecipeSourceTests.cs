using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GitGraphRecipeSourceTests {
    [Theory]
    [InlineData("wide-lr", VisualThemeMode.Light)] [InlineData("wide-lr", VisualThemeMode.Dark)]
    [InlineData("compact-lr", VisualThemeMode.Light)] [InlineData("compact-lr", VisualThemeMode.Dark)]
    [InlineData("wide-tb", VisualThemeMode.Light)] [InlineData("wide-tb", VisualThemeMode.Dark)]
    [InlineData("compact-tb", VisualThemeMode.Light)] [InlineData("compact-tb", VisualThemeMode.Dark)]
    [InlineData("wide-bt", VisualThemeMode.Light)] [InlineData("wide-bt", VisualThemeMode.Dark)]
    [InlineData("compact-bt", VisualThemeMode.Light)] [InlineData("compact-bt", VisualThemeMode.Dark)]
    public void DownloadUsesTheCompiledNativeBlockFactoryAndCompleteOrdinaryCoreOnlyProgram(string variant, VisualThemeMode mode) {
        var source = ExampleRecipeSource.EmitGitGraph(variant, mode); var block = GitGraphDepthRecipe.Create(variant, mode);
        Assert.Contains("var model = GitGraphDepthRecipe.Create(\"" + variant + "\", mode);", source);
        Assert.Contains("File.WriteAllBytes(\"visual.png\", model.ToPng());", source); Assert.Contains("File.WriteAllText(\"visual.svg\", model.ToSvg());", source);
        Assert.Contains("public static class GitGraphDepthRecipe", source); Assert.Equal(1, source.Split("public static class ", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("{{", source); Assert.DoesNotContain(".Prepare(", source); Assert.DoesNotContain("ChartForgeX.Mermaid", source); Assert.DoesNotContain("FontRegistry.Register", source);
        Assert.Equal(6, block.Commits.Count); Assert.Equal("joined", block.Commits[^1].SourceCommitId);
        Assert.Equal(variant.EndsWith("-tb", StringComparison.Ordinal) ? GitGraphDirection.TopToBottom : variant.EndsWith("-bt", StringComparison.Ordinal) ? GitGraphDirection.BottomToTop : GitGraphDirection.LeftToRight, block.Direction);
        var exact = ExampleRecipeSource.EmitGitGraph(variant, mode, galleryTypography: true);
        Assert.Contains("fonts/Carlito-Regular.ttf", exact); Assert.Contains("fonts/Carlito-Bold.ttf", exact); Assert.Contains(".WithFontFamily(\"CFX Proof Carlito\")", exact);
        Assert.NotEmpty(block.ToPng());
    }
}
