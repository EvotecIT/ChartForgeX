using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveNativeXViewportBrowserTests {
    [BrowserTheory]
    [InlineData("y", 390, true)]
    [InlineData("x,y", 900, false)]
    public async Task NativePrimaryYAndXYWindowsKeepGesturesBookmarksAndExportsOnTheCommittedScene(string axes, int width, bool reversed) {
        if (!Enabled) return;
        var host = new NativeHost(kind: axes == "x,y" ? ChartSeriesKind.Candlestick : ChartSeriesKind.Line, navigation: true, adaptive: true, dark: true, yReversed: reversed);
        await using var session = await OpenAsync(host.Page(), width, 1000);
        var page = session.Page; await ConnectAsync(page, host); await ConnectCartesianAsync(page, axes);
        await page.WaitForFunctionAsync("drawing=>chartController.root.dataset.cfxAdaptiveDrawing===drawing", width < 500 ? "narrow" : "wide");
        var initial = await SceneAsync(page);
        await page.EvaluateAsync("xy=>chartController.setViewport({...(xy?{x:{minimum:2,maximum:10}}:{}),y:{minimum:20,maximum:80}})", axes == "x,y");
        await WaitForChangesAsync(page, 1);
        var viewport = await page.EvaluateAsync<JsonElement>("()=>chartController.getViewport()");
        AssertWindow(viewport.GetProperty("x"), axes == "x,y" ? 2 : 0, axes == "x,y" ? 10 : 12);
        AssertWindow(viewport.GetProperty("y"), 20, 80);
        Assert.NotEqual(initial.GetProperty("yLabels").GetRawText(), (await SceneAsync(page)).GetProperty("yLabels").GetRawText());
        Assert.Equal("false", await page.Locator(First).GetAttributeAsync("data-cfx-visible"));
        Assert.Equal(0, await page.Locator(First + " [data-cfx-browser-hit-area]").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("()=>nativeCallbackFrozen.every(Boolean)&&Object.isFrozen(nativeRequests[0].sourceViewport.y)"));
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await WaitForChangesAsync(page, 2);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getYWindow()"), 26, 74);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), axes == "x,y" ? 2.8 : 0, axes == "x,y" ? 9.2 : 12);
        var pointer = await page.EvaluateAsync<JsonElement>("""
            ()=>{const plot=JSON.parse(chartController.root.dataset.cfxPreparedChart).yProjection.plotBounds,
                matrix=chartController.root.querySelector('svg').getScreenCTM(),point=new DOMPoint(plot.x+plot.width*.5,plot.y+plot.height*.25).matrixTransform(matrix);
                chartController.root.querySelector('.cfx-stage').addEventListener('wheel',event=>window.yWheelClient=event.clientY,{once:true});
                return{x:point.x,y:point.y,top:new DOMPoint(plot.x,plot.y).matrixTransform(matrix).y,bottom:new DOMPoint(plot.x,plot.y+plot.height).matrixTransform(matrix).y};}
            """);
        await page.Mouse.MoveAsync((float)pointer.GetProperty("x").GetDouble(), (float)pointer.GetProperty("y").GetDouble());
        await page.Mouse.WheelAsync(0, -100); await WaitForChangesAsync(page, 3);
        var clientY = await page.EvaluateAsync<double>("()=>yWheelClient");
        var ratio = (clientY - pointer.GetProperty("top").GetDouble()) / (pointer.GetProperty("bottom").GetDouble() - pointer.GetProperty("top").GetDouble());
        Assert.InRange(Math.Abs(clientY - pointer.GetProperty("y").GetDouble()), 0, 1);
        var anchor = reversed ? 26 + ratio * 48 : 74 - ratio * 48;
        var bookmarkY = await page.EvaluateAsync<JsonElement>("()=>chartController.getYWindow()");
        AssertWindow(bookmarkY, anchor - (anchor - 26) / 1.08, anchor + (74 - anchor) / 1.08);
        var bookmarkX = await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()");
        await page.EvaluateAsync("()=>{const root=chartController.root;root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{sync:false}}));window.xyBookmark=JSON.parse(root.dataset.cfxInteractionSnapshot);}");
        await CaptureAsync(page, "cartesian-window-" + axes + "-" + width, await page.EvaluateAsync<JsonElement>("()=>({viewport:chartController.getViewport(),snapshot:xyBookmark})"));
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        var bounds = await BoxAsync(page, ".cfx-stage svg");
        var x = (float)(bounds.X + bounds.Width * .5); var y = (float)(bounds.Y + bounds.Height * .5);
        await page.Mouse.MoveAsync(x, y); await page.Mouse.DownAsync(); await page.Mouse.MoveAsync(x + 20, y + 30, new MouseMoveOptions { Steps = 4 }); await page.Mouse.UpAsync();
        await WaitForChangesAsync(page, 4);
        var pannedY = await page.EvaluateAsync<JsonElement>("()=>chartController.getYWindow()");
        Assert.Equal(bookmarkY.GetProperty("maximum").GetDouble() - bookmarkY.GetProperty("minimum").GetDouble(), pannedY.GetProperty("maximum").GetDouble() - pannedY.GetProperty("minimum").GetDouble(), 8);
        Assert.NotEqual(bookmarkY.GetProperty("minimum").GetDouble(), pannedY.GetProperty("minimum").GetDouble());
        await page.EvaluateAsync("()=>chartController.root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:xyBookmark,sync:false}}))"); await WaitForChangesAsync(page, 5);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getYWindow()"), bookmarkY.GetProperty("minimum").GetDouble(), bookmarkY.GetProperty("maximum").GetDouble());
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), bookmarkX.GetProperty("minimum").GetDouble(), bookmarkX.GetProperty("maximum").GetDouble());
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No Y/XY export.");
        var svg = XDocument.Load(path);
        Assert.Equal((await SceneAsync(page)).GetProperty("yLabels").EnumerateArray().Select(value => value.GetString()), await BrowserProofCaptions.ExportedAsync(page, svg, "axis-y-label"));
        await SaveDownloadAsync(download, "cartesian-" + axes + "-" + width + ".svg");
        await page.Locator("[data-cfx-reset]").ClickAsync(); await WaitForChangesAsync(page, 6);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getYWindow()"), 0, 100);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 0, 12);
        Assert.Equal(initial.GetProperty("yLabels").GetRawText(), (await SceneAsync(page)).GetProperty("yLabels").GetRawText());
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>nativeErrors.map(error=>error.message)"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task NativeLogarithmicYConfinementAndReversedPeerSynchronizationShareOneSourceWindow() {
        if (!Enabled) return;
        var first = new NativeHost(id: "log-first", group: "log-y", yScale: ChartScaleKind.Logarithmic);
        var peer = new NativeHost(id: "log-peer", group: "log-y", yScale: ChartScaleKind.Logarithmic, yReversed: true, reversed: true);
        await using var session = await OpenAsync(first.Page(peer), 1000, 1500);
        var page = session.Page; await ConnectAsync(page, first, peer); await ConnectCartesianAsync(page, "y");
        await page.EvaluateAsync("()=>chartController.setYWindow(100,10000)"); await WaitForChangesAsync(page, 2);
        var windows = (await page.EvaluateAsync<JsonElement>("()=>chartControllers.map(controller=>controller.getYWindow())")).EnumerateArray().ToArray();
        Assert.All(windows, window => AssertWindow(window, 10, 1000));
        Assert.Equal("TypeError", await page.EvaluateAsync<string>("()=>{try{chartController.setYWindow(0,100);return 'accepted';}catch(error){return error.name;}}"));
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>nativeRequests.length"));
        await CaptureAsync(page, "cartesian-log-y-sync", windows);
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>nativeErrors.map(error=>error.message)")); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task AdaptiveAutomaticMarkerPaddingKeepsNarrowEdgeWindowsPortableThroughLinkedWideResetAndExport() {
        if (!Enabled) return;
        var first = new NativeHost(kind: ChartSeriesKind.Scatter, navigation: true, adaptive: true, automaticBounds: true, id: "auto-first", group: "auto-domains");
        var peer = new NativeHost(kind: ChartSeriesKind.Scatter, navigation: true, adaptive: true, automaticBounds: true, reversed: true, id: "auto-peer", group: "auto-domains");
        await using var session = await OpenAsync(first.Page(peer), 390, 1900);
        var page = session.Page; await ConnectAsync(page, first, peer);
        await page.WaitForFunctionAsync("()=>chartControllers.every(controller=>controller.root.dataset.cfxAdaptiveDrawing==='narrow')");
        var initial = (await SceneAsync(page)).GetProperty("projection");
        var minimum = initial.GetProperty("fullMinimum").GetDouble(); var maximum = initial.GetProperty("fullMaximum").GetDouble();
        var end = minimum + (maximum - minimum) * .7;
        await page.EvaluateAsync("window=>chartController.setXWindow(window.minimum,window.maximum)", new { minimum, maximum = end }); await WaitForChangesAsync(page, 2);
        var windows = (await page.EvaluateAsync<JsonElement>("()=>chartControllers.map(controller=>controller.getXWindow())")).EnumerateArray().ToArray();
        Assert.All(windows, window => AssertWindow(window, minimum, end));
        await CaptureAsync(page, "aligned-auto-narrow-edge", windows);
        await page.SetViewportSizeAsync(1000, 1900);
        await page.WaitForFunctionAsync("()=>chartControllers.every(controller=>controller.root.dataset.cfxAdaptiveDrawing==='wide')");
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), minimum, end);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").First.ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No aligned wide export.");
        var svg = XDocument.Load(path);
        Assert.Equal((await SceneAsync(page)).GetProperty("xLabels").EnumerateArray().Select(value => value.GetString()), await BrowserProofCaptions.ExportedAsync(page, svg, "axis-x-label"));
        await page.Locator("[data-cfx-x-full]").First.ClickAsync(); await WaitForChangesAsync(page, 4);
        var reset = (await page.EvaluateAsync<JsonElement>("()=>chartControllers.map(controller=>controller.getXWindow())")).EnumerateArray().ToArray();
        Assert.All(reset, window => AssertWindow(window, minimum, maximum));
        await CaptureAsync(page, "aligned-auto-wide-reset", reset);
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>nativeErrors.map(error=>error.message)")); AssertNoConsoleErrors(session);
    }

    private static Task ConnectCartesianAsync(IPage page, string axes) => page.EvaluateAsync("axes=>chartControllers.forEach(controller=>controller.connectViewport(nativeCallbacks.get(controller),{axes:axes.split(',')}))", axes);
}
