using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Real native Polar replies use the existing managed lifetime and validate both responsive drawings before commit.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class PolarViewportBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task DirectionalAndSecondaryRadiusWindowsRetainRawFactsThroughResponsiveSelectionAndNativeExport(bool dark, bool narrow) {
        if (!Enabled) return;
        var host = new Host(dark); await using var session = await BrowserProofFonts.OpenAsync(host.Page, narrow ? 390 : 1100, 800);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await Connect(page, host);
        await page.EvaluateAsync("() => polar.setPolarViewport({angle:{minimum:5.5,maximum:6.8},radius:{minimum:20,maximum:80},secondaryRadius:{minimum:10,maximum:100}})");
        await Pending(page, 1); Assert.Equal("polar", await page.EvaluateAsync<string>("()=>requests[0].request.coordinateSystem"));
        Assert.Equal(new[] { "angle", "radius", "secondaryRadius" }, await page.EvaluateAsync<string[]>("()=>requests[0].request.axes"));
        await Resolve(page, 0); await Changed(page, 1);
        const string sources = "[data-cfx-role=polar-point-source]";
        Assert.Equal(10, await page.Locator(sources).CountAsync());
        Assert.Equal(4, await page.Locator(sources + "[data-cfx-visible=false]").CountAsync());
        Assert.Equal("-0.25", await page.Locator(sources + "[data-cfx-source-id='series-0-point-0']").GetAttributeAsync("data-cfx-angle"));
        Assert.Equal("0", await page.Locator(sources + "[data-cfx-visible=false]").EvaluateAllAsync<string>("nodes=>String(nodes.filter(node=>node.hasAttribute('tabindex')||node.getAttribute('role')==='button').length)"));
        await page.Locator(sources + "[data-cfx-source-id='series-0-point-0']").FocusAsync(); await page.Keyboard.PressAsync("Enter");
        Assert.Equal("reading-0", await page.EvaluateAsync<string>("()=>polar.getSelection()[0].observationId"));
        var revision = await page.EvaluateAsync<string>("()=>polar.sceneRevision");
        await page.SetViewportSizeAsync(narrow ? 1100 : 390, 800);
        await page.WaitForFunctionAsync("drawing=>polar.root.dataset.cfxAdaptiveDrawing===drawing", narrow ? "wide" : "narrow");
        Assert.NotEqual(revision, await page.EvaluateAsync<string>("()=>polar.sceneRevision"));
        Assert.Equal("reading-0", await page.EvaluateAsync<string>("()=>polar.getSelection()[0].observationId"));
        var window = await page.EvaluateAsync<JsonElement>("()=>polar.getPolarViewport()");
        Assert.Equal(5.5, window.GetProperty("angle").GetProperty("minimum").GetDouble());
        Assert.Equal(10, window.GetProperty("secondaryRadius").GetProperty("minimum").GetDouble());
        Assert.Equal(.25, window.GetProperty("radius").GetProperty("innerRadius").GetDouble() / window.GetProperty("radius").GetProperty("outerRadius").GetDouble(), 8);
        Assert.Equal(.5, window.GetProperty("secondaryRadius").GetProperty("innerRadius").GetDouble() / window.GetProperty("secondaryRadius").GetProperty("outerRadius").GetDouble(), 8);
        Assert.Equal(2, await page.Locator("button[data-cfx-zoom]").CountAsync());
        Assert.True(await page.Locator("button[data-cfx-zoom]").EvaluateAllAsync<bool>("nodes=>nodes.every(node=>node.disabled)"));
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The committed polar SVG did not download.");
        var svg = XDocument.Load(path);
        Assert.Equal(10, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "polar-point-source"));
        Assert.Equal(4, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-visible") == "false"));
        await Capture(page, "polar-window-" + (dark ? "dark" : "light") + "-" + (narrow ? "initial-narrow" : "initial-wide"), download);
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public async Task StaleReplyRejectedAlternateAndResetDoNotAdvanceUncommittedBasis(bool narrow, bool invalidRadius) {
        if (!Enabled) return;
        var host = new Host(narrow); await using var session = await BrowserProofFonts.OpenAsync(host.Page, narrow ? 390 : 1100, 800);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await Connect(page, host);
        const string request = "()=>polar.setPolarViewport({angle:{minimum:5.5,maximum:6.8},radius:{minimum:20,maximum:80},secondaryRadius:{minimum:10,maximum:100}})";
        await page.EvaluateAsync(request); await Pending(page, 1);
        await page.EvaluateAsync("async()=>{window.staleReply=await preparePolarHost(requests[0].request);}");
        await page.EvaluateAsync(request); await Pending(page, 2);
        Assert.True(await page.EvaluateAsync<bool>("()=>requests[0].signal.aborted&&!requests[1].signal.aborted"));
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>requests[1].request.basis"));
        await page.EvaluateAsync("async invalidRadius=>{const reply=await preparePolarHost(requests[1].request);const template=document.createElement('template');template.innerHTML=reply.html;const section=template.content.firstElementChild;const alternate=section.querySelector('template[data-cfx-adaptive-narrow]').content.firstElementChild;const metadata=JSON.parse(alternate.dataset.cfxPreparedChart);if(invalidRadius)metadata.polarProjection.secondaryRadius.innerRadius=-1;else delete metadata.polarProjection.secondaryRadius;alternate.dataset.cfxPreparedChart=JSON.stringify(metadata);requests[1].resolve({html:section.outerHTML,basis:'rejected'});}", invalidRadius);
        await page.WaitForFunctionAsync("()=>polarErrors.length===1"); Assert.Equal(0, await page.EvaluateAsync<int>("()=>polarChanges.length"));
        await page.EvaluateAsync(request); await Pending(page, 3); Assert.Equal("initial", await page.EvaluateAsync<string>("()=>requests[2].request.basis"));
        var acceptedBasis = await Resolve(page, 2); await Changed(page, 1);
        var revision = await page.EvaluateAsync<string>("()=>polar.sceneRevision");
        await page.EvaluateAsync("()=>requests[0].resolve(staleReply)");
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Equal(revision, await page.EvaluateAsync<string>("()=>polar.sceneRevision"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>polarChanges.length"));
        Assert.True(await page.Locator("[data-cfx-reset]").IsVisibleAsync());
        await page.Locator("[data-cfx-reset]").ClickAsync(); await Pending(page, 4);
        Assert.Equal(acceptedBasis, await page.EvaluateAsync<string>("()=>requests[3].request.basis"));
        Assert.Equal("reset", await page.EvaluateAsync<string>("()=>requests[3].request.kind"));
        await Resolve(page, 3); await Changed(page, 2);
        Assert.Equal(0, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().angle.minimum"));
        Assert.Equal(Math.PI * 2, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().angle.maximum"));
        Assert.Equal(0, await page.Locator("[data-cfx-role=polar-point-source][data-cfx-visible=false]").CountAsync());
        Assert.False(await page.Locator("[data-cfx-reset]").IsVisibleAsync());
        Assert.True(await page.EvaluateAsync<bool>("()=>{for(const value of [{angle:{minimum:1,maximum:1}},{angle:{minimum:0,maximum:7}},{x:{minimum:0,maximum:1}},{radius:{minimum:-1,maximum:10}}]){let rejected=false;try{polar.setPolarViewport(value);}catch{rejected=true;}if(!rejected)return false;}return requests.length===4;}"));
        await Capture(page, "polar-lifetime-reset-" + (narrow ? "narrow" : "wide") + (invalidRadius ? "-invalid-radius" : "-missing-axis"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task BookmarkReplaysPolarWindowsOnlyAfterTheSharedNativeOwnerAccepts() {
        if (!Enabled) return;
        var host = new Host(true); await using var session = await BrowserProofFonts.OpenAsync(host.Page, 390, 800);
        var page = session.Page; await Connect(page, host);
        await page.EvaluateAsync("()=>polar.setPolarViewport({angle:{minimum:5.5,maximum:6.8},radius:{minimum:20,maximum:80},secondaryRadius:{minimum:10,maximum:100}})");
        await Pending(page, 1); var firstBasis = await Resolve(page, 0); await Changed(page, 1);
        await page.EvaluateAsync("()=>{polar.root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{source:'polar-bookmark',sync:false}}));window.savedPolarBookmark=JSON.parse(polar.root.dataset.cfxInteractionSnapshot);window.polarStateApplied=0;polar.root.addEventListener('cfxstateapplied',()=>polarStateApplied++);}");
        Assert.Equal(5.5, await page.EvaluateAsync<double>("()=>savedPolarBookmark.nativePolarViewport.angle.minimum"));
        Assert.True(await page.EvaluateAsync<bool>("()=>savedPolarBookmark.nativeViewport===null"));
        await page.EvaluateAsync("()=>polar.setPolarViewport({angle:{minimum:0,maximum:2},radius:{minimum:30,maximum:70},secondaryRadius:{minimum:20,maximum:80}})");
        await Pending(page, 2); Assert.Equal(firstBasis, await page.EvaluateAsync<string>("()=>requests[1].request.basis"));
        var secondBasis = await Resolve(page, 1); await Changed(page, 2);
        await page.EvaluateAsync("()=>polar.root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:savedPolarBookmark,sync:false}}))");
        await Pending(page, 3); Assert.Equal(0, await page.EvaluateAsync<int>("()=>polarStateApplied"));
        Assert.Equal(secondBasis, await page.EvaluateAsync<string>("()=>requests[2].request.basis"));
        Assert.Equal(0, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().angle.minimum"));
        await Resolve(page, 2); await Changed(page, 3); await page.WaitForFunctionAsync("()=>polarStateApplied===1");
        Assert.Equal(5.5, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().angle.minimum"));
        Assert.Equal(10, await page.EvaluateAsync<double>("()=>polar.getPolarViewport().secondaryRadius.minimum"));
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>polarErrors.map(error=>error.message)"));
        await Capture(page, "polar-bookmark-accepted"); AssertNoConsoleErrors(session);
    }

    private static async Task Connect(IPage page, Host host) {
        await page.ExposeFunctionAsync("preparePolarHost", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("()=>{window.polar=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.requests=[];window.polarChanges=[];window.polarErrors=[];polar.root.addEventListener('cfxviewportchange',event=>polarChanges.push(event.detail));polar.root.addEventListener('cfxviewporterror',event=>polarErrors.push(event.detail));polar.connectNative((request,signal)=>new Promise(resolve=>requests.push({request,signal,resolve})),{basis:'initial',coordinateSystem:'polar',axes:['angle','radius','secondaryRadius']});}");
    }
    private static Task Pending(IPage page, int count) => page.WaitForFunctionAsync("count=>requests.length===count", count);
    private static Task Changed(IPage page, int count) => page.WaitForFunctionAsync("count=>polarChanges.length===count", count);
    private static Task<string> Resolve(IPage page, int index) => page.EvaluateAsync<string>("async index=>{const reply=await preparePolarHost(requests[index].request);requests[index].resolve(reply);return reply.basis;}", index);
    private static async Task Capture(IPage page, string name, IDownload? download = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), await page.EvaluateAsync<string>("()=>JSON.stringify({requests:requests.map(item=>item.request),changes:polarChanges,errors:polarErrors,view:polar.getPolarViewport()},null,2)"));
        if (download != null) await download.SaveAsAsync(Path.Combine(directory, name + ".export.svg"));
    }
    private sealed class Host {
        private readonly bool _dark;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        private readonly Dictionary<string, HtmlChartSection> _snapshots = new();
        internal Host(bool dark) { _dark = dark; _snapshots.Add("initial", _renderer.PreparePolarSection(Model(), configure: Configure)); }
        internal string Page => "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + _snapshots["initial"].Html
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
        internal object Prepare(JsonElement request) {
            lock (_snapshots) {
                _ = _snapshots[request.GetProperty("basis").GetString()!];
                ChartPolarViewport? viewport = null;
                if (request.GetProperty("kind").GetString() != "reset") {
                    var source = request.GetProperty("sourceViewport");
                    ChartAxisWindow? Window(string name) => source.TryGetProperty(name, out var window) ? NativeWindowTransport.Read(window) : null;
                    viewport = new ChartPolarViewport(Window("angle"), Window("radius"), Window("secondaryRadius"));
                }
                var section = _renderer.PreparePolarSection(Model(), viewport, Configure); var basis = "polar-" + _snapshots.Count;
                _snapshots.Add(basis, section); return new { html = section.Html, basis };
            }
        }
        private Chart Model() {
            var values = new[] { new ChartPoint(-.25, 40), new ChartPoint(Math.PI * 2 - .25, 50), new ChartPoint(.25, 60), new ChartPoint(3, 70), new ChartPoint(.15, 95) };
            var chart = PolarViewportContractTests.Create(values).WithSize(900, 440).WithHeader(true).WithTitle("Native directional view")
                .WithFontFamily(BrowserProofFonts.Family).WithTheme(VisualTheme.Graphite(), _dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
                .WithSecondaryYAxisBounds(1, 1000).AddPolar("Secondary", values.Select(point => new ChartPoint(point.X, point.Y == 95 ? 1000 : point.Y == 70 ? 100 : 30)).ToArray());
            chart.Series[1].UseSecondaryValueAxis(); chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
            return chart;
        }
        private static void Configure(HtmlChartInteractionOptions options) {
            options.Interaction.ChartId = "polar-native-view"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 360; options.Adaptive.NarrowHeight = 420;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Export | ChartInteractionFeatures.StateBookmarks);
            options.Tooltip.PinOnSelect = false;
        }
    }
}
