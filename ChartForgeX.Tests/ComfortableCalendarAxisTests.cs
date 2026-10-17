using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ComfortableCalendarAxisTests {
    [Theory]
    [InlineData("state-day", VisualThemeMode.Light)]
    [InlineData("state-day", VisualThemeMode.Dark)]
    [InlineData("state-ten-hours", VisualThemeMode.Light)]
    [InlineData("state-ten-hours", VisualThemeMode.Dark)]
    [InlineData("eighteen-days", VisualThemeMode.Light)]
    [InlineData("eighteen-days", VisualThemeMode.Dark)]
    [InlineData("threshold-month", VisualThemeMode.Light)]
    [InlineData("threshold-month", VisualThemeMode.Dark)]
    [InlineData("date-bars", VisualThemeMode.Light)]
    [InlineData("date-bars", VisualThemeMode.Dark)]
    public void AutomaticCalendarLabelsKeepMeasuredBreathingRoomAndTheirOwnTicks(string scenario, VisualThemeMode mode) {
        var chart = Fixture(scenario).WithThemeMode(mode);
        var prepared = Prepare(chart);
        ScatterBubbleColorRenderingTests.Capture(prepared, "comfortable-calendar-" + scenario + "-" + mode);
        var schedule = scenario.StartsWith("state-", StringComparison.Ordinal);
        var labels = Labels(prepared, schedule ? "schedule-tick-label" : "axis-x-label");
        var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == (schedule ? "schedule-grid" : "grid-x")).ToArray();
        Assert.InRange(labels.Length, 2, 8);
        Assert.Equal(labels.Length, grid.Length);
        for (var index = 0; index < labels.Length; index++) {
            var left = Left(labels[index]); var width = labels[index].Text.Metrics.Width;
            Assert.InRange(Math.Abs(left + width / 2 - grid[index].Start.X), 0, .5);
            if (index > 0) {
                var previous = labels[index - 1];
                var gap = left - Left(previous) - previous.Text.Metrics.Width;
                Assert.True(gap + .01 >= Math.Max(24, Math.Max(width, previous.Text.Metrics.Width)),
                    $"Complete calendar captions need a measured gap; actual gap {gap:F2}px for {scenario}.");
            }
            if (!schedule) Assert.Matches("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", labels[index].Text.Lines.Single().Text);
        }
        if (scenario == "state-ten-hours") Assert.InRange(labels.Length, 5, 6);
        if (scenario == "state-day") Assert.InRange(labels.Length, 4, 6);
        if (scenario == "eighteen-days") Assert.InRange(labels.Length, 5, 7);
        if (scenario == "threshold-month") Assert.InRange(labels.Length, 4, 6);
        if (scenario == "date-bars") {
            var bars = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar").ToArray();
            foreach (var label in labels) Assert.Contains(bars, bar => Math.Abs(bar.Bounds.Left + bar.Bounds.Width / 2 - Left(label) - label.Text.Metrics.Width / 2) <= .5);
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "schedule.axis-label-overflow" or "cartesian.axis-label-overflow");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData("pl-PL")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    public void DefaultShortDateFallbackIsNumericAndHostCultureNeutral(string culture) {
        var previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var start = new DateTime(2026, 9, 14);
            var chart = Base(110).ConfigureYAxis(axis => axis.Visible = false).WithPadding(0, 0, 0, 0)
                .AddLine("Readings", new[] { new ChartPoint(start, 10), new ChartPoint(start.AddDays(14), 20) });
            var prepared = Prepare(chart);
            ScatterBubbleColorRenderingTests.Capture(prepared, "numeric-short-date-" + culture);
            var labels = Labels(prepared, "axis-x-label").Select(node => node.Text.Lines.Single().Text).ToArray();
            Assert.True(labels.Length >= 2);
            Assert.All(labels, label => Assert.Matches("^[0-9]{2}-[0-9]{2}$", label));
            Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.axis-label-overflow");
            Assert.NotEmpty(prepared.ToPng());
        } finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void CrossingAYearRetainsTheYearAndAuthoredLocalizedFormatsStayAuthoritative() {
        var start = new DateTime(2026, 12, 20);
        var chart = Base(720).AddLine("Readings", Enumerable.Range(0, 30).Select(day => new ChartPoint(start.AddDays(day), day)));
        var automatic = Prepare(chart);
        Assert.All(Labels(automatic, "axis-x-label"), label => Assert.Matches("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", label.Text.Lines.Single().Text));
        var polish = CultureInfo.GetCultureInfo("pl-PL");
        chart.ConfigureXAxis(axis => axis.WithLabelFormatter(value => DateTime.FromOADate(value).ToString("d MMM yyyy", polish)));
        Assert.Contains(Labels(Prepare(chart), "axis-x-label"), label => label.Text.Lines.Single().Text.Contains("sty", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void AnAuthoredFormatterDoesNotOptIntoDenserAutomaticCalendarTicks(VisualThemeMode mode) {
        var chart = Fixture("threshold-month").WithThemeMode(mode);
        var automatic = Labels(Prepare(chart), "axis-x-label");
        chart.ConfigureXAxis(axis => axis.WithLabelFormatter(value => DateTime.FromOADate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        var formatted = Labels(Prepare(chart), "axis-x-label");
        Assert.Equal(automatic.Select(node => node.Text.Lines.Single().Text), formatted.Select(node => node.Text.Lines.Single().Text));
        Assert.Equal(automatic.Select(node => node.X), formatted.Select(node => node.X));
    }

    [BrowserTheory]
    [InlineData("state-ten-hours", VisualThemeMode.Light)]
    [InlineData("state-ten-hours", VisualThemeMode.Dark)]
    [InlineData("state-day", VisualThemeMode.Light)]
    [InlineData("state-day", VisualThemeMode.Dark)]
    [InlineData("eighteen-days", VisualThemeMode.Light)]
    [InlineData("eighteen-days", VisualThemeMode.Dark)]
    [InlineData("threshold-month", VisualThemeMode.Light)]
    [InlineData("threshold-month", VisualThemeMode.Dark)]
    [InlineData("date-bars", VisualThemeMode.Light)]
    [InlineData("date-bars", VisualThemeMode.Dark)]
    public async Task StaticBrowserCalendarCaptionsRetainTheNativeComfortableSpacing(string scenario, VisualThemeMode mode) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Fixture(scenario).WithThemeMode(mode);
        var role = scenario.StartsWith("state-", StringComparison.Ordinal) ? "schedule-tick-label" : "axis-x-label";
        var prepared = Prepare(chart);
        var native = Labels(prepared, role);
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + chart.ToHtmlFragment() + "</body></html>",
            (int)chart.Options.Size.Width, (int)chart.Options.Size.Height);
        var selector = "[data-cfx-role='" + role + "']";
        Assert.Equal(native.Select(node => node.Text.Lines.Single().Text), await session.Page.Locator(selector).AllTextContentsAsync());
        var bounds = await session.Page.EvaluateAsync<double[][]>("selector => [...document.querySelectorAll(selector)].map(node => { const r=node.getBoundingClientRect(); return [r.left,r.right,r.width]; })", selector);
        for (var index = 1; index < bounds.Length; index++) {
            var gap = bounds[index][0] - bounds[index - 1][1];
            // Native and browser font implementations may round fractional metrics differently.
            Assert.True(gap + 1 >= Math.Max(24, Math.Max(bounds[index][2], bounds[index - 1][2])), "Rendered date captions retain a complete-caption gap.");
        }
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            var name = "comfortable-calendar-" + scenario + "-" + mode;
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), prepared.ToPng());
        }
    }

    internal static Chart Fixture(string scenario) {
        var start = new DateTime(2026, 10, 13);
        if (scenario.StartsWith("state-", StringComparison.Ordinal)) {
            var hours = scenario == "state-ten-hours" ? 10 : 24;
            var chart = Base(hours == 10 ? 800 : 720).WithSize(hours == 10 ? 800 : 720, 320);
            var first = start.AddHours(hours == 10 ? 8 : 0);
            chart.AddStateTimelineLane("Primary", new[] { new ChartStateTimelineSegment(first, first.AddHours(hours), "up") });
            return chart;
        }
        if (scenario == "eighteen-days") return Base(800).AddLine("Score", Enumerable.Range(0, 19)
            .Select(day => new ChartPoint(new DateTime(2026, 9, 1).AddDays(day), 50 + day % 5 * 4)));
        if (scenario == "threshold-month") return Base(1440).AddLine("Score", Enumerable.Range(0, 30)
            .Select(day => new ChartPoint(start.AddDays(day - 30), 70 + Math.Sin(day))))
            .AddHorizontalLine(80, "Target 80");
        return Base(700).AddBar("Checks", Enumerable.Range(0, 15)
            .Select(day => new ChartPoint(new DateTime(2026, 9, 14).AddDays(day * 2), 40 + day)));
    }

    private static Chart Base(int width) => Chart.Create().WithSize(width, 320).WithHeader(false).WithLegend(false).WithXAxisTimeScale()
        .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
        .ConfigureGridStyle(style => style.ShowVerticalLines = true);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Labels(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(node => node.Role == role).OrderBy(Left).ToArray();
    private static double Left(VisualSceneText node) => node.Text.Lines.Min(node.LineLeft);
}
