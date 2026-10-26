using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class NativeTransitionBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Pie, 390, true)] [InlineData(ChartSeriesKind.Sunburst, 760, false)]
    [InlineData(ChartSeriesKind.Sankey, 390, false)] [InlineData(ChartSeriesKind.RegionMap, 760, true)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task WholeVisualNativeInitialFadeKeepsAcceptedDataRevisionHistoryExportAndOneExistingLifetime(ChartSeriesKind kind, int width, bool dark) {
        if (!Enabled) return;
        // A full US map at both adaptive widths exceeds the documented finite transport
        // budget. The maintained three-region recipe exercises the same native map fade
        // without changing that limit or discarding either captured drawing.
        var chart = (kind == ChartSeriesKind.RegionMap ? SeamPolygonMapRecipe.Create()
            : Assert.IsType<Chart>(ExampleRecipes.ForKind(kind).Create())).WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var renderer = new HtmlInteractiveChartRenderer(); var final = renderer.PrepareSection(chart, configure: options => {
            // The supplied Carlito browser face keeps this dual-drawing map payload finite;
            // native geometry and all SVG/PNG frame captures are unchanged.
            if (kind == ChartSeriesKind.RegionMap) options.TextPresentation = HtmlChartTextPresentation.Browser;
            options.Interaction.ChartId = "initial-fade-native"; options.Interaction.Enable(ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 420;
        });
        var payload = renderer.PrepareNativeTransitionJson(final, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade, 3));
        await using var session = await BrowserProofFonts.OpenAsync(Page(final), width + 24, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.nativeChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.fadeEvents=[];for(const name of ['cfxupdated','cfxprogresschange'])nativeChart.root.addEventListener(name,event=>fadeEvents.push({name,detail:event.detail}));window.fadeRevision=nativeChart.sceneRevision;window.fadeTargets=nativeChart.resolveTargets().length;}");
        await page.EvaluateAsync("payload=>nativeChart.presentInitial(payload,{sceneRevision:nativeChart.sceneRevision,motion:{duration:1000}})", payload); await page.Clock.RunForAsync(550);
        var observed = await page.EvaluateAsync<JsonElement>("()=>({revision:nativeChart.sceneRevision,targets:nativeChart.resolveTargets().length,events:fadeEvents,frame:document.querySelector('.cfx-native-motion-frame')?.outerHTML,opacity:document.querySelector('.cfx-native-motion-frame svg>[data-cfx-role=\"native-transition-opacity\"]')?.getAttribute('opacity'),originalRevision:fadeRevision,originalTargets:fadeTargets})");
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var prefix = Path.Combine(output, "native-initial-fade-browser-" + kind + "-" + width + "-" + dark);
            File.WriteAllText(prefix + ".final.html", final.Html); File.WriteAllText(prefix + ".transition.json", payload); File.WriteAllText(prefix + ".observed.json", observed.GetRawText());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = prefix + ".browser.png", FullPage = true });
            foreach (var row in new[] { ("wide", final.PreparedVisual), ("narrow", final.NarrowPreparedVisual!) }) {
                var frames = VisualValueCapture.InitialFadeFrames(row.Item2, 3); for (var index = 0; index < frames.Count; index++) {
                    File.WriteAllText(prefix + "." + row.Item1 + "-" + index + ".svg", frames[index].ToSvg()); File.WriteAllBytes(prefix + "." + row.Item1 + "-" + index + ".png", frames[index].ToPng());
                }
            }
        }
        Assert.Equal(observed.GetProperty("originalRevision").GetString(), observed.GetProperty("revision").GetString());
        Assert.Equal(observed.GetProperty("originalTargets").GetInt32(), observed.GetProperty("targets").GetInt32()); Assert.Empty(observed.GetProperty("events").EnumerateArray());
        Assert.Equal("0.5", observed.GetProperty("opacity").GetString()); Assert.Equal(1, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await ReduceAndFlushMotionAsync(page); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("payload=>nativeChart.presentInitial(payload,{sceneRevision:nativeChart.sceneRevision,motion:{duration:1000}})", payload);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync()); var file = await download.PathAsync() ?? throw new InvalidOperationException("The accepted final native SVG was not exported.");
        Assert.DoesNotContain("native-transition-opacity", await File.ReadAllTextAsync(file)); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        var stale = await page.EvaluateAsync<string>("payload=>{try{nativeChart.presentInitial(payload,{sceneRevision:'stale'});return 'accepted'}catch(error){return error.message}}", payload); Assert.Contains("sceneRevision", stale);
        await page.EvaluateAsync("()=>nativeChart.dispose()"); await page.Clock.RunForAsync(2000); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
}
