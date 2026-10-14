using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveNativeXViewportBrowserTests {
    [BrowserFact]
    public async Task NativeRepliesCommitOnlyTheLatestRequestForTheMatchingSceneRevision() {
        if (!Enabled) return;
        var host = new NativeHost();
        await using var session = await OpenAsync(host.Page(), 980, 680);
        var page = session.Page; await ControlledConnectionAsync(page);
        var middle = host.Window(2, 8); var stale = host.Window(3, 9); var narrow = host.Window(4, 6);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.WaitForFunctionAsync("()=>controlled.length===2");
        Assert.True(await page.EvaluateAsync<bool>("()=>controlled[0].signal.aborted&&!controlled[1].signal.aborted"));
        Assert.True(await page.EvaluateAsync<bool>("()=>controlled[0].request.sceneRevision===controlled[1].request.sceneRevision&&controlled[0].request.requestId!==controlled[1].request.requestId"));
        await page.EvaluateAsync("html=>controlled[1].resolve(html)", middle); await WaitForChangesAsync(page, 1);
        var committed = await SceneAsync(page);
        await page.EvaluateAsync("html=>controlled[0].resolve(html)", stale);
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Equal(committed.GetRawText(), (await SceneAsync(page)).GetRawText());
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.WaitForFunctionAsync("()=>controlled.length===3");
        Assert.Equal(committed.GetProperty("revision").GetString(), await page.EvaluateAsync<string>("()=>controlled[2].request.sceneRevision"));
        await page.EvaluateAsync("html=>controlled[2].resolve(html)", narrow); await WaitForChangesAsync(page, 2);
        Assert.Equal(4, (await SceneAsync(page)).GetProperty("projection").GetProperty("minimum").GetDouble());
        await CaptureAsync(page, "out-of-order", await page.EvaluateAsync<JsonElement>("()=>({requests:controlled.map(item=>item.request),changes:nativeChanges})"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("update")]
    [InlineData("reconnect")]
    [InlineData("disconnect")]
    [InlineData("dispose")]
    [InlineData("remove")]
    [InlineData("escape")]
    public async Task PendingNativeRepliesAbortOnSceneConnectionAndLifetimeChanges(string action) {
        if (!Enabled) return;
        var host = new NativeHost();
        await using var session = await OpenAsync(host.Page(), 980, 680);
        var page = session.Page; await ControlledConnectionAsync(page);
        var window = host.Window(2, 8);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await page.WaitForFunctionAsync("()=>controlled.length===1");
        await page.EvaluateAsync("""
            ({action,initial})=>{
                if(action==='update')chartController.update(initial);
                else if(action==='reconnect')chartController.connectXViewport(()=>new Promise(()=>{}));
                else if(action==='disconnect')chartController.connectXViewport(null);
                else if(action==='dispose')chartController.dispose();
                else if(action==='remove')chartController.root.remove();
                else chartController.root.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}));
            }
            """, new { action, initial = host.Initial });
        await page.WaitForFunctionAsync("()=>controlled[0].signal.aborted");
        await page.EvaluateAsync("html=>controlled[0].resolve(html)", window);
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>nativeChanges.length"));
        Assert.Equal(0, await page.EvaluateAsync<double>("()=>JSON.parse(chartController.root.dataset.cfxPreparedChart).xProjection.minimum"));
        if (action == "remove") {
            Assert.True(await page.EvaluateAsync<bool>("""
                ()=>{const old=chartController;document.body.appendChild(old.root);window.chartController=ChartForgeXCharts.mount(old.root);return old!==chartController;}
                """));
        }
        await CaptureAsync(page, "abort-" + action, new { action, aborted = true });
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("reject")]
    [InlineData("invalid-html")]
    [InlineData("chart-id")]
    [InlineData("missing-projection")]
    [InlineData("invalid-projection")]
    public async Task FailedNativeRepliesLeaveTheCommittedSceneAndExportWorking(string failure) {
        if (!Enabled) return;
        var host = new NativeHost();
        await using var session = await OpenAsync(host.Page(), 980, 680);
        var page = session.Page; await ControlledConnectionAsync(page);
        var before = await SceneAsync(page);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await page.WaitForFunctionAsync("()=>controlled.length===1");
        await page.EvaluateAsync("""
            ({failure,html})=>{
                if(failure==='reject'){controlled[0].reject(new Error('Host preparation failed'));return;}
                if(failure==='invalid-html'){controlled[0].resolve('<p>Invalid</p>');return;}
                const template=document.createElement('template');template.innerHTML=html;const section=template.content.firstElementChild;
                if(failure==='chart-id')section.dataset.cfxChartId='another-chart';
                else {
                    const facts=JSON.parse(section.dataset.cfxPreparedChart);
                    if(failure==='missing-projection')delete facts.xProjection;
                    else facts.xProjection.maximum=facts.xProjection.minimum;
                    section.dataset.cfxPreparedChart=JSON.stringify(facts);
                }
                controlled[0].resolve(section.outerHTML);
            }
            """, new { failure, html = host.Window(2, 8) });
        await page.WaitForFunctionAsync("()=>nativeErrors.length===1");
        Assert.Equal(before.GetRawText(), (await SceneAsync(page)).GetRawText());
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>nativeChanges.length"));
        await page.Locator(Middle).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Middle).GetAttributeAsync("aria-selected"));
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The retained scene cannot export.");
        Assert.Contains("sample-0", await File.ReadAllTextAsync(path));
        await CaptureAsync(page, "failed-" + failure, await page.EvaluateAsync<JsonElement>("()=>nativeErrors"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnsupportedAndEmptyConnectedScenesDisableNativeControlsUntilSupportedReplacement(bool empty) {
        if (!Enabled) return;
        var host = new NativeHost();
        var unsupported = empty ? Chart.Create().AddLine("Empty", Array.Empty<ChartPoint>())
            : Chart.Create().AddBar("Bars", new[] { new ChartPoint(0, 3), new ChartPoint(12, 8) });
        var section = new HtmlInteractiveChartRenderer().PrepareSection(unsupported, configure: Configure);
        await using var session = await OpenAsync(host.Page(), 980, 680);
        var page = session.Page; await ControlledConnectionAsync(page);
        await page.EvaluateAsync("html=>chartController.update(html)", section.Html);
        Assert.True(await page.Locator("[data-cfx-zoom=in]").IsDisabledAsync());
        Assert.True(await page.Locator("[data-cfx-mode-button=pan]").IsDisabledAsync());
        await page.Locator(".cfx-stage").DispatchEventAsync("wheel", new { deltaY = -100 });
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>controlled.length"));
        Assert.Equal("1.000", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        await page.EvaluateAsync("html=>chartController.update(html)", host.Initial);
        Assert.False(await page.Locator("[data-cfx-zoom=in]").IsDisabledAsync());
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await page.WaitForFunctionAsync("()=>controlled.length===1");
        await CaptureAsync(page, "capability-" + empty);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task StandaloneControlsKeepGeometricZoomAndDisconnectDoesNotPretendToResetANativeDomain() {
        if (!Enabled) return;
        var host = new NativeHost();
        await using var session = await OpenAsync(host.Page(), 980, 680);
        var page = session.Page;
        await page.EvaluateAsync("()=>window.chartController=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'))");
        var initial = await SceneAsync(page);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        Assert.Equal("1.250", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        Assert.Equal(initial.GetProperty("xLabels").GetRawText(), (await SceneAsync(page)).GetProperty("xLabels").GetRawText());
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Equal("1.000", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        await ControlledConnectionAsync(page);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await page.WaitForFunctionAsync("()=>controlled.length===1");
        await page.EvaluateAsync("html=>controlled[0].resolve(html)", host.Window(2, 8)); await WaitForChangesAsync(page, 1);
        await page.EvaluateAsync("()=>chartController.connectXViewport(null)");
        Assert.True(await page.Locator("[data-cfx-reset]").EvaluateAsync<bool>("button=>button.hidden"));
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        Assert.Equal("1.250", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        Assert.Equal(2, (await SceneAsync(page)).GetProperty("projection").GetProperty("minimum").GetDouble());
        await CaptureAsync(page, "standalone");
        AssertNoConsoleErrors(session);
    }

    private static Task ControlledConnectionAsync(IPage page) => page.EvaluateAsync("""
        ()=>{
            window.chartController=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));
            window.controlled=[];window.nativeChanges=[];window.nativeErrors=[];
            chartController.root.addEventListener('cfxviewportchange',event=>nativeChanges.push(event.detail));
            chartController.root.addEventListener('cfxviewporterror',event=>nativeErrors.push(event.detail));
            chartController.connectXViewport((request,signal)=>new Promise((resolve,reject)=>controlled.push({request,signal,resolve,reject})));
        }
        """);
}
