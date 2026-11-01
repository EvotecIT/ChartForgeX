using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>A real typed Progress host supplies complete native observations; the browser only schedules them.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed partial class ManagedProgressMotionBrowserTests {
    private const string LiveRow = ".cfx-stage > svg [data-cfx-role=progress-row][data-cfx-observation-id=reviewed]";
    private const string Layer = ".cfx-native-motion-frame";

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(390, true)]
    [InlineData(760, false)]
    public async Task CapturedNativeFramesKeepFinalFactsSelectionAndExportsWhileResetResizeAndDisposalReleasePresentation(int width, bool dark) {
        if (!Enabled) return;
        var host = new ProgressHost(width, dark);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 640);
        var page = session.Page; await ConnectAsync(page, host); await PauseAsync(page);
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = ".native-motion-scroll .cfx-stage>svg{width:900px!important;max-width:none!important}.native-motion-scroll .cfx-stage{overflow-x:auto}" });
        await page.EvaluateAsync("scroll=>{document.body.style.padding='17px';document.body.style.boxSizing='border-box';if(scroll)document.body.classList.add('native-motion-scroll')}", width == 760);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        var targets = await page.EvaluateAsync<int>("()=>progress.resolveTargets().length");
        await page.EvaluateAsync("()=>progress.select({targets:[progress.resolveTargets().find(target=>target.observationId==='reviewed')]},{silent:true})");
        await RequestAsync(page, .75); var basis = await ResolveAsync(page, 0);
        Assert.Equal("0.75", await page.Locator(LiveRow).GetAttributeAsync("data-cfx-value"));
        await page.Clock.RunForAsync(550);
        var observation = await page.Locator(Layer).EvaluateAsync<JsonElement>("""
            layer=>({index:layer.dataset.cfxUpdateFrame,reading:layer.querySelector('[data-cfx-observation-id=reviewed]').dataset.cfxValue,
                passive:layer.dataset.cfxPassive,aria:layer.getAttribute('aria-hidden'),inert:layer.hasAttribute('inert'),
                pointer:getComputedStyle(layer).pointerEvents})
            """);
        Assert.Equal("1", observation.GetProperty("index").GetString()); Assert.Equal("0.625", observation.GetProperty("reading").GetString());
        if (width == 760) {
            await page.EvaluateAsync("()=>progress.root.querySelector('.cfx-stage').scrollLeft=23");
            Assert.True(await page.EvaluateAsync<bool>("()=>progress.root.querySelector('.cfx-stage').scrollLeft>0"));
        }
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-observation-id=reviewed] [data-cfx-role=progress-fill]", "progress-frame-ink-" + width + "-" + dark);
        Assert.Equal("true", observation.GetProperty("passive").GetString()); Assert.Equal("true", observation.GetProperty("aria").GetString());
        Assert.True(observation.GetProperty("inert").GetBoolean()); Assert.Equal("none", observation.GetProperty("pointer").GetString());
        Assert.Equal(targets, await page.EvaluateAsync<int>("()=>progress.resolveTargets().length"));
        Assert.Equal("reviewed", await page.EvaluateAsync<string>("()=>progress.getSelection()[0].observationId"));
        Assert.Equal(.75, await page.EvaluateAsync<double>("()=>progress.getSelection()[0].numericValue"));
        await CaptureAsync(page, host, "native-progress-frame-" + width + "-" + dark, basis);
        var download = await page.RunAndWaitForDownloadAsync(() => page.EvaluateAsync("()=>progress.root.querySelector('[data-cfx-export=svg]').click()"));
        var downloaded = await download.PathAsync() ?? throw new InvalidOperationException("The committed Progress scene did not export.");
        var exported = XDocument.Load(downloaded);
        Assert.Equal("0.75", Assert.Single(exported.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "progress-row"
            && (string?)element.Attribute("data-cfx-observation-id") == "reviewed").Attribute("data-cfx-value")?.Value);
        Assert.DoesNotContain(exported.Descendants(), element => element.Attribute("data-cfx-update-frame") != null);
        Assert.Equal(0, await page.Locator(Layer).CountAsync());
        await RequestAsync(page, .8); await ResolveAsync(page, 1);
        await page.EvaluateAsync("()=>progress.clearSelection({silent:true})"); Assert.Equal(0, await page.Locator(Layer).CountAsync());
        await RequestAsync(page, .85); await ResolveAsync(page, 2);
        await page.EvaluateAsync("()=>progress.resize()"); Assert.Equal(0, await page.Locator(Layer).CountAsync());
        await RequestAsync(page, .9); await ResolveAsync(page, 3);
        await page.EvaluateAsync("()=>progress.root.querySelector('[data-cfx-reset]').dispatchEvent(new MouseEvent('click',{bubbles:true}))");
        Assert.Equal(0, await page.Locator(Layer).CountAsync()); Assert.Empty(await page.EvaluateAsync<JsonElement[]>("()=>progress.getSelection()"));
        await RequestAsync(page, .95); await ResolveAsync(page, 4);
        Assert.Equal(1, await page.Locator(Layer).CountAsync());
        await page.EvaluateAsync("()=>progress.dispose()"); await page.Clock.RunForAsync(2000);
        Assert.Equal(0, await page.Locator(Layer).CountAsync()); Assert.Equal(0, await page.Locator("[data-cfx-update-motion]").CountAsync());
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>progressErrors.map(error=>error.message)")); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task ReducedMotionRejectAndStaleRepliesKeepTheCanonicalAcceptedBasisAndNeverLeaveFrameWork() {
        if (!Enabled) return;
        var host = new ProgressHost(760, false);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, 784, 640);
        var page = session.Page; await ConnectAsync(page, host); await PauseAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        await RequestAsync(page, .6); var basis = await ResolveAsync(page, 0);
        Assert.Equal(0, await page.Locator(Layer).CountAsync()); Assert.Equal("0.6", await page.Locator(LiveRow).GetAttributeAsync("data-cfx-value"));
        var revision = await page.EvaluateAsync<string>("()=>progress.sceneRevision");
        await RequestAsync(page, .7);
        await page.EvaluateAsync("async()=>{const reply=await prepareProgressReply(pending[1].request);pending[1].resolve({...reply,transition:'{'});}");
        await page.WaitForFunctionAsync("()=>progressErrors.length===1");
        Assert.Equal(revision, await page.EvaluateAsync<string>("()=>progress.sceneRevision"));
        await RequestAsync(page, .8);
        Assert.Equal(basis, await page.EvaluateAsync<string>("()=>pending[2].request.basis"));
        await page.EvaluateAsync("()=>pending[2].reject(new Error('Host rejected the edit'))");
        await page.WaitForFunctionAsync("()=>progressErrors.length===2");
        await RequestAsync(page, .85);
        var external = host.External(.9);
        await page.EvaluateAsync("reply=>progress.update(reply.html,{basis:reply.basis})", external);
        await ResolveAsync(page, 3); await DrainAsync(page);
        Assert.Equal("0.9", await page.Locator(LiveRow).GetAttributeAsync("data-cfx-value"));
        await RequestAsync(page, .95); Assert.Equal(external.basis, await page.EvaluateAsync<string>("()=>pending[4].request.basis"));
        await ResolveAsync(page, 4); Assert.Equal(0, await page.Locator(Layer).CountAsync());
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await RequestAsync(page, .8); await ResolveAsync(page, 5);
        Assert.Equal(1, await page.Locator(Layer).CountAsync());
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        await page.WaitForFunctionAsync("()=>!progress.root.querySelector('.cfx-native-motion-frame')");
        Assert.Equal("0.8", await page.Locator(LiveRow).GetAttributeAsync("data-cfx-value"));
        await page.EvaluateAsync("()=>progress.select({targets:[progress.resolveTargets().find(target=>target.observationId==='reviewed')]},{silent:true})");
        await page.EvaluateAsync("reply=>progress.update(reply.html,{basis:reply.basis})", host.Reordered(.8));
        Assert.Equal("1", await page.Locator(LiveRow).GetAttributeAsync("data-cfx-point"));
        Assert.Equal("reviewed", await page.EvaluateAsync<string>("()=>progress.getSelection()[0].observationId"));
        await RequestAsync(page, .85); await ResolveAsync(page, 6);
        Assert.Equal(.85, await page.EvaluateAsync<double>("()=>progress.getSelection()[0].numericValue"));
        Assert.Equal("1", await page.Locator(LiveRow).GetAttributeAsync("data-cfx-point")); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task TwoRowProducerPacingPreservesBothTypedFactsAndIgnoredAbortAdmitsOnlyOneUnsettledCallback() {
        if (!Enabled) return;
        var host = new ProgressHost(390, true);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, 414, 640);
        var page = session.Page; await ConnectAsync(page, host, 100); await PauseAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        await RequestAsync(page, .6);
        await page.EvaluateAsync("()=>{for(const value of [1500,1600,1750])progress.setProgressValue({seriesKey:'delivery',observationId:'verified'},value)}");
        Assert.False(await page.EvaluateAsync<bool>("()=>pending[0].signal.aborted"));
        var firstBasis = await ResolveAsync(page, 0); await page.Clock.RunForAsync(100);
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>pending.length"));
        Assert.Equal(firstBasis, await page.EvaluateAsync<string>("()=>pending[1].request.basis"));
        Assert.Equal(1750, await page.EvaluateAsync<double>("()=>pending[1].request.value"));
        var secondBasis = await ResolveAsync(page, 1);
        Assert.Equal("0.6", await page.Locator(LiveRow).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("1750", await page.Locator(".cfx-stage > svg [data-cfx-observation-id=verified]").GetAttributeAsync("data-cfx-value"));
        await page.EvaluateAsync("()=>progress.setProgressValue({seriesKey:'delivery',observationId:'reviewed'},.65)");
        await page.Clock.RunForAsync(100);
        await page.EvaluateAsync("()=>{for(let i=0;i<100;i++)progress.setProgressValue({seriesKey:'delivery',observationId:'reviewed'},(700+i)/1000)}");
        await page.Clock.RunForAsync(60000);
        Assert.Equal(3, await page.EvaluateAsync<int>("()=>pending.length")); Assert.True(await page.EvaluateAsync<bool>("()=>pending[2].signal.aborted"));
        await ResolveAsync(page, 2); await page.WaitForFunctionAsync("()=>pending.length===4");
        Assert.Equal(secondBasis, await page.EvaluateAsync<string>("()=>pending[3].request.basis"));
        var finalBasis = await ResolveAsync(page, 3); Assert.Equal("0.799", await page.Locator(LiveRow).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("1750", await page.Locator(".cfx-stage > svg [data-cfx-observation-id=verified]").GetAttributeAsync("data-cfx-value"));
        await CaptureAsync(page, host, "progress-cadence-two-row-accepted", finalBasis);
        Assert.Equal(3, await page.EvaluateAsync<int>("()=>progressChanges.length"));
        Assert.Equal(await page.EvaluateAsync<string[]>("()=>[0,1,3].map(index=>pending[index].request.requestId)"),
            await page.EvaluateAsync<string[]>("()=>progressChanges.map(change=>change.requestId)"));
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>progressErrors.map(error=>error.message)")); AssertNoConsoleErrors(session);
    }

    private static async Task ConnectAsync(IPage page, ProgressHost host, int cadence = 0) {
        await page.ExposeFunctionAsync("prepareProgressReply", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("""
            cadence=>{window.progress=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));
                window.pending=[];window.progressErrors=[];window.progressChanges=[];
                progress.root.addEventListener('cfxprogresserror',event=>progressErrors.push(event.detail));
                progress.root.addEventListener('cfxprogresschange',event=>progressChanges.push(event.detail));
                progress.connectNative((request,signal)=>new Promise((resolve,reject)=>pending.push({request,signal,resolve,reject})),
                    {basis:'initial',axes:null,progressEditing:true,progressCadenceMs:cadence,motion:{duration:1000}});}
            """, cadence);
    }
    private static async Task PauseAsync(IPage page) {
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = start }); await page.Clock.PauseAtAsync(start.AddHours(1));
    }
    private static async Task RequestAsync(IPage page, double value) {
        var count = await page.EvaluateAsync<int>("()=>pending.length");
        await page.EvaluateAsync("value=>progress.setProgressValue({seriesKey:'delivery',observationId:'reviewed'},value)", value);
        await page.WaitForFunctionAsync("count=>pending.length===count+1", count);
    }
    private static Task<string> ResolveAsync(IPage page, int index) => page.EvaluateAsync<string>("""
        async index=>{const reply=await prepareProgressReply(pending[index].request);pending[index].resolve(reply);
            for(let turn=0;turn<16;turn++)await Promise.resolve();return reply.basis;}
        """, index);
    private static Task DrainAsync(IPage page) => page.EvaluateAsync("async()=>{for(let turn=0;turn<16;turn++)await Promise.resolve()}");
    private static async Task CaptureAsync(IPage page, ProgressHost host, string name, string basis) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Capture output must be absolute.");
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        await File.WriteAllTextAsync(stem + ".json", await page.EvaluateAsync<string>("()=>JSON.stringify({requests:pending.map(item=>item.request),changes:progressChanges,errors:progressErrors,frame:progress.root.querySelector('.cfx-native-motion-frame')?.outerHTML},null,2)"));
        File.WriteAllText(stem + ".native.svg", host.Section(basis).PreparedVisual.ToSvg());
        File.WriteAllBytes(stem + ".native.png", host.Section(basis).PreparedVisual.ToPng());
        File.WriteAllBytes(stem + ".midpoint.native.png", host.Midpoint().PreparedVisual.ToPng());
        if (host.Midpoint().NarrowPreparedVisual is { } narrow) File.WriteAllBytes(stem + ".midpoint.narrow.native.png", narrow.ToPng());
    }

    private sealed class ProgressHost {
        private readonly int _width; private readonly bool _dark;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        private readonly Dictionary<string, Snapshot> _snapshots = new();
        internal ProgressHost(int width, bool dark) {
            _width = width; _dark = dark; var values = new[] { .5, 1250d, -8d };
            _snapshots.Add("initial", new Snapshot(PrepareSection(values), values));
        }
        internal string Page => "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + Section("initial").Html
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
        internal HtmlChartSection Section(string basis) => _snapshots[basis].Section;
        internal HtmlChartSection Midpoint() => PrepareSection(new[] { .625, 1250d, -8d });
        internal Reply External(double value) => Store(new[] { value, 1250d, -8d });
        internal Reply Reordered(double value) => Store(new[] { value, 1250d, -8d }, true);
        internal Reply Prepare(JsonElement request) {
            var previous = _snapshots[request.GetProperty("basis").GetString()!];
            var values = (double[])previous.Values.Clone();
            var identity = request.GetProperty("observationId").GetString();
            var index = identity switch { "reviewed" => 0, "verified" => 1, "temperature" => 2, _ => throw new InvalidOperationException("Unknown native row") };
            if (request.GetProperty("seriesKey").GetString() != "delivery") throw new InvalidOperationException("Unknown native series");
            values[index] = request.GetProperty("value").GetDouble();
            var reply = Store(values, previous.Reordered);
            // This is the real C# host boundary. The retained committed native section
            // and the freshly typed UpdateProgressItem destination own all frame policy.
            return reply with { transition = _renderer.PrepareValueTransitionJson(previous.Section, Section(reply.basis), 3) };
        }
        private Reply Store(double[] values, bool reordered = false) {
            var basis = "native-" + _snapshots.Count; var section = PrepareSection(values, reordered);
            _snapshots.Add(basis, new Snapshot(section, values, reordered)); return new Reply(section.Html, basis);
        }
        private HtmlChartSection PrepareSection(double[] values, bool reordered = false) {
            var chart = NativeProgressTransitionTests.Model(_width, _dark, 0, values[0], BrowserProofFonts.Family);
            for (var index = 1; index < values.Length; index++) {
                var row = chart.Series[0].ProgressItems[index];
                chart.Series[0].UpdateProgressItem(index, new ChartProgressItem(row.Label, values[index], row.Minimum, row.Maximum!.Value, row.Color));
            }
            if (reordered) {
                var rows = chart.Series[0].ProgressItems.ToArray();
                chart.Series[0].ReplaceProgressItems(new[] { rows[1], rows[0], rows[2] })
                    .WithObservationId(0, new ChartObservationId("verified"))
                    .WithObservationId(1, new ChartObservationId("reviewed"))
                    .WithObservationId(2, new ChartObservationId("temperature"));
            }
            return _renderer.PrepareSection(chart, configure: Configure);
        }
        private static void Configure(HtmlChartInteractionOptions options) {
            options.Interaction.ChartId = "native-progress-motion"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 340;
            options.ProgressEditing.Enabled = true; options.Tooltip.PinOnSelect = false;
            options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation);
        }
        private sealed record Snapshot(HtmlChartSection Section, double[] Values, bool Reordered = false);
        internal sealed record Reply(string html, string basis, string? transition = null);
    }
}
