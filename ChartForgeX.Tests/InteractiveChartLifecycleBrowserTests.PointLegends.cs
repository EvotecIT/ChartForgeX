using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.RadialColumn)]
    [InlineData(ChartSeriesKind.PolarArea)]
    [InlineData(ChartSeriesKind.ProgressRing)]
    [InlineData(ChartSeriesKind.Funnel)]
    [InlineData(ChartSeriesKind.Treemap)]
    public async Task PointLegendMuteAndIsolationFollowAuthoredIdentityAcrossNativeReordering(ChartSeriesKind kind) {
        if (!Enabled) return;
        var dark = kind is ChartSeriesKind.Donut or ChartSeriesKind.ProgressRing or ChartSeriesKind.Funnel;
        var selector = kind == ChartSeriesKind.Treemap
            ? "[data-cfx-role='legend-item'][data-cfx-legend-target-id='north']"
            : "[data-cfx-role='legend-item'][data-cfx-observation-id='north']";
        var initial = PointLegendChart(kind, dark, reversed: false);
        var updated = PointLegendChart(kind, dark, reversed: true);
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage(Fit), dark ? 420 : 820, 680);
        var page = session.Page;
        await BindControllerAsync(page);
        var north = page.Locator(selector);
        Assert.Equal(1, await north.CountAsync());
        if (kind != ChartSeriesKind.Treemap) {
            Assert.Equal("north", await north.GetAttributeAsync("data-cfx-observation-id"));
            Assert.Equal("0", await north.GetAttributeAsync("data-cfx-point"));
        }
        await north.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await north.GetAttributeAsync("data-cfx-muted"));
        var mutedBefore = await PointLegendStateAsync(page, selector);
        await CaptureAsync(page, "review-legend-" + kind + "-mute-before", mutedBefore);
        await UpdateAsync(page, updated.ToInteractiveHtmlFragmentWithoutAssets(Fit));
        var mutedAfter = await PointLegendStateAsync(page, selector);
        await CaptureAsync(page, "review-legend-" + kind + "-mute-after", new { mutedBefore, mutedAfter });

        // Start the second independent state from fresh native output, even when the mute regression failed.
        await UpdateAsync(page, initial.ToInteractiveHtmlFragmentWithoutAssets(Fit));
        await page.Locator(selector).FocusAsync();
        await page.Keyboard.PressAsync("i");
        Assert.Equal("true", await page.Locator(selector).GetAttributeAsync("data-cfx-isolated"));
        var isolatedBefore = await PointLegendStateAsync(page, selector);
        await CaptureAsync(page, "review-legend-" + kind + "-isolate-before", isolatedBefore);
        await UpdateAsync(page, updated.ToInteractiveHtmlFragmentWithoutAssets(Fit));
        var isolatedAfter = await PointLegendStateAsync(page, selector);
        await CaptureAsync(page, "review-legend-" + kind + "-isolate-after", new { isolatedBefore, isolatedAfter });

        await UpdateAsync(page, PointLegendChart(kind, dark, reversed: true, removeNorth: true).ToInteractiveHtmlFragmentWithoutAssets(Fit));
        var removed = await page.Locator(Root).EvaluateAsync<JsonElement>("""
            root=>({isolated:root.dataset.cfxIsolatedSeries||'',
                controls:root.querySelectorAll('[data-cfx-role=legend-item][data-cfx-observation-id=north],[data-cfx-role=legend-item][data-cfx-legend-target-id=north]').length,
                emphasized:root.querySelectorAll('.cfx-series-isolated-in,.cfx-series-isolated-out,[data-cfx-muted=true]').length})
            """);
        await CaptureAsync(page, "review-legend-" + kind + "-removed", new { mutedBefore, mutedAfter, isolatedBefore, isolatedAfter, removed });

        if (kind != ChartSeriesKind.Treemap) {
            Assert.Equal("2", mutedAfter.GetProperty("point").GetString());
            Assert.NotEqual(mutedBefore.GetProperty("sourceId").GetString(), mutedAfter.GetProperty("sourceId").GetString());
        }
        Assert.Equal(mutedBefore.GetProperty("targetId").GetString(), mutedAfter.GetProperty("targetId").GetString());
        Assert.True(mutedAfter.GetProperty("muted").GetBoolean() && mutedAfter.GetProperty("markMuted").GetBoolean(),
            kind + " lost its authored North point mute after replacement: " + mutedAfter.GetRawText());
        Assert.True(isolatedAfter.GetProperty("isolated").GetBoolean() && isolatedAfter.GetProperty("markIsolated").GetBoolean(),
            kind + " lost its authored North point isolation after replacement: " + isolatedAfter.GetRawText());
        Assert.Equal(0, removed.GetProperty("controls").GetInt32());
        Assert.Equal(0, removed.GetProperty("emphasized").GetInt32());
        Assert.Empty(removed.GetProperty("isolated").GetString()!);
        AssertNoConsoleErrors(session);
    }

    private static Chart PointLegendChart(ChartSeriesKind kind, bool dark, bool reversed, bool removeNorth = false) {
        var points = new[] { new ChartPoint(1, 8), new ChartPoint(2, 4), new ChartPoint(3, 2) };
        var ordered = (reversed ? Enumerable.Reverse(points) : points.AsEnumerable()).Where(point => !removeNorth || point.X != 1).ToArray();
        var ids = new[] { "north", "south", "east" }; var labels = new[] { "North", "South", "East" };
        var series = kind == ChartSeriesKind.Treemap
            ? ChartSeries.CreateTreemap(new ChartSeriesId("regions-source"), "Regions", ordered.Select(point =>
                new ChartHierarchyItem(ids[(int)point.X - 1], labels[(int)point.X - 1], value: point.Y)))
            : new ChartSeries(new ChartSeriesId("regions-source"), "Regions", kind, ordered);
        for (var index = 0; index < ordered.Length; index++)
            series.WithObservationId(index, new ChartObservationId(ids[(int)ordered[index].X - 1]));
        return Chart.Create().WithSize(640, 380).WithTitle("Regional measurements")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXLabels(labels).WithLegend().WithPointLegend().WithDataLabels(false).AddSeries(series);
    }

    private static Task<JsonElement> PointLegendStateAsync(IPage page, string selector) => page.Locator(Root).EvaluateAsync<JsonElement>("""
        (root,selector)=>{
            const legend=root.querySelector(selector),data=legend.dataset;
            const marks=Array.from(root.querySelectorAll('[data-cfx-observation-id=north],[data-cfx-target-kind=node][data-cfx-target-id=north]'))
                .filter(node=>node.dataset.cfxRole!=='legend-item');
            return {point:data.cfxPoint||'',sourceId:data.cfxSourceId||'',targetId:data.cfxTargetId||'',observationId:data.cfxObservationId||'',
                muted:data.cfxMuted==='true',isolated:data.cfxIsolated==='true',
                markMuted:marks.some(node=>node.classList.contains('cfx-series-muted')),
                markIsolated:marks.some(node=>node.classList.contains('cfx-series-isolated-in'))};
        }
        """, selector);
}
