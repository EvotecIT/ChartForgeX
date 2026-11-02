using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>A category or series named Other is the remainder: palette v2's neutral grey, never a hue.</summary>
public sealed class NeutralOtherCategoryTests {
    [Theory]
    [InlineData(ChartSeriesKind.Pie, VisualThemeMode.Light)] [InlineData(ChartSeriesKind.Donut, VisualThemeMode.Dark)]
    public void PartToWholeOtherIsNeutralAndTheHuesStayContiguous(ChartSeriesKind kind, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(640, 360).WithTheme(VisualTheme.Graphite(), mode)
            .WithXLabels("Licenses", "Support", "Other", "Training")
            .AddSeries(new ChartSeries(new ChartSeriesId("revenue"), "Revenue", kind, new[] { new ChartPoint(1, 48), new ChartPoint(2, 41), new ChartPoint(3, 45), new ChartPoint(4, 26) }));
        var colors = chart.Options.Theme.Resolve(mode);
        var series = chart.Series[0];
        Assert.Equal(colors.Other, ChartSeriesColours.CategoryPoint(series, 2, colors));
        // Other is the second largest value but takes no slot: the remaining categories rank 0, 1 and 2.
        Assert.Equal(new[] { colors.Palette[0], colors.Palette[1], colors.Palette[2] },
            new[] { 0, 1, 3 }.Select(point => ChartSeriesColours.CategoryPoint(series, point, colors)).ToArray());
        // The painted slice uses the same neutral fill.
        Assert.Contains(XDocument.Parse(chart.ToSvg()).Descendants(), item => ((string?)item.Attribute("data-cfx-role"))?.Contains("slice", StringComparison.Ordinal) == true
            && ChartColor.TryParse((string?)item.Attribute("fill"), out var fill) && fill.Equals(colors.Other));
    }

    [Fact]
    public void SeriesNamedOtherIsNeutralWhileAnExplicitColourStillWins() {
        var chart = Chart.Create().WithXLabels("Q1", "Q2")
            .AddSeries(new ChartSeries(new ChartSeriesId("product"), "Product", ChartSeriesKind.Bar, new[] { new ChartPoint(1, 4), new ChartPoint(2, 5) }))
            .AddSeries(new ChartSeries(new ChartSeriesId("other"), "Other", ChartSeriesKind.Bar, new[] { new ChartPoint(1, 2), new ChartPoint(2, 1) }));
        var colors = chart.Options.Theme.Resolve(chart.Options.ThemeMode);
        Assert.Equal(colors.Other, ChartSeriesColours.Resolve(chart, 1, colors));
        Assert.Equal(colors.Palette[0], ChartSeriesColours.Resolve(chart, 0, colors));
        chart.Series[1].Color = ChartColor.FromHex("#123456");
        Assert.Equal(ChartColor.FromHex("#123456"), ChartSeriesColours.Resolve(chart, 1, colors));
    }
}
