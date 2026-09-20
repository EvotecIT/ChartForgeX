using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartDiscontinuityTests {
    [Theory]
    [InlineData(ChartSeriesKind.Line, false)]
    [InlineData(ChartSeriesKind.Line, true)]
    [InlineData(ChartSeriesKind.StepLine, false)]
    [InlineData(ChartSeriesKind.Area, false)]
    [InlineData(ChartSeriesKind.Area, true)]
    [InlineData(ChartSeriesKind.StepArea, false)]
    public void IsolatedSegmentsRemainVisibleWithoutMarkers(ChartSeriesKind kind, bool smooth) {
        var chart = Chart.Create().WithSize(600, 300).WithPadding(20, 20, 20, 20)
            .WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false);
        chart.Series.Add(new ChartSeries("Observed", kind, new[] { new ChartPoint(0, 5), new ChartPoint(5, 5, true), new ChartPoint(10, 5, true) }) { Color = ChartColor.FromRgb(255, 0, 0) });
        chart.Series[0].WithSmooth(smooth).WithMarkerRadius(0);
        var svg = XDocument.Parse(chart.ToSvg());
        var role = kind == ChartSeriesKind.Line ? "line" : kind == ChartSeriesKind.StepLine ? "step-line" : kind == ChartSeriesKind.Area ? "area-line" : "step-area-line";
        var paths = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
        Assert.NotEmpty(paths);
        Assert.All(paths, element => {
            Assert.Equal(3, ((string)element.Attribute("d")!).Count(character => character == 'L'));
            Assert.Equal("round", (string?)element.Attribute("stroke-linecap"));
        });
        var image = PngReader.Decode(chart.ToPng());
        var red = 0;
        for (var y = 0; y < image.Height; y++) {
            for (var x = 290; x <= 310; x++) {
                var offset = (y * image.Width + x) * 4;
                if (image.Pixels[offset] > image.Pixels[offset + 1] + 10 && image.Pixels[offset] > image.Pixels[offset + 2] + 10) red++;
            }
        }
        Assert.True(red > 0, "An isolated observation must paint a stroke cap even when markers are disabled.");
    }

    [Theory]
    [InlineData(ChartSeriesKind.Line, false)]
    [InlineData(ChartSeriesKind.Line, true)]
    [InlineData(ChartSeriesKind.StepLine, false)]
    [InlineData(ChartSeriesKind.Area, false)]
    [InlineData(ChartSeriesKind.Area, true)]
    [InlineData(ChartSeriesKind.StepArea, false)]
    public void SvgAndPngLeaveDisconnectedIntervalsEmpty(ChartSeriesKind kind, bool smooth) {
        Chart Create(bool disconnected) {
            var chart = Chart.Create().WithSize(600, 300).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false);
            chart.Series.Add(new ChartSeries("Observed", kind, new[] { new ChartPoint(0, 5), new ChartPoint(1, 7), new ChartPoint(2, 5), new ChartPoint(8, 5, disconnected), new ChartPoint(9, 7), new ChartPoint(10, 5) }) { Color = ChartColor.FromRgb(255, 0, 0) });
            chart.Series[0].WithSmooth(smooth).WithMarkerRadius(0);
            return chart;
        }
        var chart = Create(true);
        var svg = XDocument.Parse(chart.ToSvg());
        var role = kind == ChartSeriesKind.Line ? "line" : kind == ChartSeriesKind.StepLine ? "step-line" : kind == ChartSeriesKind.Area ? "area-line" : "step-area-line";
        var paths = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
        Assert.NotEmpty(paths);
        Assert.All(paths, element => Assert.Equal(2, ((string)element.Attribute("d")!).Count(character => character == 'M')));
        if (kind == ChartSeriesKind.Area || kind == ChartSeriesKind.StepArea)
            Assert.Equal(2, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == (kind == ChartSeriesKind.Area ? "area" : "step-area")));
        int RedPixelsAtCenter(byte[] png) {
            var image = PngReader.Decode(png);
            var count = 0;
            for (var y = 0; y < image.Height; y++) {
                var offset = (y * image.Width + image.Width / 2) * 4;
                if (image.Pixels[offset] > image.Pixels[offset + 1] + 10 && image.Pixels[offset] > image.Pixels[offset + 2] + 10) count++;
            }
            return count;
        }
        Assert.Equal(0, RedPixelsAtCenter(chart.ToPng()));
        Assert.True(RedPixelsAtCenter(Create(false).ToPng()) > 0);
    }

    [Theory]
    [InlineData(ChartDecimationMode.LargestTriangleThreeBuckets)]
    [InlineData(ChartDecimationMode.MinMax)]
    public void ReductionPreservesSegmentsSourceIndicesAndEndpoints(ChartDecimationMode mode) {
        var points = Enumerable.Range(0, 1000).Select(index => new ChartPoint(index, index % 100 == 17 ? -100 : index % 100 == 53 ? 100 : 0, index % 100 == 0)).ToArray();
        var result = ChartDecimator.Decimate(points, 80, mode);
        Assert.InRange(result.Points.Count, 1, 80);
        Assert.Equal(10, result.Points.Count(point => point.BreakBefore));
        for (var segment = 0; segment < 10; segment++) {
            Assert.Contains(segment * 100, result.SourceIndices);
            Assert.Contains(segment * 100 + 99, result.SourceIndices);
            if (mode == ChartDecimationMode.MinMax) {
                Assert.Contains(segment * 100 + 17, result.SourceIndices);
                Assert.Contains(segment * 100 + 53, result.SourceIndices);
            }
        }
        for (var index = 0; index < result.Points.Count; index++) Assert.Equal(points[result.SourceIndices[index]], result.Points[index]);
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartDecimator.Decimate(points, 10, mode));
    }

    [Fact]
    public void IsolatedSamplesSurviveReductionAndUnsupportedSeriesRejectBreaks() {
        var points = new[] { new ChartPoint(0, 1), new ChartPoint(1, 2, true) }.Concat(Enumerable.Range(2, 100).Select(index => new ChartPoint(index, index, index == 2))).ToArray();
        var result = ChartDecimator.Decimate(points, 6, ChartDecimationMode.MinMax);
        Assert.Contains(0, result.SourceIndices);
        Assert.Contains(1, result.SourceIndices);
        Assert.Contains(2, result.SourceIndices);
        Assert.Contains(101, result.SourceIndices);
        var chart = Chart.Create();
        chart.Series.Add(new ChartSeries("Bars", ChartSeriesKind.Bar, points));
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        Assert.Throws<InvalidOperationException>(() => chart.ToPng());
    }
    [Fact]
    public void UnevenSegmentBudgetsRemainBoundedAndOrdered() {
        var points = new List<ChartPoint>();
        foreach (var length in new[] { 1, 2, 3, 4, 9, 21, 100 })
            for (var index = 0; index < length; index++) points.Add(new ChartPoint(points.Count, Math.Sin(index), index == 0));
        foreach (var mode in Enum.GetValues<ChartDecimationMode>()) {
            var minimum = new[] { 1, 2, 3, 4, 9, 21, 100 }.Sum(length => Math.Min(length, mode == ChartDecimationMode.MinMax ? 4 : 3));
            for (var budget = minimum; budget <= points.Count; budget++) {
                var result = ChartDecimator.Decimate(points, budget, mode);
                Assert.True(result.Points.Count <= budget);
                Assert.Equal(7, result.Points.Count(point => point.BreakBefore));
                Assert.Equal(result.SourceIndices.OrderBy(index => index).Distinct(), result.SourceIndices);
            }
        }
    }

}
