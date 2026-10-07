using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class LegendDefaultsTests {
    [Theory]
    [InlineData("bar")]
    [InlineData("histogram")]
    [InlineData("pie")]
    [InlineData("donut")]
    public void OneEntryIsHiddenUntilExplicitlyEnabled(string kind) {
        var chart = Create(kind);
        Assert.DoesNotContain(XDocument.Parse(chart.ToSvg()).Descendants(), IsLegend);
        var automaticPng = chart.ToPng();
        chart.WithLegend(false);
        Assert.Equal(automaticPng, chart.ToPng());
        chart.WithLegend(true);
        Assert.Contains(XDocument.Parse(chart.ToSvg()).Descendants(), IsLegend);
        Assert.False(automaticPng.SequenceEqual(chart.ToPng()));
    }

    [Theory]
    [InlineData(40)]
    [InlineData(70)]
    [InlineData(90)]
    public void GaugeLegendUsesTheDrawnValueColor(double value) {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(480, 340).WithLegend(true).AddGauge("Score", value);
        var svg = XDocument.Parse(chart.ToSvg());
        var arc = Assert.Single(svg.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "gauge-value");
        var item = Assert.Single(svg.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "legend-entry");
        var swatch = Assert.Single(item.Descendants(), e => e.Name.LocalName == "rect");
        var arcPath = arc.DescendantsAndSelf().Single(e => e.Attribute("stroke") != null);
        Assert.Equal((string?)arcPath.Attribute("stroke"), (string?)swatch.Attribute("fill"));
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SliceNameAndPercentBelongToOneItemWithAnInlineGap(bool donut) {
        var chart = Chart.Create().WithSize(700, 440).WithXLabels("Completed", "Pending");
        var points = new[] { new ChartPoint(0, 70), new ChartPoint(1, 30) };
        if (donut) chart.AddDonut("Work", points); else chart.AddPie("Work", points);
        var items = XDocument.Parse(chart.ToSvg()).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "legend-entry").ToArray();
        Assert.Equal(2, items.Length);
        foreach (var item in items) {
            var name = Assert.Single(item.Descendants(), e => e.Name.LocalName == "text" && (string?)e.Parent?.Attribute("data-cfx-role") == "legend-label");
            var value = Assert.Single(item.Descendants(), e => e.Name.LocalName == "text" && (string?)e.Parent?.Attribute("data-cfx-role") == "legend-value");
            Assert.Contains(name.Value, new[] { "Completed", "Pending" });
            Assert.Contains(value.Value, new[] { "70%", "30%" });
            Assert.Contains(name.Value + ": " + value.Value, (string?)item.Attribute("aria-label"));
            Assert.True((double)value.Attribute("x")! > (double)name.Attribute("x")! + 80);
        }
        Assert.NotEmpty(chart.ToPng());
    }

    private static bool IsLegend(XElement e) => (string?)e.Attribute("data-cfx-role") == "legend-entry";

    private static Chart Create(string kind) {
        var chart = Chart.Create().WithSize(480, 340);
        var points = new[] { new ChartPoint(0, 40) };
        return kind switch {
            "histogram" => chart.AddHistogram("Values", new[] { 12d, 14d, 18d }, 2),
            "gauge" => chart.AddGauge("Values", 75),
            "pie" => chart.AddPie("Values", points),
            "donut" => chart.AddDonut("Values", points),
            _ => chart.AddBar("Values", points)
        };
    }
}
