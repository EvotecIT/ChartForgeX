using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteValueFormattingTests {
    [Theory]
    [InlineData(0, "0")]
    [InlineData(12.25, "12.25")]
    [InlineData(1012, "1,012")]
    [InlineData(1284, "1,284")]
    [InlineData(9999, "9,999")]
    [InlineData(10000, "10k")]
    [InlineData(12400, "12.4k")]
    [InlineData(3400000, "3.4M")]
    [InlineData(-1284, "-1,284")]
    [InlineData(-12400, "-12.4k")]
    public void ValuesUseGroupedNumbersBelowTenThousand(double value, string expected) {
        var chart = Chart.Create().WithDataLabels().WithLegend(false)
            .AddBar("Count", new[] { new ChartPoint(1, value) });
        Assert.Equal(expected, Role(chart, "data-label").Value);
        if (value <= 0) return;
        var donut = Chart.Create().AddDonut("Total", new[] { new ChartPoint(1, value) });
        Assert.Equal(expected, Role(donut, "donut-total-label").Value);
        var funnel = Chart.Create().WithXLabels("Detected").AddFunnel("Stages", new[] { new ChartPoint(1, value) });
        Assert.Equal(expected + " · 100%", Role(funnel, "funnel-value").Value);
    }

    [Theory]
    [InlineData("de-DE", "1.284")]
    [InlineData("fr-FR", "1\u202f284")]
    public void ChartValueFormatterRetainsCultureAndUnits(string culture, string expected) {
        string Format(double value) => value.ToString("#,0.##", CultureInfo.GetCultureInfo(culture)) + " items";
        var points = new[] { new ChartPoint(1, 1284) };
        var bar = Chart.Create().WithValueFormatter(Format).WithDataLabels().WithLegend(false).AddBar("Count", points);
        var donut = Chart.Create().WithValueFormatter(Format).WithSize(900, 500).AddDonut("Total", points);
        var funnel = Chart.Create().WithValueFormatter(Format).WithSize(900, 500).WithXLabels("Detected").AddFunnel("Stages", points);
        Assert.Equal(expected + " items", Role(bar, "data-label").Value);
        Assert.Equal(expected + " items", Role(donut, "donut-total-label").Value);
        Assert.Equal(expected + " items · 100%", Role(funnel, "funnel-value").Value);
    }

    [Fact]
    public void AxisTicksKeepTheirExistingCompactFormatting() {
        var chart = Chart.Create().WithDataLabels().AddBar("Count", new[] { new ChartPoint(1, 1284) });
        chart.Options.YAxis.Minimum = 0;
        chart.Options.YAxis.Maximum = 2000;
        chart.Options.YAxis.TickCount = 3;
        var ticks = XDocument.Parse(chart.ToSvg()).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "y-axis-label");
        Assert.Equal(new[] { "0", "1k", "2k" }, ticks.Select(e => e.Value));
        Assert.Equal("1,284", Role(chart, "data-label").Value);
    }

    private static XElement Role(Chart chart, string role) =>
        Assert.Single(XDocument.Parse(chart.ToSvg()).Descendants(), e => (string?)e.Attribute("data-cfx-role") == role);
}
