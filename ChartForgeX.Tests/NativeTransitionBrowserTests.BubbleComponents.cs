using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class NativeTransitionBrowserTests {
    [BrowserTheory]
    [InlineData(false, false, 390, false)] [InlineData(false, true, 760, true)]
    [InlineData(true, false, 760, true)] [InlineData(true, true, 390, false)]
    public async Task ActualTypedBubbleComponentsKeepNativeSizeInkAcceptedExportAndOneExistingPresentationLifetime(bool initial, bool area, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double y, double size) => renderer.PrepareSection(NativeBubbleComponentBeforeControls.Model(y, size, area, dark, width, dark, labels: true),
            configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-bubble-components"; options.Interaction.Enable(ChartInteractionFeatures.Export); });
        var from = Section(30, 10); var to = Section(50, 70); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5));
        var name = "bubble-browser-" + initial + "-" + area + "-" + width;
        await using var session = await BrowserProofFonts.OpenAsync(Page(initial ? to : from), width + 24, 680); var page = session.Page;
        await ReadyAndPauseAsync(page); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.bubbleChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.bubbleRevision=bubbleChart.sceneRevision;window.bubbleEvents=[];bubbleChart.root.addEventListener('cfxupdated',event=>bubbleEvents.push(event.detail));}");
        if (initial) await page.EvaluateAsync("payload=>bubbleChart.presentInitial(payload,{sceneRevision:bubbleChart.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>bubbleChart.update(reply.html,{sceneRevision:bubbleChart.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(550);
        var observed = await page.EvaluateAsync<JsonElement>("()=>{const point=scope=>bubbleChart.root.querySelector(scope+' [data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current]');return {before:bubbleRevision,revision:bubbleChart.sceneRevision,events:bubbleEvents,accepted:{y:point('.cfx-stage>svg')?.dataset.cfxY,size:point('.cfx-stage>svg')?.dataset.cfxSize},transient:{y:point('.cfx-native-motion-frame')?.dataset.cfxY,size:point('.cfx-native-motion-frame')?.dataset.cfxSize},metadata:JSON.parse(bubbleChart.root.dataset.cfxPreparedChart),frame:bubbleChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML}}");
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        Assert.Equal("50", observed.GetProperty("accepted").GetProperty("y").GetString()); Assert.Equal("70", observed.GetProperty("accepted").GetProperty("size").GetString());
        Assert.Equal("40", observed.GetProperty("transient").GetProperty("y").GetString()); Assert.Equal("40", observed.GetProperty("transient").GetProperty("size").GetString());
        Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength()); Assert.Equal(initial, observed.GetProperty("before").GetString() == observed.GetProperty("revision").GetString());
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current] [data-cfx-role=bubble]", name + "-ink");
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var file = await download.PathAsync() ?? throw new InvalidOperationException("Bubble accepted export missing."); var exported = await File.ReadAllTextAsync(file); NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exported);
        var point = System.Xml.Linq.XDocument.Parse(exported).Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "point" && (string?)row.Attribute("data-cfx-series-key") == "delivery" && (string?)row.Attribute("data-cfx-observation-id") == "current");
        Assert.Equal("50", point.Attribute("data-cfx-y")!.Value); Assert.Equal("70", point.Attribute("data-cfx-size")!.Value);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        if (initial) await page.EvaluateAsync("payload=>bubbleChart.presentInitial(payload,{sceneRevision:bubbleChart.sceneRevision,motion:{duration:1000}})", payload);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync()); await page.EvaluateAsync("()=>bubbleChart.dispose()"); await page.Clock.RunForAsync(1500);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    [BrowserTheory] [InlineData("component")] [InlineData("size")]
    public async Task ActualTypedBubbleMalformedCandidateRejectsBeforeRevisionAcceptedSourceOrEvents(string condition) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(30, 10)); var to = NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(50, 70));
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3));
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), 784, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        var observed = await page.EvaluateAsync<JsonElement>("reply=>{const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')),p=JSON.parse(reply.payload),revision=chart.sceneRevision,source=chart.root.dataset.cfxPreparedChart;let events=0,error;chart.root.addEventListener('cfxupdated',()=>events++);if(reply.condition==='component')p.readings[0].component='size';else p.drawings.wide.frames[1].nativeValueSource.readings[1].value=0;try{chart.update(reply.html,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}})}catch(failure){error=failure.message}return {error,revisionUnchanged:chart.sceneRevision===revision,sourceUnchanged:chart.root.dataset.cfxPreparedChart===source,events,layers:chart.root.querySelectorAll('.cfx-native-motion-frame').length,payload:p}}", new { html = to.Html, payload, condition });
        NativeRadialMotionBeforeControls.Capture(from, "bubble-malformed-" + condition + "-from"); NativeRadialMotionBeforeControls.Capture(to, "bubble-malformed-" + condition + "-to"); NativeRadialMotionBeforeControls.Save("bubble-malformed-" + condition + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, "bubble-malformed-" + condition + ".browser.png"), FullPage = true });
        Assert.False(string.IsNullOrWhiteSpace(observed.GetProperty("error").GetString())); Assert.True(observed.GetProperty("revisionUnchanged").GetBoolean()); Assert.True(observed.GetProperty("sourceUnchanged").GetBoolean()); Assert.Equal(0, observed.GetProperty("events").GetInt32()); Assert.Equal(0, observed.GetProperty("layers").GetInt32()); AssertNoConsoleErrors(session);
    }
}
