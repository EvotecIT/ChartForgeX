using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphSceneStaticThemeTests {
    private static GraphScene Scene() => GraphScene.Create("estate", "Estate")
        .AddNode("global", "Global")
        .AddNode("europe", "Europe", node => { node.ParentId = "global"; node.Status = "warning"; })
        .AddNode("apac", "Asia Pacific", node => node.ParentId = "global")
        .AddEdge("global-europe", "global", "europe")
        .AddEdge("global-apac", "global", "apac");

    [Fact]
    public void StaticSvgDefaultsToTheLightThemeWithoutAFixedBackground() {
        var svg = Scene().ToGraphSvg();
        Assert.Contains("data-cfx-graph-theme=\"light\"", svg, StringComparison.Ordinal);
        Assert.Contains("fill:var(--cfx-color-paper,#ffffff)", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("style=\"fill:#ffffff\"", svg, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HtmlGraphExplorerTheme.Dark, "dark")]
    [InlineData(HtmlGraphExplorerTheme.System, "system")]
    public void StaticSvgCarriesTheRequestedTheme(HtmlGraphExplorerTheme theme, string expected) {
        var svg = Scene().ToGraphSvg(configure: options => options.Theme = theme);
        Assert.Contains("data-cfx-graph-theme=\"" + expected + "\"", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void StaticPngPaintsTheDarkThemeBackground() {
        var light = PngReader.Decode(Scene().ToGraphPng(configure: options => { options.Width = 320; options.Height = 180; }));
        var dark = PngReader.Decode(Scene().ToGraphPng(configure: options => {
            options.Width = 320;
            options.Height = 180;
            options.Theme = HtmlGraphExplorerTheme.Dark;
        }));

        Assert.True(Luminance(light, 2, 2) > 240, "The light static graph should keep its white paper.");
        Assert.True(Luminance(dark, 2, 2) < 40, "The dark static graph should paint the dark paper in PNG as well as SVG.");
        Assert.True(Luminance(light, 160, 90) > 240, "The light static graph should use the light theme inside its canvas.");
        Assert.True(Luminance(dark, 160, 90) < 80, "The dark static graph should use the dark theme inside its canvas.");
        Assert.NotEqual(light.Pixels, dark.Pixels);
    }

    [Fact]
    public void SystemThemePngRendersLight() {
        var system = Scene().ToGraphPng(configure: options => { options.Width = 320; options.Height = 180; options.Theme = HtmlGraphExplorerTheme.System; });
        var light = Scene().ToGraphPng(configure: options => { options.Width = 320; options.Height = 180; });
        Assert.Equal(light, system);
    }

    [Fact]
    public void UndefinedThemeIsRejected() {
        Assert.Throws<InvalidOperationException>(() => Scene().ToGraphSvg(configure: options => options.Theme = (HtmlGraphExplorerTheme)42));
    }

    private static double Luminance(RgbaImage image, int x, int y) {
        var index = (y * image.Width + x) * 4;
        return 0.2126 * image.Pixels[index] + 0.7152 * image.Pixels[index + 1] + 0.0722 * image.Pixels[index + 2];
    }
}
