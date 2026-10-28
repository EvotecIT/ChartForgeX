using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    public static IEnumerable<object[]> MixedScheduleSelections() {
        foreach (var variant in new[] { "wide", "compact", "reversed", "compact-reversed", "time", "compact-time", "time-reversed", "compact-time-reversed" })
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) yield return new object[] { variant, mode };
    }

    [Theory]
    [MemberData(nameof(MixedScheduleSelections))]
    public void ScheduleDownloadContainsOnlyTheCompletePublicTypedFactory(string variant, VisualThemeMode mode) {
        var recipe = ExampleRecipes.Named("mixed-schedule"); var source = Source(recipe, variant);
        foreach (var contract in new[] { "ChartSeriesKind.Timeline", "CreateGanttTask", "CreateStateTimelineLane", "CreateGanttLane", "AddGanttDependency" }) Assert.Contains(contract, source);
        Assert.DoesNotContain("{{", source); Assert.DoesNotContain("ChartForgeX.Examples", source);
        var chart = Assert.IsType<Chart>(recipe.Create(variant, mode)); Assert.Equal(4, chart.Series.Select(series => series.Kind).Distinct().Count());
        Assert.Equal(6, chart.Series.Count); Assert.Single(chart.GanttDependencies);
        chart.WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
        var prepared = ScheduleReversalDepthTests.Prepare(chart);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "schedule-composition");
        var png = RasterImageDecoder.Decode(prepared.ToPng()); Assert.Equal(variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 900, png.Width);
        Assert.Equal(660, png.Height); NumericRadialEncodedGeometryTests.Capture(chart, "recipe-mixed-schedule-" + variant + "-" + mode);
    }
}
