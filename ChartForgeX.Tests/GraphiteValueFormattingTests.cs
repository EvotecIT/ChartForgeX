using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteValueFormattingTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AggregatedOtherShareChoosesPrecisionForLabelsLegendAndFormatterContext(bool donut) {
        var points = new[] { new ChartPoint(1, 99) }.Concat(Enumerable.Range(2, 25).Select(index => new ChartPoint(index, .04))).ToArray();
        var contexts = new List<ChartPieSliceLabelContext>();
        var chart = Chart.Create().WithSize(800, 440).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Outside).WithLegend(false)
            .WithPieSliceLabelFormatter(context => { contexts.Add(context); return context.FormattedPercent; });
        if (donut) chart.AddDonut("Share", points); else chart.AddPie("Share", points);
        var svg = XDocument.Parse(chart.ToSvg());
        var other = Assert.Single(contexts, context => context.PointIndex == -1);
        Assert.Equal("0.8%", other.FormattedPercent);
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-source-id") == "series-0-point-other-label-source" && node.Value == "0.8%");
        var legend = VisualRadialCompiler.LegendEntries(chart, chart.Options.Theme.Resolve(chart.Options.ThemeMode));
        Assert.Equal(new[] { "99.0%", "0.0%", "0.0%", "0.0%", "0.0%", "0.8%" }, legend.Select(entry => entry.Percentage));
    }

    [Fact]
    public void RadialSharesUseOnePrecisionAndPreserveAuthoredPercentageCulture() {
        var chart = Chart.Create().WithSize(800, 440).WithDataLabels().WithLegend(false)
            .AddDonut("Share", new[] { 48d, 41, 34, 33, 27, 26 }.Select((value, index) => new ChartPoint(index + 1, value)));
        string[] Captions() => XDocument.Parse(chart.ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "data-label").Select(node => node.Value).ToArray();
        Assert.Equal(new[] { "23.0%", "19.6%", "16.3%", "15.8%", "12.9%", "12.4%" }, Captions());
        chart.WithValueFormat(ChartValueFormat.Number("0.00%", CultureInfo.GetCultureInfo("de-DE")));
        Assert.Equal(new[] { "22,97%", "19,62%", "16,27%", "15,79%", "12,92%", "12,44%" }, Captions());
    }

    [Fact]
    public void FunnelRatiosUseOnePrecisionAcrossStages() {
        var chart = Chart.Create().WithSize(800, 440).WithDataLabels().WithLegend(false)
            .ConfigureFunnel(options => options.LabelContent = ChartStageLabelContent.Separate)
            .AddFunnel("Requests", new[] { 1000d, 410, 162.36 }.Select((value, index) => new ChartPoint(index + 1, value)));
        var captions = XDocument.Parse(chart.ToSvg()).Descendants().Where(node =>
            (string?)node.Attribute("data-cfx-role") == "funnel-dropoff-label").Select(node => node.Value).ToArray();
        Assert.Equal(new[] { "59.0% drop-off", "60.4% drop-off" }, captions);
    }

    [Fact]
    public void FiniteFunnelStagesRetainOverflowingRatioDescriptions() {
        var chart = Chart.Create().WithDataLabels().AddFunnel("Finite stages", new[] { new ChartPoint(1, 1e-308), new ChartPoint(2, 1e308) });
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "funnel-stage"
            && node.Attribute("aria-label")!.Value.Contains("retained Infinity", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("'Profit '0")]
    [InlineData("0 '%' ")]
    [InlineData("0\\%")]
    public void RawValueUnitFormatsDoNotReplaceDerivedPercentageUnits(string format) {
        var chart = Chart.Create().WithDataLabels().WithLegend(false).WithValueFormat(ChartValueFormat.Number(format))
            .AddDonut("Share", new[] { new ChartPoint(1, 48), new ChartPoint(2, 161) });
        Assert.Equal(new[] { "23%", "77%" }, XDocument.Parse(chart.ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "data-label").Select(node => node.Value).OrderBy(value => value, StringComparer.Ordinal));
    }
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
        // This fixture checks formatting rather than the optional outside-label placement at an axis edge.
        if (value < 0) chart.WithDataLabelPlacement(ChartDataLabelPlacement.Center);
        Assert.Equal(expected, Role(chart, "data-label").Value);
        if (value <= 0) return;
        var donut = Chart.Create().AddDonut("Total", new[] { new ChartPoint(1, value) });
        Assert.Equal(expected, Role(donut, "donut-total-label").Value);
        var funnel = Chart.Create().WithDataLabels().WithXLabels("Detected").AddFunnel("Stages", new[] { new ChartPoint(1, value) });
        Assert.Equal("Detected: " + expected, Role(funnel, "funnel-label").Value);
        Assert.Contains(expected, (string?)Role(funnel, "funnel-stage").Attribute("aria-label"));
    }

    [Theory]
    [InlineData("de-DE", "1.284")]
    [InlineData("fr-FR", "1\u202f284")]
    public void ChartValueFormatterRetainsCultureAndUnits(string culture, string expected) {
        string Format(double value) => value.ToString("#,0.##", CultureInfo.GetCultureInfo(culture)) + " items";
        var points = new[] { new ChartPoint(1, 1284) };
        var bar = Chart.Create().WithValueFormatter(Format).WithDataLabels().WithLegend(false).AddBar("Count", points);
        var donut = Chart.Create().WithValueFormatter(Format).WithSize(900, 500).AddDonut("Total", points);
        var funnel = Chart.Create().WithDataLabels().WithValueFormatter(Format).WithSize(900, 500).WithXLabels("Detected").AddFunnel("Stages", points);
        Assert.Equal(expected + " items", Role(bar, "data-label").Value);
        Assert.Equal(expected + " items", Role(donut, "donut-total-label").Value);
        Assert.Equal("Detected: " + expected + " items", Role(funnel, "funnel-label").Value);
    }

    [Fact]
    public void AxisTicksKeepTheirExistingCompactFormatting() {
        var chart = Chart.Create().WithDataLabels().AddBar("Count", new[] { new ChartPoint(1, 1284) });
        chart.Options.YAxis.Minimum = 0;
        chart.Options.YAxis.Maximum = 2000;
        chart.Options.YAxis.TickCount = 3;
        var ticks = XDocument.Parse(chart.ToSvg()).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "axis-y-label");
        Assert.Equal(new[] { "0", "1k", "2k" }, ticks.Select(e => e.Value));
        Assert.Equal("1,284", Role(chart, "data-label").Value);
    }

    private static XElement Role(Chart chart, string role) =>
        Assert.Single(XDocument.Parse(chart.ToSvg()).Descendants(), e => (string?)e.Attribute("data-cfx-role") == role);
}
