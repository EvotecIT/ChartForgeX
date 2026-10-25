using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class NativeTransitionBrowserTests {
    [BrowserTheory]
    [InlineData(false, "inside", 760, false, false)] [InlineData(true, "outside", 390, true, false)]
    [InlineData(false, "outside", 390, true, true)] [InlineData(true, "inside", 760, false, true)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealNativeRadialFramesKeepAcceptedExportAndInitialPresentationLifetime(bool donut, string labels, int width, bool dark, bool initial) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double value) => renderer.PrepareSection(NativeRadialMotionBeforeControls.Model(donut, false, labels, value, width, dark), configure: options => {
            options.Interaction.ChartId = "native-radial"; options.Interaction.Enable(ChartInteractionFeatures.Export); options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 420;
        });
        var from = Section(60); var to = Section(80); var payload = renderer.PrepareNativeTransitionJson(from, to,
            new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5));
        var name = "radial-browser-" + donut + "-" + labels + "-" + width + "-" + initial;
        await using var session = await BrowserProofFonts.OpenAsync(Page(initial ? to : from), width + 24, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.radial=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.radialBefore=radial.sceneRevision;window.radialEvents=[];radial.root.addEventListener('cfxupdated',event=>radialEvents.push(event.detail));}");
        if (initial) await page.EvaluateAsync("payload=>radial.presentInitial(payload,{sceneRevision:radial.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>radial.update(reply.html,{sceneRevision:radial.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(300);
        var observed = await page.EvaluateAsync<JsonElement>("()=>({before:radialBefore,revision:radial.sceneRevision,events:radialEvents,accepted:radial.root.querySelector('.cfx-stage>svg [data-cfx-role=radial-point][data-cfx-observation-id=primary]')?.dataset.cfxValue,frame:radial.root.querySelector('.cfx-native-motion-frame')?.outerHTML,targets:radial.resolveTargets()})");
        NativeRadialValueMotionTests.Capture(from, to, VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null), payload, name);
        NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            var narrow = VisualValueCapture.NativeFrames(from.NarrowPreparedVisual!, to.NarrowPreparedVisual!, 5, null);
            for (var index = 0; index < narrow.Count; index++) { File.WriteAllText(Path.Combine(output, name + ".narrow-" + index + ".svg"), narrow[index].ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".narrow-" + index + ".png"), narrow[index].ToPng()); }
        }
        Assert.Equal("80", observed.GetProperty("accepted").GetString()); Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength());
        Assert.Equal(initial, observed.GetProperty("before").GetString() == observed.GetProperty("revision").GetString());
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=radial-point][data-cfx-observation-id=primary] [data-cfx-role=" + (donut ? "donut-slice" : "pie-slice") + "]", name + "-ink");
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var file = await download.PathAsync() ?? throw new InvalidOperationException("Accepted radial export is missing."); var exportedText = await File.ReadAllTextAsync(file); NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exportedText); var exported = System.Xml.Linq.XDocument.Parse(exportedText);
        Assert.Equal("80", exported.Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "radial-point" && (string?)row.Attribute("data-cfx-observation-id") == "primary").Attribute("data-cfx-value")!.Value);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce }); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EvaluateAsync("()=>radial.dispose()"); await page.Clock.RunForAsync(2000); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    [BrowserTheory] [InlineData(false, 760, false)] [InlineData(true, 390, true)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealRadialOtherLegendUsesItsCapturedAbsoluteMembershipAndExistingReset(bool donut, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeRadialMotionBeforeControls.Model(donut, true, "outside", width: width, dark: dark);
        HtmlChartSection Section(bool hidden) => renderer.PrepareVisibilitySection(chart,
            NativeRadialVisibilityReflowTests.View(hidden ? new[] { ChartVisibilityTarget.RadialOther(new ChartSeriesId("parts")) } : Array.Empty<ChartVisibilityTarget>()),
            configure: options => { options.Interaction.ChartId = "native-radial-other"; options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.LegendToggles); });
        var from = Section(false); var to = Section(true); var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5);
        var forward = renderer.PrepareNativeTransitionJson(from, to, options); var reverse = renderer.PrepareNativeTransitionJson(to, from, options);
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), width + 24, 680); var page = session.Page;
        await page.WaitForFunctionAsync("()=>!!ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'))");
        await page.EvaluateAsync("()=>{window.radial=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.radialCalls=[];radial.connectNative((request,signal)=>new Promise(resolve=>radialCalls.push({request,signal,resolve})),{basis:'full-source',axes:null,legend:true,motion:{duration:650}})}");
        await page.Locator("[data-cfx-role=legend-item][data-cfx-derived=radial-slice][data-cfx-point='-1']").ClickAsync(); await page.WaitForFunctionAsync("()=>radialCalls.length===1");
        var request = await page.EvaluateAsync<JsonElement>("()=>radialCalls[0].request"); var name = "radial-other-browser-" + donut + "-" + width;
        NativeReflowVisibilityTests.Capture(from, to, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5), forward, name); NativeRadialMotionBeforeControls.Save(name + ".request.json", request.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".pending.png"), FullPage = true });
        Assert.Equal("radialOther", request.GetProperty("sourceVisibility").GetProperty("hiddenTargets")[0].GetProperty("kind").GetString());
        await page.EvaluateAsync("reply=>radialCalls[0].resolve(reply)", new { html = to.Html, basis = "hidden-other", transition = forward });
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-native-legend][aria-pressed=false]')!==null");
        var accepted = await page.EvaluateAsync<JsonElement>("()=>({other:[...radial.root.querySelectorAll('[data-cfx-native-legend]')].find(node=>node.dataset.cfxPoint==='-1')?.dataset,points:[...radial.root.querySelectorAll('.cfx-stage>svg [data-cfx-role=radial-point]')].map(node=>({...node.dataset})),header:JSON.parse(radial.root.dataset.cfxPreparedChart)})");
        NativeRadialMotionBeforeControls.Save(name + ".accepted.json", accepted.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } acceptedOutput) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(acceptedOutput, name + ".accepted.png"), FullPage = true });
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var file = await download.PathAsync() ?? throw new InvalidOperationException("Accepted Other export is missing.");
        var exportedText = await File.ReadAllTextAsync(file); NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exportedText);
        var exported = System.Xml.Linq.XDocument.Parse(exportedText); Assert.Equal(2, exported.Descendants().Count(row => (string?)row.Attribute("data-cfx-role") == "radial-point"));
        Assert.Equal(2, accepted.GetProperty("points").GetArrayLength()); Assert.Equal("2,3,4", accepted.GetProperty("other").GetProperty("cfxSourcePoints").GetString());
        await page.Locator("[data-cfx-reset]").ClickAsync(); await page.WaitForFunctionAsync("()=>radialCalls.length===2"); var reset = await page.EvaluateAsync<JsonElement>("()=>radialCalls[1].request");
        NativeRadialMotionBeforeControls.Save(name + ".reset.json", reset.GetRawText()); Assert.Equal("reset", reset.GetProperty("kind").GetString()); Assert.Equal("hidden-other", reset.GetProperty("basis").GetString());
        Assert.Equal(0, reset.GetProperty("sourceVisibility").GetProperty("hiddenTargets").GetArrayLength());
        await page.EvaluateAsync("reply=>radialCalls[1].resolve(reply)", new { html = from.Html, basis = "reset", transition = reverse });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-native-legend][aria-pressed=false]')"); await page.EvaluateAsync("()=>radial.dispose()"); AssertNoConsoleErrors(session);
    }
}
