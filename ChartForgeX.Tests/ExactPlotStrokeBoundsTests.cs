using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ExactPlotStrokeBoundsTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AReferenceAsWideAsThePlotCanStillFitAtItsCenter(bool gantt) {
        var chart = Chart.Create().WithSize(640, 320).WithPadding(0, 0, 0, 0)
            .WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false);
        if (gantt) chart.AddGanttTask("Task", 0, 1);
        else chart.AddLine("Values", new[] { new ChartPoint(0, 0), new ChartPoint(1, 1) });
        chart.AddVerticalLine(0, color: ChartColor.FromHex("#FF00FF"));
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 640);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var reference = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "annotation-line");
        Assert.InRange(reference.Commands[0].X, 319.999, 320.001);
        Assert.Equal(640, reference.StrokeWidth);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>", 640, 320);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "plot-width-reference-" + (gantt ? "gantt" : "cartesian");
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
    }
}
