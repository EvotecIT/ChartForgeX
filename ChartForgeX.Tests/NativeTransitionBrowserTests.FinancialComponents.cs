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
    [InlineData(false, HtmlNativeTransitionEffect.Values, 390, false)] [InlineData(true, HtmlNativeTransitionEffect.Values, 760, true)]
    [InlineData(false, HtmlNativeTransitionEffect.InitialValues, 760, true)] [InlineData(true, HtmlNativeTransitionEffect.InitialValues, 390, false)]
    [InlineData(false, HtmlNativeTransitionEffect.ValueFlash, 390, false)] [InlineData(true, HtmlNativeTransitionEffect.ValueFlash, 760, true)]
    public async Task ActualFinancialTupleFramesKeepNativeInkAcceptedPricesExportsAndExistingLifetime(bool ohlc, HtmlNativeTransitionEffect effect, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var initial = effect == HtmlNativeTransitionEffect.InitialValues; var flash = effect == HtmlNativeTransitionEffect.ValueFlash;
        HtmlChartSection Section(bool finish) => renderer.PrepareSection(finish ? NativeFinancialComponentBeforeControls.Model(ohlc, true, 60, 80, 10, 40, width, dark) : NativeFinancialComponentBeforeControls.Model(ohlc, true, width: width, dark: dark),
            configure: options => { options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-financial-components"; options.Interaction.Enable(ChartInteractionFeatures.Export); });
        var from = Section(false); var to = Section(true); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, flash ? ChartColor.Black : null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(effect, 5, flash ? ChartColor.Black : null)); var name = "financial-browser-" + ohlc + "-" + effect + "-" + width;
        await using var session = await BrowserProofFonts.OpenAsync(Page(initial ? to : from), width + 24, 680); var page = session.Page; await ReadyAndPauseAsync(page); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.financialChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.financialRevision=financialChart.sceneRevision;window.financialEvents=[];financialChart.root.addEventListener('cfxupdated',event=>financialEvents.push(event.detail));}");
        if (initial) await page.EvaluateAsync("payload=>financialChart.presentInitial(payload,{sceneRevision:financialChart.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>financialChart.update(reply.html,{sceneRevision:financialChart.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(550);
        var observed = await page.EvaluateAsync<JsonElement>("()=>{const point=scope=>financialChart.root.querySelector(scope+' [data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current]'),facts=node=>({open:node?.dataset.cfxOpen,high:node?.dataset.cfxHigh,low:node?.dataset.cfxLow,close:node?.dataset.cfxClose});return {before:financialRevision,revision:financialChart.sceneRevision,events:financialEvents,accepted:facts(point('.cfx-stage>svg')),transient:facts(point('.cfx-native-motion-frame')),metadata:JSON.parse(financialChart.root.dataset.cfxPreparedChart),frame:financialChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML}}");
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        Assert.Equal("60", observed.GetProperty("accepted").GetProperty("open").GetString()); Assert.Equal("40", observed.GetProperty("accepted").GetProperty("close").GetString());
        Assert.Equal(flash ? "60" : "50", observed.GetProperty("transient").GetProperty("open").GetString()); Assert.Equal(flash ? "80" : "75", observed.GetProperty("transient").GetProperty("high").GetString()); Assert.Equal(flash ? "10" : "15", observed.GetProperty("transient").GetProperty("low").GetString()); Assert.Equal(flash ? "40" : "50", observed.GetProperty("transient").GetProperty("close").GetString());
        Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength()); Assert.Equal(initial, observed.GetProperty("before").GetString() == observed.GetProperty("revision").GetString());
        // The whole real point includes sparse OHLC strokes or a two-pixel doji body, so measure its genuine thin native ink.
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current]", name + "-ink", .05);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync()); var file = await download.PathAsync() ?? throw new InvalidOperationException("Financial accepted export missing."); var exported = await File.ReadAllTextAsync(file); NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exported);
        var point = System.Xml.Linq.XDocument.Parse(exported).Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "point" && (string?)row.Attribute("data-cfx-series-key") == "delivery" && (string?)row.Attribute("data-cfx-observation-id") == "current"); Assert.Equal("40", point.Attribute("data-cfx-close")!.Value); Assert.Equal("80", point.Attribute("data-cfx-high")!.Value);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync()); await ReduceAndFlushMotionAsync(page);
        if (initial) await page.EvaluateAsync("payload=>financialChart.presentInitial(payload,{sceneRevision:financialChart.sceneRevision,motion:{duration:1000}})", payload);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync()); await page.EvaluateAsync("()=>financialChart.dispose()"); await page.Clock.RunForAsync(1500); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    [BrowserTheory] [InlineData("containing")] [InlineData("missing")]
    public async Task ActualFinancialMalformedTupleRejectsBeforeAcceptedSourceRevisionAndEvents(string condition) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeFinancialComponentBeforeControls.Section(renderer, NativeFinancialComponentBeforeControls.Model(false, true)); var to = NativeFinancialComponentBeforeControls.Section(renderer, NativeFinancialComponentBeforeControls.Model(false, true, 60, 80, 10, 40)); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3));
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), 784, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        var observed = await page.EvaluateAsync<JsonElement>("reply=>{const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')),p=JSON.parse(reply.payload),revision=chart.sceneRevision,source=chart.root.dataset.cfxPreparedChart,frame=p.drawings.wide.frames[1],svg=new DOMParser().parseFromString(frame.svg,'image/svg+xml'),point=svg.querySelector('[data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current]');let events=0,error;chart.root.addEventListener('cfxupdated',()=>events++);if(reply.condition==='containing'){frame.nativeValueSource.readings[1].value=45;point.setAttribute('data-cfx-high','45')}else point.removeAttribute('data-cfx-close');frame.svg=svg.documentElement.outerHTML;try{chart.update(reply.html,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}})}catch(failure){error=failure.message}return {error,revisionUnchanged:chart.sceneRevision===revision,sourceUnchanged:chart.root.dataset.cfxPreparedChart===source,events,layers:chart.root.querySelectorAll('.cfx-native-motion-frame').length,payload:p}}", new { html = to.Html, payload, condition });
        NativeRadialMotionBeforeControls.Capture(from, "financial-malformed-" + condition + "-from"); NativeRadialMotionBeforeControls.Capture(to, "financial-malformed-" + condition + "-to"); NativeRadialMotionBeforeControls.Save("financial-malformed-" + condition + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, "financial-malformed-" + condition + ".browser.png"), FullPage = true });
        Assert.False(string.IsNullOrWhiteSpace(observed.GetProperty("error").GetString())); Assert.True(observed.GetProperty("revisionUnchanged").GetBoolean()); Assert.True(observed.GetProperty("sourceUnchanged").GetBoolean()); Assert.Equal(0, observed.GetProperty("events").GetInt32()); Assert.Equal(0, observed.GetProperty("layers").GetInt32()); AssertNoConsoleErrors(session);
    }
}
