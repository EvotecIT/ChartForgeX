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

public sealed partial class NativePhysicalViewportBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390, VisualThemeMode.Dark)]
    [InlineData(true, 900, VisualThemeMode.Light)]
    public async Task AdaptivePhysicalAndLegacyRepliesRequireEveryAlternateProjectionBeforeAcceptance(bool legacy, int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(900, 440).WithFontFamily(BrowserProofFonts.Family).WithTheme(VisualTheme.Graphite(), mode)
            .WithXAxisBounds(0, 4).WithYAxisBounds(0, 100)
            .AddLine("Observed", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40), new ChartPoint(3, 80) });
        void Configure(HtmlChartInteractionOptions options) {
            options.Interaction.ChartId = "adaptive-physical-acceptance";
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Interaction.Enable(ChartInteractionFeatures.Export);
        }
        var viewport = legacy ? new ChartCartesianViewport(x: new ChartAxisWindow(1, 3)) : new ChartCartesianViewport(y: new ChartAxisWindow(20, 80));
        var reply = new HtmlInteractiveChartRenderer().PrepareSection(chart, viewport, Configure).Html;
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlFragment(Configure), width + 24, 1000);
        var page = session.Page;
        await page.EvaluateAsync("""
            legacy=>{window.adaptivePhysical=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                window.pendingAdaptive=[];window.adaptiveChanges=[];window.adaptiveErrors=[];
                adaptivePhysical.root.addEventListener('cfxviewportchange',event=>adaptiveChanges.push(event.detail));
                adaptivePhysical.root.addEventListener('cfxviewporterror',event=>adaptiveErrors.push(event.detail));
                const callback=(request,signal)=>new Promise(resolve=>pendingAdaptive.push({request,signal,resolve}));
                if(legacy)adaptivePhysical.connectXViewport(callback);else adaptivePhysical.connectNative(callback,{basis:'initial',axes:['y']});
                window.beforeAdaptiveRevision=adaptivePhysical.sceneRevision;
                window.beforeAdaptiveSvg=adaptivePhysical.root.querySelector('.cfx-stage svg').outerHTML;
                window.requestAdaptive=()=>legacy?adaptivePhysical.setXWindow(1,3):adaptivePhysical.setViewport({y:{minimum:20,maximum:80}});
                requestAdaptive();}
            """, legacy);
        await page.WaitForFunctionAsync("()=>pendingAdaptive.length===1");
        await page.EvaluateAsync("""
            ({html,legacy})=>{const template=document.createElement('template');template.innerHTML=html;
                const section=template.content.firstElementChild,narrow=section.querySelector('template[data-cfx-adaptive-narrow]').content.firstElementChild;
                const metadata=JSON.parse(narrow.dataset.cfxPreparedChart);delete metadata[legacy?'xProjection':'yProjection'];
                narrow.dataset.cfxPreparedChart=JSON.stringify(metadata);
                pendingAdaptive[0].resolve(legacy?section.outerHTML:{html:section.outerHTML,basis:'invalid'});}
            """, new { html = reply, legacy });
        await page.WaitForFunctionAsync("()=>adaptiveErrors.length===1");
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>adaptiveChanges.length===0&&adaptivePhysical.sceneRevision===beforeAdaptiveRevision
                &&adaptivePhysical.root.querySelector('.cfx-stage svg').outerHTML===beforeAdaptiveSvg
            """));
        await page.EvaluateAsync("()=>requestAdaptive()"); await page.WaitForFunctionAsync("()=>pendingAdaptive.length===2");
        if (!legacy) Assert.Equal("initial", await page.EvaluateAsync<string>("()=>pendingAdaptive[1].request.basis"));
        await page.EvaluateAsync("({html,legacy})=>pendingAdaptive[1].resolve(legacy?html:{html,basis:'accepted'})", new { html = reply, legacy });
        await page.WaitForFunctionAsync("()=>adaptiveChanges.length===1");
        var accepted = await page.EvaluateAsync<JsonElement>("legacy=>legacy?adaptivePhysical.getXWindow():adaptivePhysical.getYWindow()", legacy);
        Window(accepted, legacy ? 1 : 20, legacy ? 3 : 80);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "adaptive-native-acceptance-" + legacy + "-" + width + ".png"), FullPage = true });
        }
        AssertNoConsoleErrors(session);
    }
}
