using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises actual typed native layer replies through the existing direct host update owner.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeLayeredValueMotionBrowserTests {
    private const string Live = ".cfx-stage > svg [data-cfx-role=layered-radial-point][data-cfx-observation-id=complete]";
    private const string Layer = ".cfx-native-motion-frame";
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(390, true)] [InlineData(760, false)]
    public async Task TypedLayerCurrentValueFramesPreserveNativeGeometrySelectionAndFinalExportsAcrossSupersession(int width, bool dark) {
        if (!Enabled) return;
        var host = new LayerHost(width, dark);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 680);
        var page = session.Page; await ConnectAsync(page, host, true);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = start }); await page.Clock.PauseAtAsync(start.AddHours(1));
        await page.EvaluateAsync("()=>layered.select({targets:[layered.resolveTargets().find(target=>target.observationId==='complete')]},{silent:true})");
        var targets = await page.EvaluateAsync<int>("()=>layered.resolveTargets().length");
        var basis = await CommitAsync(page, 80); await page.Clock.RunForAsync(550);
        Assert.Equal("80", await page.Locator(Live).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("1", await page.Locator(Layer).GetAttributeAsync("data-cfx-update-frame"));
        Assert.Equal("50", await page.Locator(Layer + " [data-cfx-observation-id=complete]").GetAttributeAsync("data-cfx-value"));
        Assert.Equal("true", await page.Locator(Layer).GetAttributeAsync("aria-hidden"));
        Assert.True(await page.Locator(Layer).EvaluateAsync<bool>("node=>node.hasAttribute('inert')&&getComputedStyle(node).pointerEvents==='none'"));
        Assert.Equal(targets, await page.EvaluateAsync<int>("()=>layered.resolveTargets().length"));
        Assert.Equal(80, await page.EvaluateAsync<double>("()=>layered.getSelection()[0].numericValue"));
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-observation-id=complete] [data-cfx-role=layered-radial-layer]", "layered-frame-ink-" + width + "-" + dark, .05);
        await CaptureAsync(page, host, basis, "layer-value-midpoint-" + width + "-" + dark);
        var download = await page.RunAndWaitForDownloadAsync(() => page.EvaluateAsync("()=>layered.root.querySelector('[data-cfx-export=svg]').click()"));
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The accepted LayeredRadial did not export.");
        var svg = XDocument.Load(path);
        Assert.Equal("80", Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "layered-radial-point"
            && (string?)element.Attribute("data-cfx-observation-id") == "complete").Attribute("data-cfx-value")?.Value);
        Assert.DoesNotContain(svg.Descendants(), element => element.Attribute("data-cfx-update-frame") != null);
        Assert.Equal(0, await page.Locator(Layer).CountAsync());
        await CommitAsync(page, 90); await page.EvaluateAsync("()=>window.oldFrame=layered.root.querySelector('.cfx-native-motion-frame')");
        await CommitAsync(page, 100);
        Assert.True(await page.EvaluateAsync<bool>("()=>!oldFrame.isConnected")); Assert.Equal(1, await page.Locator(Layer).CountAsync());
        Assert.Equal("100", await page.Locator(Live).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("complete", await page.EvaluateAsync<string>("()=>layered.getSelection()[0].observationId"));
        await page.EvaluateAsync("()=>layered.resize()"); Assert.Equal(0, await page.Locator(Layer).CountAsync());
        await page.EvaluateAsync("()=>layered.dispose()"); AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false)] [InlineData(true)]
    public async Task DirectNativeRepliesRejectMissingStaleOrMalformedSourceObservationsBeforeBasisMutationAndCannotCommitAfterDisposal(bool managed) {
        if (!Enabled) return;
        var host = new LayerHost(760, false);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, 784, 680);
        var page = session.Page; await ConnectAsync(page, host, managed);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        await page.EvaluateAsync("async()=>{window.held=await prepareLayerValue(80);window.beforeRevision=layered.sceneRevision;window.beforeHtml=layered.root.innerHTML}");
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>{try{layered.update(held.reply.html,{basis:held.reply.basis,transition:held.reply.transition,motion:{duration:1000}});return false}
                catch(error){return error instanceof TypeError&&layered.sceneRevision===beforeRevision&&layered.root.innerHTML===beforeHtml}}
            """));
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>{const payload=JSON.parse(held.reply.transition);payload.wide[1]=payload.wide[1].replace('data-cfx-observation-id="complete"','data-cfx-observation-id="wrong"');
                try{layered.update(held.reply.html,{basis:held.reply.basis,sceneRevision:held.sceneRevision,transition:JSON.stringify(payload),motion:{duration:1000}});return false}
                catch(error){return error instanceof TypeError&&layered.sceneRevision===beforeRevision&&layered.root.innerHTML===beforeHtml}}
            """));
        // The source values are deliberately identical: only the committed observation/basis changed.
        var external = host.External(20);
        await page.EvaluateAsync("reply=>{layered.update(reply.html,{basis:reply.basis});layerBasis=reply.basis;}", external);
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>{const revision=layered.sceneRevision,html=layered.root.innerHTML,basis=layerBasis;
                try{acceptLayerValue(held);return false}catch(error){return error instanceof TypeError&&layered.sceneRevision===revision
                    &&layered.root.innerHTML===html&&layerBasis===basis&&revision!==held.sceneRevision}}
            """));
        Assert.Equal(external.basis, await page.EvaluateAsync<string>("()=>layerBasis"));
        await CommitAsync(page, 80); Assert.Equal("80", await page.Locator(Live).GetAttributeAsync("data-cfx-value"));
        Assert.Equal(0, await page.Locator(Layer).CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            async()=>{const pending=await prepareLayerValue(90),basis=layerBasis;layered.dispose();
                try{acceptLayerValue(pending);return false}catch{return layerBasis===basis&&!layered.root.querySelector('.cfx-native-motion-frame')}}
            """));
        Assert.Equal("80", await page.Locator(Live).GetAttributeAsync("data-cfx-value")); AssertNoConsoleErrors(session);
    }

    private static async Task ConnectAsync(IPage page, LayerHost host, bool managed) {
        await page.ExposeFunctionAsync("prepareNativeLayer", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("""
            managed=>{window.layered=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));window.layerBasis='initial';
                if(managed)layered.connectNative(()=>{throw new Error('No annotation command is configured for this radial source')},{basis:'initial',axes:null});
                window.prepareLayerValue=async value=>{const sceneRevision=layered.sceneRevision,basis=layerBasis;
                    const reply=await prepareNativeLayer({basis,value});return {reply,sceneRevision}};
                window.acceptLayerValue=prepared=>{const reply=prepared.reply;layered.update(reply.html,{basis:reply.basis,
                    sceneRevision:prepared.sceneRevision,transition:reply.transition,motion:{duration:1000}});layerBasis=reply.basis};}
            """, managed);
    }
    private static Task<string> CommitAsync(IPage page, double value) => page.EvaluateAsync<string>(
        "async value=>{const prepared=await prepareLayerValue(value);acceptLayerValue(prepared);return prepared.reply.basis}", value);
    private static async Task CaptureAsync(IPage page, LayerHost host, string basis, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Capture output must be absolute.");
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        await File.WriteAllTextAsync(stem + ".json", await page.EvaluateAsync<string>("()=>JSON.stringify({revision:layered.sceneRevision,basis:layerBasis,selection:layered.getSelection(),frame:layered.root.querySelector('.cfx-native-motion-frame')?.outerHTML},null,2)"));
        File.WriteAllText(stem + ".native.svg", host.Section(basis).PreparedVisual.ToSvg());
        File.WriteAllBytes(stem + ".native.png", host.Section(basis).PreparedVisual.ToPng());
        var midpoint = host.Midpoint(); File.WriteAllBytes(stem + ".midpoint.native.png", midpoint.PreparedVisual.ToPng());
        if (midpoint.NarrowPreparedVisual is { } narrow) File.WriteAllBytes(stem + ".midpoint.narrow.native.png", narrow.ToPng());
    }
    private sealed class LayerHost {
        private readonly int _width; private readonly bool _dark;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        private readonly Dictionary<string, HtmlChartSection> _snapshots = new();
        internal LayerHost(int width, bool dark) { _width = width; _dark = dark; _snapshots.Add("initial", PrepareSection(20)); }
        internal string Page => "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + Section("initial").Html
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
        internal HtmlChartSection Section(string basis) => _snapshots[basis];
        internal HtmlChartSection Midpoint() => PrepareSection(50);
        internal Reply External(double value) {
            var next = PrepareSection(value); var basis = "layer-" + _snapshots.Count; _snapshots.Add(basis, next); return new Reply(next.Html, basis);
        }
        internal Reply Prepare(JsonElement request) {
            var before = Section(request.GetProperty("basis").GetString()!); var next = External(request.GetProperty("value").GetDouble());
            // Actual typed UpdateRadialLayer and native PrepareSection own every frame.
            return next with { transition = _renderer.PrepareValueTransitionJson(before, Section(next.basis), 3) };
        }
        private HtmlChartSection PrepareSection(double value) => _renderer.PrepareSection(
            NativeLayeredValueTransitionTests.Model(_width, _dark, 2, value, BrowserProofFonts.Family), configure: options => {
                NativeLayeredValueTransitionTests.Configure(options);
                options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation);
                options.Tooltip.PinOnSelect = false;
            });
        internal sealed record Reply(string html, string basis, string? transition = null);
    }
}
