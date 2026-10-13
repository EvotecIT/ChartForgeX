using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HorizontalPhysicalAxisClosureTests {
    [Theory]
    [InlineData(false, 20)]
    [InlineData(false, 1000)]
    [InlineData(true, 20)]
    [InlineData(true, 1000)]
    public void PartialValueCaptionsRetainGeneratedTicksFormattingAndDetachedSemantics(bool step, double position) {
        var chart = Model(step ? ChartSeriesKind.StepLine : ChartSeriesKind.Line).WithXAxisBounds(0, 40);
        chart.Options.XAxis.TickCount = 3;
        chart.ConfigureGridStyle(style => style.ShowVerticalLines = true);
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(position, "Target"));
        var calls = new Dictionary<double, int>();
        chart.Options.XAxis.WithLabelFormatter(value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return value.ToString("0", CultureInfo.InvariantCulture) + " ms";
        });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Regions.Where(region => region.Role == "axis-x-label").Select(region => region.Label).ToArray();
        Assert.Contains(labels, label => label!.StartsWith("0 ms")); Assert.Contains(labels, label => label!.StartsWith("40 ms"));
        Assert.Equal(position == 20, labels.Any(label => label!.StartsWith("Target")));
        Assert.DoesNotContain(position, calls.Keys);
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var grids = XDocument.Parse(svg).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "grid-x");
        Assert.Equal(ChartTicks.ForValueAxis(chart.Options.XAxis, 0, 40).Count, grids);
        chart.Options.XAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Changed formatter"));
        chart.Options.XAxis.Labels.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.StepLine)]
    [InlineData(ChartSeriesKind.HorizontalBar)]
    [InlineData(ChartSeriesKind.Waterfall)]
    public async Task RetainedStatisticsUsePhysicalXAfterObservationsBecomeEmpty(ChartSeriesKind kind) {
        var chart = Model(kind);
        chart.AddStandardDeviationBand("Deviation", chart.Series[0].Points, color: HorizontalRasterAssertions.Band, opacity: 1)
            .AddMeanLine("Mean", chart.Series[0].Points, HorizontalRasterAssertions.Guide);
        chart.Series[0].Points.Clear();
        var range = ChartRange.FromChart(chart);
        Assert.True(range.MinX < 25 && range.MaxX > 25);
        Assert.Equal(0, range.MinY); Assert.Equal(1, range.MaxY);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var guides = prepared.Regions.Where(region => region.Role == "annotation").ToArray();
        Assert.Equal(2, guides.Length);
        Assert.All(guides, guide => Assert.True(guide.Bounds.Height > 200));
        var line = Assert.Single(guides, guide => guide.Bounds.Width == 0);
        var band = Assert.Single(guides, guide => guide.Bounds.Width > 0);
        Assert.True(band.Bounds.Left < line.Bounds.Left && band.Bounds.Right > line.Bounds.Right);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Contains("data-cfx-kind=\"VerticalLine\"", svg);
        Assert.Contains("data-cfx-kind=\"VerticalBand\"", svg);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            File.WriteAllText(Path.Combine(capture, "empty-" + kind + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(capture, "empty-" + kind + ".native.png"), png);
        }
        HorizontalRasterAssertions.Statistics(prepared, line.Bounds, band.Bounds);
        if (kind != ChartSeriesKind.Line || !InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 700, 450);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "empty-series-statistics.svg"), svg);
        File.WriteAllBytes(Path.Combine(directory, "empty-series-statistics.native.png"), png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "empty-series-statistics.browser.png"), FullPage = true });
    }

    private static Chart Model(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(700, 450).WithHeader(false).WithLegend(false).WithLineMarkers(ChartLineMarkerMode.None);
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30), new ChartPoint(4, 40) };
        if (kind == ChartSeriesKind.HorizontalBar) chart.AddHorizontalBar("Readings", points);
        else if (kind == ChartSeriesKind.Waterfall) chart.AddWaterfall("Readings", points).ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal);
        else if (kind == ChartSeriesKind.StepLine) chart.AddStepLine("Readings", points);
        else chart.AddLine("Readings", points);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        return chart;
    }
}
