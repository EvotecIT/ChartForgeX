using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveNativeXViewportBrowserTests {
    private const string Navigation = "[data-cfx-x-navigation]";

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Candlestick, false, false, 900, false)]
    [InlineData(ChartSeriesKind.Ohlc, true, true, 390, false)]
    [InlineData(ChartSeriesKind.RangeBand, true, false, 900, false)]
    [InlineData(ChartSeriesKind.RangeArea, false, true, 390, true)]
    [InlineData(ChartSeriesKind.DifferenceArea, true, true, 900, false)]
    public async Task NativeRangeControlsCarryTheCompleteWorkflowAcrossTupleFamilies(ChartSeriesKind kind, bool time, bool reversed, int width, bool adaptive) {
        if (!Enabled) return;
        var host = new NativeHost(kind: kind, time: time, dark: reversed, reversed: reversed, navigation: true, adaptive: adaptive);
        await using var session = await OpenAsync(host.Page(), width, 940);
        var page = session.Page;
        Assert.True(await page.Locator(Navigation).IsHiddenAsync());
        await ConnectAsync(page, host);
        if (adaptive) await page.WaitForFunctionAsync("()=>chartController.root.dataset.cfxAdaptiveDrawing==='narrow'");
        if (adaptive) await page.EvaluateAsync("()=>{window.navigationCommitDrawings=[];chartController.root.addEventListener('cfxviewportchange',()=>navigationCommitDrawings.push(chartController.root.dataset.cfxAdaptiveDrawing));}");
        Assert.True(await page.Locator(Navigation).IsVisibleAsync());
        var initial = await SceneAsync(page);
        var overview = await page.Locator(".cfx-x-navigation__overview image").GetAttributeAsync("href");
        Assert.Equal(3, await page.Locator(Navigation + " input[type=range]").CountAsync());
        Assert.True(await page.Locator("[data-cfx-x-range=position]").IsDisabledAsync());
        await page.Locator("[data-cfx-x-preset='4']").ClickAsync(); await WaitForChangesAsync(page, 1);
        if (adaptive) await page.WaitForFunctionAsync("()=>chartController.root.dataset.cfxAdaptiveDrawing==='narrow'");
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), host.Origin + 8, host.Origin + 12);
        Assert.Equal(initial.GetProperty("yLabels").GetRawText(), (await SceneAsync(page)).GetProperty("yLabels").GetRawText());
        Assert.Equal(overview, await page.Locator(".cfx-x-navigation__overview image").GetAttributeAsync("href"));
        Assert.Equal("true", await page.Locator("[data-cfx-x-preset='4']").GetAttributeAsync("aria-pressed"));
        Assert.Equal("1.000", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        Assert.Equal("false", await page.Locator(First).GetAttributeAsync("data-cfx-visible"));
        Assert.Equal(0, await page.Locator(First + " [data-cfx-browser-hit-area]").CountAsync());
        await CaptureAsync(page, "navigation-preset-" + kind + "-" + width, await SceneAsync(page));

        var end = page.Locator("[data-cfx-x-range=end]");
        await end.FocusAsync();
        await page.Keyboard.PressAsync(reversed ? "ArrowRight" : "ArrowLeft"); await WaitForChangesAsync(page, 2);
        var shortened = await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()");
        Assert.Equal(host.Origin + 8, shortened.GetProperty("minimum").GetDouble(), 8);
        Assert.InRange(shortened.GetProperty("maximum").GetDouble(), host.Origin + 8.01, host.Origin + 11.99);
        Assert.True(await page.Locator("[data-cfx-x-range=end]").EvaluateAsync<bool>("input=>input===input.getRootNode().activeElement"));
        Assert.False(string.IsNullOrEmpty(await page.Locator("[data-cfx-x-range=end]").GetAttributeAsync("aria-valuetext")));
        await page.Locator("[data-cfx-x-range=start]").FocusAsync();
        await page.Keyboard.PressAsync(reversed ? "ArrowLeft" : "ArrowRight"); await WaitForChangesAsync(page, 3);
        var narrowed = await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()");
        Assert.InRange(narrowed.GetProperty("minimum").GetDouble(), host.Origin + 8.01, shortened.GetProperty("maximum").GetDouble() - .01);
        Assert.Equal(shortened.GetProperty("maximum").GetDouble(), narrowed.GetProperty("maximum").GetDouble(), 8);
        var span = narrowed.GetProperty("maximum").GetDouble() - narrowed.GetProperty("minimum").GetDouble();
        await page.Locator("[data-cfx-x-range=position]").FocusAsync(); await page.Keyboard.PressAsync("Home"); await WaitForChangesAsync(page, 4);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), host.Origin, host.Origin + span);

        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No committed export was downloaded.");
        var exported = XDocument.Load(path);
        Assert.DoesNotContain(exported.Descendants(), node => (string?)node.Attribute("data-cfx-x-navigation") != null);
        var exportedX = exported.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "axis-x-label").Select(node => node.Value);
        Assert.Equal((await SceneAsync(page)).GetProperty("xLabels").EnumerateArray().Select(value => value.GetString()), exportedX);
        await page.Locator("[data-cfx-x-full]").ClickAsync(); await WaitForChangesAsync(page, 5);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), host.Origin, host.Origin + 12);
        Assert.Equal("true", await page.Locator("[data-cfx-x-full]").GetAttributeAsync("aria-pressed"));
        Assert.True(await page.Locator("[data-cfx-x-range=position]").IsDisabledAsync());
        if (adaptive) {
            var drawings = await page.EvaluateAsync<string[]>("()=>navigationCommitDrawings");
            Assert.Equal(5, drawings.Length);
            Assert.All(drawings, drawing => Assert.Equal("narrow", drawing));
        }
        await CaptureAsync(page, "navigation-full-" + kind + "-" + width, await SceneAsync(page));
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>nativeErrors.map(error=>error.message)"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task NavigatorDragSelectsNativeSourceRangeAndSynchronizesAReversedPeer() {
        if (!Enabled) return;
        var first = new NativeHost(kind: ChartSeriesKind.Candlestick, navigation: true, id: "navigation-first", group: "range-controls");
        var peer = new NativeHost(kind: ChartSeriesKind.RangeArea, navigation: true, reversed: true, width: 560, id: "navigation-peer", group: "range-controls");
        await using var session = await OpenAsync(first.Page(peer), 1000, 1500);
        var page = session.Page; await ConnectAsync(page, first, peer);
        var overview = page.Locator(".cfx-x-navigation__overview").First;
        var bounds = await overview.BoundingBoxAsync() ?? throw new InvalidOperationException("No navigator bounds.");
        var left = (float)(bounds.X + bounds.Width * .2); var right = (float)(bounds.X + bounds.Width * .8); var y = (float)(bounds.Y + bounds.Height / 2);
        await page.Mouse.MoveAsync(left, y); await page.Mouse.DownAsync(); await page.Mouse.MoveAsync(right, y, new MouseMoveOptions { Steps = 5 });
        Assert.Empty(await page.EvaluateAsync<JsonElement[]>("()=>nativeRequests"));
        await page.Mouse.UpAsync(); await WaitForChangesAsync(page, 2);
        var windows = (await page.EvaluateAsync<JsonElement>("()=>chartControllers.map(controller=>controller.getXWindow())")).EnumerateArray().ToArray();
        AssertWindow(windows[1], windows[0].GetProperty("minimum").GetDouble(), windows[0].GetProperty("maximum").GetDouble());
        Assert.InRange(windows[0].GetProperty("minimum").GetDouble(), 2.3, 2.5);
        Assert.InRange(windows[0].GetProperty("maximum").GetDouble(), 9.5, 9.7);
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>nativeRequests.length"));
        await CaptureAsync(page, "navigation-synchronized", windows);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task NavigatorFollowsCommittedRepliesAndHidesOnDisconnectOrDispose() {
        if (!Enabled) return;
        var host = new NativeHost(navigation: true);
        await using var session = await OpenAsync(host.Page(), 900, 900);
        var page = session.Page; await ControlledConnectionAsync(page);
        await page.Locator("[data-cfx-x-preset='4']").ClickAsync();
        await page.WaitForFunctionAsync("()=>controlled.length===1");
        Assert.Equal("true", await page.Locator(Navigation).GetAttributeAsync("aria-busy"));
        await page.Locator("[data-cfx-x-preset='8']").ClickAsync();
        await page.WaitForFunctionAsync("()=>controlled.length===2");
        Assert.True(await page.EvaluateAsync<bool>("()=>controlled[0].signal.aborted"));
        await page.EvaluateAsync("html=>controlled[1].resolve(html)", host.Window(4, 12)); await WaitForChangesAsync(page, 1);
        await page.EvaluateAsync("html=>controlled[0].resolve(html)", host.Window(8, 12));
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 4, 12);
        Assert.Equal("true", await page.Locator("[data-cfx-x-preset='8']").GetAttributeAsync("aria-pressed"));
        await page.Locator("[data-cfx-x-preset='4']").ClickAsync(); await page.WaitForFunctionAsync("()=>controlled.length===3");
        await page.EvaluateAsync("()=>controlled[2].reject(new Error('Host preparation failed'))");
        await page.WaitForFunctionAsync("()=>nativeErrors.length===1");
        Assert.Equal("false", await page.Locator(Navigation).GetAttributeAsync("aria-busy"));
        Assert.Contains("could not be updated", await page.Locator("[data-cfx-x-status]").InnerTextAsync());
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 4, 12);
        await page.EvaluateAsync("html=>chartController.update(html)", host.Window(2, 7));
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 2, 7);
        Assert.Equal("false", await page.Locator("[data-cfx-x-preset='8']").GetAttributeAsync("aria-pressed"));
        await page.EvaluateAsync("()=>chartController.connectXViewport(null)");
        Assert.True(await page.Locator(Navigation).IsHiddenAsync());
        await page.EvaluateAsync("()=>chartController.connectXViewport(()=>new Promise(()=>{}))");
        Assert.True(await page.Locator(Navigation).IsVisibleAsync());
        await page.EvaluateAsync("()=>chartController.dispose()");
        Assert.True(await page.Locator(Navigation).IsHiddenAsync());
        AssertNoConsoleErrors(session);
    }
}
