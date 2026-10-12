using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttDependencyArrowTests {
    [Theory]
    [InlineData(false, false, 960, false)]
    [InlineData(true, false, 320, false)]
    [InlineData(false, true, 960, false)]
    [InlineData(true, true, 320, false)]
    [InlineData(false, false, 960, true)]
    [InlineData(true, false, 320, true)]
    [InlineData(false, true, 960, true)]
    [InlineData(true, true, 320, true)]
    public async Task ArrowheadInkRemainsVisibleOverSuccessorBarsAndMilestones(bool dark, bool milestone, int width, bool endpoint) {
        var chart = Create(dark, milestone, width, endpoint).AddGanttDependency(0, 1);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var arrow = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
        var tip = arrow.Commands[1];
        var actual = prepared.ToRgba();
        // Keep the automatic projection identical while omitting the dependency ink from the control scene.
        var scene = prepared.Scene;
        var controlScene = new VisualScene(scene.Size, scene.Nodes.Where(node => node.Role != "gantt-dependency-line" && node.Role != "gantt-dependency-arrow").ToArray(), scene.Diagnostics, scene.Regions);
        var projection = scene.Nodes.TakeWhile(node => !ReferenceEquals(node, arrow)).OfType<VisualSceneGroup>().Last(group => group.Clip.HasValue).Clip!.Value;
        Assert.All(arrow.Commands, command => Assert.InRange(command.X, projection.Left, projection.Right));
        var control = VisualSceneRasterRenderer.Render(controlScene);
        Assert.Equal((control.Width, control.Height), (actual.Width, actual.Height));

        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "gantt-arrows-" + (dark ? "dark-" : "light-") + width + (milestone ? "-milestone" : "-task") + (endpoint ? "-endpoint" : "");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
        }
        if (InteractiveChartBrowser.Enabled) {
            await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", width, 400);
            Assert.Equal(1, await session.Page.Locator("[data-cfx-role='gantt-dependency-arrow']").CountAsync());
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
            if (!string.IsNullOrWhiteSpace(captures)) await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
        }

        // The wings overlap a task's fill or sit beside a milestone's boundary. Compare with the same scene without links,
        // excluding the central connector line so an invisible arrow cannot pass on line ink alone.
        var changed = 0;
        for (var y = (int)Math.Floor(tip.Y - 5); y <= Math.Ceiling(tip.Y + 5); y++) {
            if (Math.Abs(y + .5 - tip.Y) < 1.2) continue;
            for (var x = (int)Math.Ceiling(tip.X + 1); x <= Math.Floor(tip.X + 5); x++) {
                var offset = (y * actual.Width + x) * 4;
                if (Enumerable.Range(0, 3).Any(channel => Math.Abs(actual.Pixels[offset + channel] - control.Pixels[offset + channel]) > 30)) changed++;
            }
        }
        Assert.True(changed >= 4, "Dependency arrow wings must paint visible ink over the successor fill; changed pixels: " + changed);
    }

    private static Chart Create(bool dark, bool milestone, int width, bool endpoint) {
        var chart = Chart.Create().WithSize(width, 360).WithLegend(false).WithDataLabels(false)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .AddGanttTask("First", 1, 4);
        if (milestone) chart.AddGanttMilestone("Gate", endpoint ? 9 : 4);
        else chart.AddGanttTask("Next", endpoint ? 9 : 4, endpoint ? 9 : 8);
        return chart.AddGanttTask("Window", 1, 9);
    }
}
