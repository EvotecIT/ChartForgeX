using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveDerivedLegendBrowserTests {
    [Theory]
    [InlineData(360, false, false)]
    [InlineData(720, true, false)]
    [InlineData(360, false, true)]
    [InlineData(720, true, true)]
    public async Task PointLegendControlsMatchIntervalsAndKeepTheIsolatedBinBright(int width, bool dark, bool isolate) {
        if (!Enabled) return;
        var source = new[] { new ChartPoint(.2, 5), new ChartPoint(1.2, 8), new ChartPoint(2, 3) };
        Chart Histogram(string title, double[] boundaries, ChartHistogramAggregation aggregation = ChartHistogramAggregation.Count,
            ChartHistogramEncoding encoding = ChartHistogramEncoding.Value) {
            var chart = Chart.Create().WithSize(640, 360).WithTitle(title).WithPointLegend()
                .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
                .AddHistogram("Observations", source, ChartHistogramBinLayout.FromBoundaries(boundaries), aggregation, encoding);
            chart.Series[0].WithInteractionKey("shared-observations");
            return chart;
        }
        await using var session = await OpenAsync(Dashboard(new[] {
            Histogram("Source intervals", new[] { 0d, 1, 2 }),
            Histogram("Matching shifted interval", new[] { -1d, 0, 1, 2 }),
            Histogram("Different interval", new[] { 0d, 2 }),
            Histogram("Different aggregate", new[] { 0d, 1, 2 }, ChartHistogramAggregation.Mean),
            Histogram("Different encoding", new[] { 0d, 1, 2 }, encoding: ChartHistogramEncoding.Density)
        }), width, 1000);
        var roots = session.Page.Locator(".cfx-interactive-chart");
        var sourceLegend = roots.Nth(0).Locator(PointLegend(0));
        await sourceLegend.FocusAsync();
        await session.Page.Keyboard.PressAsync(isolate ? "i" : "Space");
        await MoveAwayAsync(session.Page);
        await CaptureAsync(session.Page, $"histogram-legend-{(isolate ? "isolate" : "mute")}-{width}");
        if (isolate) {
            Assert.Equal("true", await sourceLegend.GetAttributeAsync("data-cfx-isolated"));
            Assert.Equal("true", await roots.Nth(1).Locator(PointLegend(1)).GetAttributeAsync("data-cfx-isolated"));
            Assert.True(await BrightAsync(roots.Nth(0).Locator(Point(0, 0))));
            Assert.True(await BrightAsync(roots.Nth(1).Locator(Point(0, 1))));
            Assert.False(await BrightAsync(roots.Nth(1).Locator(Point(0, 0))));
        } else {
            Assert.Equal("true", await sourceLegend.GetAttributeAsync("data-cfx-muted"));
            Assert.Equal("true", await roots.Nth(1).Locator(PointLegend(1)).GetAttributeAsync("data-cfx-muted"));
            Assert.DoesNotContain("cfx-series-muted", await roots.Nth(1).Locator(Point(0, 0)).GetAttributeAsync("class"));
            Assert.Contains("cfx-series-muted", await roots.Nth(1).Locator(Point(0, 1)).GetAttributeAsync("class"));
        }
        foreach (var peer in new[] { 2, 3, 4 }) {
            Assert.Equal(0, await roots.Nth(peer).Locator(".cfx-series-muted,.cfx-series-isolated-in,.cfx-series-isolated-out").CountAsync());
            Assert.Equal(0, await roots.Nth(peer).Locator("[data-cfx-isolated='true'],[data-cfx-muted='true']").CountAsync());
        }
        await sourceLegend.FocusAsync();
        await session.Page.Keyboard.PressAsync(isolate ? "i" : "Space");
        Assert.Equal(0, await roots.Nth(1).Locator(".cfx-series-muted,.cfx-series-isolated-in,.cfx-series-isolated-out").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ZeroRadialLegendRetainsDerivedIdentityAndCannotToggleRawRows(bool donut, bool isolate) {
        if (!Enabled) return;
        Chart Radial(double value) {
            var chart = Chart.Create().WithSize(640, 360).WithTitle(value == 0 ? "Zero radial source" : "Nonzero radial peer").WithXLabels("A", "B");
            var points = new[] { new ChartPoint(1, value), new ChartPoint(2, 4) };
            if (donut) chart.AddDonut("Radial values", points); else chart.AddPie("Radial values", points);
            chart.Series[0].WithInteractionKey("radial-raw-sibling");
            return chart;
        }
        var raw = Chart.Create().WithSize(640, 360).WithTitle("Raw peer").WithPointLegend()
            .AddBar("Raw values", new[] { new ChartPoint(1, 2), new ChartPoint(2, 4) });
        raw.Series[0].WithInteractionKey("radial-raw-sibling");
        await using var session = await OpenAsync(Dashboard(new[] { Radial(0), raw, Radial(2) }), 700, 1000);
        var roots = session.Page.Locator(".cfx-interactive-chart");
        var legend = roots.Nth(0).Locator(PointLegend(0));
        Assert.Equal(await roots.Nth(2).Locator(PointLegend(0)).GetAttributeAsync("data-cfx-target-id"), await legend.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal("radial-raw-sibling:derived:radial-slice:0", await legend.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal("0", await legend.GetAttributeAsync("data-cfx-source-points"));
        Assert.Null(await legend.GetAttributeAsync("data-cfx-source-point"));
        await legend.FocusAsync(); await session.Page.Keyboard.PressAsync(isolate ? "i" : "Space");
        await CaptureAsync(session.Page, $"zero-{(donut ? "donut" : "pie")}-legend-{(isolate ? "isolate" : "mute")}");
        Assert.Equal("true", await roots.Nth(2).Locator(PointLegend(0)).GetAttributeAsync(isolate ? "data-cfx-isolated" : "data-cfx-muted"));
        Assert.Equal(0, await roots.Nth(1).Locator(".cfx-series-muted,.cfx-series-isolated-in,.cfx-series-isolated-out").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task OtherLegendControlsMatchContributorSetsAcrossGroupingPolicies() {
        if (!Enabled) return;
        Chart Pie(int maximum) {
            var chart = Chart.Create().WithSize(640, 360).WithXLabels("A", "B", "C", "D")
                .AddPie("Parts", new[] { new ChartPoint(1, 8), new ChartPoint(2, 4), new ChartPoint(3, 2), new ChartPoint(4, 1) });
            chart.Options.MaximumPieSlices = maximum;
            chart.Series[0].WithInteractionKey("shared-parts"); return chart;
        }
        await using var session = await OpenAsync(Dashboard(new[] { Pie(2), Pie(3), Pie(2) }), 700, 1000);
        var roots = session.Page.Locator(".cfx-interactive-chart");
        var source = roots.Nth(0).Locator(PointLegend(-1));
        await source.FocusAsync(); await session.Page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await roots.Nth(2).Locator(PointLegend(-1)).GetAttributeAsync("data-cfx-muted"));
        Assert.Null(await roots.Nth(1).Locator(PointLegend(-1)).GetAttributeAsync("data-cfx-muted"));
        await source.FocusAsync(); await session.Page.Keyboard.PressAsync("i");
        await MoveAwayAsync(session.Page);
        await CaptureAsync(session.Page, "other-legend-isolation");
        Assert.True(await BrightAsync(roots.Nth(0).Locator("[data-cfx-role='radial-point'][data-cfx-point='-1']")));
        Assert.True(await BrightAsync(roots.Nth(2).Locator("[data-cfx-role='radial-point'][data-cfx-point='-1']")));
        Assert.Equal(0, await roots.Nth(1).Locator(".cfx-series-muted,.cfx-series-isolated-in,.cfx-series-isolated-out").CountAsync());
        await source.FocusAsync(); await session.Page.Keyboard.PressAsync("i");
        foreach (var peer in new[] { 0, 2 }) {
            Assert.Equal(0, await roots.Nth(peer).Locator(".cfx-series-isolated-in,.cfx-series-isolated-out").CountAsync());
            Assert.Equal("true", await roots.Nth(peer).Locator(PointLegend(-1)).GetAttributeAsync("data-cfx-muted"));
            Assert.Contains("cfx-series-muted", await roots.Nth(peer).Locator("[data-cfx-role='radial-point'][data-cfx-point='-1']").GetAttributeAsync("class"));
        }
        AssertNoConsoleErrors(session);
    }

    private static string PointLegend(int point) => "[data-cfx-role='legend-item'][data-cfx-point='" + point + "']";
    private static string Dashboard(Chart[] charts) => charts.ToInteractiveHtmlDashboardPage(options => {
        options.Columns = 1;
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
            | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.LegendToggles;
    });
    private static Task<bool> BrightAsync(ILocator node) => node.EvaluateAsync<bool>(
        "node => {for(let parent=node;parent;parent=parent.parentElement) if(Number(getComputedStyle(parent).opacity)<.99) return false; return true;}");
    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
    }
}
