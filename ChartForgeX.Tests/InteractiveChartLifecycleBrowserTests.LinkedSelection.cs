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
    [InlineData(ChartSelectionMode.Toggle)]
    [InlineData(ChartSelectionMode.Replace)]
    [InlineData(ChartSelectionMode.Single)]
    public async Task SelectionChangesBubbleWithoutCompareAndRespectActivationModeAndEscape(ChartSelectionMode mode) {
        if (!Enabled) return;
        await using var session = await OpenAsync(LinkedBars().ToInteractiveHtmlPage(options => {
            Fit(options); options.Tooltip.PinOnSelect = false;
            options.Interaction.SelectionMode = mode;
            options.Interaction.Disable(ChartInteractionFeatures.CompareMarkers);
        }), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        await page.Locator(LinkedPoint("north")).ClickAsync();
        await page.Locator(LinkedPoint("south")).ClickAsync();
        Assert.Equal(mode == ChartSelectionMode.Toggle ? 2 : 1, await LinkedSelectionCountAsync(page));
        Assert.Equal("click", await page.EvaluateAsync<string>("()=>linkedEvents.at(-1).detail.reason"));
        Assert.Equal(0, await page.Locator("[data-cfx-compare-tray]").CountAsync());
        await page.Locator(LinkedPoint("north")).ClickAsync(new LocatorClickOptions { Modifiers = new[] { KeyboardModifier.Control } });
        Assert.Equal(mode == ChartSelectionMode.Single ? 1 : mode == ChartSelectionMode.Replace ? 2 : 1, await LinkedSelectionCountAsync(page));
        await page.Locator(LinkedPoint("north")).ClickAsync(new LocatorClickOptions { Modifiers = new[] { KeyboardModifier.Meta } });
        Assert.Equal(mode == ChartSelectionMode.Single ? 1 : mode == ChartSelectionMode.Replace ? 1 : 2, await LinkedSelectionCountAsync(page));
        await page.Locator(LinkedPoint("west")).ClickAsync(new LocatorClickOptions { Modifiers = new[] { KeyboardModifier.Shift } });
        Assert.Equal(mode == ChartSelectionMode.Single ? 1 : mode == ChartSelectionMode.Replace ? 2 : 3, await LinkedSelectionCountAsync(page));
        await page.Locator(LinkedPoint("south")).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("keyboard", await page.EvaluateAsync<string>("()=>linkedEvents.at(-1).detail.reason"));
        await page.Keyboard.PressAsync("Escape");
        Assert.Equal(0, await LinkedSelectionCountAsync(page));
        Assert.Equal("clear", await page.EvaluateAsync<string>("()=>linkedEvents.at(-1).detail.reason"));
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-has-selection"));
        await page.EvaluateAsync("()=>chartController.select({seriesKeys:['orders']})");
        Assert.Equal(mode == ChartSelectionMode.Single ? 1 : 3, await LinkedSelectionCountAsync(page));
        await page.EvaluateAsync("()=>chartController.clearSelection()");
        Assert.True(await page.EvaluateAsync<bool>("()=>linkedEvents.every(event=>event.fromRoot&&event.detail.targets.every(target=>['point','node','region'].includes(target.targetKind)))"));
        await CaptureAsync(page, "linked-selection-" + mode, await page.EvaluateAsync<JsonElement>("()=>linkedEvents"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicSelectionAndHighlightCommandsMatchSourceFactsSilentlyAndRestoreIndependentState(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(LinkedBars(dark).ToInteractiveHtmlPage(options => {
            Fit(options); options.Tooltip.PinOnSelect = false;
            options.Highlight.DimOthersWhileSelected = true;
            options.Interaction.Disable(ChartInteractionFeatures.CompareMarkers | ChartInteractionFeatures.StateBookmarks);
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts); options.Interaction.GroupName = "linked-probe";
        }), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        Assert.True(await page.EvaluateAsync<bool>("()=>ChartForgeXCharts.get(chartController.root)===chartController&&ChartForgeXCharts.get(document.body)===null"));
        var proof = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const root=chartController.root, counts=[];
                root.addEventListener('cfxsync',()=>window.linkedSync=(window.linkedSync||0)+1);
                const select=(match,mode)=>{chartController.select(match,{mode});counts.push(chartController.getSelection().length)};
                select({categories:['South']});
                select({observationIds:['north'],seriesKey:'orders'},'add');
                select({sourcePoints:[{seriesKey:'orders',index:2}]},'add');
                select({targets:[chartController.getSelection()[0]]},'remove');
                select({seriesKeys:['orders']});
                select({ranges:[[.9,1.1]]});
                chartController.select({ranges:[['2026-03-01T00:00:00Z','2026-03-11T00:00:00Z']]});
                const mismatchedRange=chartController.getSelection().length;
                chartController.select({categories:['South']});
                const selected=chartController.getSelection(); selected[0].category='mutated';
                root.dispatchEvent(new CustomEvent('cfx-highlight',{detail:{match:{observationIds:['north']},dim:true,source:'grid'}}));
                return {counts,mismatchedRange,source:chartController.getSelection()[0].category,
                    highlighted:[...root.querySelectorAll('[data-cfx-highlighted=true]')].map(node=>node.dataset.cfxObservationId),
                    selection:chartController.getSelection().map(target=>target.observationId),
                    dimmed:getComputedStyle(root.querySelector('[data-cfx-observation-id=west]')).opacity,
                    selectedOpacity:getComputedStyle(root.querySelector('[data-cfx-observation-id=south]')).opacity,
                    highlightedOpacity:getComputedStyle(root.querySelector('[data-cfx-observation-id=north]')).opacity,
                    announcement:root.querySelector('[data-cfx-live]').textContent,events:linkedEvents.length,sync:window.linkedSync||0};
            }
            """);
        Assert.Equal(new[] { 1, 2, 3, 2, 3, 1 }, proof.GetProperty("counts").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal("South", proof.GetProperty("source").GetString());
        Assert.Equal(0, proof.GetProperty("mismatchedRange").GetInt32());
        Assert.Equal("north", Assert.Single(proof.GetProperty("highlighted").EnumerateArray()).GetString());
        Assert.Equal("south", Assert.Single(proof.GetProperty("selection").EnumerateArray()).GetString());
        Assert.Equal("0.3", proof.GetProperty("dimmed").GetString());
        Assert.Equal("1", proof.GetProperty("selectedOpacity").GetString());
        Assert.Equal("1", proof.GetProperty("highlightedOpacity").GetString());
        Assert.Equal("1 marks highlighted", proof.GetProperty("announcement").GetString());
        Assert.Equal(0, proof.GetProperty("events").GetInt32());
        Assert.Equal(0, proof.GetProperty("sync").GetInt32());
        await CaptureAsync(page, "linked-host-highlight-" + dark, proof);
        await page.EvaluateAsync("()=>{chartController.clearHighlight();chartController.clearSelection();}");
        Assert.Equal(0, await page.Locator(".cfx-highlighted,.cfx-linked-dim,.cfx-selection-dim").CountAsync());
        Assert.Equal(0, await LinkedSelectionCountAsync(page));
        await ScenarioCommandAsync(page, "cfx-select", new { match = new { categories = new[] { "West" } }, emit = true, sync = true });
        Assert.Equal("command", await page.EvaluateAsync<string>("()=>linkedEvents.at(-1).detail.reason"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>linkedSync"));
        await ScenarioCommandAsync(page, "cfx-clear-selection");
        Assert.Equal(0, await LinkedSelectionCountAsync(page));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>linkedEvents.length"));
        var lifecycle = await page.EvaluateAsync<JsonElement>("""
            ()=>{chartController.highlight({observationIds:['north']},{dim:true});const root=chartController.root;
                chartController.dispose();const disposed=ChartForgeXCharts.get(root)===null;
                const remaining=root.querySelectorAll('.cfx-highlighted,.cfx-linked-dim,.cfx-selection-dim').length;
                window.chartController=ChartForgeXCharts.mount(root);return{disposed,remaining,selection:chartController.getSelection().length}}
            """);
        Assert.True(lifecycle.GetProperty("disposed").GetBoolean());
        Assert.Equal(0, lifecycle.GetProperty("remaining").GetInt32());
        Assert.Equal(0, lifecycle.GetProperty("selection").GetInt32());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task HistogramContributorIdsAndRangesJoinWithoutAssigningOneContributorsIdentityToTheBin() {
        if (!Enabled) return;
        var series = ChartSeries.CreateHistogram(new ChartSeriesId("histogram"), "Duration", new[] { .2, .4, 1, 1.5 },
            ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1, 2 }))
            .WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("second"), new ChartObservationId("boundary"), new ChartObservationId("third") });
        var chart = Chart.Create().WithSize(640, 360).AddSeries(series);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Fit), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var result = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                chartController.select({observationIds:['second']});const target=chartController.getSelection()[0];
                chartController.highlight({sourcePoints:[{seriesKey:'histogram',index:2}]},{dim:true});
                const highlighted=chartController.root.querySelector('[data-cfx-highlighted=true]').dataset.cfxPoint;
                chartController.select({ranges:[[1,1]]});
                return {target,highlighted,boundaryCount:chartController.getSelection().length};
            }
            """);
        var target = result.GetProperty("target");
        Assert.Equal(new[] { "first", "second" }, target.GetProperty("observationIds").EnumerateArray().Select(item => item.GetString()));
        Assert.False(target.TryGetProperty("observationId", out _));
        Assert.Equal(new[] { 0d, Math.BitDecrement(1d) }, target.GetProperty("range").EnumerateArray().Select(item => item.GetDouble()));
        var inclusiveRange = target.GetProperty("range").EnumerateArray().Select(item => item.GetDouble()).ToArray();
        Assert.Equal(new[] { .2, .4 }, new[] { .2, .4, 1, 1.5 }.Where(value => value >= inclusiveRange[0] && value <= inclusiveRange[1]));
        Assert.Equal("1", result.GetProperty("highlighted").GetString());
        Assert.Equal(1, result.GetProperty("boundaryCount").GetInt32());
        await CaptureAsync(page, "linked-histogram-contributors", result);
        AssertNoConsoleErrors(session);
    }

    private static Chart LinkedBars(bool dark = false) {
        var series = ChartSeries.CreateBar(new ChartSeriesId("orders"), "Orders", new[] { new ChartPoint(0, 34), new ChartPoint(1, 52), new ChartPoint(2, 41) })
            .WithObservationIds(new[] { new ChartObservationId("north"), new ChartObservationId("south"), new ChartObservationId("west") });
        return Chart.Create().WithSize(640, 360).WithTheme(ChartForgeX.Themes.VisualTheme.Graphite(), dark ? ChartForgeX.Themes.VisualThemeMode.Dark : ChartForgeX.Themes.VisualThemeMode.Light)
            .WithXLabels(new[] { new ChartAxisLabel(0, "North"), new ChartAxisLabel(1, "South"), new ChartAxisLabel(2, "West") }).WithYAxisBounds(0, 60).AddSeries(series);
    }
    private static string LinkedPoint(string id) => "[data-cfx-role=point][data-cfx-observation-id=" + id + "]";
    private static Task<int> LinkedSelectionCountAsync(IPage page) => page.EvaluateAsync<int>("()=>chartController.getSelection().length");
    private static Task BindLinkedEventsAsync(IPage page) => page.EvaluateAsync("""
        ()=>{
            window.chartController=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
            window.linkedEvents=[];
            document.body.addEventListener('cfxselectionchange',event=>linkedEvents.push({fromRoot:event.target===chartController.root,detail:event.detail}));
        }
        """);
}
