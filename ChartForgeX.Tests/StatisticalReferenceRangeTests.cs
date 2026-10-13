using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StatisticalReferenceRangeTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ThickStatisticsFitTheResolvedValueAxisWithoutCompressingCategories(bool horizontal, bool reversed) {
        var chart = Chart.Create().WithSize(640, 360).WithPadding(0, 0, 0, 0)
            .WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithLineMarkers(ChartLineMarkerMode.None)
            .AddLine("Readings", new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30), new ChartPoint(4, 40) });
        chart.Options.LineOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis).Reversed = reversed;
        chart.AddMeanLine("", new[] { new ChartPoint(1, 40) }, ChartColor.FromHex("#FF00FF"));
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 40, dash: Array.Empty<double>());
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var guide = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "annotation-line");
        var points = prepared.Regions.Where(region => region.Role == "point").ToArray();
        var coordinates = points.Select(point => horizontal ? point.Bounds.Y : point.Bounds.X).ToArray();
        Assert.True(coordinates.Max() - coordinates.Min() > (horizontal ? 360 : 640) * .9);
        var coordinate = horizontal ? guide.Commands[0].X : guide.Commands[0].Y;
        Assert.InRange(coordinate, 20 - .001, (horizontal ? 640 : 360) - 20 + .001);
        var pixels = prepared.ToRgba();
        var painted = 0;
        for (var position = 0; position < (horizontal ? pixels.Width : pixels.Height); position++) {
            var offset = horizontal ? ((pixels.Height / 2) * pixels.Width + position) * 4
                : (position * pixels.Width + pixels.Width / 2) * 4;
            if (pixels.Pixels[offset] > 240 && pixels.Pixels[offset + 1] < 20 && pixels.Pixels[offset + 2] > 240) painted++;
        }
        Assert.InRange(painted, 39, 41);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>", 640, 360);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        Assert.Equal("40px", await session.Page.Locator("[data-cfx-role=annotation-line]").EvaluateAsync<string>("element => getComputedStyle(element).strokeWidth"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "statistical-guide-fit-" + (horizontal ? "horizontal" : "vertical") + (reversed ? "-reversed" : "");
        File.WriteAllText(Path.Combine(captures, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(captures, name + ".native.png"), prepared.ToPng());
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".browser.png"), FullPage = true });
    }
}
