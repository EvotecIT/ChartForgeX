using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>The complete ordinary-consumption recipe uses public native and HTML preparation surfaces for the same artifact.</summary>
public sealed class PolarViewportRecipeTests {
    [Theory]
    [InlineData(360, VisualThemeMode.Light)] [InlineData(360, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)] [InlineData(800, VisualThemeMode.Dark)]
    public void CompleteRecipeExportsTheSameCompletedNativeDrawingThroughBothPublicRoutes(int width, VisualThemeMode mode) {
        var chart = PolarViewportRecipe.Create(width, mode); var prepared = PolarViewportRecipe.Prepare(chart);
        var section = PolarViewportRecipe.PrepareSection(chart);
        Assert.Equal(prepared.ToSvg(), section.PreparedVisual.ToSvg()); Assert.Equal(prepared.ToPng(), section.PreparedVisual.ToPng());
        Assert.NotNull(section.NarrowPreparedVisual); Assert.Equal(10, prepared.Regions.Count(region => region.Role == "polar-point"));
        Assert.Equal(5.5, prepared.PolarProjection!.AngleWindow.Minimum); Assert.Equal(10, prepared.PolarProjection.GetRadiusWindow(ChartAxisSide.Secondary).Minimum);
        var png = prepared.ToPng(); var image = RasterImageDecoder.Decode(png); Assert.Equal(width, image.Width); Assert.Equal(440, image.Height);
        PolarViewportContractTests.Capture("polar-viewport-recipe-" + width + "-" + mode.ToString().ToLowerInvariant(), prepared.ToSvg(), png);
    }
}
