using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("stacked", VisualThemeMode.Dark)]
    [InlineData("compact-stacked", VisualThemeMode.Light)]
    [InlineData("radius", VisualThemeMode.Light)]
    [InlineData("compact-radius", VisualThemeMode.Dark)]
    public void CategoricalRadialAreaHasCompleteCompiledSourceAndUnmodifiedObservations(string variant, VisualThemeMode mode) {
        var recipe = ExampleRecipes.Named("categorical-radial-area"); var source = Source(recipe, variant);
        Assert.Equal(nameof(CategoricalRadialAreaRecipe), recipe.Factory);
        Assert.Contains("public static class CategoricalRadialAreaRecipe", source);
        Assert.Contains("ChartRadialValueEncoding.Area", source); Assert.Contains("ChartRadialValueEncoding.Radius", source);
        Assert.DoesNotContain("ChartForgeX.Examples", source); Assert.DoesNotContain("{{", source);
        var chart = Assert.IsType<Chart>(recipe.Create(variant, mode)); Assert.Equal(mode, chart.Options.ThemeMode);
        Assert.Equal(new[] { "completed", "follow-up" }, chart.Series.Select(series => series.Id.Value));
        Assert.Equal(new[] { 25d, 80, 0, 45 }, chart.Series[0].Points.Select(point => point.Y));
        Assert.Equal(new[] { 15d, 25, 20, 30 }, chart.Series[1].Points.Select(point => point.Y));
        Assert.All(chart.Series, series => Assert.Equal(new[] { "north", "south", "east", "west" }, series.ObservationIds.OrderBy(pair => pair.Key).Select(pair => pair.Value.Value)));
        var radius = variant.EndsWith("radius", StringComparison.Ordinal);
        Assert.Equal(radius ? ChartRadialValueEncoding.Radius : ChartRadialValueEncoding.Area, chart.Options.RadialGeometry.ValueEncoding);
        var svg = XDocument.Parse(chart.ToSvg()); var points = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(8, points.Length); Assert.All(points, point => Assert.Equal(radius ? null : "area", (string?)point.Attribute("data-cfx-value-encoding")));
        var east = points.Single(point => (string?)point.Attribute("data-cfx-series-key") == "completed" && (string?)point.Attribute("data-cfx-observation-id") == "east");
        Assert.Equal("0", (string?)east.Attribute("data-cfx-y"));
        var image = RasterImageDecoder.Decode(chart.ToPng()); var width = variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 900;
        Assert.Equal(width, (int?)svg.Root!.Attribute("width")); Assert.Equal(width, image.Width);
        Assert.Equal(width == 390 ? 520 : 600, image.Height);
        RadialAreaEncodingTests.Capture(chart, "recipe-" + variant + "-" + mode);
    }
}
