using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CalendarCaptionAcceptanceTests {
    private static readonly DateTime Start = new(2026, 3, 2, 6, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Finish = new(2026, 3, 19, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("auto", VisualThemeMode.Light)]
    [InlineData("auto", VisualThemeMode.Dark)]
    [InlineData("rejected", VisualThemeMode.Light)]
    [InlineData("rejected", VisualThemeMode.Dark)]
    [InlineData("count", VisualThemeMode.Light)]
    [InlineData("count", VisualThemeMode.Dark)]
    [InlineData("window", VisualThemeMode.Light)]
    [InlineData("window", VisualThemeMode.Dark)]
    public void AnAutomaticGanttMonthCaptionIsAcceptedOnceWhenItCannotDistinguishTheProjectEnds(string policy, VisualThemeMode mode) {
        var chart = Fixture(ChartSeriesKind.Gantt).WithThemeMode(mode);
        if (policy == "rejected") chart.WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Millisecond));
        if (policy == "count") chart.ConfigureXAxis(axis => axis.TickCount = 2);
        if (policy == "window") chart.ConfigureXAxis(axis => axis.WithBounds(Start.AddDays(4).ToOADate(), Finish.AddDays(-4).ToOADate()));
        var prepared = Prepare(chart);
        var caption = Assert.Single(Captions(prepared));
        Assert.Equal("2026-03", caption.Text.Lines.Single().Text);
        var grid = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "schedule-grid");
        Assert.Equal(grid.Start.X, caption.X, 7);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "schedule.axis-label-overflow" or "text.overflow");
        Assert.Equal(Start.ToOADate(), chart.Series[0].GanttItems[0].Start);
        Assert.Equal(Finish.ToOADate(), chart.Series[0].GanttItems[0].End);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"accepted-month-gantt-{policy}-{mode}");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Timeline)]
    [InlineData(ChartSeriesKind.StateTimeline)]
    [InlineData(ChartSeriesKind.GanttLane)]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Bar)]
    public void SharedCalendarFallbackNeverPaintsASequenceRejectedByTheAuthoredCaptionPrecision(ChartSeriesKind kind) {
        var prepared = Prepare(Fixture(kind));
        var caption = Assert.Single(Captions(prepared));
        Assert.Equal("2026-03", caption.Text.Lines.Single().Text);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "schedule.axis-label-overflow" or "cartesian.axis-label-overflow");
        ScatterBubbleColorRenderingTests.Capture(prepared, "accepted-month-sibling-" + kind);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData("MM")]
    [InlineData("MMM")]
    public void EqualPeriodicEndpointCaptionsRetainMeaningfulDifferentCalendarCaptionsBetweenThem(string format) {
        var start = new DateTime(2026, 1, 1);
        var chart = Base().WithSize(960, 300).AddGanttTask("Year", start, start.AddYears(1))
            .ConfigureXAxis(axis => axis.WithLabelFormatter(value => DateTime.FromOADate(value).ToString(format, CultureInfo.InvariantCulture)));
        var prepared = Prepare(chart);
        var labels = Captions(prepared).Select(node => node.Text.Lines.Single().Text).ToArray();
        Assert.InRange(labels.Length, 3, 8);
        Assert.Equal(start.ToString(format, CultureInfo.InvariantCulture), labels[0]);
        Assert.Equal(labels[0], labels[^1]);
        Assert.True(labels.Distinct(StringComparer.Ordinal).Count() > 1);
        for (var index = 1; index < labels.Length; index++) Assert.NotEqual(labels[index - 1], labels[index]);
        Assert.Equal(labels.Length, prepared.Scene.Nodes.OfType<VisualSceneLine>().Count(node => node.Role == "schedule-grid"));
        ScatterBubbleColorRenderingTests.Capture(prepared, "accepted-periodic-year-" + format);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredFixedIntervalsAndAllDensityKeepIntentionalRepeatedMonthCaptions(bool fixedInterval) {
        var chart = Fixture(ChartSeriesKind.Gantt).WithSize(1200, 300);
        if (fixedInterval) chart.WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Day, 2));
        else chart.ConfigureXAxis(axis => axis.LabelDensity = ChartLabelDensity.All);
        var prepared = Prepare(chart);
        var labels = Captions(prepared);
        Assert.True(labels.Length > 1);
        Assert.All(labels, label => Assert.Equal("2026-03", label.Text.Lines.Single().Text));
        Assert.Equal(labels.Length, prepared.Regions.Count(region => region.Role == "schedule-tick-label" && region.Bounds.Width > 0));
        Assert.NotEmpty(prepared.ToPng());
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Gantt, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Gantt, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Timeline, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Timeline, VisualThemeMode.Dark)]
    public async Task AcceptedMonthFallbackMatchesNativeSvgAndStaticBrowserPaint(ChartSeriesKind kind, VisualThemeMode mode) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Fixture(kind).WithThemeMode(mode);
        var prepared = Prepare(chart);
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>", 400, 300);
        Assert.Equal(new[] { "2026-03" }, await session.Page.Locator("[data-cfx-role='schedule-tick-label']").AllTextContentsAsync());
        Assert.Single(Captions(prepared));
        Assert.Equal(1, await session.Page.Locator("[data-cfx-role='schedule-grid']").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            var name = $"accepted-month-browser-{kind}-{mode}";
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(directory, name + "-native.png"), prepared.ToPng());
        }
    }

    private static Chart Fixture(ChartSeriesKind kind) {
        var chart = Base().ConfigureXAxis(axis => axis.WithLabelFormatter(value => DateTime.FromOADate(value).ToString("yyyy-MM", CultureInfo.InvariantCulture)));
        switch (kind) {
            case ChartSeriesKind.Gantt: chart.AddGanttTask("Project", Start, Finish); break;
            case ChartSeriesKind.Timeline: chart.AddTimelineItem("Project", Start, Finish); break;
            case ChartSeriesKind.StateTimeline: chart.AddStateTimelineLane("Project", new[] { new ChartStateTimelineSegment(Start, Finish, "up") }); break;
            case ChartSeriesKind.GanttLane: chart.AddGanttLane("Project", new[] { new ChartGanttLaneItem(Start, Finish, "up") }); break;
            case ChartSeriesKind.Line: chart.AddLine("Project", new[] { new ChartPoint(Start, 10), new ChartPoint(Finish, 20) }); break;
            case ChartSeriesKind.Bar: chart.AddBar("Project", Enumerable.Range(0, 18).Select(day => new ChartPoint(Start.AddDays(day), day + 10))); break;
        }
        return chart;
    }
    private static Chart Base() => Chart.Create().WithSize(400, 300).WithHeader(false).WithLegend(false).WithXAxisTimeScale()
        .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
        .ConfigureGridStyle(style => style.ShowVerticalLines = true);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(node => node.Role is "schedule-tick-label" or "axis-x-label").ToArray();
}
