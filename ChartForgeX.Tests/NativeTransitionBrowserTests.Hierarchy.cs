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
    [InlineData(false, false, 760, false, false)] [InlineData(false, false, 390, true, true)]
    [InlineData(true, false, 390, false, true)] [InlineData(true, true, 760, true, false)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealNativeHierarchyValueFramesKeepAcceptedIdentityExportAndInitialLifetime(bool sunburst, bool authored, int width, bool dark, bool initial) {
        if (!Enabled) return; var renderer = new HtmlInteractiveChartRenderer();
        Chart Model(double value) => NativeHierarchyMotionBeforeControls.Model(sunburst, authored, value).WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        HtmlChartSection Section(double value) => renderer.PrepareHierarchySection(Model(value), configure: options => {
            options.Interaction.ChartId = "native-hierarchy-values"; options.Interaction.Enable(ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 420;
        });
        var from = Section(initial ? 0 : 30); var to = Section(40); var payload = renderer.PrepareNativeTransitionJson(from, to,
            new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 5));
        var name = "hierarchy-browser-value-" + sunburst + "-" + authored + "-" + width + "-" + initial;
        await using var session = await BrowserProofFonts.OpenAsync(Page(initial ? to : from), width + 24, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.hmotion=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.hbefore=hmotion.sceneRevision;window.hevents=[];hmotion.root.addEventListener('cfxupdated',event=>hevents.push(event.detail));}");
        if (initial) await page.EvaluateAsync("payload=>hmotion.presentInitial(payload,{sceneRevision:hmotion.sceneRevision,motion:{duration:1000}})", payload);
        else await page.EvaluateAsync("reply=>hmotion.update(reply.html,{sceneRevision:hmotion.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload });
        await page.Clock.RunForAsync(300);
        var observed = await page.EvaluateAsync<JsonElement>("()=>({before:hbefore,revision:hmotion.sceneRevision,events:hevents,accepted:hmotion.root.querySelector('.cfx-stage>svg [data-cfx-node=leaf-a]')?.dataset.cfxAuthoredValue,frame:hmotion.root.querySelector('.cfx-native-motion-frame')?.outerHTML,targets:hmotion.resolveTargets()})");
        NativeRadialValueMotionTests.Capture(from, to, VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null), payload, name); NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            var narrow = VisualValueCapture.NativeFrames(from.NarrowPreparedVisual!, to.NarrowPreparedVisual!, 5, null);
            for (var index = 0; index < narrow.Count; index++) { File.WriteAllText(Path.Combine(output, name + ".narrow-" + index + ".svg"), narrow[index].ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".narrow-" + index + ".png"), narrow[index].ToPng()); }
        }
        Assert.Equal("40", observed.GetProperty("accepted").GetString()); Assert.Equal(initial ? 0 : 1, observed.GetProperty("events").GetArrayLength());
        Assert.Equal(initial, observed.GetProperty("before").GetString() == observed.GetProperty("revision").GetString());
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-node=leaf-a] [data-cfx-role=" + (sunburst ? "sunburst-segment-mark" : "treemap-tile-mark") + "]", name + "-ink");
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync()); var file = await download.PathAsync() ?? throw new InvalidOperationException("Accepted hierarchy export missing.");
        var exportedText = await File.ReadAllTextAsync(file); NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exportedText); var exported = System.Xml.Linq.XDocument.Parse(exportedText);
        Assert.Equal("40", exported.Descendants().Single(row => (string?)row.Attribute("data-cfx-node") == "leaf-a").Attribute("data-cfx-authored-value")!.Value);
        await ReduceAndFlushMotionAsync(page); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EvaluateAsync("()=>hmotion.dispose()"); await page.Clock.RunForAsync(2000); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
    [BrowserTheory]
    [InlineData(false, false, 760, false, false)] [InlineData(false, false, 390, true, true)]
    [InlineData(true, false, 760, true, true)] [InlineData(true, true, 390, false, true)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task ActualNativeHierarchyMembershipAndRootShareTheExistingTransportAndAbsoluteReset(bool sunburst, bool authored, int width, bool dark, bool rootChange) {
        if (!Enabled) return; var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeHierarchyMotionBeforeControls.Model(sunburst, authored, 30).WithLegend(true).WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light); chart.Options.ShowPointLegend = !sunburst;
        HtmlChartSection Section(ChartVisibilityView view) => renderer.PrepareVisibilitySection(chart, view, configure: options => { options.Interaction.ChartId = "native-hierarchy-reflow"; options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.LegendToggles | ChartInteractionFeatures.HierarchyNavigation); });
        var from = Section(NativeHierarchyVisibilityReflowTests.View()); var to = Section(rootChange ? NativeHierarchyVisibilityReflowTests.View(root: "branch") : NativeHierarchyVisibilityReflowTests.View("leaf-a"));
        var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5); var forward = renderer.PrepareNativeTransitionJson(from, to, options); var reverse = renderer.PrepareNativeTransitionJson(to, from, options);
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), width + 24, 680); var page = session.Page;
        await page.WaitForFunctionAsync("()=>!!ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'))");
        await page.EvaluateAsync("()=>{window.hmotion=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.hcalls=[];hmotion.connectNative((request,signal)=>new Promise(resolve=>hcalls.push({request,signal,resolve})),{basis:'full-source',axes:null,hierarchy:true,legend:true,motion:{duration:650}})}");
        if (rootChange) await page.EvaluateAsync("()=>hmotion.setHierarchyView({rootId:'branch'})");
        else await page.Locator("[data-cfx-role=legend-item][data-cfx-legend-target-kind=node][data-cfx-legend-target-id=leaf-a]").ClickAsync();
        await page.WaitForFunctionAsync("()=>hcalls.length===1"); var request = await page.EvaluateAsync<JsonElement>("()=>hcalls[0].request"); var name = "hierarchy-browser-reflow-" + sunburst + "-" + authored + "-" + width + "-" + rootChange;
        NativeReflowVisibilityTests.Capture(from, to, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5), forward, name); NativeRadialMotionBeforeControls.Save(name + ".request.json", request.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".pending.png"), FullPage = true });
        Assert.Equal(rootChange ? "root" : "visibility", request.GetProperty("kind").GetString());
        if (rootChange) Assert.Equal("branch", request.GetProperty("sourceVisibility").GetProperty("hierarchyRootId").GetString());
        else { var target = request.GetProperty("sourceVisibility").GetProperty("hiddenTargets")[0]; Assert.Equal("hierarchyItem", target.GetProperty("kind").GetString()); Assert.Equal("leaf-a", target.GetProperty("nodeId").GetString()); Assert.False(target.TryGetProperty("observationId", out _)); }
        await page.EvaluateAsync("reply=>hcalls[0].resolve(reply)", new { html = to.Html, basis = "changed", transition = forward });
        await page.WaitForFunctionAsync("()=>JSON.parse(hmotion.root.dataset.cfxPreparedChart).visibility.hierarchyRootId==='branch'||document.querySelector('[data-cfx-native-legend][aria-pressed=false]')!==null");
        var accepted = await page.EvaluateAsync<JsonElement>("()=>({header:JSON.parse(hmotion.root.dataset.cfxPreparedChart),nodes:[...hmotion.root.querySelectorAll('.cfx-stage>svg [data-cfx-role=hierarchy-series] [data-cfx-node]')].map(node=>({...node.dataset})),legend:[...hmotion.root.querySelectorAll('[data-cfx-native-legend]')].map(node=>({...node.dataset,pressed:node.getAttribute('aria-pressed')}))})");
        NativeRadialMotionBeforeControls.Save(name + ".accepted.json", accepted.GetRawText());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } acceptedOutput) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(acceptedOutput, name + ".accepted.png"), FullPage = true });
        Assert.Equal(5, accepted.GetProperty("nodes").GetArrayLength());
        await page.Locator("[data-cfx-reset]").ClickAsync(); await page.WaitForFunctionAsync("()=>hcalls.length===2"); var reset = await page.EvaluateAsync<JsonElement>("()=>hcalls[1].request"); NativeRadialMotionBeforeControls.Save(name + ".reset.json", reset.GetRawText());
        Assert.Equal("reset", reset.GetProperty("kind").GetString()); Assert.Equal("changed", reset.GetProperty("basis").GetString()); Assert.Equal(0, reset.GetProperty("sourceVisibility").GetProperty("hiddenTargets").GetArrayLength()); Assert.Equal(JsonValueKind.Null, reset.GetProperty("sourceVisibility").GetProperty("hierarchyRootId").ValueKind);
        await page.EvaluateAsync("reply=>hcalls[1].resolve(reply)", new { html = from.Html, basis = "reset", transition = reverse });
        await page.WaitForFunctionAsync("()=>JSON.parse(hmotion.root.dataset.cfxPreparedChart).visibility.hierarchyRootId===null&&!document.querySelector('[data-cfx-native-legend][aria-pressed=false]')");
        await page.EvaluateAsync("()=>hmotion.dispose()"); AssertNoConsoleErrors(session);
    }
}
