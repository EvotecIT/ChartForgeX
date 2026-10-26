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
    public async Task ActualTypedRangeComponentsKeepNativeBoundaryInkAcceptedExportAndOneExistingPresentationLifetime(bool initial, bool area, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double y, double size) => renderer.PrepareSection(NativeRangeComponentBeforeControls.Model(y, size, area, true, width, dark),
            configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-range-components"; options.Interaction.Enable(ChartInteractionFeatures.Export); });
        var from = Section(10, 35); var to = Section(20, 60); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5));
        var name = "range-browser-" + initial + "-" + area + "-" + width;
        await using var session = await BrowserProofFonts.OpenAsync(Page(initial ? to : from), width + 24, 680); var page = session.Page;
        await ReadyAndPauseAsync(page); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.rangeChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.rangeRevision=rangeChart.sceneRevision;window.rangeEvents=[];rangeChart.root.addEventListener('cfxupdated',event=>rangeEvents.push(event.detail));}");
        if (initial) await page.EvaluateAsync("payload=>rangeChart.presentInitial(payload,{sceneRevision:rangeChart.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>rangeChart.update(reply.html,{sceneRevision:rangeChart.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(550);
        var observed = await page.EvaluateAsync<JsonElement>("()=>{const point=scope=>rangeChart.root.querySelector(scope+' [data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current]');return {before:rangeRevision,revision:rangeChart.sceneRevision,events:rangeEvents,accepted:{lower:point('.cfx-stage>svg')?.dataset.cfxLower,upper:point('.cfx-stage>svg')?.dataset.cfxUpper},transient:{lower:point('.cfx-native-motion-frame')?.dataset.cfxLower,upper:point('.cfx-native-motion-frame')?.dataset.cfxUpper},metadata:JSON.parse(rangeChart.root.dataset.cfxPreparedChart),frame:rangeChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML}}");
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        Assert.Equal("20", observed.GetProperty("accepted").GetProperty("lower").GetString()); Assert.Equal("60", observed.GetProperty("accepted").GetProperty("upper").GetString());
        Assert.Equal("15", observed.GetProperty("transient").GetProperty("lower").GetString()); Assert.Equal("47.5", observed.GetProperty("transient").GetProperty("upper").GetString());
        Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength()); Assert.Equal(initial, observed.GetProperty("before").GetString() == observed.GetProperty("revision").GetString());
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=series][data-cfx-series-key=delivery] [data-cfx-role=fill-segment][data-cfx-fill-point-start='0'] [data-cfx-role=" + (area ? "range-area" : "range-band") + "]", name + "-ink");
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var file = await download.PathAsync() ?? throw new InvalidOperationException("Range accepted export missing."); var exported = await File.ReadAllTextAsync(file); NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exported);
        var point = System.Xml.Linq.XDocument.Parse(exported).Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "point" && (string?)row.Attribute("data-cfx-series-key") == "delivery" && (string?)row.Attribute("data-cfx-observation-id") == "current");
        Assert.Equal("20", point.Attribute("data-cfx-lower")!.Value); Assert.Equal("60", point.Attribute("data-cfx-upper")!.Value);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        if (initial) await page.EvaluateAsync("payload=>rangeChart.presentInitial(payload,{sceneRevision:rangeChart.sceneRevision,motion:{duration:1000}})", payload);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync()); await page.EvaluateAsync("()=>rangeChart.dispose()"); await page.Clock.RunForAsync(1500);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    [BrowserTheory] [InlineData("component")] [InlineData("tuple")]
    public async Task ActualTypedRangeMalformedTupleRejectsBeforeRevisionAcceptedSourceOrEvents(string condition) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeRangeComponentBeforeControls.Section(renderer, NativeRangeComponentBeforeControls.Model(10, 35, true, true)); var to = NativeRangeComponentBeforeControls.Section(renderer, NativeRangeComponentBeforeControls.Model(20, 60, true, true));
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3));
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), 784, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        var observed = await page.EvaluateAsync<JsonElement>("reply=>{const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')),p=JSON.parse(reply.payload),revision=chart.sceneRevision,source=chart.root.dataset.cfxPreparedChart;let events=0,error;chart.root.addEventListener('cfxupdated',()=>events++);if(reply.condition==='component')p.readings[0].component='upper';else {const frame=p.drawings.wide.frames[1];frame.nativeValueSource.readings[0].value=80;frame.svg=frame.svg.replace('data-cfx-lower=\"15\"','data-cfx-lower=\"80\"').replace('data-cfx-y=\"15\"','data-cfx-y=\"80\"');}try{chart.update(reply.html,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}})}catch(failure){error=failure.message}return {error,revisionUnchanged:chart.sceneRevision===revision,sourceUnchanged:chart.root.dataset.cfxPreparedChart===source,events,layers:chart.root.querySelectorAll('.cfx-native-motion-frame').length,payload:p}}", new { html = to.Html, payload, condition });
        NativeRadialMotionBeforeControls.Capture(from, "range-malformed-" + condition + "-from"); NativeRadialMotionBeforeControls.Capture(to, "range-malformed-" + condition + "-to"); NativeRadialMotionBeforeControls.Save("range-malformed-" + condition + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, "range-malformed-" + condition + ".browser.png"), FullPage = true });
        Assert.False(string.IsNullOrWhiteSpace(observed.GetProperty("error").GetString())); Assert.True(observed.GetProperty("revisionUnchanged").GetBoolean()); Assert.True(observed.GetProperty("sourceUnchanged").GetBoolean()); Assert.Equal(0, observed.GetProperty("events").GetInt32()); Assert.Equal(0, observed.GetProperty("layers").GetInt32()); AssertNoConsoleErrors(session);
    }
}
