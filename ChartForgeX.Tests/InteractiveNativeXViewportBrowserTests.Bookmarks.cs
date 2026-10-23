using System.Text.Json;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveNativeXViewportBrowserTests {
    [BrowserFact]
    public async Task NativeBookmarkPublishesItsRestoredScenarioAndModeOnlyAfterTheAcceptedWindow() {
        if (!Enabled) return;
        var host = new NativeHost(scenarios: true);
        await using var session = await OpenAsync(host.Page(), 980, 720);
        var page = session.Page; await ConnectAsync(page, host);
        await page.EvaluateAsync("()=>chartController.setXWindow(2,10)"); await WaitForChangesAsync(page, 1);
        await page.EvaluateAsync("""
            ()=>{
                const root=chartController.root;
                root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{emit:false,sync:false,snapshot:{
                    mode:'pan',scenario:{id:'review',stepIndex:'0',playback:'paused'},selectedTargets:[]}}}));
                root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{source:'bookmark',sync:false}}));
                window.scenarioBookmark=JSON.parse(root.dataset.cfxInteractionSnapshot);
            }
            """);
        Assert.Equal("review", await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario"));
        Assert.Equal("pan", await page.Locator(Root).GetAttributeAsync("data-cfx-mode"));
        await page.EvaluateAsync("()=>chartController.setXWindow(4,8)"); await WaitForChangesAsync(page, 2);
        var beforeCommit = await page.EvaluateAsync<int>("""
            ()=>{
                const root=chartController.root;window.bookmarkCommits=[];
                root.addEventListener('cfxstateapplied',()=>bookmarkCommits.push({window:chartController.getXWindow(),
                    mode:root.dataset.cfxMode,scenario:root.dataset.cfxActiveScenario,step:root.dataset.cfxActiveScenarioStep}));
                root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:scenarioBookmark,sync:false}}));
                scenarioBookmark.mode='';scenarioBookmark.scenario.id='';
                return bookmarkCommits.length;
            }
            """);
        Assert.Equal(0, beforeCommit);
        await WaitForChangesAsync(page, 3);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 2, 10);
        Assert.Equal("pan", await page.Locator(Root).GetAttributeAsync("data-cfx-mode"));
        Assert.Equal("review", await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario"));
        Assert.Equal("0", await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario-step"));
        Assert.Equal("paused", await page.Locator(Root).GetAttributeAsync("data-cfx-scenario-playback"));
        var receipts = await page.EvaluateAsync<JsonElement>("()=>bookmarkCommits");
        var receipt = Assert.Single(receipts.EnumerateArray());
        AssertWindow(receipt.GetProperty("window"), 2, 10);
        Assert.Equal("pan", receipt.GetProperty("mode").GetString());
        Assert.Equal("review", receipt.GetProperty("scenario").GetString());
        await CaptureAsync(page, "scenario-bookmark-accepted", receipts);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SupersededAndRejectedNativeBookmarksCannotApplyTheirScenarioOrPublishCompletion() {
        if (!Enabled) return;
        var host = new NativeHost(scenarios: true);
        await using var session = await OpenAsync(host.Page(), 980, 720);
        var page = session.Page; await ControlledConnectionAsync(page);
        await page.EvaluateAsync("""
            ()=>{
                window.bookmarkNotifications=0;
                chartController.root.addEventListener('cfxstateapplied',()=>bookmarkNotifications++);
                window.replayBookmark=()=>chartController.root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{sync:false,snapshot:{
                    nativeViewport:{x:{minimum:2,maximum:10,scale:'linear'}},mode:'pan',
                    scenario:{id:'review',stepIndex:'0',playback:'paused'},selectedTargets:[]}}}));
                replayBookmark();
            }
            """);
        await page.WaitForFunctionAsync("()=>controlled.length===1");
        await page.EvaluateAsync("()=>chartController.setXWindow(4,8)");
        await page.WaitForFunctionAsync("()=>controlled.length===2");
        Assert.True(await page.EvaluateAsync<bool>("()=>controlled[0].signal.aborted"));
        await page.EvaluateAsync("html=>controlled[0].resolve(html)", host.Window(2, 10));
        await page.EvaluateAsync("html=>controlled[1].resolve(html)", host.Window(4, 8));
        await WaitForChangesAsync(page, 1);
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 4, 8);
        Assert.NotEqual("pan", await page.Locator(Root).GetAttributeAsync("data-cfx-mode"));
        Assert.NotEqual("review", await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario"));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>bookmarkNotifications"));
        await page.EvaluateAsync("()=>replayBookmark()");
        await page.WaitForFunctionAsync("()=>controlled.length===3");
        await page.EvaluateAsync("()=>controlled[2].reject(new Error('The host could not prepare this window.'))");
        await page.WaitForFunctionAsync("()=>nativeErrors.length===1");
        AssertWindow(await page.EvaluateAsync<JsonElement>("()=>chartController.getXWindow()"), 4, 8);
        Assert.NotEqual("pan", await page.Locator(Root).GetAttributeAsync("data-cfx-mode"));
        Assert.NotEqual("review", await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario"));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>bookmarkNotifications"));
        AssertNoConsoleErrors(session);
    }
}
