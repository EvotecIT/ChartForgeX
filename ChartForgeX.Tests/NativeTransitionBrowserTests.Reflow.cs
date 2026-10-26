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
    [InlineData("line", 390, true)] [InlineData("bar", 760, false)] [InlineData("boxes", 390, false)] [InlineData("raw", 760, true)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealNativeReflowDisplaysCapturedSourceThenDestinationWithoutChangingAcceptedExport(string kind, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeReflowVisibilityTests.Model(kind, labels: true)
            .WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        if (kind == "line") chart.WithLineMarkers(ChartLineMarkerMode.All);
        HtmlChartSection Section(bool hidden) => renderer.PrepareVisibilitySection(chart, NativeReflowVisibilityTests.View(kind, hidden), configure: value => {
            value.Interaction.ChartId = "native-reflow"; value.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.LegendToggles);
            value.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; value.Adaptive.NarrowWidth = 350; value.Adaptive.NarrowHeight = 420;
        });
        var from = Section(false); var to = Section(true); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5));
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), width + 24, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.reflow=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.reflowEvents=[];window.reflowBefore=reflow.sceneRevision;reflow.root.addEventListener('cfxupdated',event=>reflowEvents.push(event.detail));}");
        await page.EvaluateAsync("reply=>reflow.update(reply.html,{sceneRevision:reflow.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(300);
        var observed = await page.EvaluateAsync<JsonElement>("()=>({revision:reflow.sceneRevision,originalRevision:reflowBefore,events:reflowEvents,targets:reflow.resolveTargets(),accepted:reflow.root.querySelectorAll('.cfx-stage>svg [data-cfx-role=point]').length,painted:reflow.root.querySelectorAll('.cfx-native-motion-frame [data-cfx-role=point]').length,header:JSON.parse(reflow.root.dataset.cfxPreparedChart),frame:reflow.root.querySelector('.cfx-native-motion-frame')?.outerHTML})");
        var name = "reflow-browser-" + kind + "-" + width + "-" + dark;
        NativeReflowVisibilityTests.Capture(from, to, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5), payload, name);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".observed.json"), observed.GetRawText()); await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            var narrow = VisualSeriesVisibilityCapture.ReflowFrames(from.NarrowPreparedVisual!, to.NarrowPreparedVisual!, 5);
            for (var index = 0; index < narrow.Count; index++) { File.WriteAllText(Path.Combine(output, name + ".narrow-" + index + ".svg"), narrow[index].Frame.ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".narrow-" + index + ".png"), narrow[index].Frame.ToPng()); }
        }
        Assert.NotEqual(observed.GetProperty("originalRevision").GetString(), observed.GetProperty("revision").GetString()); Assert.Equal(1, observed.GetProperty("events").GetArrayLength());
        Assert.Equal(2, observed.GetProperty("accepted").GetInt32()); Assert.Equal(3, observed.GetProperty("painted").GetInt32());
        Assert.Equal("reflow", observed.GetProperty("header").GetProperty("visibility").GetProperty("layout").GetString());
        Assert.Equal(1, await page.Locator(".cfx-native-motion-frame").CountAsync());
        var identity = kind == "raw" ? "[data-cfx-group-id=earlier]" : "[data-cfx-observation-id=earlier]";
        await NativeValueFrameBrowserProof.AssertAsync(page, identity + " [data-cfx-role=" + (kind == "line" ? "marker" : kind == "bar" ? "bar" : "boxplot-body") + "]", name + "-ink");
        await page.Clock.RunForAsync(500);
        Assert.Equal(2, await page.Locator(".cfx-native-motion-frame [data-cfx-role=point]").CountAsync());
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync()); var file = await download.PathAsync() ?? throw new InvalidOperationException("Accepted native Reflow export missing.");
        var exported = System.Xml.Linq.XDocument.Parse(await File.ReadAllTextAsync(file));
        Assert.Equal(2, exported.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "point"));
        Assert.DoesNotContain(exported.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "native-transition-opacity");
        await ReduceAndFlushMotionAsync(page); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EvaluateAsync("()=>reflow.dispose()"); await page.Clock.RunForAsync(2000); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    [BrowserTheory] [InlineData("bar", 390, true)] [InlineData("raw", 760, false)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task ActualRetainedNativePointAndGroupLegendUsesTypedAbsoluteIntentAndExistingReset(string kind, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeReflowVisibilityTests.Model(kind).WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        HtmlChartSection Section(bool hidden) => renderer.PrepareVisibilitySection(chart, NativeReflowVisibilityTests.View(kind, hidden), configure: value => { value.Interaction.ChartId = "native-reflow-legend"; value.Interaction.Enable(ChartInteractionFeatures.LegendToggles | ChartInteractionFeatures.Export); });
        var from = Section(false); var to = Section(true); var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5);
        var forward = renderer.PrepareNativeTransitionJson(from, to, options); var reverse = renderer.PrepareNativeTransitionJson(to, from, options);
        NativeReflowVisibilityBeforeControls.Save("reflow-legend-" + kind + ".forward.json", forward); NativeReflowVisibilityBeforeControls.Save("reflow-legend-" + kind + ".reverse.json", reverse);
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), width + 24, 680); var page = session.Page;
        await page.WaitForFunctionAsync("()=>!!ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'))");
        await page.EvaluateAsync("()=>{window.reflow=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.reflowCalls=[];reflow.connectNative((request,signal)=>new Promise(resolve=>reflowCalls.push({request,signal,resolve})),{basis:'source',axes:['x'],legend:true,motion:{duration:650}})}");
        var selector = "[data-cfx-role=legend-item]" + (kind == "raw" ? "[data-cfx-group-id=current]" : "[data-cfx-observation-id=current]");
        await page.Locator(selector).ClickAsync(); await page.WaitForFunctionAsync("()=>reflowCalls.length===1");
        var request = await page.EvaluateAsync<JsonElement>("()=>reflowCalls[0].request");
        NativeReflowVisibilityBeforeControls.Save("reflow-legend-" + kind + ".request.json", request.GetRawText()); NativeReflowVisibilityBeforeControls.Capture(from, "reflow-legend-" + kind + "-from"); NativeReflowVisibilityBeforeControls.Capture(to, "reflow-legend-" + kind + "-to");
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, "reflow-legend-" + kind + ".pending.png"), FullPage = true });
        Assert.Equal(kind == "raw" ? "boxPlotGroup" : "observation", request.GetProperty("sourceVisibility").GetProperty("hiddenTargets")[0].GetProperty("kind").GetString());
        await page.EvaluateAsync("reply=>reflowCalls[0].resolve(reply)", new { html = to.Html, basis = "hidden", transition = forward });
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-native-legend][aria-pressed=false]')!==null");
        var accepted = await page.EvaluateAsync<JsonElement>("()=>({header:JSON.parse(reflow.root.dataset.cfxPreparedChart),legend:[...reflow.root.querySelectorAll('[data-cfx-native-legend]')].map(node=>({...node.dataset,pressed:node.getAttribute('aria-pressed')}))})");
        NativeReflowVisibilityBeforeControls.Save("reflow-legend-" + kind + ".accepted.json", accepted.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } acceptedOutput) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(acceptedOutput, "reflow-legend-" + kind + ".accepted.png"), FullPage = true });
        if (kind == "bar") Assert.Equal("80", accepted.GetProperty("legend").EnumerateArray().Single(row => row.GetProperty("cfxObservationId").GetString() == "current").GetProperty("cfxY").GetString());
        await page.Locator("[data-cfx-reset]").ClickAsync(); await page.WaitForFunctionAsync("()=>reflowCalls.length===2"); var reset = await page.EvaluateAsync<JsonElement>("()=>reflowCalls[1].request");
        NativeReflowVisibilityBeforeControls.Save("reflow-legend-" + kind + ".reset.json", reset.GetRawText()); Assert.Equal("reset", reset.GetProperty("kind").GetString()); Assert.Equal("hidden", reset.GetProperty("basis").GetString());
        Assert.Equal(0, reset.GetProperty("sourceVisibility").GetProperty("hiddenTargets").GetArrayLength());
        await page.EvaluateAsync("reply=>reflowCalls[1].resolve(reply)", new { html = from.Html, basis = "reset", transition = reverse });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-native-legend][aria-pressed=false]')"); await page.EvaluateAsync("()=>reflow.dispose()"); AssertNoConsoleErrors(session);
    }
}
