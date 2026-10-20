using System.Text.Json;
using System.Threading;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false, 980)]
    [InlineData(true, 390)]
    public async Task KeyedPointTransactionsRetainSelectedFactsAndCanceledUpdatesLeaveCommittedBrushUntouched(bool dark, int width) {
        if (!Enabled) return;
        var series = ChartSeries.CreateScatter(new ChartSeriesId("live"), "Live",
            new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) })
            .WithObservationIds(new[] { new ChartObservationId("a"), new ChartObservationId("b"), new ChartObservationId("c") })
            .WithPointColor(1, "#123456").WithPointLabel(1, "Retained sample");
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(640, 360).WithXAxisBounds(0, 6).WithYAxisBounds(0, 60).AddSeries(series);
        void Options(HtmlChartInteractionOptions options) { Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Brush); }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Options), width, 850);
        var page = session.Page;
        await BindControllerAsync(page);
        await DrawLinkedBrushAsync(page);
        var brush = await page.Locator(Root).GetAttributeAsync("data-cfx-brush");
        Assert.False(string.IsNullOrEmpty(brush));
        var initial = await page.Locator(".cfx-stage svg").EvaluateAsync<string>("svg=>svg.outerHTML");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => series.ApplyPointChanges(new[] {
            ChartObservationChange<ChartPoint>.Replace(new ChartObservationId("b"), new ChartPoint(2, 99))
        }, cancellationToken: cancellation.Token));
        Assert.Equal(brush, await page.Locator(Root).GetAttributeAsync("data-cfx-brush"));
        Assert.Equal(initial, await page.Locator(".cfx-stage svg").EvaluateAsync<string>("svg=>svg.outerHTML"));
        await CaptureAsync(page, "batch-canceled-" + dark, new { brush });

        await page.EvaluateAsync("()=>chartController.select({observationIds:['b','c']})");
        var retained = "g[data-cfx-role=point][data-cfx-observation-id=b]";
        await FocusWithKeyboardAsync(page, page.Locator(retained));
        series.ApplyPointChanges(new[] {
            ChartObservationChange<ChartPoint>.Remove(new ChartObservationId("a")),
            ChartObservationChange<ChartPoint>.Replace(new ChartObservationId("b"), new ChartPoint(2, 25)),
            ChartObservationChange<ChartPoint>.Append(new ChartObservationId("d"), new ChartPoint(4, 40))
        }, maximumObservations: 3);
        await UpdateAsync(page, chart.ToInteractiveHtmlFragmentWithoutAssets(Options));
        var selection = await page.EvaluateAsync<JsonElement>("()=>chartController.getSelection()");
        Assert.Equal(new[] { "b", "c" }, selection.EnumerateArray().Select(item => item.GetProperty("observationId").GetString()).OrderBy(value => value));
        Assert.Equal(25, selection.EnumerateArray().Single(item => item.GetProperty("observationId").GetString() == "b").GetProperty("numericValue").GetDouble());
        Assert.Equal("0", await page.Locator(retained).GetAttributeAsync("data-cfx-point"));
        Assert.True(await page.Locator(retained).EvaluateAsync<bool>("node=>node===document.activeElement"));
        Assert.Equal(0, await page.Locator("[data-cfx-observation-id=a]").CountAsync());
        Assert.Equal(1, await page.Locator("g[data-cfx-role=point][data-cfx-observation-id=d]").CountAsync());
        // An accepted replacement clears transient screen-space brush geometry; cancellation above keeps the installed scene.
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-brush"));
        Assert.True(await page.Locator(".cfx-brush-box").EvaluateAsync<bool>("node=>node.hidden"));
        await CaptureAsync(page, "batch-retained-" + dark, new { selection });
        AssertNoConsoleErrors(session);
    }
}
