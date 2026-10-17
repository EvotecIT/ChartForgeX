using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartLinkedNumericSnapshotTests {
    [Theory]
    [InlineData("bar", 1, "10,25 ms #1")]
    [InlineData("horizontalbar", 1, "10,25 ms #1")]
    [InlineData("scatter", 1, "10,25 ms #1")]
    [InlineData("histogram", 1, "2,00 ms #1")]
    [InlineData("boxplot", 1, "3,25 ms #1")]
    [InlineData("rangebar", 2, "10,25 ms #1")]
    [InlineData("rangebarendpoints", 2, "10,25 ms #1")]
    [InlineData("dumbbell", 2, "10,25 ms #1")]
    [InlineData("rangearea", 2, "10,25 ms #1")]
    [InlineData("waterfall", 1, "10,25 ms #1")]
    public void ReadoutsReuseNativeNumericSnapshotsWithoutFormattingHiddenAxes(string family, int expectedCalls, string expectedValue) {
        var chart = CreateChart(family);
        var calls = 0;
        chart.WithValueFormatter(value => value.ToString("0.00", CultureInfo.GetCultureInfo("pl-PL")) + " ms #" + ++calls);
        var prepared = chart.Prepare(new VisualRenderContext());
        var svg = prepared.ToSvg();
        var point = Mark(svg);
        Assert.Equal(expectedCalls, calls);
        Assert.Equal(expectedValue, point.Attribute("data-cfx-formatted-value")!.Value);
        if (family is "rangebar" or "dumbbell") {
            Assert.Equal("10,25 ms #1", point.Attribute("data-cfx-formatted-start")!.Value);
            Assert.Equal("24,75 ms #2", point.Attribute("data-cfx-formatted-end")!.Value);
            Assert.Equal("10,25 ms #1 – 24,75 ms #2", point.Attribute("data-cfx-range-label")!.Value);
            Assert.Equal("10.25", point.Attribute("data-cfx-start")!.Value);
            Assert.Equal("24.75", point.Attribute("data-cfx-end")!.Value);
        }
        chart.WithValueFormatter(_ => throw new InvalidOperationException("A prepared snapshot must stay detached."));
        Assert.Equal(svg, prepared.ToSvg());
        Assert.NotEmpty(prepared.ToPng());
        Assert.Equal(expectedCalls, calls);
    }

    [Theory]
    [InlineData("bar")]
    [InlineData("horizontalbar")]
    [InlineData("scatter")]
    [InlineData("histogram")]
    [InlineData("boxplot")]
    [InlineData("rangebar")]
    [InlineData("dumbbell")]
    [InlineData("rangearea")]
    [InlineData("waterfall")]
    public void CustomObservationCaptionsKeepUnneededNumericFormattingLazyDuringReadoutCapture(string family) {
        var chart = CreateChart(family);
        const string caption = "Operator supplied complete caption";
        chart.Series[0].WithPointLabel(0, caption);
        chart.WithValueFormatter(_ => throw new InvalidOperationException("The custom caption bypasses numeric formatting."));
        var prepared = chart.Prepare(new VisualRenderContext());
        var point = Mark(prepared.ToSvg());
        Assert.Equal(caption, point.Attribute("data-cfx-label")!.Value);
        Assert.Equal(family == "boxplot" ? "3.25" : family == "histogram" ? "2" : "10.25",
            point.Attribute("data-cfx-formatted-value")!.Value);
        if (family is "rangebar" or "dumbbell") Assert.Equal("10.25 – 24.75", point.Attribute("data-cfx-range-label")!.Value);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData("rangebar", "start", "end")]
    [InlineData("dumbbell", "start", "end")]
    [InlineData("rangearea", "lower", "upper")]
    public void EqualTupleMembersRetainTheirOwnCapturedFormatterResults(string family, string firstKey, string secondKey) {
        var chart = Chart.Create().WithAxes(false).WithLegend(false).WithHeader(false).WithDataLabels(false);
        if (family == "rangebar") chart.AddRangeBar("Interval", new[] { new ChartInterval(1, 10.25, 10.25) });
        else if (family == "dumbbell") chart.AddDumbbell("Comparison", new[] { new ChartDumbbell(1, 10.25, 10.25) });
        else chart.AddRangeArea("Envelope", new[] { new ChartRangeBand(1, 10.25, 10.25) });
        var calls = 0;
        chart.WithValueFormatter(value => value.ToString("0.00", CultureInfo.GetCultureInfo("pl-PL")) + " ms #" + ++calls);
        var prepared = chart.Prepare(new VisualRenderContext());
        var svg = prepared.ToSvg();
        var point = Mark(svg);
        Assert.Equal(2, calls);
        Assert.Equal("10,25 ms #1–10,25 ms #2", point.Attribute("data-cfx-label")!.Value);
        Assert.Equal("10,25 ms #1", point.Attribute("data-cfx-formatted-" + firstKey)!.Value);
        Assert.Equal("10,25 ms #2", point.Attribute("data-cfx-formatted-" + secondKey)!.Value);
        Assert.Equal("10,25 ms #1", point.Attribute("data-cfx-formatted-value")!.Value);
        Assert.Equal(svg, prepared.ToSvg());
        Assert.NotEmpty(prepared.ToPng());
        Assert.Equal(2, calls);
    }

    private static Chart CreateChart(string family) {
        var chart = Chart.Create().WithAxes(false).WithLegend(false).WithHeader(false).WithDataLabels(false);
        chart.Options.ShowGrid = false;
        return family switch {
            "bar" => chart.AddBar("Requests", new[] { new ChartPoint(1, 10.25) }),
            "horizontalbar" => chart.AddHorizontalBar("Requests", new[] { new ChartPoint(1, 10.25) }),
            "scatter" => chart.AddScatter("Samples", new[] { new ChartPoint(1, 10.25) }),
            "histogram" => chart.AddSeries(ChartSeries.CreateHistogram(new ChartSeriesId("duration"), "Duration", new[] { 1d, 1.5 },
                ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 2 }))),
            "boxplot" => chart.AddSeries(ChartSeries.CreateBoxPlot(new ChartSeriesId("latency"), "Latency", new[] { new ChartBoxPlot(1, 1, 2, 3.25, 4, 5) })),
            "rangebar" => chart.AddRangeBar("Interval", new[] { new ChartInterval(1, 10.25, 24.75) }),
            "rangebarendpoints" => chart.AddRangeBar("Interval", new[] { new ChartInterval(1, 10.25, 24.75) })
                .ConfigureRangeBar(options => options.LabelContent = ChartRangeBarLabelContent.Endpoints),
            "dumbbell" => chart.AddDumbbell("Comparison", new[] { new ChartDumbbell(1, 10.25, 24.75) }),
            "rangearea" => chart.AddRangeArea("Envelope", new[] { new ChartRangeBand(1, 10.25, 24.75) }),
            "waterfall" => chart.AddWaterfall("Net change", new[] { ChartWaterfallItem.OpeningBalance(1, 10.25) }),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
    }

    private static XElement Mark(string svg) => XDocument.Parse(svg).Descendants().Single(node => node.Attribute("data-cfx-point") != null);
}
