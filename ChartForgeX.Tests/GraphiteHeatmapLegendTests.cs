using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteHeatmapLegendTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroPrecedesRampAndObservedNonZeroRangeUsesValueFormat(bool dark) {
        var chart = Chart.Create().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithSize(640, 320)
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + (value == 120 ? "+ events" : " events"))
            .AddHeatmapRow("Count", new[] { 0d, 2, 20, 120 });
        chart.Options.HeatmapRelativeScale = true;
        var legend = Assert.Single(XDocument.Parse(chart.ToSvg()).Descendants(), e => (string?)e.Attribute("data-cfx-role") == "heatmap-scale");
        var items = legend.Elements().ToArray();
        Assert.Equal("0 events", items[0].Value);
        Assert.Equal("heatmap-scale-zero", (string?)items[1].Attribute("data-cfx-role"));
        Assert.Equal(chart.Options.Theme.Neutral3.ToCss(), (string?)items[1].Attribute("fill"));
        var steps = items.Where(e => (string?)e.Attribute("data-cfx-role") == "heatmap-scale-step").ToArray();
        Assert.Equal(5, steps.Length);
        Assert.All(steps, step => Assert.True((double)step.Attribute("x")! > (double)items[1].Attribute("x")!));
        Assert.Equal(new[] { "0 events", "2 events", "…", "120+ events" }, items.Where(e => e.Name.LocalName == "text").Select(e => e.Value));
        Assert.Equal("heatmap-scale-step", (string?)items[2].Attribute("data-cfx-role"));
        Assert.True((double)items[^1].Attribute("x")! > (double)steps[^1].Attribute("x")!);
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    public void LegendDoesNotInventNonZeroBoundsForConstantData(double value, int steps) {
        var chart = Chart.Create().WithSize(640, 320).AddHeatmapRow("Count", new[] { value, value });
        var legend = Assert.Single(XDocument.Parse(chart.ToSvg()).Descendants(), e => (string?)e.Attribute("data-cfx-role") == "heatmap-scale");
        Assert.Equal(steps, legend.Elements().Count(e => (string?)e.Attribute("data-cfx-role") == "heatmap-scale-step"));
        if (value == 0) Assert.Equal("0", Assert.Single(legend.Elements(), e => e.Name.LocalName == "text").Value);
        else Assert.Equal(new[] { "0", "5", "…", "5" }, legend.Elements().Where(e => e.Name.LocalName == "text").Select(e => e.Value));
    }
}
