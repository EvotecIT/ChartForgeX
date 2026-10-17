using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData("controller")]
    [InlineData("event")]
    [InlineData("selection")]
    [InlineData("escape")]
    public async Task PublicBrushClearRemovesCommittedRangeAndModeWithoutEcho(string command) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings().ToInteractiveHtmlPage(options => {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Brush);
        }), 820, 700);
        var page = session.Page;
        await BindControllerAsync(page);
        await DrawLinkedBrushAsync(page);
        await page.EvaluateAsync("()=>chartController.select({observationIds:['north']})");
        Assert.False(await page.Locator(".cfx-brush-box").EvaluateAsync<bool>("box=>box.hidden"));
        Assert.False(string.IsNullOrEmpty(await page.Locator(Root).GetAttributeAsync("data-cfx-brush")));
        await page.Locator(Root).EvaluateAsync("""
            root=>{window.brushClearEvents=[];for(const name of ['cfxbrush','cfxbrushclear','cfxselectionchange','cfxsync'])
                root.addEventListener(name,event=>brushClearEvents.push({name,detail:event.detail}));}
            """);
        if (command == "escape") {
            await page.Locator(Root).EvaluateAsync("root=>{root.tabIndex=-1;root.focus()}");
            await page.Keyboard.PressAsync("Escape");
        } else await page.EvaluateAsync("""
            command=>{if(command==='controller')chartController.clearBrush();
                else if(command==='event')chartController.root.dispatchEvent(new CustomEvent('cfx-clear-brush'));
                else chartController.clearSelection();}
            """, command);
        var result = await page.Locator(Root).EvaluateAsync<JsonElement>("""
            root=>({hidden:root.querySelector('.cfx-brush-box').hidden,hasRange:root.hasAttribute('data-cfx-brush'),
                mode:root.dataset.cfxMode,pressed:root.querySelector('[data-cfx-mode-button=brush]').getAttribute('aria-pressed'),
                selected:chartController.getSelection().length,events:brushClearEvents})
            """);
        Assert.True(result.GetProperty("hidden").GetBoolean());
        Assert.False(result.GetProperty("hasRange").GetBoolean());
        Assert.Equal("", result.GetProperty("mode").GetString());
        Assert.Equal("false", result.GetProperty("pressed").GetString());
        Assert.Equal(command is "selection" or "escape" ? 0 : 1, result.GetProperty("selected").GetInt32());
        var events = result.GetProperty("events").EnumerateArray().ToArray();
        if (command == "escape") {
            Assert.Single(events, item => item.GetProperty("name").GetString() == "cfxbrushclear");
            Assert.Single(events, item => item.GetProperty("name").GetString() == "cfxselectionchange"
                && item.GetProperty("detail").GetProperty("reason").GetString() == "clear");
        } else Assert.Empty(events);
        await CaptureAsync(page, "brush-cleared-" + command, result);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ClearingAnActiveBrushPreventsPointerReleaseFromRecommittingItsRange() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings().ToInteractiveHtmlPage(options => {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Brush);
            options.Interaction.Disable(ChartInteractionFeatures.Selection);
        }), 820, 700);
        var page = session.Page;
        await BindControllerAsync(page);
        await page.Locator(Root).EvaluateAsync("root=>{window.brushCommits=0;root.addEventListener('cfxbrush',()=>brushCommits++)}");
        await DrawLinkedBrushAsync(page, release: false);
        await page.EvaluateAsync("()=>chartController.clearBrush({emit:true})");
        await page.Mouse.UpAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>brushCommits"));
        Assert.True(await page.Locator(".cfx-brush-box").EvaluateAsync<bool>("box=>box.hidden"));
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-brush"));
        Assert.Equal("", await page.Locator(Root).GetAttributeAsync("data-cfx-mode"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, 390, false)]
    [InlineData(true, 390, true)]
    [InlineData(false, 1440, true)]
    [InlineData(true, 1440, false)]
    public async Task LinkedSelectionKeepsHostHeightAndToolbarBandAligned(bool dark, int width, bool compare) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings(dark).ToInteractiveHtmlPage(options => {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Brush);
            if (!compare) options.Interaction.Disable(ChartInteractionFeatures.CompareMarkers);
        }), width, 800);
        var page = session.Page;
        await BindControllerAsync(page);
        var before = await BoxAsync(page, Root);
        var frame = await BoxAsync(page, ".cfx-frame");
        var toolbar = await BoxAsync(page, ".cfx-toolbar");
        Assert.InRange(Math.Abs(toolbar.X - frame.X), 0, .5);
        Assert.InRange(Math.Abs(toolbar.X + toolbar.Width - frame.X - frame.Width), 0, .5);
        var buttonBounds = await BoxAsync(page, ".cfx-toolbar .cfx-tool:last-child");
        Assert.InRange(Math.Abs(buttonBounds.X + buttonBounds.Width - toolbar.X - toolbar.Width), 0, .5);
        await page.EvaluateAsync("()=>chartController.select({observationIds:['north']})");
        Assert.Equal(before.Height, (await BoxAsync(page, Root)).Height, 1);
        Assert.True(await page.Locator("[data-cfx-reset]").IsVisibleAsync());
        Assert.Equal(compare, await page.Locator("[data-cfx-compare-tray]").CountAsync() == 1);
        await CaptureAsync(page, "linked-stable-height-" + dark + "-" + width + "-" + compare);
        await page.EvaluateAsync("()=>chartController.clearSelection()");
        Assert.Equal(before.Height, (await BoxAsync(page, Root)).Height, 1);
        Assert.False(await page.Locator("[data-cfx-reset]").IsVisibleAsync());
        AssertNoConsoleErrors(session);
    }

    private static async Task DrawLinkedBrushAsync(IPage page, bool release = true) {
        var positions = await page.Locator(Root).EvaluateAsync<JsonElement>("""
            root=>{const svg=root.querySelector('.cfx-stage svg'),p=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
                const screen=(x,y)=>{const point=new DOMPoint(p.x+x*p.width,p.y+y*p.height).matrixTransform(svg.getScreenCTM());return{x:point.x,y:point.y}};
                return{from:screen(.15,.2),to:screen(.85,.8)}}
            """);
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await page.Mouse.MoveAsync((float)positions.GetProperty("from").GetProperty("x").GetDouble(), (float)positions.GetProperty("from").GetProperty("y").GetDouble());
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)positions.GetProperty("to").GetProperty("x").GetDouble(), (float)positions.GetProperty("to").GetProperty("y").GetDouble(), new MouseMoveOptions { Steps = 3 });
        if (release) await page.Mouse.UpAsync();
    }
}
