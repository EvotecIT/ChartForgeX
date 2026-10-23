using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    public static IEnumerable<object[]> NativeTrendModelSelections() {
        foreach (var variant in new[] { "polynomial", "logarithmic", "exponential", "power", "confidence", "prediction" })
            foreach (var compact in new[] { false, true }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
                yield return new object[] { compact ? "compact-" + variant : variant, mode };
    }

    [Theory]
    [MemberData(nameof(NativeTrendModelSelections))]
    public void NativeModelRecipesRetainMeasuredSourcesAndEmitCompleteProgramsAndIntervals(string variant, VisualThemeMode mode) {
        var selection = new ExampleFamilySelection(ChartSeriesKind.TrendLine, variant);
        Assert.Contains(selection, ExampleRecipes.FamilySelections(ChartSeriesKind.TrendLine));
        Assert.Contains(selection, ExampleRecipes.StandaloneFamilySelections());
        var recipe = selection.Recipe; var source = Source(recipe, variant);
        Assert.Contains("public static class TrendLineModelRecipe", source); Assert.DoesNotContain("ChartForgeX.Examples", source);
        var chart = Assert.IsType<Chart>(recipe.Create(variant, mode)); var observations = chart.Series[0]; var fit = chart.Series[1];
        Assert.Equal(observations.Points, fit.TrendLineSourcePoints); Assert.Equal(12, fit.TrendLineSourcePoints.Count);
        Assert.Equal(2, fit.Points.Count); Assert.True(fit.TrendLineSamples.Count >= 129);
        Assert.All(fit.TrendLineSamples, sample => { Assert.True(double.IsFinite(sample.Value)); Assert.True(sample.Lower <= sample.Value && sample.Upper >= sample.Value); });
        var width = variant.StartsWith("compact-", StringComparison.Ordinal) ? 360 : 800;
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 440)),
            VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode));
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "trend-line-interval");
        Assert.Equal(14, prepared.Regions.Count(region => region.Role == "point"));
        Assert.NotEmpty(prepared.ToSvg()); Assert.NotEmpty(prepared.ToPng());
        ComparisonOrientationTests.Save("trend-model-" + variant + "-" + mode, prepared);
    }
}
