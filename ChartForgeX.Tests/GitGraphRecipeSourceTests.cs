using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GitGraphRecipeSourceTests {
    [Theory]
    [InlineData("wide-lr", ThemeMode.Light)] [InlineData("wide-lr", ThemeMode.Dark)]
    [InlineData("compact-lr", ThemeMode.Light)] [InlineData("compact-lr", ThemeMode.Dark)]
    [InlineData("wide-tb", ThemeMode.Light)] [InlineData("wide-tb", ThemeMode.Dark)]
    [InlineData("compact-tb", ThemeMode.Light)] [InlineData("compact-tb", ThemeMode.Dark)]
    [InlineData("wide-bt", ThemeMode.Light)] [InlineData("wide-bt", ThemeMode.Dark)]
    [InlineData("compact-bt", ThemeMode.Light)] [InlineData("compact-bt", ThemeMode.Dark)]
    public void DownloadUsesTheCompiledNativeBlockFactoryAndCompleteOrdinaryCoreOnlyProgram(string variant, ThemeMode mode) {
        var source = ExampleRecipeSource.EmitGitGraph(variant, mode); var block = GitGraphDepthRecipe.Create(variant, mode);
        Assert.Contains("var model = GitGraphDepthRecipe.Create(\"" + variant + "\", mode);", source);
        Assert.Contains("File.WriteAllBytes(\"visual.png\", model.ToPng());", source); Assert.Contains("File.WriteAllText(\"visual.svg\", model.ToSvg());", source);
        Assert.Contains("public static class GitGraphDepthRecipe", source); Assert.Equal(1, source.Split("public static class ", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("{{", source); Assert.DoesNotContain(".Prepare(", source); Assert.DoesNotContain("ChartForgeX.Mermaid", source); Assert.DoesNotContain("FontRegistry.Register", source);
        Assert.Equal(6, block.Commits.Count); Assert.Equal("joined", block.Commits[^1].SourceCommitId);
        Assert.Equal(variant.EndsWith("-tb", StringComparison.Ordinal) ? GitGraphDirection.TopToBottom : variant.EndsWith("-bt", StringComparison.Ordinal) ? GitGraphDirection.BottomToTop : GitGraphDirection.LeftToRight, block.Direction);
        var exact = ExampleRecipeSource.EmitGitGraph(variant, mode, galleryTypography: true);
        Assert.DoesNotContain("FontRegistry.Register", exact); Assert.Contains(".WithFontFamily(ChartFontStacks.ProductSans)", exact);
        Assert.NotEmpty(block.ToPng());
    }
}
