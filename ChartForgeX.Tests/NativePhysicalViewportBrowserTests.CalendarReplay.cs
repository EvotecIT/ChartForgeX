using System.Text.Json;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class NativePhysicalViewportBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390, VisualThemeMode.Dark)]
    [InlineData(true, 900, VisualThemeMode.Light)]
    public async Task CalendarExclusiveEdgeSurvivesNativeBookmarkReplayAndKeyboardNavigation(bool reversed, int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var host = new TimeSpacingHost(true, reversed, width, mode);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 1000);
        var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.ExposeFunctionAsync("prepareDiscreteTime", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("""
            ()=>{
                window.timeController=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                window.timeRequests=[];window.timeChanges=[];window.timeErrors=[];window.timeBrush=[];window.calendarApplied=[];
                timeController.root.addEventListener('cfxviewportchange',event=>timeChanges.push(event.detail));
                timeController.root.addEventListener('cfxviewporterror',event=>timeErrors.push(event.detail));
                timeController.root.addEventListener('cfxbrush',event=>timeBrush.push(event.detail));
                timeController.root.addEventListener('cfxstateapplied',event=>calendarApplied.push(event.detail));
                timeController.connectNative(async(request,signal)=>{
                    timeRequests.push(request);const reply=await prepareDiscreteTime(request);
                    if(signal.aborted)throw new DOMException('Canceled','AbortError');return reply;
                },{basis:'initial',axes:['x']});
            }
            """);
        var february = TimeSpacingHost.February.ToOADate();
        var march = TimeSpacingHost.February.AddMonths(1).ToOADate();
        await page.EvaluateAsync("bounds=>timeController.setViewport({x:bounds})", new { minimum = february, maximum = march, maximumInclusive = false });
        var changes = 1;
        await TimeSpacingChanges(page, changes);
        var firstSvg = host.Last.PreparedVisual.ToSvg();
        var firstWindow = await page.EvaluateAsync<JsonElement>("()=>timeController.getViewport().x");
        Assert.False(firstWindow.GetProperty("maximumInclusive").GetBoolean());
        Assert.Equal(1, ProjectionSpan(await TimeSpacingScene(page)), 8);
        for (var round = 0; round < 3; round++) {
            await page.EvaluateAsync("""
                ()=>{
                    const root=timeController.root;
                    root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{source:'calendar-replay',sync:false}}));
                    window.calendarSaved=JSON.parse(root.dataset.cfxInteractionSnapshot);
                }
                """);
            Assert.Equal(2, await page.EvaluateAsync<int>("()=>calendarSaved.version"));
            Assert.False(await page.EvaluateAsync<bool>("()=>calendarSaved.nativeViewport.x.maximumInclusive"));
            await page.Locator("[data-cfx-x-full]").ClickAsync();
            await TimeSpacingChanges(page, ++changes);
            await page.EvaluateAsync("""
                ()=>timeController.root.dispatchEvent(new CustomEvent('cfx-apply-state',{
                    detail:{snapshot:JSON.parse(JSON.stringify(calendarSaved)),sync:false}}))
                """);
            await TimeSpacingChanges(page, ++changes);
            await page.WaitForFunctionAsync("count=>calendarApplied.length===count", round + 1);
            Assert.Equal(firstWindow.GetRawText(), (await page.EvaluateAsync<JsonElement>("()=>timeController.getViewport().x")).GetRawText());
            Assert.False(await page.EvaluateAsync<bool>("()=>timeRequests.at(-1).sourceViewport.x.maximumInclusive"));
            Assert.True(await page.EvaluateAsync<bool>("()=>Object.isFrozen(timeRequests.at(-1).sourceViewport.x)"));
            Assert.Equal(1, ProjectionSpan(await TimeSpacingScene(page)), 8);
            Assert.Equal(firstSvg, host.Last.PreparedVisual.ToSvg());
        }
        var beforeInvalid = await page.EvaluateAsync<int>("()=>timeRequests.length");
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>{try{timeController.setViewport({x:{minimum:45000,maximum:45001,maximumInclusive:'false'}});return false;}
                catch(error){return error instanceof TypeError;}}
            """));
        Assert.Equal(beforeInvalid, await page.EvaluateAsync<int>("()=>timeRequests.length"));
        var position = page.Locator("[data-cfx-x-range=position]");
        await position.FocusAsync(); await page.Keyboard.PressAsync("Home"); await TimeSpacingChanges(page, ++changes);
        var moved = await TimeSpacingScene(page);
        Assert.Equal(host.FullMinimum, moved.GetProperty("projection").GetProperty("minimum").GetDouble(), 8);
        Assert.Equal(1, ProjectionSpan(moved), 8);
        Assert.False(moved.GetProperty("projection").GetProperty("maximumInclusive").GetBoolean());
        Assert.True(await position.EvaluateAsync<bool>("input=>input===input.getRootNode().activeElement"));
        await CaptureTimeSpacing(page, host, "exclusive-calendar-replay");
        await page.EvaluateAsync("()=>timeController.dispose()");
        Assert.Empty(errors); Assert.Empty(await page.EvaluateAsync<JsonElement[]>("()=>timeErrors")); AssertNoConsoleErrors(session);
    }
}
