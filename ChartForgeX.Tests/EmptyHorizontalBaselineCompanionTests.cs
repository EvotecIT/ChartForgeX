using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class EmptyHorizontalBaselineCompanionTests {
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EmptyBaselineCompanionDoesNotLowerALogarithmicLineDomain(int physicalAxis) {
        var chart = Chart.Create().AddLine("Readings", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) });
        if (physicalAxis == 0) {
            chart.Options.LineOrientation = ChartOrientation.Horizontal;
            chart.Options.XAxis.Scale = ChartScaleKind.Logarithmic;
        } else if (physicalAxis == 1) chart.Options.YAxis.Scale = ChartScaleKind.Logarithmic;
        else {
            chart.Series[0].UseSecondaryYAxis();
            chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
        }
        ChartRange Domain() {
            var primary = ChartRange.FromChart(chart);
            return physicalAxis == 2 ? ChartRange.FromSecondaryYAxis(chart, primary) : primary;
        }
        var expected = Domain();
        if (physicalAxis == 0) chart.AddHorizontalBar("Empty", Array.Empty<ChartPoint>());
        else {
            chart.AddBar("Empty", Array.Empty<ChartPoint>());
            if (physicalAxis == 2) chart.Series[1].UseSecondaryYAxis();
        }
        var actual = Domain();
        Assert.Equal(expected.MinX, actual.MinX); Assert.Equal(expected.MaxX, actual.MaxX);
        Assert.Equal(expected.MinY, actual.MinY); Assert.Equal(expected.MaxY, actual.MaxY);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyBaselineCompanionDoesNotChangePopulatedHorizontalLineDomain(bool negative) {
        var sign = negative ? -1 : 1;
        var chart = Chart.Create().WithHeader(false).WithLegend(false).WithDataLabels(false)
            .AddLine("Readings", new[] { new ChartPoint(1, sign * 20), new ChartPoint(2, sign * 40) });
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var expected = ChartRange.FromChart(chart);
        var before = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        chart.AddHorizontalBar("Empty", Array.Empty<ChartPoint>());
        var actual = ChartRange.FromChart(chart);
        Assert.Equal(expected.MinX, actual.MinX); Assert.Equal(expected.MaxX, actual.MaxX);
        Assert.Equal(expected.MinY, actual.MinY); Assert.Equal(expected.MaxY, actual.MaxY);
        var after = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var beforePlot = Assert.Single(before.Regions, region => region.Role == "cartesian-plot").Bounds;
        var afterPlot = Assert.Single(after.Regions, region => region.Role == "cartesian-plot").Bounds;
        var beforePoints = before.Regions.Where(region => region.Role == "point").OrderBy(region => region.Id).ToArray();
        var afterPoints = after.Regions.Where(region => region.Role == "point").OrderBy(region => region.Id).ToArray();
        Assert.Equal(2, afterPoints.Length);
        for (var index = 0; index < afterPoints.Length; index++) {
            var first = beforePoints[index].Bounds; var second = afterPoints[index].Bounds;
            var expectedPosition = (first.Left + first.Width / 2 - beforePlot.Left) / beforePlot.Width;
            var actualPosition = (second.Left + second.Width / 2 - afterPlot.Left) / afterPlot.Width;
            Assert.True(Math.Abs(expectedPosition - actualPosition) * afterPlot.Width <= 1,
                "An empty companion must preserve the line's physical value placement within one raster pixel.");
        }
        var png = after.ToPng();
        if (!InteractiveChartBrowser.Enabled) return;
        var svg = after.ToSvg();
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 800, 600);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var stem = Path.Combine(directory, $"empty-baseline-{negative}");
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + "-native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-browser.png", FullPage = true });
        }
    }
}
