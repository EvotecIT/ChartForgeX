using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [BrowserFact]
    public async Task AdaptiveRedrawPreservesResetDismissalWhileRestoringNativeLinkWithoutKeyboardNavigation() {
        if (!Enabled) return;
        const string cell = "[data-cfx-role='heatmap-cell'][data-cfx-point='0']";
        const string link = "a[href='#evidence']";
        var chart = Chart.Create().WithSize(800, 440).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithXLabels("Evidence")
            .WithStateCategories(new ChartStateCategory("pass", "Passed", ChartColor.FromHex("#1d8a52")))
            .AddHeatmapCategoryRow("Reading", new[] { new ChartHeatmapCell("pass", "1", href: "#evidence") });
        chart.Series[0].WithObservationId(0, new ChartObservationId("reading"));
        var html = chart.ToInteractiveHtmlPage(options => {
            ConfigureDelay(options, "exact");
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 360;
            options.Adaptive.NarrowHeight = 440;
        }).Replace("<main ", "<button id=\"before-native-reset\">Before chart</button><main ", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, 950, 760);
        var page = session.Page;
        await page.Locator(".cfx-interactive-chart").EvaluateAsync("root=>root.style.width='360px'");
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing==='narrow'", null,
            new PageWaitForFunctionOptions { Timeout = 10000 });
        Assert.Equal(0, await page.Locator("[data-cfx-keyboard-component]").CountAsync());
        Assert.Equal("0", await page.Locator(link).GetAttributeAsync("tabindex"));
        var pointer = await PointerAsync(page, cell);
        await page.Locator("#before-native-reset").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await page.Locator(link).EvaluateAsync<bool>("node=>node===document.activeElement&&node.matches(':focus-visible')"));
        Assert.False(await TipHiddenAsync(page));
        var reset = await page.EvaluateAsync<JsonElement>("""
            () => {
                const root=window.nativeLinkResetRoot=document.querySelector('.cfx-interactive-chart');
                window.nativeLinkResetController=ChartForgeXCharts.get(root);
                nativeLinkResetController.select({observationIds:['reading']});
                const reset=root.querySelector('[data-cfx-reset]'), link=root.querySelector("a[href='#evidence']");
                const resetVisible=!reset.hidden;
                // Hosts may activate the generated command without moving native link focus.
                reset.click();
                return {resetVisible,focusRetained:document.activeElement===link,hidden:root.querySelector('.cfx-tooltip').hidden};
            }
            """);
        Assert.True(reset.GetProperty("resetVisible").GetBoolean());
        Assert.True(reset.GetProperty("focusRetained").GetBoolean());
        Assert.True(reset.GetProperty("hidden").GetBoolean());
        await page.EvaluateAsync("""
            pointer => {
                const root=nativeLinkResetRoot;
                window.nativeLinkResetPointer=pointer;
                window.nativeLinkResetPreviousSvg=root.querySelector('.cfx-stage svg');
                window.nativeLinkResetEvents={moves:[],keys:[],down:[],focus:[],shown:[],layouts:[]};
                window.nativeLinkResetPhase='redraw';
                for(const name of ['pointermove','pointerdown','keydown']) document.addEventListener(name,event=>{
                    if(nativeLinkResetPhase==='redraw')nativeLinkResetEvents[name==='pointermove'?'moves':name==='keydown'?'keys':'down'].push(event.type);
                },true);
                root.addEventListener('focus',event=>{
                    if(nativeLinkResetPhase==='redraw')nativeLinkResetEvents.focus.push({tag:event.target.localName,href:event.target.getAttribute('href')});
                },true);
                root.addEventListener('cfxlayoutchange',event=>nativeLinkResetEvents.layouts.push(event.detail));
                new MutationObserver(records=>{
                    if(nativeLinkResetPhase!=='redraw')return;
                    for(const record of records)if(record.type==='attributes'&&record.target.matches('.cfx-tooltip')
                        &&(record.oldValue===null||!record.target.hidden))nativeLinkResetEvents.shown.push({text:record.target.innerText});
                }).observe(root,{subtree:true,attributes:true,attributeFilter:['hidden'],attributeOldValue:true});
                root.style.width='800px';
            }
            """, pointer);
        await page.WaitForFunctionAsync("()=>nativeLinkResetRoot.dataset.cfxAdaptiveDrawing==='wide'&&nativeLinkResetEvents.layouts.some(event=>event.drawing==='wide')", null,
            new PageWaitForFunctionOptions { Timeout = 10000 });
        await Task.Delay(Delay + 80);
        var dismissed = await page.EvaluateAsync<JsonElement>("""
            () => {
                const root=nativeLinkResetRoot, cell=root.querySelector('[data-cfx-role="heatmap-cell"][data-cfx-point="0"]');
                const link=root.querySelector("a[href='#evidence']"), box=cell.getBoundingClientRect(), [x,y]=nativeLinkResetPointer;
                return {hidden:root.querySelector('.cfx-tooltip').hidden,focused:document.activeElement===link,
                    focusVisible:link.matches(':focus-visible'),target:cell.dataset.cfxTargetId,
                    hit:document.elementFromPoint(x,y)?.closest('[data-cfx-target-id]')?.dataset.cfxTargetId,
                    pointerInsideMark:x>box.left&&x<box.right&&y>box.top&&y<box.bottom,
                    oldSvgDetached:!nativeLinkResetPreviousSvg.isConnected,
                    sameController:ChartForgeXCharts.get(root)===nativeLinkResetController,events:nativeLinkResetEvents};
            }
            """);
        await CaptureDelayAsync(page, "delay-adaptive-native-link-reset-dismissed", dismissed, session);
        Assert.True(dismissed.GetProperty("oldSvgDetached").GetBoolean());
        Assert.True(dismissed.GetProperty("sameController").GetBoolean());
        Assert.True(dismissed.GetProperty("focused").GetBoolean());
        Assert.True(dismissed.GetProperty("focusVisible").GetBoolean());
        Assert.True(dismissed.GetProperty("pointerInsideMark").GetBoolean());
        Assert.Equal(dismissed.GetProperty("target").GetString(), dismissed.GetProperty("hit").GetString());
        Assert.Contains(dismissed.GetProperty("events").GetProperty("focus").EnumerateArray(), item => item.GetProperty("href").GetString() == "#evidence");
        foreach (var input in new[] { "moves", "keys", "down" }) Assert.Empty(dismissed.GetProperty("events").GetProperty(input).EnumerateArray());
        Assert.True(dismissed.GetProperty("hidden").GetBoolean(), "Restoring a native link after adaptive redraw must retain reset dismissal without fresh input.");
        Assert.Empty(dismissed.GetProperty("events").GetProperty("shown").EnumerateArray());

        await page.EvaluateAsync("()=>window.nativeLinkResetPhase='recovery'");
        await page.Locator("#before-native-reset").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        Assert.False(await TipHiddenAsync(page));
        Assert.Contains("Reading", await TooltipTextAsync(page));
        await page.EvaluateAsync("()=>{nativeLinkResetController.select({observationIds:['reading']});nativeLinkResetRoot.querySelector('[data-cfx-reset]').click();}");
        Assert.True(await TipHiddenAsync(page));
        await PointerAsync(page, cell);
        await WaitForTipAsync(page);
        Assert.Contains("Reading", await TooltipTextAsync(page));
        AssertNoConsoleErrors(session);
    }
}
