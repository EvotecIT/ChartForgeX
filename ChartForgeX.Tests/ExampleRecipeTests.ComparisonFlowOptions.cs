using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    public static IEnumerable<object[]> ComparisonAndFlowPolicySelections() {
        var variants = new[] {
            (ChartSeriesKind.Lollipop, "horizontal"), (ChartSeriesKind.Dumbbell, "horizontal"), (ChartSeriesKind.Dumbbell, "endpoint-markers"),
            (ChartSeriesKind.Sankey, "source-colors"), (ChartSeriesKind.Sankey, "target-colors"), (ChartSeriesKind.Sankey, "quantitative-colors")
        };
        foreach (var (kind, variant) in variants) foreach (var compact in new[] { false, true }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
            yield return new object[] { kind, compact ? "compact-" + variant : variant, mode };
    }

    [Theory]
    [MemberData(nameof(ComparisonAndFlowPolicySelections))]
    public void ComparisonAndFlowPoliciesHaveExecutableDownloadableSourcesAndNativeCompactWideBothThemeExports(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        var selection = new ExampleFamilySelection(kind, variant);
        Assert.Contains(selection, ExampleRecipes.FamilySelections(kind));
        Assert.Contains(selection, ExampleRecipes.FamilySelections(kind, curated: true));
        Assert.Contains(selection, ExampleRecipes.StandaloneFamilySelections());
        var recipe = selection.Recipe; var source = Source(recipe, variant);
        Assert.Contains("public static class " + recipe.Factory, source);
        Assert.Equal(1, source.Split("public static class ", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("ChartForgeX.Examples", source); Assert.DoesNotContain("{{", source);
        var chart = Assert.IsType<Chart>(recipe.Create(variant, mode)); var series = Assert.Single(chart.Series); Assert.Equal(kind, series.Kind);
        var compact = variant.StartsWith("compact-", StringComparison.Ordinal); var width = compact ? 360 : 800; var height = compact ? 360 : 440;
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)),
            VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode, new VisualFrame("Native policy example"));
        var prepared = chart.Prepare(context);
        if (kind is ChartSeriesKind.Lollipop or ChartSeriesKind.Dumbbell) {
            Assert.Equal(variant.EndsWith("horizontal", StringComparison.Ordinal) ? ChartOrientation.Horizontal : ChartOrientation.Vertical, prepared.CartesianOrientation);
            Assert.Equal(series.ObservationCount, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(group => group.Role == "point" && group.Metadata.ContainsKey("data-cfx-observation-id")));
            if (kind == ChartSeriesKind.Dumbbell) {
                Assert.Equal(4, series.Dumbbell.StartMarker.Radius); Assert.Equal(8, series.Dumbbell.EndMarker.Radius);
                Assert.Equal(ChartMarkerShape.Circle, series.Dumbbell.StartMarker.Shape); Assert.Equal(ChartMarkerShape.Diamond, series.Dumbbell.EndMarker.Shape);
                Assert.NotEqual(series.Dumbbell.StartMarker.Fill, series.Dumbbell.EndMarker.Fill);
                Assert.Equal(5, prepared.Scene.Nodes.Count(node => node.Role == "dumbbell-start")); Assert.Equal(5, prepared.Scene.Nodes.Count(node => node.Role == "dumbbell-end"));
                Assert.Contains(series.Dumbbells, value => value.End < value.Start); Assert.Contains(series.Dumbbells, value => value.End > value.Start);
            }
        } else {
            var options = chart.Options.Sankey; Assert.Null(options.RibbonFill);
            var quantitative = variant.EndsWith("quantitative-colors", StringComparison.Ordinal);
            Assert.Equal(quantitative ? ChartSankeyFlowColorMode.Quantitative : variant.EndsWith("target-colors", StringComparison.Ordinal)
                ? ChartSankeyFlowColorMode.Target : ChartSankeyFlowColorMode.Source, options.FlowColorMode);
            Assert.Equal(new[] { 72d, 28, 52, 20, 28, 72, 28, 5, 10 }, series.FlowLinks.Select(link => link.Value));
            Assert.Equal(9, prepared.Scene.Nodes.Count(node => node.Role == "sankey-ribbon"));
            Assert.True(prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "sankey-ribbon").Select(node => node.Fill).Distinct().Count() > 1);
            if (quantitative) {
                Assert.Equal(new double?[] { 180, 95, 125, 40, 0, null, 65, 110, 210 }, series.FlowLinks.Select(link => link.ColorValue));
                Assert.Equal("Illustrative latency", options.ColorDimension.Name);
                Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "sankey-color-scale");
            } else Assert.All(series.FlowLinks, link => Assert.Null(link.ColorValue));
        }
        var svg = prepared.ToSvg("example-policy"); var image = RasterImageDecoder.Decode(prepared.ToPng());
        Assert.Equal(width, (int?)XDocument.Parse(svg).Root!.Attribute("width")); Assert.Equal(height, image.Height); Assert.Equal(width, image.Width);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var name = "recipe-" + kind.ToString().ToLowerInvariant() + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
            File.WriteAllText(Path.Combine(directory, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
        }
    }

    [Fact]
    public void NewOptionSourcesLeaveOrdinaryComparisonAndSankeyOptionsDefaultsIntact() {
        Assert.Equal(nameof(LollipopRecipe), ExampleRecipes.ForKind(ChartSeriesKind.Lollipop).Factory);
        Assert.Equal(nameof(DumbbellRecipe), ExampleRecipes.ForKind(ChartSeriesKind.Dumbbell).Factory);
        Assert.Equal(nameof(SankeyRecipe), ExampleRecipes.ForKind(ChartSeriesKind.Sankey).Factory);
        Assert.Equal(ChartOrientation.Vertical, LollipopRecipe.Create().Options.LollipopOrientation);
        Assert.Equal(ChartOrientation.Vertical, DumbbellRecipe.Create().Options.DumbbellOrientation);
        Assert.Equal(ChartColor.FromHex("#607DAA"), SankeyOptionsRecipe.Create().Options.Sankey.RibbonFill);
        var source = SankeyOptionsRecipe.Create("source-colors"); var target = SankeyOptionsRecipe.Create("target-colors");
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 440)), VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family));
        var first = source.Prepare(context).Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "sankey-ribbon").ToArray();
        var last = target.Prepare(context).Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "sankey-ribbon").ToArray();
        Assert.False(first.Select(node => node.Fill).SequenceEqual(last.Select(node => node.Fill)));
        Assert.Equal(first.Length, last.Length); for (var index = 0; index < first.Length; index++) Assert.Equal(first[index].Commands, last[index].Commands);
    }
}
