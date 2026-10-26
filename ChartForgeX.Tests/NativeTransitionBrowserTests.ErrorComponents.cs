using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class NativeTransitionBrowserTests {
    [BrowserTheory]
    [InlineData("x", HtmlNativeTransitionEffect.Values, 390, false)] [InlineData("y", HtmlNativeTransitionEffect.InitialValues, 760, true)]
    [InlineData("xy", HtmlNativeTransitionEffect.Values, 760, true)] [InlineData("xy", HtmlNativeTransitionEffect.InitialValues, 390, false)]
    [InlineData("xy", HtmlNativeTransitionEffect.ValueFlash, 390, false)]
    public async Task ActualNativeErrorMembersKeepWhiskerInkAcceptedRawValuesExportsAndExistingLifetime(string form, HtmlNativeTransitionEffect effect, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var initial = effect == HtmlNativeTransitionEffect.InitialValues; var flash = effect == HtmlNativeTransitionEffect.ValueFlash;
        HtmlChartSection Section(bool finish) => renderer.PrepareSection(finish ? NativeErrorComponentBeforeControls.Model(form, true, 50, .25, 1.75, 40, 60, width, dark) : NativeErrorComponentBeforeControls.Model(form, true, width: width, dark: dark),
            configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-error-components"; options.Interaction.Enable(ChartInteractionFeatures.Export); });
        var from = Section(false); var to = Section(true); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, flash ? ChartColor.Black : null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(effect, 5, flash ? ChartColor.Black : null)); var name = "error-browser-" + form + "-" + effect + "-" + width;
        await using var session = await BrowserProofFonts.OpenAsync(Page(initial ? to : from), width + 24, 680); var page = session.Page; await ReadyAndPauseAsync(page); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.errorChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.errorRevision=errorChart.sceneRevision;window.errorEvents=[];errorChart.root.addEventListener('cfxupdated',event=>errorEvents.push(event.detail));}");
        if (initial) await page.EvaluateAsync("payload=>errorChart.presentInitial(payload,{sceneRevision:errorChart.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>errorChart.update(reply.html,{sceneRevision:errorChart.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(550);
        var observed = await page.EvaluateAsync<JsonElement>("()=>{const point=scope=>errorChart.root.querySelector(scope+' [data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current]'),facts=node=>({value:node?.dataset.cfxValue,xLower:node?.dataset.cfxXLower,yLower:node?.dataset.cfxYLower});return {before:errorRevision,revision:errorChart.sceneRevision,events:errorEvents,accepted:facts(point('.cfx-stage>svg')),transient:facts(point('.cfx-native-motion-frame')),metadata:JSON.parse(errorChart.root.dataset.cfxPreparedChart),frame:errorChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML}}");
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        Assert.Equal("50", observed.GetProperty("accepted").GetProperty("value").GetString()); Assert.Equal(flash ? "50" : "40", observed.GetProperty("transient").GetProperty("value").GetString());
        if (form.Contains('x')) Assert.Equal(flash ? "0.25" : "0.375", observed.GetProperty("transient").GetProperty("xLower").GetString()); else Assert.False(observed.GetProperty("transient").TryGetProperty("xLower", out _));
        if (form.Contains('y')) Assert.Equal(flash ? "40" : "30", observed.GetProperty("transient").GetProperty("yLower").GetString()); else Assert.False(observed.GetProperty("transient").TryGetProperty("yLower", out _));
        Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength()); Assert.Equal(initial, observed.GetProperty("before").GetString() == observed.GetProperty("revision").GetString());
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current] [data-cfx-role=error-marker]", name + "-ink");
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync()); var file = await download.PathAsync() ?? throw new InvalidOperationException("ErrorBar accepted export missing."); var exported = await File.ReadAllTextAsync(file); NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exported);
        var point = System.Xml.Linq.XDocument.Parse(exported).Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "point" && (string?)row.Attribute("data-cfx-series-key") == "delivery" && (string?)row.Attribute("data-cfx-observation-id") == "current"); Assert.Equal("50", point.Attribute("data-cfx-value")!.Value);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync()); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        if (initial) await page.EvaluateAsync("payload=>errorChart.presentInitial(payload,{sceneRevision:errorChart.sceneRevision,motion:{duration:1000}})", payload);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync()); await page.EvaluateAsync("()=>errorChart.dispose()"); await page.Clock.RunForAsync(1500); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    [BrowserTheory] [InlineData("containing")] [InlineData("nullable")]
    public async Task ActualNativeErrorMalformedIntervalRejectsBeforeAcceptedSourceRevisionAndEvents(string condition) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeErrorComponentBeforeControls.Section(renderer, NativeErrorComponentBeforeControls.Model("xy", true)); var to = NativeErrorComponentBeforeControls.Section(renderer, NativeErrorComponentBeforeControls.Model("xy", true, 50, .25, 1.75, 40, 60));
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3));
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), 784, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        var observed = await page.EvaluateAsync<JsonElement>("reply=>{const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')),p=JSON.parse(reply.payload),revision=chart.sceneRevision,source=chart.root.dataset.cfxPreparedChart,frame=p.drawings.wide.frames[1],svg=new DOMParser().parseFromString(frame.svg,'image/svg+xml'),point=svg.querySelector('[data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current]');let events=0,error;chart.root.addEventListener('cfxupdated',()=>events++);if(reply.condition==='containing'){frame.nativeValueSource.readings[1].value=1.25;point.setAttribute('data-cfx-x-lower','1.25')}else point.removeAttribute('data-cfx-y-upper');frame.svg=svg.documentElement.outerHTML;try{chart.update(reply.html,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}})}catch(failure){error=failure.message}return {error,revisionUnchanged:chart.sceneRevision===revision,sourceUnchanged:chart.root.dataset.cfxPreparedChart===source,events,layers:chart.root.querySelectorAll('.cfx-native-motion-frame').length,payload:p}}", new { html = to.Html, payload, condition });
        NativeRadialMotionBeforeControls.Capture(from, "error-malformed-" + condition + "-from"); NativeRadialMotionBeforeControls.Capture(to, "error-malformed-" + condition + "-to"); NativeRadialMotionBeforeControls.Save("error-malformed-" + condition + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, "error-malformed-" + condition + ".browser.png"), FullPage = true });
        Assert.False(string.IsNullOrWhiteSpace(observed.GetProperty("error").GetString())); Assert.True(observed.GetProperty("revisionUnchanged").GetBoolean()); Assert.True(observed.GetProperty("sourceUnchanged").GetBoolean()); Assert.Equal(0, observed.GetProperty("events").GetInt32()); Assert.Equal(0, observed.GetProperty("layers").GetInt32()); AssertNoConsoleErrors(session);
    }
}
