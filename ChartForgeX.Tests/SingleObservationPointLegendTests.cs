using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SingleObservationPointLegendTests {
    [Theory]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.Bubble)]
    [InlineData(ChartSeriesKind.BoxPlot)]
    [InlineData(ChartSeriesKind.Candlestick)]
    public void ExplicitPointLegendDescribesOneWholeAuthoredObservation(ChartSeriesKind kind) {
        var id = new ChartSeriesId("readings");
        var series = kind switch {
            ChartSeriesKind.Bubble => ChartSeries.CreateBubble(id, "Reading", new[] { new ChartBubble(1, 5, 8) }),
            ChartSeriesKind.BoxPlot => ChartSeries.CreateBoxPlot(id, "Reading", new[] { new ChartBoxPlot(1, 0, 2, 5, 8, 10) }),
            ChartSeriesKind.Candlestick => ChartSeries.CreateCandlestick(id, "Reading", new[] { new ChartCandlestick(1, 3, 10, 0, 5) }),
            _ => ChartSeries.CreateBar(id, "Reading", new[] { new ChartPoint(1, 5) })
        };
        Assert.Equal(1, series.ObservationCount);
        Assert.Equal(1, series.SourcePointCount);
        series.WithObservationId(0, new ChartObservationId("north"));
        var chart = Chart.Create().WithSize(640, 400).WithXLabels("North").WithPointLegend().WithLegend().AddSeries(series);
        var prepared = chart.Prepare(new VisualRenderContext());
        var nodes = XDocument.Parse(prepared.ToSvg()).Descendants().ToArray();
        var legend = Assert.Single(nodes, node => (string?)node.Attribute("data-cfx-role") == "legend-entry");
        Assert.Equal("legend-series-0-point-0", (string?)legend.Attribute("data-cfx-source-id"));
        Assert.Equal("North", Assert.Single(legend.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "legend-label").Value);
        var point = Assert.Single(nodes, node => (string?)node.Attribute("data-cfx-role") == "point");
        if (kind == ChartSeriesKind.Bar) {
            Assert.Equal("1", (string?)point.Attribute("data-cfx-x"));
            Assert.Equal("5", (string?)point.Attribute("data-cfx-y"));
        } else Assert.Equal("1", (string?)point.Attribute("data-cfx-source-count"));
        Assert.Equal("0", (string?)point.Attribute("data-cfx-source-point"));
        Assert.Equal("north", (string?)point.Attribute("data-cfx-observation-id"));
        if (kind == ChartSeriesKind.Bubble) Assert.Equal("8", (string?)point.Attribute("data-cfx-size"));
        if (kind == ChartSeriesKind.BoxPlot) Assert.Equal("5", (string?)point.Attribute("data-cfx-median"));
        if (kind == ChartSeriesKind.Candlestick) Assert.Equal("5", (string?)point.Attribute("data-cfx-close"));
    }
}
