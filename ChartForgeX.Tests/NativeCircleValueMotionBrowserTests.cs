using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises actual typed native circle replies through the existing direct host update owner.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeCircleValueMotionBrowserTests {
    private const string Live = ".cfx-stage > svg [data-cfx-role=circle-chart][data-cfx-observation-id=current]";
    private const string Layer = ".cfx-native-motion-frame";
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(390, true)] [InlineData(760, false)]
    public async Task TypedCircleCurrentValueFramesKeepNativeStatusPaintSelectionAndFinalExportsAcrossSupersession(int width, bool dark) {
        if (!Enabled) return;
        var host = new CircleHost(width, dark);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 680);
        var page = session.Page; await ConnectAsync(page, host, true);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = start }); await page.Clock.PauseAtAsync(start.AddHours(1));
        await page.EvaluateAsync("()=>circle.select({targets:[circle.resolveTargets().find(target=>target.observationId==='current')]},{silent:true})");
        var targets = await page.EvaluateAsync<int>("()=>circle.resolveTargets().length");
        var basis = await CommitAsync(page, 90); await page.Clock.RunForAsync(550);
        Assert.Equal("90", await page.Locator(Live).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("1", await page.Locator(Layer).GetAttributeAsync("data-cfx-update-frame"));
        Assert.Equal("65", await page.Locator(Layer + " [data-cfx-observation-id=current]").GetAttributeAsync("data-cfx-value"));
        Assert.Equal("positive", await page.Locator(Live).GetAttributeAsync("data-cfx-status"));
        Assert.Equal("warning", await page.Locator(Layer + " [data-cfx-role=circle-chart]").GetAttributeAsync("data-cfx-status"));
        Assert.Equal("warning", await page.Locator(Layer + " [data-cfx-role=circle-status-label-source]").GetAttributeAsync("data-cfx-label"));
        Assert.Equal("65", await page.Locator(Layer + " [data-cfx-role=circle-label-source]").GetAttributeAsync("data-cfx-label"));
        Assert.Equal("true", await page.Locator(Layer).GetAttributeAsync("aria-hidden"));
        Assert.True(await page.Locator(Layer).EvaluateAsync<bool>("node=>node.hasAttribute('inert')&&getComputedStyle(node).pointerEvents==='none'"));
        Assert.Equal(targets, await page.EvaluateAsync<int>("()=>circle.resolveTargets().length"));
        Assert.Equal(90, await page.EvaluateAsync<double>("()=>circle.getSelection()[0].numericValue"));
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-observation-id=current] [data-cfx-role=circle-value]", "circle-frame-ink-" + width + "-" + dark, .05);
        await CaptureAsync(page, host, basis, "circle-value-midpoint-" + width + "-" + dark);
        var download = await page.RunAndWaitForDownloadAsync(() => page.EvaluateAsync("()=>circle.root.querySelector('[data-cfx-export=svg]').click()"));
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The accepted Circle did not export.");
        var svg = XDocument.Load(path);
        Assert.Equal("90", Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "circle-chart"
            && (string?)element.Attribute("data-cfx-observation-id") == "current").Attribute("data-cfx-value")?.Value);
        Assert.DoesNotContain(svg.Descendants(), element => element.Attribute("data-cfx-update-frame") != null);
        Assert.Equal(0, await page.Locator(Layer).CountAsync());
        await CommitAsync(page, 95); await page.EvaluateAsync("()=>window.oldFrame=circle.root.querySelector('.cfx-native-motion-frame')");
        await CommitAsync(page, 100);
        Assert.True(await page.EvaluateAsync<bool>("()=>!oldFrame.isConnected")); Assert.Equal(1, await page.Locator(Layer).CountAsync());
        Assert.Equal("100", await page.Locator(Live).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("current", await page.EvaluateAsync<string>("()=>circle.getSelection()[0].observationId"));
        await page.EvaluateAsync("()=>circle.resize()"); Assert.Equal(0, await page.Locator(Layer).CountAsync());
        await CommitAsync(page, 95);
        await page.EvaluateAsync("()=>circle.root.querySelector('[data-cfx-reset]').dispatchEvent(new MouseEvent('click',{bubbles:true}))");
        Assert.Equal(0, await page.Locator(Layer).CountAsync()); Assert.Equal("95", await page.Locator(Live).GetAttributeAsync("data-cfx-value"));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>circle.getSelection().length"));
        await CommitAsync(page, 90); Assert.Equal(1, await page.Locator(Layer).CountAsync());
        await page.EvaluateAsync("()=>circle.dispose()"); await page.Clock.RunForAsync(2000);
        Assert.Equal(0, await page.Locator(Layer).CountAsync()); Assert.Equal("90", await page.Locator(Live).GetAttributeAsync("data-cfx-value"));
        Assert.Equal(0, await page.Locator("[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false)] [InlineData(true)]
    public async Task RejectedCancelledStaleAndDisposedCirclePreparationsKeepTheAcceptedSourceAndReducedMotionCommitsWithoutFrames(bool managed) {
        if (!Enabled) return;
        var host = new CircleHost(760, false);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, 784, 680);
        var page = session.Page; await ConnectAsync(page, host, managed);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        Assert.True(await page.EvaluateAsync<bool>("""
            async()=>{const revision=circle.sceneRevision,html=circle.root.innerHTML,basis=circleBasis;
                for(const options of [{reject:true},{cancel:true}]){
                    try{await prepareCircleValue(90,options);return false}catch{}
                    if(circle.sceneRevision!==revision||circle.root.innerHTML!==html||circleBasis!==basis)return false;
                }return !circle.root.querySelector('.cfx-native-motion-frame')}
            """));
        await page.EvaluateAsync("async()=>{window.held=await prepareCircleValue(90);window.beforeRevision=circle.sceneRevision;window.beforeHtml=circle.root.innerHTML}");
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>{try{circle.update(held.reply.html,{basis:held.reply.basis,transition:held.reply.transition,motion:{duration:1000}});return false}
                catch(error){return error instanceof TypeError&&circle.sceneRevision===beforeRevision&&circle.root.innerHTML===beforeHtml}}
            """));
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>{const payload=JSON.parse(held.reply.transition);payload.wide[1]=payload.wide[1].replace('data-cfx-observation-id="current"','data-cfx-observation-id="wrong"');
                try{circle.update(held.reply.html,{basis:held.reply.basis,sceneRevision:held.sceneRevision,transition:JSON.stringify(payload),motion:{duration:1000}});return false}
                catch(error){return error instanceof TypeError&&circle.sceneRevision===beforeRevision&&circle.root.innerHTML===beforeHtml}}
            """));
        // The source values are deliberately identical: only the committed observation/basis changed.
        var external = host.External(40);
        await page.EvaluateAsync("reply=>{circle.update(reply.html,{basis:reply.basis});circleBasis=reply.basis;}", external);
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>{const revision=circle.sceneRevision,html=circle.root.innerHTML,basis=circleBasis;
                try{acceptCircleValue(held);return false}catch(error){return error instanceof TypeError&&circle.sceneRevision===revision
                    &&circle.root.innerHTML===html&&circleBasis===basis&&revision!==held.sceneRevision}}
            """));
        Assert.Equal(external.basis, await page.EvaluateAsync<string>("()=>circleBasis"));
        await CommitAsync(page, 90); Assert.Equal("90", await page.Locator(Live).GetAttributeAsync("data-cfx-value"));
        Assert.Equal(0, await page.Locator(Layer).CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            async()=>{const pending=await prepareCircleValue(100),basis=circleBasis;circle.dispose();
                try{acceptCircleValue(pending);return false}catch{return circleBasis===basis&&!circle.root.querySelector('.cfx-native-motion-frame')}}
            """));
        Assert.Equal("90", await page.Locator(Live).GetAttributeAsync("data-cfx-value")); AssertNoConsoleErrors(session);
    }

    private static async Task ConnectAsync(IPage page, CircleHost host, bool managed) {
        await page.ExposeFunctionAsync("prepareNativeCircle", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("""
            managed=>{window.circle=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));window.circleBasis='initial';
                if(managed)circle.connectNative(()=>{throw new Error('No annotation command is configured for this radial source')},{basis:'initial',axes:null});
                window.prepareCircleValue=async(value,options={})=>{const sceneRevision=circle.sceneRevision,basis=circleBasis;
                    const reply=await prepareNativeCircle({basis,value,...options});return {reply,sceneRevision}};
                window.acceptCircleValue=prepared=>{const reply=prepared.reply;circle.update(reply.html,{basis:reply.basis,
                    sceneRevision:prepared.sceneRevision,transition:reply.transition,motion:{duration:1000}});circleBasis=reply.basis};}
            """, managed);
    }
    private static Task<string> CommitAsync(IPage page, double value) => page.EvaluateAsync<string>(
        "async value=>{const prepared=await prepareCircleValue(value);acceptCircleValue(prepared);return prepared.reply.basis}", value);
    private static async Task CaptureAsync(IPage page, CircleHost host, string basis, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Capture output must be absolute.");
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        await File.WriteAllTextAsync(stem + ".json", await page.EvaluateAsync<string>("()=>JSON.stringify({revision:circle.sceneRevision,basis:circleBasis,selection:circle.getSelection(),frame:circle.root.querySelector('.cfx-native-motion-frame')?.outerHTML},null,2)"));
        File.WriteAllText(stem + ".native.svg", host.Section(basis).PreparedVisual.ToSvg());
        File.WriteAllBytes(stem + ".native.png", host.Section(basis).PreparedVisual.ToPng());
        var midpoint = host.Midpoint(); File.WriteAllBytes(stem + ".midpoint.native.png", midpoint.PreparedVisual.ToPng());
        if (midpoint.NarrowPreparedVisual is { } narrow) File.WriteAllBytes(stem + ".midpoint.narrow.native.png", narrow.ToPng());
    }
    private sealed class CircleHost {
        private readonly int _width; private readonly bool _dark;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        private readonly Dictionary<string, HtmlChartSection> _snapshots = new();
        internal CircleHost(int width, bool dark) { _width = width; _dark = dark; _snapshots.Add("initial", PrepareSection(40)); }
        internal string Page => "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + Section("initial").Html
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
        internal HtmlChartSection Section(string basis) => _snapshots[basis];
        internal HtmlChartSection Midpoint() => PrepareSection(65);
        internal Reply External(double value) {
            var next = PrepareSection(value); var basis = "circle-" + _snapshots.Count; _snapshots.Add(basis, next); return new Reply(next.Html, basis);
        }
        internal Reply Prepare(JsonElement request) {
            var before = Section(request.GetProperty("basis").GetString()!);
            if (request.TryGetProperty("reject", out var reject) && reject.GetBoolean()) throw new InvalidOperationException("The source rejected this replacement.");
            var next = PrepareSection(request.GetProperty("value").GetDouble());
            // Actual typed UpdateGaugeValue and native PrepareSection own every frame.
            using var cancellation = new CancellationTokenSource();
            if (request.TryGetProperty("cancel", out var cancel) && cancel.GetBoolean()) cancellation.Cancel();
            var transition = _renderer.PrepareValueTransitionJson(before, next, 3, cancellation.Token);
            var basis = "circle-" + _snapshots.Count; _snapshots.Add(basis, next); return new Reply(next.Html, basis, transition);
        }
        private HtmlChartSection PrepareSection(double value) => _renderer.PrepareSection(
            NativeCircleValueTransitionTests.Model(_width, _dark, value, BrowserProofFonts.Family), configure: options => {
                NativeCircleValueTransitionTests.Configure(options);
                options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation);
                options.Tooltip.PinOnSelect = false;
            });
        internal sealed record Reply(string html, string basis, string? transition = null);
    }
}
