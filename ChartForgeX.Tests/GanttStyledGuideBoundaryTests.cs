using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using System.Xml.Linq;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttStyledGuideBoundaryTests {
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public void InvisibleCurrentTimeGuidesDoNotCompressTaskGeometry(bool lanes, int hiddenBy) {
        var start = new DateTime(2026, 1, 2);
        var chart = Chart.Create().WithSize(320, 360).WithLegend(false).WithGanttToday(start.AddDays(-1));
        if (lanes) chart.WithStateCategories(new ChartStateCategory("work", "Work", ChartColor.FromHex("#3291CC")))
            .AddGanttLane("Task", new[] { new ChartGanttLaneItem(start, start.AddDays(3), "work") });
        else chart.AddGanttTask("Task", start, start.AddDays(3));
        chart.Options.GanttTodayStyle = new ChartStrokeStyle(width: 0);
        var control = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        chart.Options.GanttTodayStyle = new ChartStrokeStyle(hiddenBy == 0 ? ChartColor.Transparent : null,
            hiddenBy == 2 ? 0 : 1024, hiddenBy == 1 ? 0 : 1);
        var actual = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(control.Regions.Select(region => region.Bounds), actual.Regions.Select(region => region.Bounds));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void InvisibleReferenceAnnotationsDoNotCompressTaskGeometry(int hiddenBy) {
        var chart = Chart.Create().WithSize(320, 360).WithLegend(false).AddGanttTask("Task", 1, 4).AddVerticalLine(0, "Reference");
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 0);
        var control = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(hiddenBy == 0 ? ChartColor.Transparent : null,
            hiddenBy == 2 ? 0 : 1024, hiddenBy == 1 ? 0 : 1);
        var actual = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(control.Regions.Select(region => region.Bounds), actual.Regions.Select(region => region.Bounds));
    }

    [Theory]
    [InlineData(1024.1)]
    [InlineData(double.MaxValue)]
    public void NativeReferenceWidthsRejectValuesOutsideTheBoundedRasterContract(double width) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartStrokeStyle(width: width));

    [Theory]
    [InlineData(1024.1)]
    [InlineData(double.MaxValue)]
    public void NativeReferenceDashesRejectValuesThatCouldOverflowRasterScaling(double length) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartStrokeStyle(dash: new[] { length, 1d }));

    [Fact]
    public void MaximumNativeReferenceStyleStillPaintsRasterInk() {
        var chart = Chart.Create().WithSize(320, 360).WithLegend(false).WithGanttToday(2).AddGanttTask("Task", 1, 4);
        chart.Options.GanttTodayStyle = new ChartStrokeStyle(ChartColor.FromHex("#FF00FF"), 1024, dash: new[] { 1024d, .25d });
        var pixels = chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToRgba().Pixels;
        Assert.Contains(Enumerable.Range(0, pixels.Length / 4), pixel => pixels[pixel * 4] == 255 && pixels[pixel * 4 + 1] == 0 && pixels[pixel * 4 + 2] == 255);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task WideCurrentTimeGuidesRemainInsideAutomaticClassicAndLaneWindows(bool lanes, bool after) {
        var start = new DateTime(2026, 1, 2);
        var chart = Chart.Create().WithSize(320, 360).WithLegend(false).WithDataLabels(false)
            .WithTheme(after ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithGanttToday(after ? start.AddDays(5) : start.AddDays(-1));
        if (lanes) chart.WithStateCategories(new ChartStateCategory("work", "Work", ChartColor.FromHex("#3291CC")))
            .AddGanttLane("Task", new[] { new ChartGanttLaneItem(start, start.AddDays(3), "work") });
        else chart.AddGanttTask("Task", start, start.AddDays(3));
        chart.Options.GanttTodayStyle = new ChartStrokeStyle(ChartColor.FromHex("#FF00FF"), 20, dash: new[] { 12d, 16d });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var guide = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "gantt-now-line");
        var axis = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "schedule-axis");
        var svgGuide = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "gantt-now-line");
        Assert.Equal("20", (string?)svgGuide.Attribute("stroke-width"));
        Assert.Equal("12 16", (string?)svgGuide.Attribute("stroke-dasharray"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "gantt-wide-guide-" + (lanes ? "lanes-" : "classic-") + (after ? "max" : "min");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
        }
        if (InteractiveChartBrowser.Enabled) {
            await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", 320, 400);
            var element = session.Page.Locator("[data-cfx-role='gantt-now-line']");
            Assert.Equal("round", await element.EvaluateAsync<string>("node => getComputedStyle(node).strokeLinecap"));
            Assert.Equal("12px, 16px", await element.EvaluateAsync<string>("node => getComputedStyle(node).strokeDasharray"));
            if (!string.IsNullOrWhiteSpace(captures)) await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
        Assert.True(guide.Start.X - guide.StrokeWidth / 2 >= axis.Start.X - .001, "Guide ink crosses the automatic minimum.");
        Assert.True(guide.Start.X + guide.StrokeWidth / 2 <= axis.End.X + .001, "Guide ink crosses the automatic maximum.");
        var actual = prepared.ToRgba();
        var control = VisualSceneRasterRenderer.Render(new VisualScene(prepared.Scene.Size,
            prepared.Scene.Nodes.Where(node => node.Role != "gantt-now-line").ToArray(), prepared.Scene.Diagnostics, prepared.Scene.Regions));
        for (var y = 0; y < actual.Height; y++) for (var x = 0; x < actual.Width; x++) {
            if (x + .5 >= axis.Start.X - 1 && x + .5 <= axis.End.X + 1 && y + .5 >= guide.Start.Y - 1 && y + .5 <= guide.End.Y + 1) continue;
            var offset = (y * actual.Width + x) * 4;
            Assert.Equal(control.Pixels.AsSpan(offset, 4).ToArray(), actual.Pixels.AsSpan(offset, 4).ToArray());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WideReferenceAnnotationsReserveTheirOwnWidthAtAutomaticWindowSides(bool after) {
        var chart = Chart.Create().WithLegend(false).AddGanttTask("Task", 1, 4).AddVerticalLine(after ? 5 : 0, "Reference");
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 20);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "annotation-line");
        var axis = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "schedule-axis");
        var svgLine = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "annotation-line");
        Assert.Equal("20", (string?)svgLine.Attribute("stroke-width"));
        Assert.InRange(line.Commands[0].X, axis.Start.X + line.StrokeWidth / 2, axis.End.X - line.StrokeWidth / 2);
    }
}
