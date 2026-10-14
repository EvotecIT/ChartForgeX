using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidThemeLayoutTests {
    private const string Timeline = "timeline\n  title Rendering history\n  2024 : Fluent chart API : SVG and PNG output\n  2025 : Topology diagrams";

    [Theory]
    [InlineData(null)]
    [InlineData("default")]
    [InlineData("dark")]
    public void SourceThemeKeepsTheGraphiteLayoutOfTheDefaultChart(string? theme) {
        var baseline = ChartOf(Timeline).Options.Theme;
        var themed = ChartOf(theme == null ? Timeline : "%%{init: {'theme':'" + theme + "'}}%%\n" + Timeline).Options.Theme;

        Assert.True(themed.UseGraphiteLayout);
        Assert.Equal(baseline.TitleFontSize, themed.TitleFontSize);
        Assert.Equal(baseline.SubtitleFontSize, themed.SubtitleFontSize);
        Assert.Equal(baseline.TickLabelFontSize, themed.TickLabelFontSize);
        Assert.Equal(baseline.LegendFontSize, themed.LegendFontSize);
        Assert.Equal(baseline.DataLabelFontSize, themed.DataLabelFontSize);
        Assert.Equal(baseline.CornerRadius, themed.CornerRadius);
        Assert.Equal(baseline.PlotCornerRadius, themed.PlotCornerRadius);
        Assert.Equal(baseline.MarkerRadius, themed.MarkerRadius);
        Assert.Equal(baseline.FlatMarks, themed.FlatMarks);
    }

    [Fact]
    public void DarkSourceThemeSwitchesToTheGraphiteDarkColors() {
        var dark = ChartOf("%%{init: {'theme':'dark'}}%%\n" + Timeline).Options.Theme;
        var expected = ChartTheme.GraphiteDark();

        Assert.Equal(expected.CardBackground, dark.CardBackground);
        Assert.Equal(expected.Text, dark.Text);
        Assert.Equal(expected.Palette, dark.Palette);
    }

    private static Chart ChartOf(string source) {
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        return Assert.IsType<Chart>(result.Artifact!.Model);
    }
}
