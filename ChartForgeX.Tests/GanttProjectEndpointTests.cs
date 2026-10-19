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
    public void AutomaticAndRejectedFixedPlansShareRegularProjectDayCaptions(int width, VisualThemeMode mode) {
        var automatic = Prepare(PlanChart(width, mode, string.Empty));
        var fallbackChart = PlanChart(width, mode, "tickInterval 1millisecond\n");
        var fallback = Prepare(fallbackChart);
        foreach (var (prepared, policy) in new[] { (automatic, "auto"), (fallback, "fallback") }) {
            var labels = Captions(prepared);
            Assert.Equal("2026-03-02", labels[0].Text.Lines.Single().Text);
            Assert.InRange(labels.Length, width == 400 ? 3 : 5, 8);
            var dates = labels.Select(label => DateTime.ParseExact(label.Text.Lines.Single().Text, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray();
            for (var index = 2; index < dates.Length; index++) Assert.Equal(dates[1] - dates[0], dates[index] - dates[index - 1]);
            Assert.InRange(dates[^1], Start, Finish);
            Assert.Equal(labels.Length, labels.Select(label => label.Text.Lines.Single().Text).Distinct(StringComparer.Ordinal).Count());
            var grid = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid").ToArray();
            Assert.Equal(labels.Length, grid.Length);
            for (var index = 0; index < labels.Length; index++) Assert.Equal(grid[index].Start.X, labels[index].X, 7);
            var firstTask = prepared.Regions.First(region => region.Role == "gantt-task");
            var release = Assert.Single(prepared.Regions, region => region.Role == "gantt-milestone");
            Assert.Equal(firstTask.Bounds.Left, labels[0].X, 7);
            Assert.True(release.Bounds.Left + release.Bounds.Width / 2 >= labels[^1].X);
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
        if (width == 400) {
            var daily = Prepare(PlanChart(width, mode, "tickInterval 1day\n"));
            Assert.Equal(Captions(automatic).Select(label => (label.Text.Lines.Single().Text, label.X)),
                Captions(daily).Select(label => (label.Text.Lines.Single().Text, label.X)));
        }
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
        Assert.Equal(18, grid.Length);
        var regions = prepared.Regions.Where(region => region.Role == "schedule-tick-label").ToArray();
        Assert.Equal(18, regions.Length);
        Assert.Equal("2026-03-02", regions[0].Label);
        Assert.Equal("2026-03-19", regions[^1].Label);
        var task = Assert.Single(prepared.Regions, region => region.Role == "gantt-task");
        Assert.True(grid[0].Start.X < task.Bounds.Left);
        Assert.True(grid[^1].Start.X < task.Bounds.Right);
        Assert.Equal(Start.AddHours(6).ToOADate(), chart.Series[0].GanttItems[0].Start);
        Assert.Equal(Finish.AddHours(12).ToOADate(), chart.Series[0].GanttItems[0].End);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredDateFormattersRunOncePerValueAcrossPrecisionLayoutAndTaskSummaries(bool valueFormat) {
        var seen = new HashSet<double>();
        string Format(double value) {
            if (!seen.Add(value)) throw new InvalidOperationException("Formatted twice");
            return DateTime.FromOADate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        var chart = Chart.Create().WithSize(900, 300).WithHeader(false).WithLegend(false).WithPngFont(Font())
            .AddGanttTask("Project", Start.AddHours(10), Finish.AddHours(10)).ConfigureXAxis(axis => {
                if (valueFormat) axis.WithValueFormat(ChartValueFormat.Custom(Format));
                else axis.WithLabelFormatter(Format);
            });
        for (var preparation = 0; preparation < 2; preparation++) {
            seen.Clear();
            var prepared = Prepare(chart);
            Assert.Equal("2026-03-02", Captions(prepared)[0].Text.Lines.Single().Text);
            var task = Assert.Single(prepared.Regions, region => region.Role == "gantt-task");
            Assert.Contains("2026-03-02 – 2026-03-19", task.Label);
            Assert.Contains(Start.AddHours(10).ToOADate(), seen);
            Assert.Contains(Finish.AddHours(10).ToOADate(), seen);
            Assert.NotEmpty(prepared.ToSvg());
            Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            if (preparation == 0) ScatterBubbleColorRenderingTests.Capture(prepared, "gantt-once-per-value-" + valueFormat);
        }
    }

    [Theory]
    [InlineData("mm", false)]
    [InlineData("mm", true)]
    [InlineData("ss", false)]
    [InlineData("fff", false)]
    public void SuccessfulSubdayFixedIntervalsKeepTheirTaskWindowWithPeriodicClockCaptions(string format, bool valueFormat) {
        var start = Start.AddHours(10).AddMinutes(17).AddSeconds(23).AddMilliseconds(456); var end = start.AddDays(1);
        var interval = new ChartTimeTickInterval(ChartTimeTickUnit.Minute, 15);
        var chart = Chart.Create().WithSize(900, 300).WithHeader(false).WithLegend(false).WithPngFont(Font())
            .ConfigureGridStyle(style => style.ShowVerticalLines = true).AddGanttTask("Window", start, end)
            .WithGanttTickInterval(interval).ConfigureXAxis(axis => {
                Func<double, string> clock = value => DateTime.FromOADate(value).ToString(format, CultureInfo.InvariantCulture);
                if (valueFormat) axis.WithValueFormat(ChartValueFormat.Custom(clock));
                else axis.WithLabelFormatter(clock);
            });
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg());
        var window = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "gantt-chart");
        Assert.Equal(start.ToOADate(), double.Parse((string)window.Attribute("data-cfx-min")!, CultureInfo.InvariantCulture), 7);
        Assert.Equal(end.ToOADate(), double.Parse((string)window.Attribute("data-cfx-max")!, CultureInfo.InvariantCulture), 7);
        Assert.Equal(96, prepared.Scene.Nodes.OfType<VisualSceneLine>().Count(line => line.Role == "schedule-grid"));
        Assert.Same(interval, chart.Options.GanttTickInterval);
        ScatterBubbleColorRenderingTests.Capture(prepared, "gantt-fixed-periodic-clock-" + format + "-" + valueFormat);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData("mm")]
    [InlineData("ss")]
    [InlineData("fff")]
    [InlineData("ff")]
    [InlineData("%f")]
    public void AutomaticPeriodicClockCaptionsDoNotExpandAMidnightAliasedWindow(string format) {
        var start = Start.AddHours(10); var end = start.AddDays(1);
        var chart = Chart.Create().WithSize(900, 300).WithHeader(false).WithLegend(false).WithPngFont(Font())
            .AddGanttTask("Window", start, end).ConfigureXAxis(axis => axis.WithLabelFormatter(
                value => DateTime.FromOADate(value).ToString(format, CultureInfo.InvariantCulture)));
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg());
        var window = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "gantt-chart");
        Assert.Equal(start.ToOADate(), double.Parse((string)window.Attribute("data-cfx-min")!, CultureInfo.InvariantCulture), 7);
        Assert.Equal(end.ToOADate(), double.Parse((string)window.Attribute("data-cfx-max")!, CultureInfo.InvariantCulture), 7);
        ScatterBubbleColorRenderingTests.Capture(prepared, "gantt-auto-periodic-clock-" + format.Replace("%", string.Empty));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
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
        if (fixedInterval) {
            Assert.Equal("2026-03-06", shown[0]);
            Assert.Equal(10, grid.Length);
        }
        else {
            Assert.InRange(grid.Length, 1, 2);
            if (grid.Length == 2) Assert.True(grid[1].Start.X > grid[0].Start.X);
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
    public async Task ProjectDayCaptionsStayOnTheirRegularGridInStaticBrowserAndNativeOutput(int width, VisualThemeMode mode) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = PlanChart(width, mode, string.Empty);
        var prepared = Prepare(chart);
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToHtmlFragment(), width, 340);
        var labels = await session.Page.Locator("[data-cfx-role='schedule-tick-label']").AllTextContentsAsync();
        Assert.Equal(Captions(prepared).Select(label => label.Text.Lines.Single().Text), labels);
        Assert.Equal("2026-03-02", labels[0]);
        var dates = labels.Select(label => DateTime.ParseExact(label, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray();
        for (var index = 2; index < dates.Length; index++) Assert.Equal(dates[1] - dates[0], dates[index] - dates[index - 1]);
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
        .WithTheme(VisualTheme.Graphite(), mode).WithFontFamily(BrowserProofFonts.Family).WithPngFont(Font()).ConfigureGridStyle(style => style.ShowVerticalLines = true);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role == "schedule-tick-label").ToArray();
    private static string Font() => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
}
