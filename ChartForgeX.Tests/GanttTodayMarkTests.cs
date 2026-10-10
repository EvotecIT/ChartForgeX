using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttTodayMarkTests {
    [Theory]
    [InlineData(false, false, 960)]
    [InlineData(true, false, 320)]
    [InlineData(false, true, 960)]
    [InlineData(true, true, 320)]
    public async Task TodayLinePaintsVisibleInkAcrossClassicAndPackedTaskFills(bool dark, bool lanes, int width) {
        var start = new DateTime(2026, 1, 1);
        var chart = Chart.Create().WithSize(width, 360).WithLegend(false).WithDataLabels(false)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithGanttToday(start.AddDays(2));
        if (lanes) chart.WithStateCategories(new ChartStateCategory("work", "Work", ChartColor.FromHex("#3291CC")))
            .AddGanttLane("Task", new[] { new ChartGanttLaneItem(start, start.AddDays(4), "work") });
        else chart.AddGanttTask("Task", start, start.AddDays(4));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var scene = prepared.Scene;
        var marker = Assert.Single(scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "gantt-now-line");
        var task = Assert.Single(prepared.Regions, region => region.Role == (lanes ? "gantt-lane-item" : "gantt-task"));
        var actual = prepared.ToRgba();
        var control = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size,
            scene.Nodes.Where(node => node.Role != "gantt-now-line").ToArray(), scene.Diagnostics, scene.Regions));
        var changed = 0;
        for (var y = (int)Math.Ceiling(task.Bounds.Top + 3); y < task.Bounds.Bottom - 3; y++) {
            for (var x = (int)Math.Floor(marker.Start.X - 1); x <= Math.Ceiling(marker.Start.X + 1); x++) {
                var offset = (y * actual.Width + x) * 4;
                if (Enumerable.Range(0, 3).Any(channel => Math.Abs(actual.Pixels[offset + channel] - control.Pixels[offset + channel]) > 30)) changed++;
            }
        }
        Assert.True(changed >= 4, "The today line must remain visible within the task fill; changed pixels: " + changed);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "gantt-today-" + (dark ? "dark-" : "light-") + width + (lanes ? "-lanes" : "-classic");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
        }
        if (InteractiveChartBrowser.Enabled) {
            await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", width, 400);
            Assert.Equal(1, await session.Page.Locator("[data-cfx-role='gantt-now-line']").CountAsync());
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
            if (!string.IsNullOrWhiteSpace(captures)) await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
        }
    }
}
