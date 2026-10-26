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
    [InlineData("candlestick", "elapsed", true, 390, false)] [InlineData("candlestick", "sessions", false, 760, true)]
    [InlineData("candlestick", "ordinal", false, 390, false)] [InlineData("candlestick", "calendar", false, 760, true)]
    [InlineData("bubble", "ordinal", false, 390, false)] [InlineData("range", "calendar", true, 760, true)] [InlineData("error", "elapsed", false, 390, false)]
    public async Task ActualFixedTimeComponentsRetainNativeTimeAxesMidpointInkAcceptedValuesAndExports(string family, string policy, bool initial, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double value) => renderer.PrepareSection(NativeComponentTimeBeforeControls.Model(family, policy, value, width, dark), configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-time-components"; options.Interaction.Enable(ChartInteractionFeatures.Export); });
        var from = Section(40); var to = Section(60); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5)); var name = "time-component-browser-" + family + "-" + policy;
        await using var session = await BrowserProofFonts.OpenAsync(Page(initial ? to : from), width + 24, 680); var page = session.Page; await ReadyAndPauseAsync(page); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.timeChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.timeRevision=timeChart.sceneRevision;window.timeEvents=[];timeChart.root.addEventListener('cfxupdated',event=>timeEvents.push(event.detail));}");
        if (initial) await page.EvaluateAsync("payload=>timeChart.presentInitial(payload,{sceneRevision:timeChart.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>timeChart.update(reply.html,{sceneRevision:timeChart.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(550);
        var observed = await page.EvaluateAsync<JsonElement>("family=>{const field=family==='range'?'cfxUpper':family==='bubble'?'cfxY':family==='error'?'cfxValue':'cfxClose',point=scope=>timeChart.root.querySelector(scope+' [data-cfx-role=point][data-cfx-observation-id=current]'),facts=node=>({value:node?.dataset[field],x:node?.dataset.cfxX,scale:node?.dataset.cfxXScale,instant:node?.dataset.cfxXInstant});return {before:timeRevision,revision:timeChart.sceneRevision,events:timeEvents,accepted:facts(point('.cfx-stage>svg')),transient:facts(point('.cfx-native-motion-frame')),metadata:JSON.parse(timeChart.root.dataset.cfxPreparedChart),frame:timeChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML}}", family);
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        Assert.Equal("60", observed.GetProperty("accepted").GetProperty("value").GetString()); Assert.Equal("50", observed.GetProperty("transient").GetProperty("value").GetString());
        foreach (var key in new[] { "x", "scale", "instant" }) Assert.Equal(observed.GetProperty("accepted").GetProperty(key).GetString(), observed.GetProperty("transient").GetProperty(key).GetString()); Assert.Equal("time", observed.GetProperty("accepted").GetProperty("scale").GetString());
        Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength()); Assert.Equal(initial, observed.GetProperty("before").GetString() == observed.GetProperty("revision").GetString());
        await NativeValueFrameBrowserProof.AssertAsync(page, family == "range" ? "[data-cfx-role=range-area]" : "[data-cfx-role=point][data-cfx-observation-id=current]", name + "-ink", family is "candlestick" or "error" ? .05 : .25);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync()); var file = await download.PathAsync() ?? throw new InvalidOperationException("Time component accepted export missing."); var exported = await File.ReadAllTextAsync(file); NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exported);
        var point = System.Xml.Linq.XDocument.Parse(exported).Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "point" && (string?)row.Attribute("data-cfx-observation-id") == "current"); Assert.Equal("time", point.Attribute("data-cfx-x-scale")!.Value); Assert.Equal(observed.GetProperty("accepted").GetProperty("instant").GetString(), point.Attribute("data-cfx-x-instant")!.Value);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync()); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce }); if (initial) await page.EvaluateAsync("payload=>timeChart.presentInitial(payload,{sceneRevision:timeChart.sceneRevision,motion:{duration:1000}})", payload);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync()); await page.EvaluateAsync("()=>timeChart.dispose()"); await page.Clock.RunForAsync(1500); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    [BrowserTheory] [InlineData("coordinate")] [InlineData("instant")] [InlineData("scale")]
    public async Task ActualTemporalComponentCandidateCannotForgeFixedNativeXOrInstantFacts(string condition) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeComponentTimeBeforeControls.Section(renderer, NativeComponentTimeBeforeControls.Model("candlestick", "ordinal", 40)); var to = NativeComponentTimeBeforeControls.Section(renderer, NativeComponentTimeBeforeControls.Model("candlestick", "ordinal", 60)); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3));
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), 784, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        var observed = await page.EvaluateAsync<JsonElement>("reply=>{const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')),p=JSON.parse(reply.payload),revision=chart.sceneRevision,source=chart.root.dataset.cfxPreparedChart,frame=p.drawings.wide.frames[1],svg=new DOMParser().parseFromString(frame.svg,'image/svg+xml'),point=svg.querySelector('[data-cfx-role=point][data-cfx-observation-id=current]');let events=0,error;chart.root.addEventListener('cfxupdated',()=>events++);if(reply.condition==='coordinate')point.setAttribute('data-cfx-x',String(Number(point.getAttribute('data-cfx-x'))+1));else if(reply.condition==='instant')point.setAttribute('data-cfx-x-instant','2026-01-11T12:00:00.0000000Z');else point.setAttribute('data-cfx-x-scale','linear');frame.svg=svg.documentElement.outerHTML;try{chart.update(reply.html,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}})}catch(failure){error=failure.message}return {error,revisionUnchanged:chart.sceneRevision===revision,sourceUnchanged:chart.root.dataset.cfxPreparedChart===source,events,layers:chart.root.querySelectorAll('.cfx-native-motion-frame').length,payload:p}}", new { html = to.Html, payload, condition });
        NativeRadialMotionBeforeControls.Capture(from, "time-component-malformed-" + condition + "-from"); NativeRadialMotionBeforeControls.Capture(to, "time-component-malformed-" + condition + "-to"); NativeRadialMotionBeforeControls.Save("time-component-malformed-" + condition + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, "time-component-malformed-" + condition + ".browser.png"), FullPage = true });
        Assert.False(string.IsNullOrWhiteSpace(observed.GetProperty("error").GetString())); Assert.True(observed.GetProperty("revisionUnchanged").GetBoolean()); Assert.True(observed.GetProperty("sourceUnchanged").GetBoolean()); Assert.Equal(0, observed.GetProperty("events").GetInt32()); Assert.Equal(0, observed.GetProperty("layers").GetInt32()); AssertNoConsoleErrors(session);
    }
}
