using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StandaloneHtmlSurfaceTests {
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 96)]
    [InlineData(true, 96)]
    public void StandalonePagesUseVisibleFallbackWithoutChangingAuthoredAlphaOrNativeExports(bool dark, byte alpha) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var lightBackground = ChartColor.FromRgba(40, 70, 100, alpha);
        var darkBackground = ChartColor.FromRgba(16, 32, 48, alpha);
        var lightSurface = ChartColor.FromHex("#F4F7FA");
        var darkSurface = ChartColor.FromHex("#14263A");
        var theme = VisualTheme.Graphite().Configure(builder => {
            builder.Light.Background = lightBackground;
            builder.Dark.Background = darkBackground;
            builder.Light.ElevatedSurface = lightSurface;
            builder.Dark.ElevatedSurface = darkSurface;
        });
        var expected = alpha == 0 ? dark ? darkSurface : lightSurface : dark ? darkBackground : lightBackground;
        var chart = Chart.Create().WithSize(320, 220).WithTheme(theme, mode).WithTransparentBackground(false).WithCard(false)
            .AddLine("Observed", new[] { new ChartPoint(1, 10), new ChartPoint(2, 20) });
        var chartGrid = ChartGrid.Create().WithTheme(theme, mode).WithColumns(1).Add(chart);
        var table = ChartTable.Create().WithSize(320, 220).WithTheme(theme, mode).WithColumns("Name").AddRow("Observed");
        var visualGrid = VisualGrid.Create().WithTheme(theme, mode).WithColumns(1).Add(table);
        var originalSvg = chart.ToSvg();
        var pages = new Dictionary<string, string> {
            ["chart"] = chart.ToHtmlPage(), ["chart grid"] = chartGrid.ToHtmlPage(),
            ["table"] = table.ToHtmlPage(), ["visual grid"] = visualGrid.ToHtmlPage()
        };
        foreach (var page in pages) {
            var background = Regex.Match(page.Value, @"body\{[^}]*\bbackground:([^;}]+)");
            Assert.True(background.Success, page.Key + " must declare its standalone page background.");
            Assert.Equal(expected.ToCss(), background.Groups[1].Value);
        }
        var fragments = new[] { chart.ToHtmlFragment(), chartGrid.ToHtmlFragment(), table.ToHtmlFragment(), visualGrid.ToHtmlFragment() };
        foreach (var fragment in fragments) Assert.DoesNotContain("<body", fragment);
        Assert.Equal(originalSvg, chart.ToSvg());
        var native = PngReader.Decode(chart.ToPng());
        Assert.Equal(alpha, native.Pixels[3]);
    }
}
