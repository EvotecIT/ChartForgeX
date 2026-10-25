using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class NativeTransitionBrowserTests {
    [BrowserTheory, InlineData(390, true), InlineData(760, false)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealGeneratedNativeFilterAndMaskResourcesUseTheExistingInitialLifetimeAndExactEndpointExport(int width, bool dark) {
        if (!Enabled) return;
        var final = NativeSettledMotionFixture.Section(width, dark, VisualSvgEffectProfile.NativeFilters);
        var payload = new HtmlInteractiveChartRenderer().PrepareNativeTransitionJson(final, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade, 3));
        await using var session = await BrowserProofFonts.OpenAsync(Page(final), width + 24, 680); var page = session.Page;
        await page.WaitForFunctionAsync("()=>window.ChartForgeXCharts?.get(document.querySelector('.cfx-interactive-chart'))");
        var baselineDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var baselinePath = await baselineDownload.PathAsync() ?? throw new InvalidOperationException("The accepted effect control did not export.");
        var acceptedExport = await File.ReadAllTextAsync(baselinePath);
        await ReadyAndPauseAsync(page); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.nativeChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.effectEvents=[];window.effectRevision=nativeChart.sceneRevision;nativeChart.root.addEventListener('cfxupdated',event=>effectEvents.push(event.detail));}");
        await page.EvaluateAsync("payload=>nativeChart.presentInitial(payload,{sceneRevision:nativeChart.sceneRevision,motion:{duration:1000}})", payload); await page.Clock.RunForAsync(550);
        var observed = await page.EvaluateAsync<JsonElement>("()=>({revision:nativeChart.sceneRevision,before:effectRevision,events:effectEvents,frame:document.querySelector('.cfx-native-motion-frame')?.outerHTML,filter:document.querySelector('.cfx-native-motion-frame filter')?.id,mask:document.querySelector('.cfx-native-motion-frame mask')?.id})");
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var prefix = Path.Combine(output, "settled-native-effects-browser-" + width + "-" + dark);
            File.WriteAllText(prefix + ".accepted-export.svg", acceptedExport); File.WriteAllText(prefix + ".final.html", final.Html); File.WriteAllText(prefix + ".transition.json", payload); File.WriteAllText(prefix + ".observed.json", observed.GetRawText());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = prefix + ".browser.png", FullPage = true });
            var frames = VisualValueCapture.InitialFadeFrames(final.PreparedVisual.ForInteractivePngExport(), 3);
            for (var index = 0; index < frames.Count; index++) { File.WriteAllText(prefix + ".frame-" + index + ".svg", frames[index].ToSvg()); File.WriteAllBytes(prefix + ".frame-" + index + ".png", frames[index].ToPng()); }
        }
        Assert.Equal(observed.GetProperty("before").GetString(), observed.GetProperty("revision").GetString()); Assert.Empty(observed.GetProperty("events").EnumerateArray());
        Assert.Contains("-surface-shadow-", observed.GetProperty("filter").GetString()); Assert.EndsWith("-retained-mask-1", observed.GetProperty("mask").GetString());
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=point] [data-cfx-role=bar]:first-of-type", "settled-effects-ink-" + width + "-" + dark);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce }); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.Clock.ResumeAsync();
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync()); var path = await download.PathAsync() ?? throw new InvalidOperationException("The accepted effect endpoint was not exported.");
        var exported = await File.ReadAllTextAsync(path);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } exportOutput)
            File.WriteAllText(Path.Combine(exportOutput, "settled-native-effects-browser-" + width + "-" + dark + ".exported.svg"), exported);
        Assert.Equal(acceptedExport, exported);
        await page.EvaluateAsync("()=>nativeChart.dispose()"); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
}
