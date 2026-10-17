using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSelectionMode.Toggle, false)]
    [InlineData(ChartSelectionMode.Toggle, true)]
    [InlineData(ChartSelectionMode.Replace, false)]
    [InlineData(ChartSelectionMode.Replace, true)]
    [InlineData(ChartSelectionMode.Single, false)]
    [InlineData(ChartSelectionMode.Single, true)]
    public async Task NativePeerSelectionReconcilesRepresentedIdentitiesWithoutClearingUnrelatedRawOrDerivedFacts(ChartSelectionMode mode, bool compare) {
        if (!Enabled) return;
        var values = new[] { new ChartPoint(0, 2), new ChartPoint(1, 5), new ChartPoint(2, 3), new ChartPoint(3, 7) };
        Chart Raw() => Chart.Create().WithSize(640, 360).AddSeries(
            ChartSeries.CreateLine(new ChartSeriesId("observations"), "Raw", values)
                .WithObservationIds(new[] { new ChartObservationId("north"), new ChartObservationId("south"), new ChartObservationId("west"), new ChartObservationId("east") }));
        Chart Summary() => Chart.Create().WithSize(640, 360).AddSeries(
            ChartSeries.CreateTrendLine(new ChartSeriesId("observations"), "Summary", values));
        var html = new[] { Raw(), Raw(), Summary(), Summary() }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1; options.Interaction.GroupName = "scoped-native-selection";
            options.Interaction.SelectionMode = mode;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts;
            if (compare) options.Interaction.Enable(ChartInteractionFeatures.CompareMarkers);
        });
        await using var session = await OpenAsync(html, 800, 2200);
        var page = session.Page; var roots = page.Locator(Root);
        await page.EvaluateAsync("""
            ()=>{window.scopedControllers=[...document.querySelectorAll('.cfx-interactive-chart')].map(root=>ChartForgeXCharts.get(root));
                window.scopedEvents=[];document.body.addEventListener('cfxselectionchange',event=>scopedEvents.push({
                    source:scopedControllers.findIndex(controller=>controller.root===event.target),reason:event.detail.reason,targets:event.detail.targets}));}
            """);
        var operations = new List<JsonElement>();
        async Task Activate(int root, string selector) {
            await roots.Nth(root).Locator(selector).FocusAsync();
            await page.Keyboard.PressAsync("Space");
            operations.Add(await page.EvaluateAsync<JsonElement>("()=>({sets:scopedControllers.map(controller=>controller.getSelection()),events:scopedEvents.length})"));
            Assert.Equal(operations.Count, await page.EvaluateAsync<int>("()=>scopedEvents.length"));
        }
        async Task AssertSet(int root, params string[] ids) => Assert.Equal(ids,
            await roots.Nth(root).EvaluateAsync<string[]>("root=>ChartForgeXCharts.get(root).getSelection().map(target=>target.targetId)"));
        var rawIds = await roots.Nth(0).Locator("[data-cfx-role=point]").EvaluateAllAsync<string[]>("nodes=>nodes.map(node=>node.dataset.cfxTargetId)");
        var summaryIds = await roots.Nth(2).Locator("[data-cfx-role=point]").EvaluateAllAsync<string[]>("nodes=>nodes.map(node=>node.dataset.cfxTargetId)");
        Assert.Equal(2, summaryIds.Length);
        const string firstSummary = "[data-cfx-role=point][data-cfx-point='0']";
        const string secondSummary = "[data-cfx-role=point][data-cfx-point='1']";
        await Activate(2, firstSummary);
        await AssertSet(0); await AssertSet(1); await AssertSet(3, summaryIds[0]);
        await Activate(0, LinkedPoint("north"));
        await AssertSet(1, rawIds[0]); await AssertSet(2, summaryIds[0]); await AssertSet(3, summaryIds[0]);
        await Activate(0, LinkedPoint("south"));
        var selectedRaw = mode == ChartSelectionMode.Toggle ? new[] { rawIds[0], rawIds[1] } : new[] { rawIds[1] };
        await AssertSet(1, selectedRaw); await AssertSet(2, summaryIds[0]); await AssertSet(3, summaryIds[0]);
        await Activate(2, secondSummary);
        var selectedSummary = mode == ChartSelectionMode.Toggle ? summaryIds : new[] { summaryIds[1] };
        await AssertSet(0, selectedRaw); await AssertSet(1, selectedRaw); await AssertSet(3, selectedSummary);
        if (mode == ChartSelectionMode.Toggle) {
            await Activate(2, firstSummary); await AssertSet(3, summaryIds[1]); await AssertSet(1, selectedRaw);
            await Activate(2, secondSummary); await AssertSet(2); await AssertSet(3); await AssertSet(1, selectedRaw);
            await Activate(0, LinkedPoint("north")); await AssertSet(1, rawIds[1]);
            await Activate(0, LinkedPoint("south")); await AssertSet(0); await AssertSet(1);
            await Activate(2, firstSummary); await Activate(0, LinkedPoint("north"));
        }
        await roots.Nth(0).Locator("[data-cfx-reset]").ClickAsync();
        foreach (var index in Enumerable.Range(0, 4)) await AssertSet(index);
        Assert.Equal(operations.Count + 1, await page.EvaluateAsync<int>("()=>scopedEvents.length"));
        Assert.Equal("reset", await page.EvaluateAsync<string>("()=>scopedEvents.at(-1).reason"));
        Assert.True(await page.EvaluateAsync<bool>("()=>scopedEvents.every(event=>event.source===0||event.source===2)"));
        await CaptureAsync(page, "scoped-peer-selection-" + mode + "-" + compare, operations);
        AssertNoConsoleErrors(session);
    }
}
