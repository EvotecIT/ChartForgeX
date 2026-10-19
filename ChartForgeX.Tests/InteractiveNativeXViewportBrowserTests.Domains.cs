using System.Text.Json;
using ChartForgeX.Core;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveNativeXViewportBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Area, false, false)]
    [InlineData(ChartSeriesKind.Area, true, true)]
    [InlineData(ChartSeriesKind.Bubble, false, true)]
    [InlineData(ChartSeriesKind.Bubble, true, false)]
    public async Task NativeSourceWindowsReprepareSmoothAreaAndBubbleOnLinearAndTimeAxes(ChartSeriesKind kind, bool time, bool reversed) {
        if (!Enabled) return;
        var host = new NativeHost(kind: kind, time: time, reversed: reversed);
        await using var session = await OpenAsync(host.Page(), 980, 680);
        var page = session.Page; await ConnectAsync(page, host);
        var initial = await SceneAsync(page);
        Assert.True(await page.EvaluateAsync<bool>("bounds=>chartController.setXWindow(bounds.minimum,bounds.maximum)===chartController",
            new { minimum = host.Origin + 2, maximum = host.Origin + 10 }));
        await WaitForChangesAsync(page, 1);
        var window = await SceneAsync(page);
        AssertWindow(window.GetProperty("projection"), host.Origin + 2, host.Origin + 10);
        Assert.Equal(time ? "time" : "linear", window.GetProperty("projection").GetProperty("scale").GetString());
        Assert.Equal(initial.GetProperty("yLabels").GetRawText(), window.GetProperty("yLabels").GetRawText());
        Assert.NotEqual(initial.GetProperty("xLabels").GetRawText(), window.GetProperty("xLabels").GetRawText());
        Assert.Equal(13, await page.Locator("g[data-cfx-role=point][data-cfx-series-key=samples]").CountAsync());
        Assert.Equal(4, await page.Locator("g[data-cfx-role=point][data-cfx-series-key=samples][data-cfx-visible=false]").CountAsync());
        Assert.Equal(0, await page.Locator(First + " [data-cfx-browser-hit-area]").CountAsync());
        Assert.NotEqual("0", await page.Locator(First).GetAttributeAsync("tabindex"));
        await page.Locator(Middle).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Middle).GetAttributeAsync("aria-selected"));
        if (kind == ChartSeriesKind.Area) {
            Assert.NotEmpty(await page.Locator("[data-cfx-role=area]").GetAttributeAsync("d") ?? string.Empty);
            Assert.NotEmpty(await page.Locator("[data-cfx-role=line]").First.GetAttributeAsync("d") ?? string.Empty);
        }
        else {
            Assert.Equal("28", await page.Locator(Middle).GetAttributeAsync("data-cfx-size"));
            Assert.Equal(9, await page.Locator("[data-cfx-role=bubble]").CountAsync());
        }
        Assert.Equal("window", await page.EvaluateAsync<string>("()=>nativeRequests[0].kind"));
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>nativeErrors.map(error=>error.message)"));
        await CaptureAsync(page, "domain-" + kind + "-" + time, window);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeWheelKeepsThePointerSourceAnchorWhileButtonsUseThePlotCentre(bool reversed) {
        if (!Enabled) return;
        var host = new NativeHost(reversed: reversed, kind: ChartSeriesKind.Area);
        await using var session = await OpenAsync(host.Page(), 980, 680);
        var page = session.Page; await ConnectAsync(page, host);
        await page.EvaluateAsync("()=>chartController.setXWindow(2,10)"); await WaitForChangesAsync(page, 1);
        var pointer = await page.EvaluateAsync<JsonElement>("""
            ()=>{const projection=JSON.parse(chartController.root.dataset.cfxPreparedChart).xProjection,
                plot=projection.plotBounds,svg=chartController.root.querySelector('svg');
                const matrix=svg.getScreenCTM(),point=new DOMPoint(plot.x+plot.width*.25,plot.y+plot.height*.5).matrixTransform(matrix);
                chartController.root.querySelector('.cfx-stage').addEventListener('wheel',event=>window.nativeWheelClientX=event.clientX,{once:true});
                return{x:point.x,y:point.y,left:new DOMPoint(plot.x,plot.y).matrixTransform(matrix).x,
                    right:new DOMPoint(plot.x+plot.width,plot.y).matrixTransform(matrix).x};}
            """);
        await page.Mouse.MoveAsync((float)pointer.GetProperty("x").GetDouble(), (float)pointer.GetProperty("y").GetDouble());
        await page.Mouse.WheelAsync(0, -100); await WaitForChangesAsync(page, 2);
        // Real pointer input is quantized by the browser. Use the received screen coordinate,
        // independently mapped through the captured plot endpoints, rather than assuming its requested float survived exactly.
        var clientX = await page.EvaluateAsync<double>("()=>nativeWheelClientX");
        var ratio = (clientX - pointer.GetProperty("left").GetDouble()) / (pointer.GetProperty("right").GetDouble() - pointer.GetProperty("left").GetDouble());
        Assert.InRange(ratio, .249, .251);
        var anchor = reversed ? 10 - ratio * 8 : 2 + ratio * 8;
        var wheel = await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()");
        AssertWindow(wheel, anchor - (anchor - 2) / 1.08, anchor + (10 - anchor) / 1.08);
        Assert.NotEqual(6, (wheel.GetProperty("minimum").GetDouble() + wheel.GetProperty("maximum").GetDouble()) / 2);
        await page.EvaluateAsync("()=>chartController.setXWindow(2,10)"); await WaitForChangesAsync(page, 3);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await WaitForChangesAsync(page, 4);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 2.8, 9.2);
        await CaptureAsync(page, "pointer-anchor-" + reversed, new { wheel, centred = await SceneAsync(page) });
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task PublicNativeWindowValidatesBoundsAndLetsTheCapturedHostProjectionConstrainTheSourceSpan() {
        if (!Enabled) return;
        var host = new NativeHost(reversed: true);
        await using var session = await OpenAsync(host.Page(), 980, 680);
        var page = session.Page; await ConnectAsync(page, host);
        var errors = await page.EvaluateAsync<string[]>("""
            ()=>[[NaN,8],[2,Infinity],[-Infinity,8],[8,2],[2,2],['2',8]].map(bounds=>{
                try{chartController.setXWindow(...bounds);return 'accepted';}catch(error){return error.name;}})
            """);
        Assert.All(errors, error => Assert.Equal("TypeError", error));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>nativeRequests.length"));
        await page.EvaluateAsync("()=>chartController.setXWindow(-5,3)"); await WaitForChangesAsync(page, 1);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 0, 8);
        await page.EvaluateAsync("()=>chartController.setXWindow(10,18)"); await WaitForChangesAsync(page, 2);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 4, 12);
        await page.EvaluateAsync("()=>chartController.setXWindow(-5,50)"); await WaitForChangesAsync(page, 3);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 0, 12);
        Assert.True(await page.EvaluateAsync<bool>("()=>{const copy=chartController.getXWindow();copy.minimum=99;return chartController.getXWindow().minimum===0;}"));
        Assert.True(await page.EvaluateAsync<bool>("()=>nativeRequests.every(request=>Number.isFinite(request.left)&&Number.isFinite(request.right)&&request.left<request.right)"));
        await page.EvaluateAsync("()=>chartController.connectXViewport(null)");
        Assert.Equal("TypeError", await page.EvaluateAsync<string>("()=>{try{chartController.setXWindow(2,8);return 'accepted';}catch(error){return error.name;}}"));
        Assert.Equal(4, host.PreparedCount);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task BookmarkRestorationRequestsTheCapturedTimeDomainAndReconcilesObservationIdentity() {
        if (!Enabled) return;
        var host = new NativeHost(kind: ChartSeriesKind.Area, time: true, reversed: true);
        await using var session = await OpenAsync(host.Page(), 980, 680);
        var page = session.Page; await ConnectAsync(page, host);
        await page.EvaluateAsync("bounds=>chartController.setXWindow(bounds.minimum,bounds.maximum)", new { minimum = host.Origin + 2, maximum = host.Origin + 10 });
        await WaitForChangesAsync(page, 1);
        await page.Locator(Middle).FocusAsync(); await page.Keyboard.PressAsync("Space");
        var snapshot = await page.EvaluateAsync<JsonElement>("""
            ()=>{chartController.root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{source:'native-test',sync:false}}));
                window.nativeBookmark=JSON.parse(chartController.root.dataset.cfxInteractionSnapshot);return nativeBookmark;}
            """);
        AssertWindow(snapshot.GetProperty("xWindow"), host.Origin + 2, host.Origin + 10);
        Assert.Equal("time", snapshot.GetProperty("xWindow").GetProperty("scale").GetString());
        await page.EvaluateAsync("bounds=>chartController.setXWindow(bounds.minimum,bounds.maximum)", new { minimum = host.Origin + 4, maximum = host.Origin + 8 });
        await WaitForChangesAsync(page, 2);
        await page.EvaluateAsync("()=>{chartController.clearSelection();chartController.root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:nativeBookmark,sync:false}}));}");
        await WaitForChangesAsync(page, 3);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), host.Origin + 2, host.Origin + 10);
        Assert.Equal("true", await page.Locator(Middle).GetAttributeAsync("aria-selected"));
        Assert.Equal("1.000", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        Assert.Equal("window", await page.EvaluateAsync<string>("()=>nativeRequests.at(-1).kind"));
        await CaptureAsync(page, "native-bookmark-restored", snapshot);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task LinkedNativeWindowsUseSourceDomainsAcrossDifferentPlotSizesAndDirectionsWithoutFeedback() {
        if (!Enabled) return;
        var first = new NativeHost(kind: ChartSeriesKind.Area, time: true, id: "native-wide", group: "native-domains");
        var peer = new NativeHost(kind: ChartSeriesKind.Bubble, time: true, reversed: true, width: 560, id: "native-reversed", group: "native-domains");
        await using var session = await OpenAsync(first.Page(peer), 1100, 1000);
        var page = session.Page; await ConnectAsync(page, first, peer);
        await page.EvaluateAsync("bounds=>chartControllers[0].setXWindow(bounds.minimum,bounds.maximum)", new { minimum = first.Origin + 2, maximum = first.Origin + 10 });
        await WaitForChangesAsync(page, 2);
        var receipt = await page.EvaluateAsync<JsonElement>("()=>chartControllers.map(controller=>({...controller.getXWindow(),projection:JSON.parse(controller.root.dataset.cfxPreparedChart).xProjection}))");
        await CaptureAsync(page, "linked-native-first-commit", receipt);
        Assert.Equal(JsonValueKind.Array, receipt.ValueKind);
        var windows = receipt.EnumerateArray().ToArray();
        Assert.Equal(2, windows.Length);
        Assert.All(windows, window => AssertWindow(window, first.Origin + 2, first.Origin + 10));
        Assert.NotEqual(windows[0].GetProperty("projection").GetProperty("plotBounds").GetProperty("width").GetDouble(),
            windows[1].GetProperty("projection").GetProperty("plotBounds").GetProperty("width").GetDouble());
        Assert.False(windows[0].GetProperty("projection").GetProperty("reversed").GetBoolean());
        Assert.True(windows[1].GetProperty("projection").GetProperty("reversed").GetBoolean());
        await page.EvaluateAsync("bounds=>chartControllers[1].setXWindow(bounds.minimum,bounds.maximum)", new { minimum = first.Origin + 4, maximum = first.Origin + 8 });
        await WaitForChangesAsync(page, 4);
        var secondReceipt = await page.EvaluateAsync<JsonElement>("()=>chartControllers.map(controller=>controller.getXWindow())");
        await CaptureAsync(page, "linked-native-second-commit", secondReceipt);
        Assert.Equal(JsonValueKind.Array, secondReceipt.ValueKind);
        var secondWindows = secondReceipt.EnumerateArray().ToArray();
        Assert.Equal(2, secondWindows.Length);
        Assert.All(secondWindows, window => AssertWindow(window, first.Origin + 4, first.Origin + 8));
        // A committed peer reply is silent; each authored request causes exactly one request in the other plot.
        Assert.Equal(4, await page.EvaluateAsync<int>("()=>nativeRequests.length"));
        Assert.Equal(3, first.PreparedCount); Assert.Equal(3, peer.PreparedCount);
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>nativeErrors.map(error=>error.message)"));
        await CaptureAsync(page, "linked-native-domains", windows);
        AssertNoConsoleErrors(session);
    }

    private static void AssertWindow(JsonElement projection, double minimum, double maximum, int precision = 8) {
        Assert.Equal(JsonValueKind.Object, projection.ValueKind);
        Assert.Equal(minimum, projection.GetProperty("minimum").GetDouble(), precision);
        Assert.Equal(maximum, projection.GetProperty("maximum").GetDouble(), precision);
    }
}
