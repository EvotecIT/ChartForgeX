using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveNativeXViewportBrowserTests {
    [BrowserTheory]
    [InlineData(390, true)]
    [InlineData(900, false)]
    public async Task IncludedTimeNavigationPreservesWeekendBoundsAndMovesByIncludedDuration(int width, bool reversed) {
        if (!Enabled) return;
        var host = new NativeHost(kind: ChartSeriesKind.Candlestick, sessions: true, navigation: true, adaptive: true, dark: true, reversed: reversed);
        await using var session = await OpenAsync(host.Page(), width, 1000);
        var page = session.Page; await ConnectAsync(page, host);
        await page.WaitForFunctionAsync("drawing=>chartController.root.dataset.cfxAdaptiveDrawing===drawing", width < 500 ? "narrow" : "wide");
        var initial = await SceneAsync(page);
        Assert.True(await page.EvaluateAsync<bool>("()=>{const p=JSON.parse(chartController.root.dataset.cfxPreparedChart);return JSON.stringify(p.xProjection.timeSessions)===JSON.stringify(p.dataProjection.x.timeSessions)&&p.xProjection.timeSessions.length===2;}"));
        await page.Locator("[data-cfx-x-preset='1']").ClickAsync(); await WaitForChangesAsync(page, 1);
        var minimum = host.Maximum - 1;
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), minimum, host.Maximum);
        Assert.Equal(initial.GetProperty("yLabels").GetRawText(), (await SceneAsync(page)).GetProperty("yLabels").GetRawText());
        Assert.True(await page.EvaluateAsync<bool>("()=>nativeCallbackFrozen.length===1&&nativeCallbackFrozen[0]&&Object.isFrozen(nativeRequests[0].sourceWindow)"));
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>nativeRequests[0].sourceWindow"), minimum, host.Maximum);
        Assert.Equal("false", await page.Locator("g[data-cfx-role=point][data-cfx-series-key=samples][data-cfx-observation-id=sample-6]").GetAttributeAsync("data-cfx-visible"));
        Assert.NotEqual("false", await page.Locator("g[data-cfx-role=point][data-cfx-series-key=samples][data-cfx-observation-id=sample-7]").GetAttributeAsync("data-cfx-visible"));
        Assert.True(await page.EvaluateAsync<bool>("()=>{const axis=JSON.parse(chartController.root.dataset.cfxPreparedChart).dataProjection.x;return axis.displayTicks.length>0&&axis.displayTicks.every(tick=>axis.timeSessions.some(session=>tick.value>=session.minimum&&tick.value<=session.maximum));}"));
        var rawCaption = await page.Locator("[data-cfx-x-range=start]").GetAttributeAsync("aria-valuetext");
        Assert.False(string.IsNullOrEmpty(rawCaption));
        await page.EvaluateAsync("()=>{const root=chartController.root;root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{sync:false}}));window.sessionBookmark=JSON.parse(root.dataset.cfxInteractionSnapshot);}");
        await CaptureAsync(page, "sessions-weekend-preset-" + width, await SceneAsync(page));

        await page.Locator("[data-cfx-x-range=end]").FocusAsync();
        await page.Keyboard.PressAsync(reversed ? "ArrowRight" : "ArrowLeft"); await WaitForChangesAsync(page, 2);
        var shortened = await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()");
        Assert.Equal(minimum, shortened.GetProperty("minimum").GetDouble(), 8);
        Assert.InRange(shortened.GetProperty("maximum").GetDouble(), host.Origin + 3, host.Maximum - .0001);
        Assert.Equal(rawCaption, await page.Locator("[data-cfx-x-range=start]").GetAttributeAsync("aria-valuetext"));
        var includedSpan = shortened.GetProperty("maximum").GetDouble() - (host.Origin + 3);
        await page.Locator("[data-cfx-x-range=position]").FocusAsync(); await page.Keyboard.PressAsync("Home"); await WaitForChangesAsync(page, 3);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), host.Origin, host.Origin + includedSpan);
        await page.EvaluateAsync("()=>chartController.root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:sessionBookmark,sync:false}}))");
        await WaitForChangesAsync(page, 4);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), minimum, host.Maximum);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No compressed-time SVG export.");
        var exported = XDocument.Load(path);
        Assert.Equal((await SceneAsync(page)).GetProperty("xLabels").EnumerateArray().Select(value => value.GetString()), await BrowserProofCaptions.ExportedAsync(page, exported, "axis-x-label"));
        await SaveDownloadAsync(download, "sessions-weekend-" + width + ".svg");
        Assert.Equal("TypeError", await page.EvaluateAsync<string>("minimum=>{try{chartController.setXWindow(minimum,minimum+.25);return 'accepted';}catch(error){return error.name;}}", host.Origin + 1));
        Assert.Equal(4, await page.EvaluateAsync<int>("()=>nativeRequests.length"));
        await page.Locator("[data-cfx-x-full]").ClickAsync(); await WaitForChangesAsync(page, 5);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), host.Origin, host.Maximum);
        Assert.True(await page.Locator("[data-cfx-x-range=position]").IsDisabledAsync());
        await CaptureAsync(page, "sessions-full-" + width, await SceneAsync(page));
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>nativeErrors.map(error=>error.message)"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task IncludedTimeRawWindowSynchronizesReversedPeerWithoutExpandingWeekendBounds() {
        if (!Enabled) return;
        var first = new NativeHost(kind: ChartSeriesKind.Candlestick, sessions: true, navigation: true, id: "session-first", group: "sessions");
        var peer = new NativeHost(kind: ChartSeriesKind.RangeArea, sessions: true, navigation: true, reversed: true, width: 560, id: "session-peer", group: "sessions");
        await using var session = await OpenAsync(first.Page(peer), 1000, 1700);
        var page = session.Page; await ConnectAsync(page, first, peer);
        await page.Locator("[data-cfx-x-preset='1']").First.ClickAsync(); await WaitForChangesAsync(page, 2);
        var windows = (await page.EvaluateAsync<JsonElement>("()=>chartControllers.map(controller=>controller.getXWindow())")).EnumerateArray().ToArray();
        Assert.All(windows, window => AssertWindow(window, first.Maximum - 1, first.Maximum));
        Assert.True(await page.EvaluateAsync<bool>("()=>nativeRequests.every(request=>request.kind==='window'&&Object.isFrozen(request.sourceWindow))"));
        await CaptureAsync(page, "sessions-synchronized", windows);
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>nativeErrors.map(error=>error.message)"));
        AssertNoConsoleErrors(session);
    }
}
