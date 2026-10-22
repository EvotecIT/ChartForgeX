using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the authored homepage observations shared by its chart and Markdown presentation.</summary>
public sealed class WebsiteHeroRecipeTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void HomepageRecipeRetainsTheFullSeriesInNativeExports(VisualThemeMode mode) {
        var (names, weeks, values) = HeroSignInsWebsiteRecipe.GetData();
        var chart = HeroSignInsWebsiteRecipe.Create(mode);
        Assert.Equal(new[] { "Passkey", "Authenticator app", "SMS code", "Password only" }, names);
        Assert.Equal(12, weeks.Length);
        Assert.Equal("W1", weeks[0]);
        Assert.Equal("W12", weeks[^1]);
        Assert.Equal(names, chart.Series.Select(series => series.Name));
        for (var index = 0; index < names.Length; index++) {
            Assert.Equal(12, chart.Series[index].SourcePointCount);
            Assert.Equal(values[index], chart.Series[index].Points.Select(point => point.Y));
        }
        Assert.Equal(new[] { 8900d, 6200, 1300, 1500 }, values.Select(series => series[^1]));

        var svg = XDocument.Parse(chart.ToSvg("homepage-recipe"));
        Assert.Equal("960", (string?)svg.Root!.Attribute("width"));
        Assert.Equal("540", (string?)svg.Root.Attribute("height"));
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "desc"
            && element.Value.Contains("8,900", StringComparison.Ordinal));
        var png = chart.ToPng();
        Assert.Equal(960, ReadInt32(png, 16));
        Assert.Equal(540, ReadInt32(png, 20));
    }

    private static int ReadInt32(byte[] bytes, int offset) =>
        bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
}
