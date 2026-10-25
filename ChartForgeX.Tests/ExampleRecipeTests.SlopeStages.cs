using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    public static IEnumerable<object[]> SlopeStageSelections() {
        foreach (var variant in new[] { "stages", "horizontal-stages" }) foreach (var compact in new[] { false, true })
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
                yield return new object[] { compact ? "compact-" + variant : variant, mode };
    }
    [Theory]
    [MemberData(nameof(SlopeStageSelections))]
    public void MultiStageSlopeHasCompleteCompiledDownloadsAndNativeBothThemePoses(string variant, VisualThemeMode mode) {
        var selection = new ExampleFamilySelection(ChartSeriesKind.Slope, variant);
        Assert.Contains(selection, ExampleRecipes.FamilySelections(ChartSeriesKind.Slope));
        Assert.Contains(selection, ExampleRecipes.FamilySelections(ChartSeriesKind.Slope, curated: true));
        Assert.Contains(selection, ExampleRecipes.StandaloneFamilySelections());
        var source = Source(selection.Recipe, variant);
        Assert.Equal(nameof(SlopeStagesRecipe), selection.Recipe.Factory);
        Assert.Contains("public static class SlopeStagesRecipe", source); Assert.Contains(".AddSlope(\"Standard\"", source);
        Assert.Equal(1, source.Split("public static class ", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("ChartForgeX.Examples", source); Assert.DoesNotContain("{{", source);
        var chart = Assert.IsType<Chart>(selection.Recipe.Create(variant, mode)); Assert.Equal(mode, chart.Options.ThemeMode); Assert.Equal(2, chart.Series.Count);
        Assert.All(chart.Series, series => Assert.Equal(new[] { 1d, 2, 3, 4 }, series.Points.Select(point => point.X)));
        Assert.Equal(new[] { 28d, 43, 38, 64 }, chart.Series[0].Points.Select(point => point.Y));
        Assert.Equal(new[] { 52d, 49, 58, 71 }, chart.Series[1].Points.Select(point => point.Y));
        Assert.True(chart.Series[1].Points[2].BreakBefore); Assert.All(chart.Series, series => Assert.Equal(4, series.ObservationIds.Count));
        var width = variant.StartsWith("compact-", StringComparison.Ordinal) ? 390 : 900;
        var prepared = chart.Prepare(SlopeStagesTests.Context(width, mode));
        Assert.Equal(variant.EndsWith("horizontal-stages", StringComparison.Ordinal) ? ChartOrientation.Horizontal : ChartOrientation.Vertical, prepared.CartesianOrientation);
        Assert.Equal(8, prepared.Scene.Nodes.Count(node => node.Role == "slope-marker"));
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "slope-line").ToArray();
        Assert.Equal(2, paths.Length); Assert.Equal(1, paths[0].Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        Assert.Equal(2, paths[1].Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        var svg = XDocument.Parse(prepared.ToSvg()); var png = RasterImageDecoder.Decode(prepared.ToPng());
        Assert.Equal(width, (int?)svg.Root!.Attribute("width")); Assert.Equal(width, png.Width); Assert.Equal(400, png.Height);
        SlopeStagesTests.Save(prepared, "recipe-" + variant + "-" + mode);
        Assert.Equal(nameof(SlopeRecipe), ExampleRecipes.ForKind(ChartSeriesKind.Slope).Factory);
    }
}
