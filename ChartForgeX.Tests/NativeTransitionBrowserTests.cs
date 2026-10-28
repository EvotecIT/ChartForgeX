using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed partial class NativeTransitionBrowserTests {
    public static IEnumerable<object[]> ValueCases() {
        foreach (var pair in new[] { ("progress", HtmlNativeTransitionEffect.InitialValues), ("circle", HtmlNativeTransitionEffect.InitialValues),
            ("progress", HtmlNativeTransitionEffect.ValueFlash), ("bullet-horizontal", HtmlNativeTransitionEffect.Values) }) {
            yield return new object[] { pair.Item1, pair.Item2, 390, true }; yield return new object[] { pair.Item1, pair.Item2, 760, false };
        }
    }
    [BrowserTheory, MemberData(nameof(ValueCases))]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task CapturedInitialFlashAndMultiSeriesValuePlaybackRetainAcceptedFactsAndUseExistingExportReducedAndDisposalLifetime(string kind, HtmlNativeTransitionEffect effect, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var from = ValueSection(kind, width, dark, .2); var to = ValueSection(kind, width, dark, .8);
        var options = new HtmlNativeTransitionOptions(effect, 3, effect == HtmlNativeTransitionEffect.ValueFlash ? ChartColor.FromHex("#FFD900") : null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, options); var initial = effect == HtmlNativeTransitionEffect.InitialValues;
        await using var session = await BrowserProofFonts.OpenAsync(Page(initial ? to : from), width + 24, 680); var page = session.Page;
        await ReadyAndPauseAsync(page); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.nativeChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.nativeEvents=[];window.nativeErrors=[];for(const name of ['cfxupdated','cfxprogresschange'])nativeChart.root.addEventListener(name,event=>nativeEvents.push({name,detail:event.detail}));nativeChart.root.addEventListener('cfxprogresserror',event=>nativeErrors.push(event.detail));}");
        var revision = await page.EvaluateAsync<string>("()=>nativeChart.sceneRevision");
        if (initial) await page.EvaluateAsync("payload=>nativeChart.presentInitial(payload,{sceneRevision:nativeChart.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>nativeChart.update(reply.html,{sceneRevision:nativeChart.sceneRevision,transition:reply.transition,motion:{duration:1000}})", new { html = to.Html, transition = payload });
        await page.Clock.RunForAsync(550);
        await CaptureAsync(page, from, to, options, payload, "native-browser-" + kind + "-" + effect + "-" + width + "-" + dark);
        var rows = kind == "progress" ? "[data-cfx-role=progress-row][data-cfx-observation-id=reviewed]" : kind == "circle" ? "[data-cfx-role=circle-chart]"
            : "[data-cfx-role=bullet-row][data-cfx-series-key=verified]";
        Assert.Equal(kind == "progress" ? "0.8" : "80", await page.Locator(".cfx-stage>svg " + rows).GetAttributeAsync("data-cfx-value"));
        Assert.Equal(effect == HtmlNativeTransitionEffect.ValueFlash ? "0.8" : kind == "progress" ? "0.5" : "50",
            await page.Locator(".cfx-native-motion-frame " + rows).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("1", await page.Locator(".cfx-native-motion-frame").GetAttributeAsync("data-cfx-update-frame"));
        if (initial) Assert.Equal(revision, await page.EvaluateAsync<string>("()=>nativeChart.sceneRevision"));
        else Assert.NotEqual(revision, await page.EvaluateAsync<string>("()=>nativeChart.sceneRevision"));
        Assert.Equal(initial ? 0 : 1, await page.EvaluateAsync<int>("()=>nativeEvents.filter(event=>event.name==='cfxupdated').length"));
        var ink = kind == "progress" ? rows + " [data-cfx-role=progress-fill]" : kind == "circle" ? rows + " [data-cfx-role=circle-value-cap]:first-of-type" : rows + " [data-cfx-role=bullet-value]";
        await NativeValueFrameBrowserProof.AssertAsync(page, ink, "native-browser-ink-" + kind + "-" + effect + "-" + width + "-" + dark);
        await ReduceAndFlushMotionAsync(page); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        if (initial) {
            await page.EvaluateAsync("payload=>nativeChart.presentInitial(payload,{sceneRevision:nativeChart.sceneRevision,motion:{duration:1000}})", payload);
            Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        }
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        if (initial) await page.EvaluateAsync("payload=>nativeChart.presentInitial(payload,{sceneRevision:nativeChart.sceneRevision,motion:{duration:1000}})", payload);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var exported = await download.PathAsync() ?? throw new InvalidOperationException("Native accepted endpoint did not export.");
        Assert.Contains(kind == "progress" ? "data-cfx-value=\"0.8\"" : "data-cfx-value=\"80\"", await File.ReadAllTextAsync(exported), StringComparison.Ordinal);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        if (initial && kind == "progress") {
            await page.EvaluateAsync("()=>{window.nativeRequests=[];nativeChart.connectNative(request=>{nativeRequests.push(request);return Promise.reject(new Error('Expected host boundary observation'));},{basis:'accepted-final',axes:null,progressEditing:true});}");
            await page.EvaluateAsync("()=>nativeChart.setProgressValue({seriesKey:'delivery',observationId:'reviewed'},.9)");
            await page.WaitForFunctionAsync("()=>nativeRequests.length===1&&nativeErrors.length===1");
            Assert.Equal("accepted-final", await page.EvaluateAsync<string>("()=>nativeRequests[0].basis"));
        }
        await page.EvaluateAsync("()=>nativeChart.dispose()"); await page.Clock.RunForAsync(2000);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    [BrowserTheory, InlineData(false, 390, true), InlineData(false, 760, false), InlineData(true, 390, true), InlineData(true, 760, false)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealNativeLegendUsesOnePlotCaptionCompositeAndExistingAxisResetChannel(bool labels, int width, bool dark) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeSeriesVisibilityDefaultControls.Model(labels ? "mixed-labels" : "mixed", width, dark);
        HtmlChartSection Section(bool hide) => renderer.PrepareVisibilitySection(chart, new ChartVisibilityView(hide ? new[] { new ChartSeriesId("observed") } : null,
            labels ? ChartVisibilityLabelLayering.GroupWithSeries : ChartVisibilityLabelLayering.PreserveOverlay), configure: value => Configure(value, dark, false));
        var from = Section(false); var to = Section(true); var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3);
        var payload = renderer.PrepareNativeTransitionJson(from, to, options); var reverse = renderer.PrepareNativeTransitionJson(to, from, options);
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), width + 24, 680); var page = session.Page;
        await ReadyAndPauseAsync(page); await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.nativeChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.nativePending=[];window.nativeErrors=[];nativeChart.root.addEventListener('cfxserieserror',event=>nativeErrors.push(event.detail));nativeChart.root.addEventListener('cfxviewporterror',event=>nativeErrors.push(event.detail));nativeChart.connectNative((request,signal)=>new Promise(resolve=>nativePending.push({request,signal,resolve})),{basis:'visible',axes:['x'],legend:true,motion:{duration:1000}});}");
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=observed]").ClickAsync(); await page.WaitForFunctionAsync("()=>nativePending.length===1");
        Assert.Equal("legend", await page.EvaluateAsync<string>("()=>nativePending[0].request.channel"));
        await page.EvaluateAsync("reply=>nativePending[0].resolve(reply)", new { html = to.Html, basis = "hidden", transition = payload });
        await page.WaitForFunctionAsync("()=>JSON.parse(nativeChart.root.dataset.cfxPreparedChart).visibility.hiddenSeries.length===1"); await page.Clock.RunForAsync(550);
        await CaptureAsync(page, from, to, options, payload, "native-legend-browser-" + labels + "-" + width + "-" + dark);
        Assert.Equal(1, await page.Locator(".cfx-native-motion-frame [opacity='0.5']").CountAsync());
        Assert.Equal(0, await page.Locator(".cfx-series-muted").CountAsync());
        Assert.False(await page.EvaluateAsync<bool>("()=>nativeChart.resolveTargets().some(target=>target.seriesKey==='observed'&&target.targetKind==='point')"));
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>JSON.parse(nativeChart.root.dataset.cfxPreparedChart).series.length"));
        if (labels) Assert.True(await page.EvaluateAsync<bool>("()=>[...document.querySelectorAll('.cfx-native-motion-frame [data-cfx-label-for]')].every(label=>label.closest('[data-cfx-visibility-series-key]'))"));
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-series-key=observed][data-cfx-observation-id=reviewed] [data-cfx-role=bar]", "native-legend-browser-ink-" + labels + "-" + width + "-" + dark);
        await page.Locator("[data-cfx-reset]").ClickAsync(); await page.WaitForFunctionAsync("()=>nativePending.length===2");
        Assert.Equal("viewport", await page.EvaluateAsync<string>("()=>nativePending[1].request.channel"));
        Assert.Equal("reset", await page.EvaluateAsync<string>("()=>nativePending[1].request.kind"));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>nativePending[1].request.sourceVisibility.hiddenSeries.length"));
        Assert.Equal("hidden", await page.EvaluateAsync<string>("()=>nativePending[1].request.basis"));
        await page.EvaluateAsync("reply=>nativePending[1].resolve(reply)", new { html = from.Html, basis = "visible-again", transition = reverse });
        await page.WaitForFunctionAsync("()=>JSON.parse(nativeChart.root.dataset.cfxPreparedChart).visibility.hiddenSeries.length===0");
        await page.EvaluateAsync("()=>nativeChart.dispose()"); await page.Clock.RunForAsync(2000);
        Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); Assert.Empty(await page.EvaluateAsync<JsonElement[]>("()=>nativeErrors")); AssertNoConsoleErrors(session);
    }
    [BrowserTheory, InlineData("duplicate"), InlineData("dangling"), InlineData("wrong-kind"), InlineData("glyph-matrix")]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealGeneratedPngResourcesRejectMalformedCandidatesBeforeInitialPresentationOrAcceptance(string tamper) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer(); var from = ValueSection("progress", 390, true, .2); var to = ValueSection("progress", 390, true, .8);
        var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues, 3); var payload = renderer.PrepareNativeTransitionJson(from, to, options);
        await using var session = await BrowserProofFonts.OpenAsync(Page(to), 414, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        var observation = await page.EvaluateAsync<JsonElement>("""
            ({payload,tamper})=>{const chart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')),revision=chart.sceneRevision,body=chart.root.innerHTML;
              const data=JSON.parse(payload),drawing=data.drawings.narrow,frame=drawing.frames[tamper==='glyph-matrix'?2:1],xml=new DOMParser().parseFromString(frame.svg,'image/svg+xml'),svg=xml.documentElement;
              const glyph=svg.querySelector('defs path[id*="-png-glyph-"]');if(!glyph)throw new Error('The actual native fixture must contain captured glyph resources.');
              if(tamper==='duplicate')glyph.parentNode.append(glyph.cloneNode(true));
              if(tamper==='dangling')svg.querySelector('[data-cfx-png-glyphs]').setAttribute('data-cfx-png-glyphs',frame.prefix+'-png-glyph-999999;matrix(1 0 0 1 0 0)');
              if(tamper==='wrong-kind'){const replacement=xml.createElementNS(svg.namespaceURI,'rect');for(const attr of [...glyph.attributes])replacement.setAttribute(attr.name,attr.value);glyph.replaceWith(replacement);}
              if(tamper==='glyph-matrix'){const text=svg.querySelector('[data-cfx-png-glyphs]');text.setAttribute('data-cfx-png-glyphs',text.getAttribute('data-cfx-png-glyphs').replace(/matrix\(1 0 0 1 ([^ ]+)/,(_,x)=>'matrix(1 0 0 1 '+(Number(x)+1)));}
              frame.svg=new XMLSerializer().serializeToString(svg);window.invalidNativePayload=JSON.stringify(data);let error='';try{chart.presentInitial(invalidNativePayload,{sceneRevision:revision,motion:{duration:1000}});}catch(value){error=value.name+': '+value.message;}
              return {error,revision,after:chart.sceneRevision,bodySame:body===chart.root.innerHTML,layers:chart.root.querySelectorAll('.cfx-native-motion-frame').length};}
            """, new { payload, tamper });
        var invalid = await page.EvaluateAsync<string>("()=>invalidNativePayload");
        await CaptureAsync(page, from, to, options, invalid, "native-resource-reject-" + tamper);
        Assert.StartsWith("TypeError: Native transition:", observation.GetProperty("error").GetString(), StringComparison.Ordinal);
        Assert.Equal(observation.GetProperty("revision").GetString(), observation.GetProperty("after").GetString()); Assert.True(observation.GetProperty("bodySame").GetBoolean()); Assert.Equal(0, observation.GetProperty("layers").GetInt32()); AssertNoConsoleErrors(session);
    }
    private static HtmlChartSection ValueSection(string kind, int width, bool dark, double ratio) => new HtmlInteractiveChartRenderer().PrepareSection(
        NativeTransitionDefaultControls.Model(kind, width, dark, ratio), configure: options => Configure(options, dark, kind == "progress"));
    private static void Configure(HtmlChartInteractionOptions options, bool dark, bool progress) {
        NativeTransitionDefaultControls.Configure(options, dark); options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.LegendToggles);
        options.ProgressEditing.Enabled = progress;
    }
    private static string Page(HtmlChartSection section) => "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + section.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
    private static async Task ReadyAndPauseAsync(IPage page) {
        await page.WaitForFunctionAsync("()=>{const root=document.querySelector('.cfx-interactive-chart');return !!ChartForgeXCharts.get(root)&&(root.dataset.cfxResponsiveLayout!=='adaptive'||root.dataset.cfxAdaptiveDrawing===(root.getBoundingClientRect().width<Number(root.dataset.cfxAdaptiveBreakpoint)?'narrow':'wide'))}");
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc); await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = start }); await page.Clock.PauseAtAsync(start.AddHours(1));
    }
    private static async Task ReduceAndFlushMotionAsync(IPage page) {
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        // Media changes dispatch asynchronously outside the paused page clock. Await the
        // actual cancellation without advancing virtual time to the natural motion endpoint.
        await page.WaitForFunctionAsync("()=>matchMedia('(prefers-reduced-motion: reduce)').matches&&!document.querySelector('.cfx-native-motion-frame')",
            null, new PageWaitForFunctionOptions { PollingInterval = 10 });
    }

}
