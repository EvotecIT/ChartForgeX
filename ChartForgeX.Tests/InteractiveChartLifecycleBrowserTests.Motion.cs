using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false, 390)]
    [InlineData(true, 900)]
    public async Task OptInUpdateMotionKeepsAcceptedFactsAndExportFinalWhileReplacementAndDisposalReleaseItsWork(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings(dark).ToInteractiveHtmlPage(Fit), width + 24, 760);
        var page = session.Page; await BindControllerAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        var next = Readings(dark, updated: true).ToInteractiveHtmlFragmentWithoutAssets(Fit);
        var observed = await page.EvaluateAsync<JsonElement>("""
            html => {
                chartController.update(html,{motion:{duration:1000}});
                window.updateSvg=chartController.root.querySelector('.cfx-stage svg');
                window.updateAnimation=updateSvg.getAnimations()[0]; updateAnimation.pause();
                return {active:updateSvg.dataset.cfxUpdateMotion,value:updateSvg.querySelector('[data-cfx-observation-id=north]').dataset.cfxY};
            }
            """, next);
        Assert.Equal("true", observed.GetProperty("active").GetString());
        Assert.Equal("17", observed.GetProperty("value").GetString());
        await page.EvaluateAsync("() => updateAnimation.currentTime=500");
        await CaptureAsync(page, "motion-midpoint-" + width + "-" + dark);
        await FocusWithKeyboardAsync(page, page.Locator(North)); await page.Keyboard.PressAsync("Space");
        Assert.Equal("17", await page.EvaluateAsync<string>("() => lifecycleSelections.at(-1).target.value"));
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The committed SVG did not download.");
        var svg = XDocument.Load(path);
        Assert.DoesNotContain(svg.Root!.DescendantsAndSelf(), element => element.Attribute("data-cfx-update-motion") != null);
        Assert.Equal("17", Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-observation-id") == "north" && (string?)element.Attribute("data-cfx-role") == "point").Attribute("data-cfx-y")?.Value);
        Assert.Equal("idle", await page.EvaluateAsync<string>("() => updateAnimation.playState"));
        await CaptureAsync(page, "motion-export-final-" + width + "-" + dark);
        await page.EvaluateAsync("html => {chartController.update(html,{motion:{duration:1000}}); window.oldMotion=chartController.root.querySelector('svg').getAnimations()[0]; oldMotion.pause(); chartController.update(html);}", next);
        Assert.Equal("idle", await page.EvaluateAsync<string>("() => oldMotion.playState"));
        Assert.Equal(0, await page.Locator("[data-cfx-update-motion]").CountAsync());
        await page.EvaluateAsync("html => {chartController.update(html,{motion:{duration:1000}});window.disposedMotion=chartController.root.querySelector('svg').getAnimations()[0]; disposedMotion.pause();chartController.dispose();}", next);
        Assert.Equal("idle", await page.EvaluateAsync<string>("() => disposedMotion.playState"));
        Assert.Equal(0, await page.Locator("[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task ReducedMotionAndInvalidMotionOptionsCannotLeavePresentationWorkOrMutateTheCommittedScene() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings().ToInteractiveHtmlPage(Fit));
        var page = session.Page; await BindControllerAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        var next = Readings(updated: true).ToInteractiveHtmlFragmentWithoutAssets(Fit);
        Assert.True(await page.EvaluateAsync<bool>("""
            html => {
                chartController.update(html,{motion:{duration:1000}});
                return !chartController.root.querySelector('[data-cfx-update-motion]') && chartController.root.querySelector('svg').getAnimations().length===0;
            }
            """, next));
        var rejected = await page.EvaluateAsync<JsonElement>("""
            html => {
                const before=chartController.root.innerHTML,revision=chartController.sceneRevision;
                const invalid=[{duration:-1},{duration:1001},{duration:Infinity},{duration:'200'},true];
                return {errors:invalid.map(motion=>{try{chartController.update(html,{motion});return false;}catch(error){return error instanceof TypeError;}}),
                    same:before===chartController.root.innerHTML&&revision===chartController.sceneRevision};
            }
            """, next);
        Assert.All(rejected.GetProperty("errors").EnumerateArray(), error => Assert.True(error.GetBoolean()));
        Assert.True(rejected.GetProperty("same").GetBoolean());
        await CaptureAsync(page, "motion-reduced-immediate"); AssertNoConsoleErrors(session);
    }
}
