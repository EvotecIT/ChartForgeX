using System.Text.Json;
using ChartForgeX.Core;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveNativeXViewportBrowserTests {
    [BrowserTheory]
    [InlineData("start", false)]
    [InlineData("start", true)]
    [InlineData("end", false)]
    [InlineData("end", true)]
    [InlineData("position", false)]
    [InlineData("position", true)]
    public async Task NativeRangeKeyboardUsesConsistentSourceStepsAndKeepsFocusAcrossCommittedScenes(string control, bool reversed) {
        if (!Enabled) return;
        var host = new NativeHost(kind: ChartSeriesKind.Candlestick, navigation: true, reversed: reversed);
        await using var session = await OpenAsync(host.Page(), reversed ? 390 : 900, 940);
        var page = session.Page;
        await ConnectAsync(page, host);
        await page.EvaluateAsync("()=>chartController.setXWindow(2,10)");
        await WaitForChangesAsync(page, 1);
        var initial = await SceneAsync(page);
        var selector = "[data-cfx-x-range=" + control + "]";
        await page.Locator(selector).FocusAsync();

        var firstStep = control == "position" ? .04 : .12;
        var pageStep = control == "position" ? .4 : 1.2;
        var count = 1;
        async Task PressAsync(string key, double delta) {
            var minimum = 2 + (control == "end" ? 0 : delta);
            var maximum = 10 + (control == "start" ? 0 : delta);
            await page.Keyboard.PressAsync(key);
            await WaitForChangesAsync(page, ++count);
            AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), minimum, maximum);
            AssertWindow(await page.EvaluateAsync<JsonElement>("()=>nativeRequests.at(-1).sourceWindow"), minimum, maximum);
            Assert.Equal(count, await page.EvaluateAsync<int>("()=>nativeRequests.length"));
            Assert.Equal("window", await page.EvaluateAsync<string>("()=>nativeRequests.at(-1).kind"));
            Assert.True(await page.Locator(selector).EvaluateAsync<bool>("input=>input===input.getRootNode().activeElement"));
            Assert.Equal(initial.GetProperty("yLabels").GetRawText(), (await SceneAsync(page)).GetProperty("yLabels").GetRawText());
            Assert.Equal("1.000", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        }

        await PressAsync(reversed ? "ArrowLeft" : "ArrowRight", firstStep);
        await PressAsync(reversed ? "ArrowRight" : "ArrowLeft", 0);
        await PressAsync("ArrowUp", firstStep);
        await PressAsync("ArrowDown", 0);
        await PressAsync("PageUp", pageStep);
        await PressAsync("PageDown", 0);
        if (control == "position") {
            await PressAsync("Home", -2);
            await PressAsync("End", 2);
        } else {
            await PressAsync(control == "start" ? "Home" : "End", control == "start" ? -2 : 2);
        }

        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Equal(count, await page.EvaluateAsync<int>("()=>nativeChanges.length"));
        Assert.Equal(count, await page.EvaluateAsync<int>("()=>nativeRequests.length"));
        Assert.True(await page.EvaluateAsync<bool>("()=>nativeCallbackFrozen.every(Boolean)"));
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>nativeErrors.map(error=>error.message)"));
        await CaptureAsync(page, "navigation-keyboard-" + control + "-" + reversed, await SceneAsync(page));
        AssertNoConsoleErrors(session);
    }
}
