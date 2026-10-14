using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveRadialObservationIdentityBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    public async Task AuthoredSliceIdsPreserveSelectionAndLegendSyncAcrossReorderedPeers(ChartSeriesKind kind) {
        if (!Enabled) return;
        var html = new[] { Radial(kind, false), Radial(kind, true) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 2;
            options.Interaction.GroupName = "radial-observation-ids";
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.LegendToggles;
        });
        await using var session = await OpenAsync(html, 1100, 650);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        var source = roots.Nth(0).Locator(Slice(0));
        var peer = roots.Nth(1).Locator(Slice(3));
        var sourceLegend = roots.Nth(0).Locator(PointLegend(0));
        var peerLegend = roots.Nth(1).Locator(PointLegend(3));
        var id = JsonSerializer.Serialize(new[] { "parts", "north" });
        foreach (var target in new[] { source, peer, sourceLegend, peerLegend }) {
            Assert.Equal("north", await target.GetAttributeAsync("data-cfx-observation-id"));
            Assert.Equal(id, await target.GetAttributeAsync("data-cfx-target-id"));
            Assert.Equal("8", await target.GetAttributeAsync("data-cfx-value"));
        }
        await page.EvaluateAsync("() => { window.radialSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.radialSelections.push(event.detail.target)); }");
        await source.FocusAsync();
        Assert.Contains("North", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await source.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await peer.GetAttributeAsync("aria-selected"));
        Assert.Null(await roots.Nth(1).Locator(Slice(0)).GetAttributeAsync("aria-selected"));
        var selected = await page.EvaluateAsync<JsonElement>("() => window.radialSelections.at(-1)");
        Assert.Equal(id, selected.GetProperty("targetId").GetString());
        Assert.Equal("north", selected.GetProperty("observationId").GetString());
        Assert.Equal("8", selected.GetProperty("value").GetString());

        // Point legends control the same authored observation even when its local ordinal differs.
        await sourceLegend.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await sourceLegend.GetAttributeAsync("data-cfx-muted"));
        Assert.Equal("true", await peerLegend.GetAttributeAsync("data-cfx-muted"));
        Assert.Contains("cfx-series-muted", await peer.GetAttributeAsync("class"));
        Assert.Null(await roots.Nth(1).Locator(PointLegend(0)).GetAttributeAsync("data-cfx-muted"));
        await peerLegend.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await sourceLegend.GetAttributeAsync("data-cfx-muted"));
        Assert.Equal("false", await peerLegend.GetAttributeAsync("data-cfx-muted"));
        await CaptureAsync(page, kind);
        AssertNoConsoleErrors(session);
    }

    private static Chart Radial(ChartSeriesKind kind, bool reversed) {
        var points = new[] { new ChartPoint(1, 8), new ChartPoint(2, 4), new ChartPoint(3, 2), new ChartPoint(4, 1) };
        var ordered = reversed ? System.Linq.Enumerable.Reverse(points).ToArray() : points;
        var series = new ChartSeries(new ChartSeriesId("parts"), reversed ? "Localized parts" : "Parts", kind, ordered);
        var ids = new[] { "north", "south", "east", "west" };
        for (var index = 0; index < ordered.Length; index++)
            series.WithObservationId(index, new ChartObservationId(ids[(int)ordered[index].X - 1]));
        return Chart.Create().WithSize(480, 360).WithTitle(reversed ? "Reordered peer" : "Source")
            .WithXLabels("North", "South", "East", "West").WithLegend().AddSeries(series);
    }

    private static string Slice(int point) => "[data-cfx-role='radial-point'][data-cfx-point='" + point + "']";
    private static string PointLegend(int point) => "[data-cfx-role='legend-item'][data-cfx-point='" + point + "']";

    private static async Task CaptureAsync(IPage page, ChartSeriesKind kind) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var stem = Path.Combine(directory, "radial-observation-ids-" + kind);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".png" });
        await File.WriteAllTextAsync(stem + ".json", await page.EvaluateAsync<string>("""
            () => JSON.stringify({selections:window.radialSelections,charts:Array.from(document.querySelectorAll('.cfx-interactive-chart')).map(root =>
                Array.from(root.querySelectorAll('[data-cfx-target-id]')).map(node => ({role:node.dataset.cfxRole,point:node.dataset.cfxPoint,
                    observationId:node.dataset.cfxObservationId,id:node.dataset.cfxTargetId,value:node.dataset.cfxValue,
                    selected:node.getAttribute('aria-selected'),muted:node.dataset.cfxMuted})))},null,2)
            """));
    }
}
