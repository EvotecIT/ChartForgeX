using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    [Theory]
    [InlineData("wide", ThemeMode.Light)]
    [InlineData("compact", ThemeMode.Light)]
    [InlineData("wide", ThemeMode.Dark)]
    [InlineData("compact", ThemeMode.Dark)]
    public void ChordDepthUsesOneCompleteStandaloneRecipeAndPreparedBothOutputScene(string variant, ThemeMode mode) {
        var recipe = ExampleRecipes.Named("chord-depth"); var source = Source(recipe, variant);
        Assert.Contains("public static class ChordDepthRecipe", source, StringComparison.Ordinal);
        Assert.Equal(1, source.Split("public static class ", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("{{", source, StringComparison.Ordinal);
        var chart = Assert.IsType<Chart>(recipe.Create(variant, mode)); var width = variant == "compact" ? 390 : 800;
        var visual = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, variant == "compact" ? 460 : 520)), themeMode: mode));
        Assert.Equal(5, PreparedChordTests.Role(visual, "chord-node").Length); Assert.Equal(6, PreparedChordTests.Role(visual, "chord-link").Length);
        var png = RasterImageDecoder.Decode(visual.ToPng()); Assert.Equal(width, png.Width);
    }
}
