using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveNativeXViewportBrowserTests {
    [BrowserFact]
    public async Task InteriorAxisRepliesShareExistingAbortSupersessionAndCommittedSceneOwnership() {
        if (!Enabled) return;
        var host = new NativeHost(configureChart: chart => { chart.Options.XAxis.CrossAt = 50; chart.Options.YAxis.CrossAt = 6; });
        await using var session = await OpenAsync(host.Page(), 980, 700);
        var page = session.Page; await ControlledConnectionAsync(page);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.WaitForFunctionAsync("()=>controlled.length===2");
        Assert.True(await page.EvaluateAsync<bool>("()=>controlled[0].signal.aborted&&!controlled[1].signal.aborted"));
        await page.EvaluateAsync("html=>controlled[1].resolve(html)", host.Window(7, 10)); await WaitForChangesAsync(page, 1);
        Assert.Equal(0, await page.Locator(".cfx-stage [data-cfx-role=axis-y-label]").CountAsync());
        Assert.Equal(1, await page.Locator(".cfx-stage [data-cfx-role=axis-crossing][data-cfx-crossing-axis=primary-x][data-cfx-visible=false]").CountAsync());
        var committed = await SceneAsync(page);
        await page.EvaluateAsync("html=>controlled[0].resolve(html)", host.Window(2, 8));
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Equal(committed.GetRawText(), (await SceneAsync(page)).GetRawText());
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await page.WaitForFunctionAsync("()=>controlled.length===3");
        await page.EvaluateAsync("()=>chartController.root.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}))");
        Assert.True(await page.EvaluateAsync<bool>("()=>controlled[2].signal.aborted"));
        await page.EvaluateAsync("html=>controlled[2].resolve(html)", host.Window(2, 8));
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Equal(committed.GetRawText(), (await SceneAsync(page)).GetRawText());
        await page.Locator("[data-cfx-reset]").ClickAsync(); await page.WaitForFunctionAsync("()=>controlled.length===4");
        await page.EvaluateAsync("html=>controlled[3].resolve(html)", host.Window(0, 12)); await WaitForChangesAsync(page, 2);
        Assert.True(await page.Locator(".cfx-stage [data-cfx-role=axis-y-label]").CountAsync() > 0);
        Assert.Equal(2, await page.Locator(".cfx-stage [data-cfx-role=axis-crossing]").CountAsync());
        await CaptureAsync(page, "interior-axis-cancellation-reset"); AssertNoConsoleErrors(session);
    }
}
