using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Aggregate marks report contributors separately from their stable rendered identity.</summary>
public sealed class InteractiveSourceCollectionBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.TrendLine, 2)]
    [InlineData(ChartSeriesKind.BoxPlot, 1)]
    public async Task SummaryMarksRetainDerivedFactsAndInteractiveIdentityForLargeRawInput(ChartSeriesKind kind, int marks) {
        if (!Enabled) return;
        var observations = Enumerable.Range(0, 4096).Select(index => new ChartPoint(index % 10 + .5, index % 7 - 3)).ToArray();
        var chart = Chart.Create().WithSize(640, 400).WithTitle("Raw observations, prepared summary");
        if (kind == ChartSeriesKind.TrendLine) chart.AddTrendLine("Regression", observations);
        else chart.AddBoxPlot("Distribution", 1, observations.Select(point => point.Y));
        chart.Series[0].WithInteractionKey("summary-source");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        await CaptureEventsAsync(page);
        var root = page.Locator(".cfx-interactive-chart");
        using (var metadata = JsonDocument.Parse(await root.EvaluateAsync<string>("node => node.dataset.cfxPreparedChart"))) {
            Assert.Equal(marks, metadata.RootElement.GetProperty("regions").EnumerateArray()
                .Count(region => region.GetProperty("role").GetString() == "point"));
            Assert.Equal(marks, metadata.RootElement.GetProperty("regions").EnumerateArray()
                .Count(region => region.GetProperty("id").GetString()!.StartsWith("series-0-", StringComparison.Ordinal)));
        }
        Assert.Equal(marks, await page.Locator(PointTarget).CountAsync());
        Assert.Equal(observations.Length.ToString(), await page.Locator("[data-cfx-role='series']").GetAttributeAsync("data-cfx-source-points"));
        var point = page.Locator(Point(0, 0));
        Assert.Equal(kind == ChartSeriesKind.TrendLine ? "regression-endpoint" : "five-number-summary", await point.GetAttributeAsync("data-cfx-derived"));
        Assert.Equal("-1", await point.GetAttributeAsync("data-cfx-source-0-index"));
        await point.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        using (var events = await ReadEventsAsync(page)) {
            foreach (var type in new[] { "cfxhover", "cfxselect" }) {
                var selected = events.RootElement.EnumerateArray().Last(item => item.GetProperty("type").GetString() == type);
                var target = selected.GetProperty("target");
                Assert.Equal("point", target.GetProperty("targetKind").GetString());
                Assert.Equal("summary-source:derived:" + (kind == ChartSeriesKind.TrendLine ? "regression-endpoint" : "five-number-summary") + ":0", target.GetProperty("targetId").GetString());
                Assert.False(target.TryGetProperty("sourcePoint", out _));
                Assert.False(target.TryGetProperty("sourcePoints", out _));
            }
        }
        await CaptureAsync(page, "raw-summary-" + kind.ToString().ToLowerInvariant());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(ChartSeriesKind.TrendLine)]
    [InlineData(ChartSeriesKind.BoxPlot)]
    public async Task DerivedSummarySelectionSynchronizesSummariesWithoutSelectingRawObservations(ChartSeriesKind kind) {
        if (!Enabled) return;
        var observations = new[] { new ChartPoint(0, 2), new ChartPoint(1, 5), new ChartPoint(2, 3), new ChartPoint(3, 7) };
        Chart Summary(ChartSeriesKind summaryKind) {
            var chart = Chart.Create().WithSize(640, 360);
            if (summaryKind == ChartSeriesKind.TrendLine) chart.AddTrendLine("Summary", observations);
            else chart.AddBoxPlot("Summary", 1, observations.Select(point => point.Y));
            chart.Series[0].WithInteractionKey("observations");
            return chart;
        }
        var raw = Chart.Create().WithSize(640, 360).AddLine("Raw", observations);
        raw.Series[0].WithInteractionKey("observations");
        var otherKind = kind == ChartSeriesKind.TrendLine ? ChartSeriesKind.BoxPlot : ChartSeriesKind.TrendLine;
        var bins = Chart.Create().WithSize(640, 360).AddHistogram("Bins", observations,
            ChartHistogramBinLayout.FromBoundaries(new[] { -.5, .5, 1.5, 2.5, 3.5 }), ChartHistogramAggregation.Count);
        bins.Series[0].WithInteractionKey("observations");
        var html = new[] { raw, Summary(kind), Summary(kind), Summary(otherKind), bins }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts;
        });
        await using var session = await OpenAsync(html, 700, 2060);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        await CaptureEventsAsync(page);
        var summary = roots.Nth(1).Locator(Point(0, 0));
        await summary.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        foreach (var index in new[] { 0, 3, 4 })
            Assert.Null(await roots.Nth(index).Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        foreach (var index in new[] { 1, 2 }) {
            var peer = roots.Nth(index).Locator(Point(0, 0));
            Assert.Equal("true", await peer.GetAttributeAsync("aria-selected"));
            Assert.Null(await peer.GetAttributeAsync("data-cfx-source-point"));
            Assert.Equal("observations:derived:" + (kind == ChartSeriesKind.TrendLine ? "regression-endpoint" : "five-number-summary") + ":0", await peer.GetAttributeAsync("data-cfx-target-id"));
        }
        using (var events = await ReadEventsAsync(page)) {
            var target = events.RootElement.EnumerateArray().Last(item => item.GetProperty("type").GetString() == "cfxselect")
                .GetProperty("target");
            Assert.Equal("observations:derived:" + (kind == ChartSeriesKind.TrendLine ? "regression-endpoint" : "five-number-summary") + ":0", target.GetProperty("targetId").GetString());
            Assert.False(target.TryGetProperty("sourcePoint", out _));
        }
        await roots.Nth(0).Locator(Point(0, 0)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await roots.Nth(0).Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        foreach (var index in new[] { 1, 2 })
            Assert.Equal("true", await roots.Nth(index).Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        using (var events = await ReadEventsAsync(page)) {
            var target = events.RootElement.EnumerateArray().Last(item => item.GetProperty("type").GetString() == "cfxselect")
                .GetProperty("target");
            Assert.Equal("observations:0", target.GetProperty("targetId").GetString());
            Assert.Equal("0", target.GetProperty("sourcePoint").GetString());
        }
        await CaptureAsync(page, "summary-raw-peer-" + kind.ToString().ToLowerInvariant());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(360, false)]
    [InlineData(720, true)]
    public async Task MeanBinsRetainPopulatedAndEmptySourcesInHoverSelectionAndPeerSynchronization(int width, bool dark) {
        if (!Enabled) return;
        var observations = new[] { new ChartPoint(.2, 10), new ChartPoint(.4, -4), new ChartPoint(1.2, 8),
            new ChartPoint(2.5, 12), new ChartPoint(6.1, -4), new ChartPoint(9, -8) };
        Chart Mean(IEnumerable<ChartPoint> source) {
            var chart = Chart.Create().WithSize(640, 400)
                .WithTheme(dark ? ChartForgeX.Themes.ChartTheme.GraphiteDark() : ChartForgeX.Themes.ChartTheme.GraphiteLight())
                .AddHistogram("Quantity", source, ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1, 3, 6, 10 }), ChartHistogramAggregation.Mean);
            chart.Series[0].WithInteractionKey("quantity-source");
            return chart;
        }
        // The peer's observations have different source ordinals for the same bins.
        var html = new[] { Mean(observations), Mean(observations.Skip(2).Concat(observations.Take(2))) }
            .ToInteractiveHtmlDashboardPage(options => {
                options.Columns = 1;
                options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                    | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts;
            });
        await using var session = await OpenAsync(html, width, 1000);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        await CaptureEventsAsync(page);
        var populated = roots.Nth(0).Locator(Point(0, 1));
        var empty = roots.Nth(0).Locator(Point(0, 2));
        Assert.Equal("2,3", await populated.GetAttributeAsync("data-cfx-source-points"));
        Assert.Equal("0,1", await roots.Nth(1).Locator(Point(0, 1)).GetAttributeAsync("data-cfx-source-points"));
        Assert.Equal("", await empty.GetAttributeAsync("data-cfx-source-points"));
        Assert.Equal("10", await populated.GetAttributeAsync("data-cfx-bin-value"));
        Assert.Equal("2", await populated.GetAttributeAsync("data-cfx-bin-count"));
        Assert.Equal("false", await empty.GetAttributeAsync("data-cfx-bin-has-value"));
        Assert.Null(await empty.GetAttributeAsync("data-cfx-bin-value"));
        var targetIds = new Dictionary<int, string>();
        foreach (var bin in new[] { 1, 2 }) {
            var point = roots.Nth(0).Locator(Point(0, bin));
            Assert.Null(await point.GetAttributeAsync("data-cfx-source-point"));
            targetIds[bin] = (await point.GetAttributeAsync("data-cfx-target-id"))!;
            Assert.StartsWith("quantity-source:derived:histogram-bin:", targetIds[bin]);
            Assert.Equal(targetIds[bin], await roots.Nth(1).Locator(Point(0, bin)).GetAttributeAsync("data-cfx-target-id"));
            await point.FocusAsync();
            await page.Keyboard.PressAsync("Space");
            foreach (var peer in new[] { 0, 1 })
                Assert.Equal("true", await roots.Nth(peer).Locator(Point(0, bin)).GetAttributeAsync("aria-selected"));
        }
        using (var events = await ReadEventsAsync(page)) {
            foreach (var bin in new[] { 1, 2 }) {
                var contributors = bin == 1 ? new[] { 2, 3 } : Array.Empty<int>();
                AssertEvent(events, "cfxhover", "point", targetIds[bin], contributors);
                AssertEvent(events, "cfxselect", "point", targetIds[bin], contributors);
            }
        }
        var ids = await roots.Nth(0).Locator(PointTarget).EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.cfxTargetId)");
        Assert.Equal(4, ids.Distinct().Count());
        await populated.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        foreach (var peer in new[] { 0, 1 }) {
            Assert.Equal("false", await roots.Nth(peer).Locator(Point(0, 1)).GetAttributeAsync("aria-selected"));
            Assert.Equal("true", await roots.Nth(peer).Locator(Point(0, 2)).GetAttributeAsync("aria-selected"));
        }
        // The series group's similarly named metadata is a count, not a contributor collection.
        await roots.Nth(0).Locator("[data-cfx-role='series'][data-cfx-series='0']").FocusAsync();
        using (var events = await ReadEventsAsync(page)) {
            var seriesHover = events.RootElement.EnumerateArray().Last(item => item.GetProperty("type").GetString() == "cfxhover");
            var target = seriesHover.GetProperty("target");
            Assert.Equal("series", target.GetProperty("targetKind").GetString());
            Assert.False(target.TryGetProperty("sourcePoints", out _));
        }
        await empty.FocusAsync();
        await CaptureAsync(page, "mean-source-collection-" + width + "-" + (dark ? "dark" : "light"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task PieOtherPointAndItsLegendShareContributorIdentityWithoutSelectingAnotherSlice() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 400).WithXLabels("A", "B", "C", "D")
            .AddPie("Slices", new[] { new ChartPoint(0, 8), new ChartPoint(1, 4), new ChartPoint(2, 2), new ChartPoint(3, 1) });
        chart.Options.MaximumPieSlices = 2;
        chart.Series[0].WithInteractionKey("slice-source");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        await CaptureEventsAsync(page);
        var other = page.Locator("[data-cfx-role='radial-point'][data-cfx-point='-1']");
        var legend = page.Locator("[data-cfx-role='legend-item'][data-cfx-point='-1']");
        var otherId = (await other.GetAttributeAsync("data-cfx-target-id"))!;
        Assert.StartsWith("slice-source:derived:radial-slice:", otherId);
        foreach (var target in new[] { other, legend }) {
            Assert.Equal("1,2,3", await target.GetAttributeAsync("data-cfx-source-points"));
            Assert.Null(await target.GetAttributeAsync("data-cfx-source-point"));
            Assert.Equal(otherId, await target.GetAttributeAsync("data-cfx-target-id"));
            await target.FocusAsync();
        }
        await other.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await other.GetAttributeAsync("aria-selected"));
        Assert.Null(await page.Locator("[data-cfx-role='radial-point'][data-cfx-point='0']").GetAttributeAsync("aria-selected"));
        using var events = await ReadEventsAsync(page);
        AssertEvent(events, "cfxhover", "point", otherId, new[] { 1, 2, 3 });
        AssertEvent(events, "cfxhover", "legend", otherId, new[] { 1, 2, 3 });
        AssertEvent(events, "cfxselect", "point", otherId, new[] { 1, 2, 3 });
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task MergedTimelineRunReportsAllContributingBuckets() {
        if (!Enabled) return;
        var day = new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithSize(640, 240)
            .AddStateTimelineLane("Availability", Enumerable.Range(0, 4)
                .Select(index => new ChartStateTimelineSegment(day.AddHours(index), day.AddHours(index + 1), "up")));
        chart.Series[0].WithInteractionKey("availability-source");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        await CaptureEventsAsync(page);
        var run = page.Locator("[data-cfx-role='state-timeline-segment']");
        Assert.Equal(1, await run.CountAsync());
        var runId = (await run.GetAttributeAsync("data-cfx-target-id"))!;
        Assert.StartsWith("availability-source:derived:timeline-run:", runId);
        Assert.Equal("0,1,2,3", await run.GetAttributeAsync("data-cfx-source-points"));
        Assert.Null(await run.GetAttributeAsync("data-cfx-source-point"));
        await run.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        using var events = await ReadEventsAsync(page);
        AssertEvent(events, "cfxhover", "point", runId, new[] { 0, 1, 2, 3 });
        AssertEvent(events, "cfxselect", "point", runId, new[] { 0, 1, 2, 3 });
        AssertNoConsoleErrors(session);
    }

    private static Task CaptureEventsAsync(IPage page) => page.EvaluateAsync("""
        () => {
            window.cfxCollectionEvents = [];
            for (const root of document.querySelectorAll('.cfx-interactive-chart'))
                for (const type of ['cfxhover', 'cfxselect'])
                    root.addEventListener(type, event => window.cfxCollectionEvents.push({ type, target: event.detail.target }));
        }
        """);

    private static async Task<JsonDocument> ReadEventsAsync(IPage page) => JsonDocument.Parse(
        await page.EvaluateAsync<string>("() => JSON.stringify(window.cfxCollectionEvents)"));

    private static void AssertEvent(JsonDocument events, string type, string kind, string id, int[] sources) {
        var matching = events.RootElement.EnumerateArray().Where(item => item.GetProperty("type").GetString() == type
            && item.GetProperty("target").GetProperty("targetKind").GetString() == kind
            && item.GetProperty("target").GetProperty("targetId").GetString() == id).ToArray();
        Assert.NotEmpty(matching);
        foreach (var item in matching) {
            var target = item.GetProperty("target");
            Assert.False(target.TryGetProperty("sourcePoint", out _));
            Assert.Equal(sources, target.GetProperty("sourcePoints").EnumerateArray().Select(value => value.GetInt32()).ToArray());
        }
    }

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
        await File.WriteAllTextAsync(Path.Combine(directory, name + "-events.json"),
            await page.EvaluateAsync<string>("() => JSON.stringify(window.cfxCollectionEvents, null, 2)"));
    }
}
