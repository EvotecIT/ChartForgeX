using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ExactPlotStrokeBoundsTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task AuthoredAxisEndStillAllowsFullWidthGuidesToFitTheAutomaticEnd(bool gantt, bool maximum) {
        var chart = Chart.Create().WithSize(640, 320).WithPadding(0, 0, 0, 0)
            .WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false);
        if (gantt) chart.AddGanttTask("Task", 0, 1);
        else chart.AddLine("Values", new[] { new ChartPoint(0, 0), new ChartPoint(1, 1) });
        if (maximum) chart.Options.XAxis.Maximum = 1;
        else chart.Options.XAxis.Minimum = 0;
        chart.AddVerticalLine(0).AddVerticalLine(1);
        foreach (var annotation in chart.Annotations)
            annotation.StrokeStyle = new ChartStrokeStyle(ChartColor.FromHex("#FF00FF"), 640, .15);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var references = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "annotation-line").ToArray();
        Assert.Equal(2, references.Length);
        Assert.InRange(references[maximum ? 0 : 1].Commands[0].X, 319.999, 320.001);
        Assert.InRange(references[maximum ? 1 : 0].Commands[0].X, maximum ? 639.999 : -.001, maximum ? 640.001 : .001);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>", 640, 320);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "plot-width-authored-" + (maximum ? "maximum-" : "minimum-") + (gantt ? "gantt" : "cartesian");
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConflictingPlotWidthReferencesPreserveDataAndSmallerGuideGeometry(bool gantt) {
        var chart = Chart.Create().WithSize(640, 320).WithPadding(0, 0, 0, 0)
            .WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false);
        if (gantt) chart.AddGanttTask("Task", 0, 1);
        else chart.AddLine("Values", new[] { new ChartPoint(0, 0), new ChartPoint(1, 1) });
        chart.AddVerticalLine(0).AddVerticalLine(1).AddVerticalLine(.5);
        chart.Annotations[0].StrokeStyle = chart.Annotations[1].StrokeStyle = new ChartStrokeStyle(width: 0);
        chart.Annotations[2].StrokeStyle = new ChartStrokeStyle(width: 2);
        var control = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        chart.Annotations[0].StrokeStyle = chart.Annotations[1].StrokeStyle = new ChartStrokeStyle(ChartColor.FromHex("#FF00FF"), 640, .15);
        var actual = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.NotEmpty(control.Regions);
        Assert.Equal(control.Regions.Select(region => region.Bounds), actual.Regions.Select(region => region.Bounds));
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + actual.ToSvg() + "</body></html>", 640, 320);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "conflicting-plot-width-references-" + (gantt ? "gantt" : "cartesian");
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), actual.ToPng());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task AReferenceAsWideAsThePlotCanStillFitAtItsCenter(bool gantt, bool coincident) {
        var chart = Chart.Create().WithSize(640, 320).WithPadding(0, 0, 0, 0)
            .WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false);
        if (gantt) chart.AddGanttTask("Task", 0, 1);
        else chart.AddLine("Values", new[] { new ChartPoint(0, 0), new ChartPoint(1, 1) });
        chart.AddVerticalLine(0, color: ChartColor.FromHex("#FF00FF"));
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 640);
        if (coincident) {
            chart.AddVerticalLine(0, color: ChartColor.FromHex("#FF00FF"));
            chart.Annotations[1].StrokeStyle = new ChartStrokeStyle(width: 640);
        }
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var references = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "annotation-line").ToArray();
        Assert.Equal(coincident ? 2 : 1, references.Length);
        Assert.All(references, reference => {
            Assert.InRange(reference.Commands[0].X, 319.999, 320.001);
            Assert.Equal(640, reference.StrokeWidth);
        });
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>", 640, 320);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "plot-width-reference-" + (coincident ? "coincident-" : "") + (gantt ? "gantt" : "cartesian");
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
    }
}
