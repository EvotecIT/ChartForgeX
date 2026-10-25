using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("right", VisualThemeMode.Dark)]
    [InlineData("bottom", VisualThemeMode.Light)]
    [InlineData("top", VisualThemeMode.Dark)]
    [InlineData("image", VisualThemeMode.Light)]
    [InlineData("contour", VisualThemeMode.Dark)]
    public void PictorialArtworkRecipeContainsCompleteSafeSourceAndRetainedFractionalFacts(string variant, VisualThemeMode mode) {
        var recipe = ExampleRecipes.Named("pictorial-artwork"); var source = Source(recipe, variant);
        Assert.Equal(nameof(PictorialArtworkRecipe), recipe.Factory); Assert.Contains("public static class PictorialArtworkRecipe", source);
        Assert.Contains("ChartPictorialArtwork.FromSvg", source); Assert.Contains("ChartPictorialArtwork.FromImage", source);
        Assert.DoesNotContain("ChartForgeX.Examples", source); Assert.DoesNotContain("{{", source);
        var chart = Assert.IsType<Chart>(recipe.Create(variant, mode)); Assert.Equal(mode, chart.Options.ThemeMode);
        Assert.Equal(new[] { 2.5, 1.25, .75 }, chart.Series[0].Points.Select(point => point.Y));
        Assert.Equal(new[] { "team-a", "team-b", "team-c" }, chart.Series[0].ObservationIds.OrderBy(pair => pair.Key).Select(pair => pair.Value.Value));
        Assert.Equal(variant == "contour", chart.Options.PictorialArtwork == null);
        var svg = XDocument.Parse(chart.ToSvg()); var png = RasterImageDecoder.Decode(chart.ToPng());
        Assert.Equal(variant == "compact" ? 390 : 900, png.Width); Assert.Equal(460, png.Height);
        Assert.Equal(3, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "pictorial-item"));
        Assert.Equal(12, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "pictorial-symbol"));
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-fill") == "0.5");
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName is "script" or "foreignObject");
        Assert.All(svg.Descendants().Where(node => node.Name.LocalName == "image"), node => Assert.StartsWith("data:image/png;base64,", node.Attribute("href")!.Value));
        PictorialArtworkTests.Save(chart.Prepare(VisualExportRequest.ForChart(chart).Context), "recipe-" + variant + "-" + mode);
    }
}
