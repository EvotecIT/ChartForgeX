using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttDependencyWindowTests {
    [Theory]
    [InlineData(1, 2, 3, 4, false, 0)]
    [InlineData(21, 22, 23, 24, false, 0)]
    [InlineData(1, 1, 4, 4, true, 0)]
    [InlineData(21, 21, 24, 24, true, 0)]
    [InlineData(1, 2, 10, 12, false, 1)]
    [InlineData(18, 20, 23, 24, false, 1)]
    [InlineData(1, 2, 23, 24, false, 1)]
    [InlineData(9, 10, 20, 21, false, 1)]
    public void ExplicitWindowOmitsOnlyLinksWhollyOutsideTheSameSide(double firstStart, double firstEnd,
        double nextStart, double nextEnd, bool milestones, int expected) {
        var chart = Create(firstStart, firstEnd, nextStart, nextEnd, milestones, 640);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(expected, prepared.Regions.Count(region => region.Role == "gantt-dependency"));
        Assert.Equal(expected, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(path => path.Role == "gantt-dependency-line"));
        Assert.Equal(expected, XDocument.Parse(prepared.ToSvg()).Descendants().Count(element =>
            (string?)element.Attribute("data-cfx-role") == "gantt-dependency-arrow"));
        Assert.NotEmpty(prepared.ToPng());
        Assert.Equal(firstStart, chart.Series[0].GanttItems[0].Start);
        Assert.Equal(nextEnd, chart.Series[1].GanttItems[0].End);
        Assert.Single(chart.GanttDependencies);
    }

    [BrowserTheory]
    [InlineData(false, 320)]
    [InlineData(true, 960)]
    public async Task ClippedTasksDoNotLeavePhantomDependencyInkInStaticBrowserOutput(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Create(1, 2, 3, 4, false, width)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.AddGanttTask("Visible", 12, 18).AddGanttDependency(1, 2);
        await using var session = await InteractiveChartBrowser.OpenAsync(
            "<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", width, 400);
        Assert.Equal(1, await session.Page.Locator("[data-cfx-role='gantt-dependency-arrow']").CountAsync());
        Assert.Equal(0, await session.Page.Locator("#gantt-dependency-0-1").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = "gantt-window-" + (dark ? "dark-" : "light-") + width;
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), chart.ToPng());
        }
    }

    private static Chart Create(double firstStart, double firstEnd, double nextStart, double nextEnd, bool milestones, int width) {
        var chart = Chart.Create().WithSize(width, 360).WithLegend(false).WithDataLabels(false)
            .ConfigureXAxis(axis => { axis.Minimum = 10; axis.Maximum = 20; });
        if (milestones) chart.AddGanttMilestone("First", firstStart).AddGanttMilestone("Next", nextStart);
        else chart.AddGanttTask("First", firstStart, firstEnd).AddGanttTask("Next", nextStart, nextEnd);
        return chart.AddGanttDependency(0, 1);
    }
}
