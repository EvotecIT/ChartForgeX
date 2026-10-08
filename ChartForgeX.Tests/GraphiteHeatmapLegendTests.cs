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
        var document = XDocument.Parse(chart.ToSvg());
        var legend = Assert.Single(document.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "heatmap-scale");
        var zero = Assert.Single(legend.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "heatmap-scale-zero");
        var zeroCell = Assert.Single(document.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "heatmap-cell" && (string?)element.Attribute("data-cfx-value") == "0");
        Assert.Equal((string?)zeroCell.RenderedAttribute("fill"), (string?)zero.RenderedAttribute("fill"));
        var steps = legend.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "heatmap-scale-step").ToArray();
        Assert.Equal(5, steps.Length);
        Assert.All(steps, step => Assert.True((double)step.RenderedAttribute("x")! > (double)zero.RenderedAttribute("x")!));
        Assert.Equal("2", (string?)steps[0].Attribute("data-cfx-value"));
        Assert.Equal("120", (string?)steps[^1].Attribute("data-cfx-value"));
        Assert.Equal(new[] { "0 events", "2 events", "120+ events" }, legend.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value));
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    public void LegendDoesNotInventNonZeroBoundsForConstantData(double value, int steps) {
        var chart = Chart.Create().WithSize(640, 320).AddHeatmapRow("Count", new[] { value, value });
        var legend = Assert.Single(XDocument.Parse(chart.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "heatmap-scale");
        var swatches = legend.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "heatmap-scale-step").ToArray();
        Assert.Equal(steps, swatches.Length);
        Assert.All(swatches, swatch => Assert.Equal(value, double.Parse((string)swatch.Attribute("data-cfx-value")!, CultureInfo.InvariantCulture)));
        Assert.All(legend.Descendants().Where(element => element.Name.LocalName == "text"), label => Assert.Equal(value.ToString(CultureInfo.InvariantCulture), label.Value));
    }
}