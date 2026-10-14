using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserFact]
    public async Task DocumentedScenarioCommandsDriveVisibleControlsAndRespectDisabledFeatures() {
        if (!Enabled) return;
        void Configure(HtmlChartInteractionOptions options, bool enabled) {
            Fit(options);
            options.Interaction.AddScenario("review", "Measurement review", scenario => scenario.WithPlayback(30000, loop: true)
                .AddSeriesStep("samples", "Measured samples").AddSeriesStep("reference", "Reference line"));
            if (!enabled) options.Interaction.Disable(ChartInteractionFeatures.Scenarios | ChartInteractionFeatures.StepPlayback);
        }
        await using var session = await OpenAsync(Readings().ToInteractiveHtmlPage(options => Configure(options, true)), 820, 780);
        var page = session.Page;
        await BindControllerAsync(page);
        await page.Locator(Root).EvaluateAsync("""
            root=>{
                window.scenarioCommandEvents=[];
                for(const name of ['cfxscenario','cfxscenarioclear','cfxscenariostep','cfxscenarioplayback'])
                    root.addEventListener(name,event=>scenarioCommandEvents.push({name,detail:event.detail}));
            }
            """);
        await ScenarioCommandAsync(page, "cfx-set-scenario", new { scenarioId = "review" });
        Assert.Equal("review", await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario"));
        Assert.Equal("true", await page.Locator("[data-cfx-scenario=review]").GetAttributeAsync("aria-pressed"));
        await ScenarioCommandAsync(page, "cfx-set-scenario-step", new { scenarioId = "review", index = 1 });
        Assert.Equal("1", await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario-step"));
        Assert.Equal("step", await page.Locator("[data-cfx-scenario-step-index='2']").GetAttributeAsync("aria-current"));
        Assert.True(await page.Locator("[data-cfx-series-key=reference].cfx-scenario-step-active").CountAsync() > 0);
        await CaptureAsync(page, "review-host-scenario-step", await page.EvaluateAsync<JsonElement>("()=>scenarioCommandEvents"));
        await ScenarioCommandAsync(page, "cfx-play-scenario", new { scenarioId = "review" });
        Assert.Equal("playing", await page.Locator(Root).GetAttributeAsync("data-cfx-scenario-playback"));
        Assert.Equal("Pause", await page.Locator("[data-cfx-scenario-step-control=play]").InnerTextAsync());
        await ScenarioCommandAsync(page, "cfx-pause-scenario");
        Assert.Equal("paused", await page.Locator(Root).GetAttributeAsync("data-cfx-scenario-playback"));
        Assert.Equal("Play", await page.Locator("[data-cfx-scenario-step-control=play]").InnerTextAsync());
        await ScenarioCommandAsync(page, "cfx-clear-scenario-step");
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario-step"));
        Assert.Equal(0, await page.Locator(".cfx-scenario-step-active").CountAsync());
        await ScenarioCommandAsync(page, "cfx-clear-scenario");
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario"));
        Assert.Equal("false", await page.Locator("[data-cfx-scenario=review]").GetAttributeAsync("aria-pressed"));
        var events = await page.EvaluateAsync<JsonElement>("()=>scenarioCommandEvents");
        Assert.Contains(events.EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxscenario");
        Assert.Contains(events.EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxscenarioclear");
        Assert.Contains(events.EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxscenariostep" && item.GetProperty("detail").GetProperty("index").GetInt32() == 1);
        Assert.Contains(events.EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxscenarioplayback" && item.GetProperty("detail").GetProperty("state").GetString() == "playing");
        await UpdateAsync(page, Readings().ToInteractiveHtmlFragmentWithoutAssets(options => Configure(options, false)));
        await page.EvaluateAsync("()=>scenarioCommandEvents=[]");
        foreach (var command in new[] { "cfx-set-scenario", "cfx-set-scenario-step", "cfx-play-scenario", "cfx-pause-scenario", "cfx-clear-scenario-step", "cfx-clear-scenario" })
            await ScenarioCommandAsync(page, command, new { scenarioId = "review", index = 1 });
        Assert.Equal(0, await page.Locator("[data-cfx-scenario],[data-cfx-scenario-step-control]").CountAsync());
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-active-scenario"));
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>scenarioCommandEvents.map(item=>item.name)"));
        await CaptureAsync(page, "review-host-scenario-disabled", events);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task BookmarkCommandsCaptureAndReplayNativeSelectionAndViewportOnlyWhenEnabled() {
        if (!Enabled) return;
        void Configure(HtmlChartInteractionOptions options, bool bookmarks) {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
            if (bookmarks) options.Interaction.Enable(ChartInteractionFeatures.StateBookmarks);
        }
        await using var session = await OpenAsync(Readings().ToInteractiveHtmlPage(options => Configure(options, true)), 820, 680);
        var page = session.Page;
        await BindControllerAsync(page);
        await page.Locator(Root).EvaluateAsync("""
            root=>{
                window.bookmarkEvents=[];window.bookmarkApplied=[];
                root.addEventListener('cfxstate',event=>bookmarkEvents.push(event.detail));
                root.addEventListener('cfxstateapplied',event=>bookmarkApplied.push(event.detail));
            }
            """);
        await SelectAsync(page, North);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        var stage = await BoxAsync(page, ".cfx-stage");
        await page.Mouse.MoveAsync((float)(stage.X + stage.Width * .4), (float)(stage.Y + stage.Height * .4));
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(stage.X + stage.Width * .4 + 24), (float)(stage.Y + stage.Height * .4 + 16), new MouseMoveOptions { Steps = 4 });
        await page.Mouse.UpAsync();
        await ScenarioCommandAsync(page, "cfx-capture-state", new { source = "review-bookmark", sync = false });
        var snapshot = await page.EvaluateAsync<JsonElement>("()=>bookmarkEvents[0].snapshot");
        Assert.Equal("north", Assert.Single(snapshot.GetProperty("selectedTargets").EnumerateArray()).GetProperty("observationId").GetString());
        Assert.True(snapshot.GetProperty("viewport").GetProperty("zoom").GetDouble() > 1);
        Assert.NotEqual(0, snapshot.GetProperty("viewport").GetProperty("panX").GetDouble());
        await CaptureAsync(page, "review-bookmark-captured", snapshot);
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Equal(0, await page.Locator("[aria-selected=true]").CountAsync());
        Assert.Equal("1.000", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        await page.Locator(Root).EvaluateAsync("root=>root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:bookmarkEvents[0].snapshot,sync:false}}))");
        Assert.Equal("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
        Assert.True(await page.Locator(Root).EvaluateAsync<bool>("root=>Number(root.dataset.cfxZoom)===bookmarkEvents[0].snapshot.viewport.zoom && Number(root.dataset.cfxPanX)===bookmarkEvents[0].snapshot.viewport.panX"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>bookmarkApplied.length"));
        await CaptureAsync(page, "review-bookmark-restored", snapshot);
        await UpdateAsync(page, Readings().ToInteractiveHtmlFragmentWithoutAssets(options => Configure(options, false)));
        var currentZoom = await page.Locator(Root).GetAttributeAsync("data-cfx-zoom");
        await ScenarioCommandAsync(page, "cfx-capture-state", new { source = "disabled-bookmark", sync = false });
        await ScenarioCommandAsync(page, "cfx-apply-state", new { snapshot = new { viewport = new { zoom = 2, panX = 0, panY = 0 }, selectedTargets = Array.Empty<object>() }, sync = false });
        Assert.Equal(currentZoom, await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        Assert.Equal("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>bookmarkEvents.length"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>bookmarkApplied.length"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task NativeWheelZoomsAndCanceledTouchBrushReleasesUncommittedPresentation() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings(dark: true).ToInteractiveHtmlPage(options => {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Brush);
        }), 820, 680);
        var page = session.Page;
        await page.Locator(Root).EvaluateAsync("""
            root=>{
                window.nativeInput={viewport:[],brush:[],lasso:[],canceled:[],pointerId:null};
                for(const name of ['cfxviewport','cfxbrush','cfxlasso'])root.addEventListener(name,event=>nativeInput[name.slice(3)].push(event.detail));
                root.addEventListener('pointerdown',event=>nativeInput.pointerId=event.pointerId,true);
                root.addEventListener('pointercancel',event=>nativeInput.canceled.push({trusted:event.isTrusted,type:event.pointerType,id:event.pointerId}),true);
            }
            """);
        var stage = await BoxAsync(page, ".cfx-stage");
        await page.Mouse.MoveAsync((float)(stage.X + stage.Width * .5), (float)(stage.Y + stage.Height * .5));
        await page.Mouse.WheelAsync(0, -120);
        await page.WaitForFunctionAsync("()=>Number(document.querySelector('.cfx-interactive-chart').dataset.cfxZoom)>1");
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>nativeInput.viewport.length"));
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        var mark = await BoxAsync(page, North);
        var x = mark.X + mark.Width / 2; var y = mark.Y + mark.Height / 2;
        var cdp = await page.Context.NewCDPSessionAsync(page); var contactActive = false;
        Task TouchAsync(string type, params object[] contacts) => cdp.SendAsync("Input.dispatchTouchEvent",
            new Dictionary<string, object> { ["type"] = type, ["touchPoints"] = contacts });
        try {
            await cdp.SendAsync("Emulation.setTouchEmulationEnabled", new Dictionary<string, object> { ["enabled"] = true, ["maxTouchPoints"] = 1 });
            await TouchAsync("touchStart", new { id = 1, x, y }); contactActive = true;
            await page.WaitForFunctionAsync("()=>!document.querySelector('.cfx-tooltip').hidden");
            await TouchAsync("touchMove", new { id = 1, x = x + 32, y = y + 26 });
            Assert.False(await page.Locator(".cfx-brush-box").EvaluateAsync<bool>("node=>node.hidden"));
            await CaptureAsync(page, "review-native-touch-brush-active", await NativeInputReceiptAsync(page));
            await TouchAsync("touchCancel"); contactActive = false;
            await page.WaitForFunctionAsync("()=>nativeInput.canceled.length===1");
            var canceled = await NativeInputReceiptAsync(page);
            await CaptureAsync(page, "review-native-touch-brush-canceled", canceled);
            Assert.True(canceled.GetProperty("canceled")[0].GetProperty("trusted").GetBoolean());
            Assert.Equal("touch", canceled.GetProperty("canceled")[0].GetProperty("type").GetString());
            Assert.Equal(0, canceled.GetProperty("brushEvents").GetInt32());
            Assert.Equal(0, canceled.GetProperty("lassoEvents").GetInt32());
            Assert.Equal(0, canceled.GetProperty("selected").GetInt32());
            Assert.False(canceled.GetProperty("captured").GetBoolean());
            Assert.True(canceled.GetProperty("tipHidden").GetBoolean());
            Assert.True(canceled.GetProperty("guideHidden").GetBoolean());
            Assert.Empty(canceled.GetProperty("hover").GetString()!);
            Assert.True(canceled.GetProperty("brushHidden").GetBoolean(), "Canceled native brush retained its provisional rectangle: " + canceled.GetRawText());

            // Canceling a later gesture retains the previously committed brush and source selection.
            mark = await BoxAsync(page, North); x = mark.X + mark.Width / 2; y = mark.Y + mark.Height / 2;
            await page.Mouse.MoveAsync((float)(x - 18), (float)(y - 18));
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync((float)(x + 18), (float)(y + 18), new MouseMoveOptions { Steps = 5 });
            await page.Mouse.UpAsync();
            Assert.Equal("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
            var committed = await NativeInputReceiptAsync(page);
            var committedBox = await BoxAsync(page, ".cfx-brush-box");
            Assert.False(committed.GetProperty("brushHidden").GetBoolean());
            Assert.Equal(1, committed.GetProperty("brushEvents").GetInt32());
            Assert.Equal(1, committed.GetProperty("lassoEvents").GetInt32());
            await CaptureAsync(page, "review-native-touch-brush-committed", new { committed, committedBox });
            mark = await BoxAsync(page, North); x = mark.X + mark.Width / 2 + 60; y = mark.Y + mark.Height / 2 - 40;
            await TouchAsync("touchStart", new { id = 2, x, y }); contactActive = true;
            await TouchAsync("touchMove", new { id = 2, x = x + 35, y = y + 25 });
            await TouchAsync("touchCancel"); contactActive = false;
            await page.WaitForFunctionAsync("()=>nativeInput.canceled.length===2");
            var preserved = await NativeInputReceiptAsync(page);
            var preservedBox = await BoxAsync(page, ".cfx-brush-box");
            await CaptureAsync(page, "review-native-touch-brush-preserved", new { committed, committedBox, preserved, preservedBox });
            Assert.Equal("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
            Assert.Equal(committed.GetProperty("brushBounds").GetString(), preserved.GetProperty("brushBounds").GetString());
            Assert.Equal(1, preserved.GetProperty("brushEvents").GetInt32());
            Assert.Equal(1, preserved.GetProperty("lassoEvents").GetInt32());
            Assert.False(preserved.GetProperty("brushHidden").GetBoolean());
            Assert.Equal(committedBox.X, preservedBox.X, 1);
            Assert.Equal(committedBox.Y, preservedBox.Y, 1);
            Assert.Equal(committedBox.Width, preservedBox.Width, 1);
            Assert.Equal(committedBox.Height, preservedBox.Height, 1);
            AssertNoConsoleErrors(session);
        } finally {
            if (contactActive) await TouchAsync("touchCancel");
            await cdp.DetachAsync();
        }
    }

    private static Task ScenarioCommandAsync(IPage page, string name, object? detail = null) => page.Locator(Root).EvaluateAsync(
        "(root,command)=>root.dispatchEvent(new CustomEvent(command.name,{detail:command.detail}))", new { name, detail });

    private static Task<JsonElement> NativeInputReceiptAsync(IPage page) => page.Locator(Root).EvaluateAsync<JsonElement>("""
        root=>({zoom:root.dataset.cfxZoom,canceled:nativeInput.canceled,brushEvents:nativeInput.brush.length,lassoEvents:nativeInput.lasso.length,
            selected:root.querySelectorAll('[aria-selected=true]').length,brushHidden:root.querySelector('.cfx-brush-box').hidden,
            brushBounds:root.dataset.cfxBrush||'',tipHidden:root.querySelector('.cfx-tooltip').hidden,
            guideHidden:root.querySelector('.cfx-crosshair').hidden,hover:root.dataset.cfxHoverKey||'',
            captured:root.querySelector('.cfx-stage').hasPointerCapture(nativeInput.pointerId)})
        """);
}
