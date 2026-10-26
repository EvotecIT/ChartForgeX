using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class NativeTransitionBrowserTests {
    [BrowserTheory]
    [InlineData(false, "lower", 390, false)] [InlineData(false, "tuples", 760, true)]
    [InlineData(true, "upper", 760, false)] [InlineData(true, "series", 390, true)]
    public async Task ActualRangeFlashUsesCapturedEnvelopeAndTupleInkWithAcceptedValuesExportsAndExistingLifetime(bool area, string change, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeRangeFlashFixture.Section(renderer, area, false, change, width: width, dark: dark);
        var to = NativeRangeFlashFixture.Section(renderer, area, true, change, width: width, dark: dark); var peak = ChartColor.FromHex("#FFC000");
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, peak);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 5, peak));
        var name = "range-flash-browser-" + area + "-" + change + "-" + width;
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), width + 24, 680); var page = session.Page;
        await ReadyAndPauseAsync(page); await BrowserProofCaptions.InstallNativeAsync(page); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.rangeFlash=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.rangeFlashRevision=rangeFlash.sceneRevision;window.rangeFlashEvents=[];rangeFlash.root.addEventListener('cfxupdated',event=>rangeFlashEvents.push(event.detail));}");
        await page.EvaluateAsync("reply=>rangeFlash.update(reply.html,{sceneRevision:rangeFlash.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(500);
        var observed = await page.EvaluateAsync<JsonElement>("""
            ()=>{const accepted=rangeFlash.root.querySelector('.cfx-stage>svg'),frame=rangeFlash.root.querySelector('.cfx-native-motion-frame svg');
                const tuples=svg=>Array.from(svg.querySelectorAll('[data-cfx-role=point]'),node=>({series:node.dataset.cfxSeriesKey,id:node.dataset.cfxObservationId,lower:node.dataset.cfxLower,upper:node.dataset.cfxUpper}));
                const paint=svg=>Array.from(svg.querySelectorAll('[data-cfx-role^="range-"]')).filter(node=>node.closest('[data-cfx-role=series]')).map(node=>({series:node.closest('[data-cfx-role=series]').dataset.cfxSeriesKey,id:node.closest('[data-cfx-role=point]')?.dataset.cfxObservationId,role:node.dataset.cfxRole,fill:getComputedStyle(node).fill,stroke:getComputedStyle(node).stroke}));
                const labels=svg=>Array.from(svg.querySelectorAll('[data-cfx-caption],text')).filter(node=>!node.parentElement.closest('[data-cfx-caption]') && node.getBoundingClientRect().width>0 && node.getBoundingClientRect().height>0 && getComputedStyle(node).display!=='none').map(node=>{const box=cfxCaptionSvgBox(node);return {text:cfxReferenceLabelText(node),x:String(box.x),y:String(box.y),fill:getComputedStyle(node.querySelector('[data-cfx-native-text-ink]')||node).fill}});
                return {revisionChanged:rangeFlash.sceneRevision!==rangeFlashRevision,events:rangeFlashEvents,metadata:JSON.parse(rangeFlash.root.dataset.cfxPreparedChart),
                    accepted:tuples(accepted),transient:tuples(frame),acceptedPaint:paint(accepted),transientPaint:paint(frame),labels:labels(accepted),transientLabels:labels(frame),frame:frame.outerHTML}}
            """);
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output)
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        Assert.True(observed.GetProperty("revisionChanged").GetBoolean()); Assert.Equal(1, observed.GetProperty("events").GetArrayLength());
        static IEnumerable<(string? Series, string? Id, string? Lower, string? Upper)> Tuples(JsonElement values) => values.EnumerateArray()
            .Select(row => (row.GetProperty("series").GetString(), row.GetProperty("id").GetString(), row.GetProperty("lower").GetString(), row.GetProperty("upper").GetString()));
        static IEnumerable<(string? Text, string? X, string? Y, string? Fill)> Labels(JsonElement values) => values.EnumerateArray()
            .Select(row => (row.GetProperty("text").GetString(), row.GetProperty("x").GetString(), row.GetProperty("y").GetString(), row.GetProperty("fill").GetString()));
        Assert.Equal(Tuples(observed.GetProperty("accepted")), Tuples(observed.GetProperty("transient")));
        Assert.Equal(Labels(observed.GetProperty("labels")), Labels(observed.GetProperty("transientLabels")));
        var accepted = observed.GetProperty("acceptedPaint").EnumerateArray().ToArray(); var transient = observed.GetProperty("transientPaint").EnumerateArray().ToArray(); Assert.Equal(accepted.Length, transient.Length);
        var sharedChanged = 0; var changedMarkers = 0; var unchangedMarkers = 0;
        for (var index = 0; index < accepted.Length; index++) {
            var row = accepted[index]; var actual = transient[index]; var series = row.GetProperty("series").GetString(); var role = row.GetProperty("role").GetString();
            var id = row.TryGetProperty("id", out var point) ? point.GetString() : null;
            var expected = id == null ? series == "delivery" || change == "series" && series == "comparison" : NativeRangeFlashFixture.Changed(series, id, change);
            var differs = row.GetProperty("fill").GetString() != actual.GetProperty("fill").GetString() || row.GetProperty("stroke").GetString() != actual.GetProperty("stroke").GetString();
            if (!expected) { Assert.False(differs); if (role == "range-marker") unchangedMarkers++; }
            else if (role == "range-marker") { Assert.True(differs); changedMarkers++; }
            else if (id == null && role is "range-band" or "range-area" or "range-upper" or "range-lower" or "range-midline") { Assert.True(differs); sharedChanged++; }
        }
        Assert.Equal(change is "tuples" or "series" ? 4 : 2, changedMarkers); Assert.True(unchangedMarkers >= 4); Assert.True(sharedChanged >= 6);
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=series][data-cfx-series-key=delivery] [data-cfx-role=fill-segment][data-cfx-fill-point-start='0'] [data-cfx-role=" + (area ? "range-area" : "range-band") + "]", name + "-envelope");
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current]>[data-cfx-role=range-marker]:first-child", name + "-marker");
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("Accepted interval flash export missing."); var exported = await File.ReadAllTextAsync(path);
        NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exported); Assert.DoesNotContain("native-motion-frame", exported, StringComparison.Ordinal);
        static IEnumerable<string> NativeMarks(string svg) => XDocument.Parse(svg).Descendants()
            .Where(node => ((string?)node.Attribute("data-cfx-role")) is "range-band" or "range-area" or "range-upper" or "range-lower" or "range-midline" or "range-marker")
            .Select(node => string.Join("|", new[] { "data-cfx-role", "d", "cx", "cy", "rx", "ry", "fill", "stroke", "stroke-width" }.Select(key => (string?)node.Attribute(key))));
        Assert.Equal(NativeMarks(to.WideEmission!.Svg), NativeMarks(exported)); Assert.NotEmpty(NativeMarks(exported));
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await ReduceAndFlushMotionAsync(page);
        await page.EvaluateAsync("html=>rangeFlash.update(html,{sceneRevision:rangeFlash.sceneRevision})", from.Html);
        await page.EvaluateAsync("reply=>rangeFlash.update(reply.html,{sceneRevision:rangeFlash.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EvaluateAsync("()=>rangeFlash.dispose()"); await page.Clock.RunForAsync(1500);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }

    [BrowserTheory] [InlineData("component")] [InlineData("tuple")]
    public async Task ActualRangeFlashMalformedFactsStillRejectBeforeAcceptedSourceRevisionAndEvents(string condition) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeRangeFlashFixture.Section(renderer, true, false); var to = NativeRangeFlashFixture.Section(renderer, true, true);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 3, ChartColor.Black));
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), 784, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        var observed = await page.EvaluateAsync<JsonElement>("""
            reply=>{const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')),p=JSON.parse(reply.payload),revision=chart.sceneRevision,source=chart.root.dataset.cfxPreparedChart;
                let events=0,error;chart.root.addEventListener('cfxupdated',()=>events++);
                if(reply.condition==='component')p.readings[0].component='upper';
                else {const frame=p.drawings.wide.frames[1];frame.nativeValueSource.readings[0].value=80;frame.svg=frame.svg.replace('data-cfx-lower="20"','data-cfx-lower="80"').replace('data-cfx-y="20"','data-cfx-y="80"');}
                try{chart.update(reply.html,{sceneRevision:revision,transition:JSON.stringify(p),motion:{duration:1000}})}catch(failure){error=failure.message}
                return {error,revisionUnchanged:chart.sceneRevision===revision,sourceUnchanged:chart.root.dataset.cfxPreparedChart===source,events,layers:chart.root.querySelectorAll('.cfx-native-motion-frame').length,payload:p}}
            """, new { html = to.Html, payload, condition });
        NativeRadialMotionBeforeControls.Capture(from, "range-flash-malformed-" + condition + "-from"); NativeRadialMotionBeforeControls.Capture(to, "range-flash-malformed-" + condition + "-to");
        NativeRadialMotionBeforeControls.Save("range-flash-malformed-" + condition + ".observed.json", observed.GetRawText());
        Assert.False(string.IsNullOrWhiteSpace(observed.GetProperty("error").GetString())); Assert.True(observed.GetProperty("revisionUnchanged").GetBoolean());
        Assert.True(observed.GetProperty("sourceUnchanged").GetBoolean()); Assert.Equal(0, observed.GetProperty("events").GetInt32()); Assert.Equal(0, observed.GetProperty("layers").GetInt32()); AssertNoConsoleErrors(session);
    }
}
