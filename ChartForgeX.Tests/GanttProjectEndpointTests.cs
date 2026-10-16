using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttProjectEndpointTests {
    private const string Plan = "gantt\ndateFormat YYYY-MM-DD\n{0}section Build\nDesign :a1, 2026-03-02, 4d\nImplement :a2, after a1, 8d\nsection Ship\nTest :a3, after a2, 5d\nRelease :milestone, m1, after a3, 0d";
    private static readonly DateTime Start = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Finish = Start.AddDays(17);

    [Theory]
    [InlineData(400, VisualThemeMode.Light)]
    [InlineData(900, VisualThemeMode.Light)]
    [InlineData(400, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Dark)]
    public void AutomaticAndRejectedFixedPlansShareCompleteProjectEndpointCaptions(int width, VisualThemeMode mode) {
        var automatic = Prepare(PlanChart(width, mode, string.Empty));
        var fallbackChart = PlanChart(width, mode, "tickInterval 1millisecond\n");
        var fallback = Prepare(fallbackChart);
        foreach (var (prepared, policy) in new[] { (automatic, "auto"), (fallback, "fallback") }) {
            var labels = Captions(prepared);
            Assert.Equal("2026-03-02", labels[0].Text.Lines.Single().Text);
            Assert.Equal("2026-03-19", labels[^1].Text.Lines.Single().Text);
            Assert.Equal(labels.Length, labels.Select(label => label.Text.Lines.Single().Text).Distinct(StringComparer.Ordinal).Count());
            var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").ToArray();
            Assert.Equal(labels.Length, grid.Length);
            for (var index = 0; index < labels.Length; index++) Assert.Equal(grid[index].Start.X, labels[index].X, 7);
            var firstTask = prepared.Regions.First(region => region.Role == "gantt-task");
            var release = Assert.Single(prepared.Regions, region => region.Role == "gantt-milestone");
            Assert.Equal(firstTask.Bounds.Left, labels[0].X, 7);
            Assert.Equal(release.Bounds.Left + release.Bounds.Width / 2, labels[^1].X, 7);
            var svg = XDocument.Parse(prepared.ToSvg());
            Assert.Equal(labels.Select(label => label.Text.Lines.Single().Text), svg.Descendants()
                .Where(node => (string?)node.Attribute("data-cfx-role") == "schedule-tick-label").Select(node => node.Value));
            Assert.All(prepared.Regions.Where(region => region.Role == "schedule-tick-label"), region => {
                Assert.True(region.Bounds.Width > 0);
                Assert.InRange(region.Bounds.Left, 0, width);
                Assert.InRange(region.Bounds.Right, 0, width);
            });
            Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "schedule.axis-label-overflow" or "text.overflow");
            ScatterBubbleColorRenderingTests.Capture(prepared, $"gantt-project-endpoints-{policy}-{width}-{mode}");
            var pixels = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
            Assert.Equal(width, pixels.Width); Assert.Equal(300, pixels.Height);
        }
        Assert.Equal(Captions(automatic).Select(label => (label.Text.Lines.Single().Text, label.X)),
            Captions(fallback).Select(label => (label.Text.Lines.Single().Text, label.X)));
        Assert.Equal(ChartTimeTickUnit.Millisecond, fallbackChart.Options.GanttTickInterval!.Unit);
        Assert.Equal(1, fallbackChart.Options.GanttTickInterval.Count);
    }

    [Fact]
    public void SuccessfulFixedIntervalsKeepTheirAuthoredGridAndDoNotInventOffIntervalEndpoints() {
        var chart = Chart.Create().WithSize(900, 300).WithHeader(false).WithLegend(false).WithPngFont(Font())
            .ConfigureGridStyle(style => style.ShowVerticalLines = true)
            .AddGanttTask("Window", Start.AddHours(6), Finish.AddHours(12))
            .WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Day));
        var prepared = Prepare(chart);
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").ToArray();
        Assert.Equal(17, grid.Length);
        var regions = prepared.Regions.Where(region => region.Role == "schedule-tick-label").ToArray();
        Assert.Equal(17, regions.Length);
        Assert.Equal("2026-03-03", regions[0].Label);
        Assert.Equal("2026-03-19", regions[^1].Label);
        var task = Assert.Single(prepared.Regions, region => region.Role == "gantt-task");
        Assert.True(grid[0].Start.X > task.Bounds.Left);
        Assert.True(grid[^1].Start.X < task.Bounds.Right);
        Assert.Equal(Start.AddHours(6).ToOADate(), chart.Series[0].GanttItems[0].Start);
        Assert.Equal(Finish.AddHours(12).ToOADate(), chart.Series[0].GanttItems[0].End);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(150, false)]
    [InlineData(400, false)]
    [InlineData(400, true)]
    public void AuthoredWindowsKeepHiddenProjectEndsOutsideTheGeneratedAxis(int width, bool fixedInterval) {
        var minimum = Start.AddDays(4); var maximum = Finish.AddDays(-4);
        var chart = Chart.Create().WithSize(width, 300).WithHeader(false).WithLegend(false).WithPadding(0, 0, 0, 0)
            .WithPngFont(Font()).ConfigureGridStyle(style => style.ShowVerticalLines = true)
            .AddGanttTask("Window", Start, Finish)
            .ConfigureXAxis(axis => {
                axis.WithBounds(minimum.ToOADate(), maximum.ToOADate());
                if (width > 150) axis.TickCount = 2;
            });
        if (fixedInterval) chart.WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Day));
        var prepared = Prepare(chart);
        var regions = prepared.Regions.Where(region => region.Role == "schedule-tick-label").ToArray();
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").ToArray();
        Assert.Equal(regions.Length, grid.Length);
        Assert.All(regions, region => {
            var date = DateTime.ParseExact(region.Label!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Assert.InRange(date, minimum, maximum);
            Assert.InRange(region.Bounds.Left, 0, width);
            Assert.InRange(region.Bounds.Right, 0, width);
        });
        var shown = Captions(prepared).Select(label => label.Text.Lines.Single().Text).ToArray();
        Assert.Equal("2026-03-06", shown[0]);
        if (fixedInterval) Assert.Equal(10, grid.Length);
        else {
            Assert.Equal(width == 150 ? 1 : 2, grid.Length);
            if (width > 150) Assert.Equal("2026-03-15", shown[^1]);
        }
        if (!fixedInterval) Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "schedule.axis-label-overflow");
        Assert.Equal(minimum.ToOADate(), chart.Options.XAxis.Minimum);
        Assert.Equal(maximum.ToOADate(), chart.Options.XAxis.Maximum);
        Assert.Equal(Start.ToOADate(), chart.Series[0].GanttItems[0].Start);
        Assert.Equal(Finish.ToOADate(), chart.Series[0].GanttItems[0].End);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"gantt-authored-window-{width}-{fixedInterval}");
        Assert.NotEmpty(prepared.ToPng());
    }

    [BrowserTheory]
    [InlineData(400, VisualThemeMode.Light)]
    [InlineData(900, VisualThemeMode.Light)]
    [InlineData(400, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Dark)]
    public async Task ProjectEndpointDatesStayOnTheirGridInStaticBrowserAndNativeOutput(int width, VisualThemeMode mode) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = PlanChart(width, mode, string.Empty);
        var prepared = Prepare(chart);
        await using var session = await InteractiveChartBrowser.OpenAsync(
            "<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", width, 340);
        var labels = await session.Page.Locator("[data-cfx-role='schedule-tick-label']").AllTextContentsAsync();
        Assert.Equal(Captions(prepared).Select(label => label.Text.Lines.Single().Text), labels);
        Assert.Equal("2026-03-02", labels[0]);
        Assert.Equal("2026-03-19", labels[^1]);
        Assert.Equal(labels.Count, await session.Page.Locator("[data-cfx-role='schedule-grid']").CountAsync());
        var positions = await session.Page.EvaluateAsync<double[][]>("""
            () => {
                const svg = document.querySelector('svg');
                const labels = [...svg.querySelectorAll('[data-cfx-role="schedule-tick-label"]')];
                const grid = [...svg.querySelectorAll('[data-cfx-role="schedule-grid"]')];
                const frame = svg.getBoundingClientRect();
                return labels.map((label, index) => {
                    const bounds = label.getBoundingClientRect();
                    return [bounds.left - frame.left, bounds.right - frame.left,
                        (bounds.left + bounds.right) / 2 - grid[index].getBoundingClientRect().left];
                });
            }
            """);
        Assert.Equal(labels.Count, positions.Length);
        Assert.All(positions, position => {
            Assert.InRange(position[0], -.5, width + .5);
            Assert.InRange(position[1], -.5, width + .5);
            Assert.InRange(Math.Abs(position[2]), 0, .5);
        });
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            var name = $"gantt-project-endpoints-{width}-{mode}";
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(directory, name + "-native.png"), prepared.ToPng());
        }
    }

    private static Chart PlanChart(int width, VisualThemeMode mode, string interval) => new MermaidParser()
        .ParseGantt(string.Format(CultureInfo.InvariantCulture, Plan, interval)).Document!.ToChart().WithSize(width, 300)
        .WithTheme(VisualTheme.Graphite(), mode).WithPngFont(Font()).ConfigureGridStyle(style => style.ShowVerticalLines = true);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role == "schedule-tick-label").ToArray();
    private static string Font() => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
}
