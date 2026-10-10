using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveDerivedEquivalenceBrowserTests {
    [Theory]
    [InlineData(360, false)]
    [InlineData(720, true)]
    public async Task HistogramPeersMatchIntervalPolicyAcrossDifferentLayouts(int width, bool dark) {
        if (!Enabled) return;
        var source = new[] { new ChartPoint(.2, 5), new ChartPoint(1.2, 8), new ChartPoint(2, 3) };
        Chart Histogram(string name, double[] boundaries, ChartHistogramAggregation aggregation = ChartHistogramAggregation.Count,
            ChartHistogramEncoding encoding = ChartHistogramEncoding.Value, bool pointLegend = false) {
            var chart = Chart.Create().WithSize(640, 360).WithTitle(name)
                .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
                .AddHistogram("Observations", source, ChartHistogramBinLayout.FromBoundaries(boundaries), aggregation, encoding);
            chart.Series[0].WithInteractionKey("shared-observations");
            if (pointLegend) chart.WithPointLegend();
            return chart;
        }
        var charts = new[] {
            Histogram("Source intervals", new[] { 0d, 1, 2 }),
            Histogram("Matching intervals at shifted ordinals", new[] { -1d, 0, 1, 2 }, pointLegend: true),
            Histogram("Different interval", new[] { 0d, 2 }),
            Histogram("Different aggregate", new[] { 0d, 1, 2 }, ChartHistogramAggregation.Mean),
            Histogram("Different encoding", new[] { 0d, 1, 2 }, encoding: ChartHistogramEncoding.Density),
            Histogram("Different terminal closure", new[] { 0d, 1, 2, 3 }),
            Histogram("Equivalent signed zero", new[] { BitConverter.Int64BitsToDouble(long.MinValue), 1, 2 })
        };
        await using var session = await OpenAsync(Dashboard(charts), width, 1000);
        var page = session.Page; var roots = page.Locator(".cfx-interactive-chart");
        var first = roots.Nth(0).Locator(Point(0, 0));
        await first.FocusAsync(); await page.Keyboard.PressAsync("Space");
        await CaptureAsync(page, "histogram-interval-equivalence-" + width);
        Assert.Equal("true", await roots.Nth(1).Locator(Point(0, 1)).GetAttributeAsync("aria-selected"));
        Assert.Null(await roots.Nth(1).Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        foreach (var peer in new[] { 2, 3, 4 })
            Assert.Null(await roots.Nth(peer).Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await roots.Nth(5).Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await roots.Nth(6).Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        var firstId = await first.GetAttributeAsync("data-cfx-target-id");
        Assert.Equal(firstId, await roots.Nth(1).Locator(Point(0, 1)).GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal(firstId, await roots.Nth(6).Locator(Point(0, 0)).GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal(firstId, await roots.Nth(1).Locator("[data-cfx-role='legend-item'][data-cfx-point='1']").GetAttributeAsync("data-cfx-target-id"));
        Assert.Contains("cfx-hover-series", await roots.Nth(1).Locator("[data-cfx-role='legend-item'][data-cfx-point='1']").GetAttributeAsync("class"));
        Assert.DoesNotContain("cfx-hover-series", await roots.Nth(1).Locator("[data-cfx-role='legend-item'][data-cfx-point='0']").GetAttributeAsync("class"));
        Assert.True(await roots.Nth(1).Locator(Point(0, 1)).EvaluateAsync<bool>("node => { for (let current = node; current; current = current.parentElement) if (Number(getComputedStyle(current).opacity) < .99) return false; return true; }"));
        Assert.True(await roots.Nth(1).Locator(Point(0, 2)).EvaluateAsync<bool>("node => { let opacity = 1; for (let current = node; current; current = current.parentElement) opacity *= Number(getComputedStyle(current).opacity); return opacity < .5; }"));
        foreach (var peer in new[] { 2, 3, 4 })
            Assert.Null(await roots.Nth(peer).GetAttributeAsync("data-cfx-hovering"));
        var terminal = roots.Nth(0).Locator(Point(0, 1));
        await terminal.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await roots.Nth(1).Locator(Point(0, 2)).GetAttributeAsync("aria-selected"));
        Assert.Null(await roots.Nth(5).Locator(Point(0, 1)).GetAttributeAsync("aria-selected"));
        Assert.NotEqual(await terminal.GetAttributeAsync("data-cfx-target-id"),
            await roots.Nth(5).Locator(Point(0, 1)).GetAttributeAsync("data-cfx-target-id"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task OtherSlicesMatchTheirContributorSetAcrossGroupingPolicies() {
        if (!Enabled) return;
        Chart Pie(int slices) {
            var chart = Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C", "D")
                .AddPie("Parts", new[] { new ChartPoint(0, 8), new ChartPoint(1, 4), new ChartPoint(2, 2), new ChartPoint(3, 1) });
            chart.Options.MaximumPieSlices = slices; chart.Series[0].WithInteractionKey("shared-parts");
            return chart;
        }
        await using var session = await OpenAsync(Dashboard(new[] { Pie(2), Pie(3), Pie(2) }), 700, 1000);
        var roots = session.Page.Locator(".cfx-interactive-chart");
        const string other = "[data-cfx-role='radial-point'][data-cfx-point='-1']";
        var source = roots.Nth(0).Locator(other);
        await source.FocusAsync(); await session.Page.Keyboard.PressAsync("Space");
        await CaptureAsync(session.Page, "slice-group-equivalence");
        Assert.Equal("true", await roots.Nth(2).Locator(other).GetAttributeAsync("aria-selected"));
        Assert.Null(await roots.Nth(1).Locator(other).GetAttributeAsync("aria-selected"));
        Assert.NotEqual(await source.GetAttributeAsync("data-cfx-target-id"),
            await roots.Nth(1).Locator(other).GetAttributeAsync("data-cfx-target-id"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task MergedTimelineRunsMatchIntervalsAcrossDifferentCoalescingResults() {
        if (!Enabled) return;
        var day = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc);
        Chart Timeline(int hours) {
            var chart = Chart.Create().WithSize(640, 240).AddStateTimelineLane("Availability", Enumerable.Range(0, hours)
                .Select(index => new ChartStateTimelineSegment(day.AddHours(index), day.AddHours(index + 1), "up")));
            chart.Series[0].WithInteractionKey("shared-availability"); return chart;
        }
        await using var session = await OpenAsync(Dashboard(new[] { Timeline(4), Timeline(2), Timeline(4) }), 700, 1000);
        var roots = session.Page.Locator(".cfx-interactive-chart");
        const string run = "[data-cfx-role='state-timeline-segment']";
        var source = roots.Nth(0).Locator(run);
        await source.FocusAsync(); await session.Page.Keyboard.PressAsync("Space");
        await CaptureAsync(session.Page, "timeline-run-equivalence");
        Assert.Equal("true", await roots.Nth(2).Locator(run).GetAttributeAsync("aria-selected"));
        Assert.Null(await roots.Nth(1).Locator(run).GetAttributeAsync("aria-selected"));
        Assert.NotEqual(await source.GetAttributeAsync("data-cfx-target-id"),
            await roots.Nth(1).Locator(run).GetAttributeAsync("data-cfx-target-id"));
        AssertNoConsoleErrors(session);
    }

    private static string Dashboard(Chart[] charts) => charts.ToInteractiveHtmlDashboardPage(options => {
        options.Columns = 1;
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
            | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts;
    });

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), await page.EvaluateAsync<string>(
            "() => JSON.stringify(Array.from(document.querySelectorAll('.cfx-interactive-chart')).map(root => Array.from(root.querySelectorAll('[data-cfx-target-id]')).map(node => ({ kind: node.dataset.cfxTargetKind, id: node.dataset.cfxTargetId, selected: node.getAttribute('aria-selected'), sources: node.dataset.cfxSourcePoints }))), null, 2)"));
    }
}
