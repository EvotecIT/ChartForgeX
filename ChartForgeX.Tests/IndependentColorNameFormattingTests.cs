using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class IndependentColorNameFormattingTests {
    [Theory]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Bubble)]
    [InlineData(ChartSeriesKind.Treemap)]
    [InlineData(ChartSeriesKind.Sunburst)]
    public void RenamingAnIndependentColorMeasurementPreservesItsUnformattedReadout(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(640, 440).WithDataLabels(false)
            .WithValueFormat(ChartValueFormat.Number("0.0 'items'"));
        var items = new[] { new ChartHierarchyItem("measured", "Measured", value: 9, colorValue: 7.125) };
        if (kind == ChartSeriesKind.Scatter) chart.AddScatter("Samples", new[] { new ChartPoint(1, 9).WithColorValue(7.125) });
        else if (kind == ChartSeriesKind.Bubble) chart.AddBubble("Samples", new[] { new ChartBubble(1, 9, 4).WithColorValue(7.125) });
        else if (kind == ChartSeriesKind.Treemap) chart.AddTreemap("Samples", items);
        else chart.AddSunburst("Samples", items);
        var options = kind == ChartSeriesKind.Treemap ? chart.Options.Treemap.ColorDimension
            : kind == ChartSeriesKind.Sunburst ? chart.Options.Sunburst.ColorDimension : chart.Series[0].ColorDimension;
        var before = Readout(chart);
        Assert.Equal("7.125", (string?)before.Attribute("data-cfx-color-value"));
        Assert.Null(before.Attribute("data-cfx-formatted-color-value"));
        options.Name = "Temperature";
        var after = Readout(chart);
        Assert.Equal("7.125", (string?)after.Attribute("data-cfx-color-value"));
        Assert.Null(after.Attribute("data-cfx-formatted-color-value"));
        options.ValueFormat = ChartValueFormat.Number("0.00 'degrees'");
        Assert.Equal("7.13 degrees", (string?)Readout(chart).Attribute("data-cfx-formatted-color-value"));
    }

    private static XElement Readout(Chart chart) => XDocument.Parse(chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg())
        .Descendants().First(element => (string?)element.Attribute("data-cfx-color-value") == "7.125");
}
