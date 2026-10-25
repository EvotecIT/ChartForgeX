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
    [InlineData("pie", true, 760, false, false)] [InlineData("donut", false, 390, true, false)]
    [InlineData("mixed", true, 390, false, true)] [InlineData("mixed", false, 760, true, true)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealNativeRadialBandsKeepScopedValuesAcceptedExportAndInitialLifetime(string family, bool automatic, int width, bool dark, bool initial) {
        if (!Enabled) return; var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double inner, double outer) => renderer.PrepareSection(NativeRadialBandBeforeControls.Model(family, automatic, inner: inner, outer: outer, width: width, dark: dark, labels: "outside"), configure: options => {
            options.Interaction.ChartId = "native-radial-bands"; options.Interaction.Enable(ChartInteractionFeatures.Export); options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 420;
        });
        var from = Section(60,45); var to = Section(80,55); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5));
        var name = "bands-browser-" + family + "-" + automatic + "-" + width + "-" + dark + "-" + initial;
        await using var session = await BrowserProofFonts.OpenAsync(Page(initial ? to : from), width + 24, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.band=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.bandBefore=band.sceneRevision;window.bandEvents=[];band.root.addEventListener('cfxupdated',event=>bandEvents.push(event.detail));}");
        if (initial) await page.EvaluateAsync("payload=>band.presentInitial(payload,{sceneRevision:band.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>band.update(reply.html,{sceneRevision:band.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(300);
        var observed = await page.EvaluateAsync<JsonElement>("()=>({before:bandBefore,revision:band.sceneRevision,events:bandEvents,rows:[...band.root.querySelectorAll('.cfx-stage>svg [data-cfx-role=radial-point][data-cfx-observation-id=primary]')].map(node=>({...node.dataset})),frame:band.root.querySelector('.cfx-native-motion-frame')?.outerHTML,targets:band.resolveTargets()})");
        NativeRadialValueMotionTests.Capture(from, to, VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null), payload, name); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            var narrow = VisualValueCapture.NativeFrames(from.NarrowPreparedVisual!, to.NarrowPreparedVisual!, 5, null);
            for (var index = 0; index < narrow.Count; index++) { File.WriteAllText(Path.Combine(output, name + ".narrow-" + index + ".svg"), narrow[index].ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".narrow-" + index + ".png"), narrow[index].ToPng()); }
        }
        var rows = observed.GetProperty("rows").EnumerateArray().ToArray(); Assert.Equal(2, rows.Length);
        Assert.Equal("80", rows.Single(row => row.GetProperty("cfxSeriesKey").GetString() == "parts").GetProperty("cfxValue").GetString()); Assert.Equal("55", rows.Single(row => row.GetProperty("cfxSeriesKey").GetString() == "outer").GetProperty("cfxValue").GetString());
        Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength()); Assert.Equal(initial, observed.GetProperty("before").GetString() == observed.GetProperty("revision").GetString());
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-role=radial-point][data-cfx-series-key=outer][data-cfx-observation-id=primary] [data-cfx-role=donut-slice]", name + "-ink");
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync()); var file = await download.PathAsync() ?? throw new InvalidOperationException("Accepted native bands export missing.");
        var exported = await File.ReadAllTextAsync(file); NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exported);
        var native = System.Xml.Linq.XDocument.Parse(exported); var outerExport = native.Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "radial-point" && (string?)row.Attribute("data-cfx-series-key") == "outer" && (string?)row.Attribute("data-cfx-observation-id") == "primary"); Assert.Equal("55", (string?)outerExport.Attribute("data-cfx-value"));
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce }); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EvaluateAsync("()=>band.dispose()"); await page.Clock.RunForAsync(2000); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    [BrowserTheory] [InlineData("mixed", true, 390, true)] [InlineData("donut", false, 760, false)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealNativeBandOtherLegendRetainsItsOwnContributorsAndUsesTheExistingAbsoluteReset(string family, bool automatic, int width, bool dark) {
        if (!Enabled) return; var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeRadialBandBeforeControls.Model(family, automatic, other: true, width: width, dark: dark, labels: "outside");
        HtmlChartSection Section(bool hidden) => renderer.PrepareVisibilitySection(chart, NativeRadialBandBeforeControls.View(hidden ? new[] { ChartVisibilityTarget.RadialOther(new ChartSeriesId("outer")) } : Array.Empty<ChartVisibilityTarget>()), configure: options => {
            options.Interaction.ChartId = "native-band-membership"; options.Interaction.Enable(ChartInteractionFeatures.LegendToggles | ChartInteractionFeatures.Export);
        });
        var from = Section(false); var to = Section(true); var transitionOptions = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5); var forward = renderer.PrepareNativeTransitionJson(from,to,transitionOptions); var reverse = renderer.PrepareNativeTransitionJson(to,from,transitionOptions);
        var name = "bands-other-browser-" + family + "-" + automatic + "-" + width;
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), width + 24, 680); var page = session.Page;
        await page.WaitForFunctionAsync("()=>!!ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'))");
        await page.EvaluateAsync("()=>{window.band=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.bandCalls=[];band.connectNative((request,signal)=>new Promise(resolve=>bandCalls.push({request,signal,resolve})),{basis:'full',axes:null,legend:true,motion:{duration:650}})}");
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=outer][data-cfx-point='-1']").ClickAsync(); await page.WaitForFunctionAsync("()=>bandCalls.length===1");
        var request = await page.EvaluateAsync<JsonElement>("()=>bandCalls[0].request"); NativeReflowVisibilityTests.Capture(from,to,VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual,to.PreparedVisual,5),forward,name); NativeRadialMotionBeforeControls.Save(name + ".request.json",request.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output,name + ".pending.png"),FullPage = true });
        Assert.Equal("outer",request.GetProperty("sourceVisibility").GetProperty("hiddenTargets")[0].GetProperty("seriesKey").GetString()); Assert.Equal("radialOther",request.GetProperty("sourceVisibility").GetProperty("hiddenTargets")[0].GetProperty("kind").GetString());
        await page.EvaluateAsync("reply=>bandCalls[0].resolve(reply)",new { html = to.Html,basis = "outer-hidden",transition = forward }); await page.WaitForFunctionAsync("()=>JSON.parse(band.root.dataset.cfxPreparedChart).visibility.hiddenTargets.length===1");
        var accepted = await page.EvaluateAsync<JsonElement>("()=>({rows:[...band.root.querySelectorAll('.cfx-stage>svg [data-cfx-role=radial-point]')].map(node=>({...node.dataset})),legend:[...band.root.querySelectorAll('[data-cfx-native-legend]')].map(node=>({...node.dataset,pressed:node.getAttribute('aria-pressed')}))})"); NativeRadialMotionBeforeControls.Save(name + ".accepted.json",accepted.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } acceptedOutput) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(acceptedOutput,name + ".accepted.png"),FullPage = true });
        Assert.Equal(3,accepted.GetProperty("rows").EnumerateArray().Count(row=>row.GetProperty("cfxSeriesKey").GetString()=="parts")); Assert.Equal(2,accepted.GetProperty("rows").EnumerateArray().Count(row=>row.GetProperty("cfxSeriesKey").GetString()=="outer"));
        var other = accepted.GetProperty("legend").EnumerateArray().Single(row=>row.GetProperty("cfxSeriesKey").GetString()=="outer" && row.GetProperty("cfxPoint").GetString()=="-1"); Assert.Equal("2,3,4",other.GetProperty("cfxSourcePoints").GetString()); Assert.Equal("false",other.GetProperty("pressed").GetString());
        await page.Locator("[data-cfx-reset]").ClickAsync(); await page.WaitForFunctionAsync("()=>bandCalls.length===2"); var reset = await page.EvaluateAsync<JsonElement>("()=>bandCalls[1].request"); NativeRadialMotionBeforeControls.Save(name + ".reset.json",reset.GetRawText());
        Assert.Equal("reset",reset.GetProperty("kind").GetString()); Assert.Equal("legend",reset.GetProperty("channel").GetString()); Assert.Equal("outer-hidden",reset.GetProperty("basis").GetString()); Assert.Equal(0,reset.GetProperty("sourceVisibility").GetProperty("hiddenTargets").GetArrayLength());
        await page.EvaluateAsync("reply=>bandCalls[1].resolve(reply)",new { html = from.Html,basis = "restored",transition = reverse }); await page.WaitForFunctionAsync("()=>JSON.parse(band.root.dataset.cfxPreparedChart).visibility.hiddenTargets.length===0");
        await page.EvaluateAsync("()=>band.dispose()"); AssertNoConsoleErrors(session);
    }
}
