using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttDependencyArrowTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, false, 960, false)]
    [InlineData(true, false, 320, false)]
    [InlineData(false, true, 960, false)]
    [InlineData(true, true, 320, false)]
    [InlineData(false, false, 960, true)]
    [InlineData(true, false, 320, true)]
    [InlineData(false, true, 960, true)]
    [InlineData(true, true, 320, true)]
    public async Task FinishToStartArrowWingsRemainVisibleBesideSuccessorBarsAndMilestones(bool dark, bool milestone, int width, bool endpoint) {
        var chart = Create(dark, milestone, width, endpoint);
        chart.AddGanttDependency(chart.Series[0], chart.Series[1]);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var arrow = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
        var tip = arrow.Commands[1];
        var actual = prepared.ToRgba();
        // Keep the projection and connector identical; only the arrow wings are absent from the control.
        var scene = prepared.Scene;
        var controlScene = new VisualScene(scene.Size, scene.Nodes.Where(node => node.Role != "gantt-dependency-arrow").ToArray(), scene.Diagnostics, scene.Regions);
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

        // Finish-to-start arrows approach the successor from the left, with a visible gap to its painted surface.
        // Compare their actual wing footprint, excluding the middle band where the unchanged connector lies.
        var changed = 0;
        var radius = arrow.StrokeWidth / 2;
        var left = Math.Max(0, (int)Math.Floor(arrow.Commands.Min(command => command.X) - radius));
        var right = Math.Min(actual.Width - 1, (int)Math.Ceiling(arrow.Commands.Max(command => command.X) + radius));
        var top = Math.Max(0, (int)Math.Floor(arrow.Commands.Min(command => command.Y) - radius));
        var bottom = Math.Min(actual.Height - 1, (int)Math.Ceiling(arrow.Commands.Max(command => command.Y) + radius));
        for (var y = top; y <= bottom; y++) {
            if (Math.Abs(y + .5 - tip.Y) < 1.2) continue;
            for (var x = left; x <= right; x++) {
                var offset = (y * actual.Width + x) * 4;
                if (Enumerable.Range(0, 3).Any(channel => Math.Abs(actual.Pixels[offset + channel] - control.Pixels[offset + channel]) > 30)) changed++;
            }
        }
        Assert.True(changed >= 4, "Dependency arrow wings must paint visible ink beside the successor; changed pixels: " + changed);
    }

    private static Chart Create(bool dark, bool milestone, int width, bool endpoint) {
        var chart = Chart.Create().WithSize(width, 360).WithLegend(false).WithDataLabels(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddGanttTask("First", 1, 4);
        if (milestone) chart.AddGanttMilestone("Gate", endpoint ? 9 : 4);
        else chart.AddGanttTask("Next", endpoint ? 9 : 4, endpoint ? 9 : 8);
        return chart.AddGanttTask("Window", 1, 9);
    }
}
