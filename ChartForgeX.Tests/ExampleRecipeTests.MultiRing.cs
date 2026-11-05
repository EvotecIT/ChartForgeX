using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    public static IEnumerable<object[]> MultiRingSelections() {
        foreach (var variant in new[] { "wide", "compact", "automatic", "compact-automatic", "vertical", "compact-vertical", "mixed", "compact-mixed" })
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) yield return new object[] { variant, mode };
    }

    [Theory]
    [MemberData(nameof(MultiRingSelections))]
    public void MultiRingDownloadIsCompleteAndPreservesEachSourceOwnedCalloutAndExplosion(string variant, VisualThemeMode mode) {
        var recipe = ExampleRecipes.Named("multi-ring-callouts"); var source = Source(recipe, variant);
        Assert.Contains("WithPointSliceOffset", source); Assert.Contains("WithObservationIds", source); Assert.Contains("WithAutomaticRadialBands", source);
        Assert.DoesNotContain("{{", source); Assert.DoesNotContain("ChartForgeX.Examples", source);
        var chart = Assert.IsType<Chart>(recipe.Create(variant, mode)); Assert.Equal(2, chart.Series.Count);
        Assert.Equal(new[] { 90d, 120 }, chart.Series.Select(series => series.Points.Sum(point => point.Y)));
        chart.WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
        var prepared = MultiRingDepthTests.Prepare(chart); Assert.Equal(4, prepared.Scene.Nodes.OfType<VisualSceneSlice>().Count());
        MultiRingDepthTests.AssertCalloutAssociations(prepared); Assert.Equal((int)chart.Options.Size.Width, RasterImageDecoder.Decode(prepared.ToPng()).Width);
        // A compact frame's side lanes are narrow; its automatic callouts use the open bands above and below the rings instead of shortening names.
        if (variant is "compact" or "compact-automatic") {
            var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").Select(node => Assert.Single(node.Text.Lines).Text).ToArray();
            Assert.Equal(4, captions.Length); Assert.All(captions, caption => Assert.DoesNotContain("…", caption));
        }
        NumericRadialEncodedGeometryTests.Capture(chart, "recipe-multiring-" + variant + "-" + mode);
    }
}
