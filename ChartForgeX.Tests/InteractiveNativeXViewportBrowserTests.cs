using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises actual C# preparation at a connected browser host boundary, without a server or browser-side data projection.</summary>
public sealed partial class InteractiveNativeXViewportBrowserTests {
    private const string Root = ".cfx-interactive-chart";
    private const string Middle = "g[data-cfx-role=point][data-cfx-observation-id=sample-6]";
    private const string First = "g[data-cfx-role=point][data-cfx-observation-id=sample-0]";

    [BrowserTheory]
    [InlineData(false, 980, false)]
    [InlineData(true, 380, true)]
    public async Task ConnectedXWindowRepreparesNativeTicksRetainsIdentityAndExportsCommittedScene(bool dark, int width, bool reversed) {
        if (!Enabled) return;
        var host = new NativeHost(dark, reversed);
        await using var session = await OpenAsync(host.Page(), width, 680);
        var page = session.Page;
        await ConnectAsync(page, host);
        var initial = await SceneAsync(page);
        await page.Locator(Middle).FocusAsync(); await page.Keyboard.PressAsync("Space");
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]").ClickAsync();
        await CaptureAsync(page, "before-" + width, initial);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await WaitForChangesAsync(page, 1);
        var zoomed = await SceneAsync(page);
        Assert.Equal(1.2, zoomed.GetProperty("projection").GetProperty("minimum").GetDouble(), 8);
        Assert.Equal(10.8, zoomed.GetProperty("projection").GetProperty("maximum").GetDouble(), 8);
        Assert.NotEqual(initial.GetProperty("xLabels").GetRawText(), zoomed.GetProperty("xLabels").GetRawText());
        Assert.Equal(initial.GetProperty("yLabels").GetRawText(), zoomed.GetProperty("yLabels").GetRawText());
        Assert.Equal("1.000", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom"));
        Assert.Equal("0.0", await page.Locator(Root).GetAttributeAsync("data-cfx-pan-x"));
        Assert.Equal("true", await page.Locator(Middle).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]").GetAttributeAsync("data-cfx-muted"));
        Assert.NotEqual("0", await page.Locator(First).GetAttributeAsync("tabindex"));
        Assert.Equal(0, await page.Locator(First + " [data-cfx-browser-hit-area]").CountAsync());
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("tip=>tip.hidden"));
        Assert.NotEqual("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.True(await page.Locator(".cfx-brush-box").EvaluateAsync<bool>("box=>box.hidden"));
        Assert.Equal("6", await page.Locator(Middle).GetAttributeAsync("data-cfx-x"));
        Assert.Equal(13, await page.Locator("g[data-cfx-role=point][data-cfx-series-key=samples]").CountAsync());
        await CaptureAsync(page, "zoomed-" + width, zoomed);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No committed export was downloaded.");
        var svg = XDocument.Load(path);
        var exportedX = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "axis-x-label")
            .Select(node => node.Value).ToArray();
        Assert.Equal(zoomed.GetProperty("xLabels").EnumerateArray().Select(item => item.GetString()), exportedX);
        Assert.Equal("false", (string?)Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-observation-id") == "sample-0").Attribute("data-cfx-visible"));
        await SaveDownloadAsync(download, "committed-" + width + ".svg");
        await page.Locator("[data-cfx-reset]").ClickAsync(); await WaitForChangesAsync(page, 2);
        var reset = await SceneAsync(page);
        Assert.Equal(0, reset.GetProperty("projection").GetProperty("minimum").GetDouble());
        Assert.Equal(12, reset.GetProperty("projection").GetProperty("maximum").GetDouble());
        Assert.Equal(initial.GetProperty("xLabels").GetRawText(), reset.GetProperty("xLabels").GetRawText());
        Assert.NotEqual("true", await page.Locator(Middle).GetAttributeAsync("aria-selected"));
        Assert.Equal(0, await page.Locator("[aria-selected=true]").CountAsync());
        Assert.True(await page.Locator("[data-cfx-reset]").EvaluateAsync<bool>("button=>button.hidden"));
        Assert.Equal("reset", await page.EvaluateAsync<string>("()=>nativeRequests.at(-1).kind"));
        Assert.False(await page.EvaluateAsync<bool>("()=>Object.hasOwn(nativeRequests.at(-1),'left')||Object.hasOwn(nativeRequests.at(-1),'right')"));
        Assert.Equal(3, host.PreparedCount);
        Assert.True(await page.EvaluateAsync<bool>("()=>nativeBases.has(chartController.sceneRevision)&&nativeBases.size===3"));
        await CaptureAsync(page, "reset-" + width, reset);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConnectedNativePanConfinesTheRequestedSpanAndPointerCancellationDoesNotCommit(bool reversed) {
        if (!Enabled) return;
        var host = new NativeHost(reversed: reversed);
        await using var session = await OpenAsync(host.Page(), 980, 680);
        var page = session.Page; await ConnectAsync(page, host);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await WaitForChangesAsync(page, 1);
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        var bounds = await BoxAsync(page, ".cfx-stage svg");
        var x = (float)(bounds.X + bounds.Width * .45);
        var y = (float)(bounds.Y + bounds.Height * .5);
        await page.Mouse.MoveAsync(x, y); await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 300, y, new MouseMoveOptions { Steps = 5 });
        Assert.Equal("0.0", await page.Locator(Root).GetAttributeAsync("data-cfx-pan-x"));
        await page.Mouse.UpAsync(); await WaitForChangesAsync(page, 2);
        var projection = (await SceneAsync(page)).GetProperty("projection");
        Assert.Equal(reversed ? 2.4 : 0, projection.GetProperty("minimum").GetDouble(), 8);
        Assert.Equal(reversed ? 12 : 9.6, projection.GetProperty("maximum").GetDouble(), 8);
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        await page.EvaluateAsync("""
            () => {
                const stage=chartController.root.querySelector('.cfx-stage'), box=stage.getBoundingClientRect();
                stage.setPointerCapture=()=>{};stage.hasPointerCapture=()=>false;
                const init={pointerId:77,pointerType:'touch',button:0,bubbles:true,clientX:box.x+100,clientY:box.y+100};
                stage.dispatchEvent(new PointerEvent('pointerdown',init));
                stage.dispatchEvent(new PointerEvent('pointermove',{...init,clientX:init.clientX+100}));
                stage.dispatchEvent(new PointerEvent('pointercancel',init));
                stage.dispatchEvent(new PointerEvent('pointerup',init));
            }
            """);
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>nativeRequests.length"));
        await CaptureAsync(page, "pan-" + reversed, projection);
        AssertNoConsoleErrors(session);
    }

    private static void Configure(HtmlChartInteractionOptions options) {
        options.Interaction.ChartId = "native-readings";
        options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Export | ChartInteractionFeatures.StateBookmarks);
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
    }

    private sealed class NativeHost {
        private readonly Dictionary<string, HtmlChartSection> _sections = new();
        private readonly bool _dark, _reversed;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        internal NativeHost(bool dark = false, bool reversed = false) {
            _dark = dark; _reversed = reversed;
            _sections.Add("initial", _renderer.PrepareSection(Chart(), configure: Configure));
        }
        internal int PreparedCount { get { lock (_sections) return _sections.Count; } }
        internal string Initial => _sections["initial"].Html;
        internal string Window(double minimum, double maximum) => _renderer.PrepareSection(Chart(), new ChartXViewport(minimum, maximum), Configure).Html;
        internal string Page() => "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body style='margin:12px'>" + Initial + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        internal object Prepare(JsonElement request) {
            lock (_sections) {
                var basis = _sections[request.GetProperty("basis").GetString()!];
                var kind = request.GetProperty("kind").GetString();
                var viewport = kind == "reset" ? null : basis.PreparedVisual.XProjection!.ConstrainViewport(
                    request.GetProperty("left").GetDouble(), request.GetProperty("right").GetDouble());
                var section = _renderer.PrepareSection(Chart(), viewport, Configure);
                var token = "prepared-" + _sections.Count; _sections.Add(token, section);
                return new { token, html = section.Html };
            }
        }
        private Chart Chart() {
            var series = ChartSeries.CreateLine(new ChartSeriesId("samples"), "Measurements", Enumerable.Range(0, 13)
                .Select(index => new ChartPoint(index, index is 0 or 12 ? 95 : 20 + index * 4)));
            for (var index = 0; index < 13; index++) series.WithObservationId(index, new ChartObservationId("sample-" + index));
            series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 4; });
            var chart = ChartForgeX.Core.Chart.Create().WithSize(800, 440).WithTheme(VisualTheme.Graphite(), _dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
                .WithTitle("Inspect a native X window").WithSubtitle("Native ticks and fixed Y bounds follow the committed window")
                .WithXAxisBounds(0, 12).WithYAxisBounds(0, 100).WithLegend().AddSeries(series)
                .AddSeries(new ChartSeries(new ChartSeriesId("reference"), "Reference", ChartSeriesKind.StepLine, new[] { new ChartPoint(0, 30), new ChartPoint(12, 30) }));
            chart.Options.XAxis.Reversed = _reversed;
            return chart;
        }
    }

    private static async Task ConnectAsync(IPage page, NativeHost host) {
        await page.ExposeFunctionAsync("nativeHostPrepare", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("""
            () => {
                window.chartController=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));
                window.nativeRequests=[];window.nativeChanges=[];window.nativeErrors=[];
                window.nativeBases=new Map([[chartController.sceneRevision,'initial']]);window.nativeCandidates=new Map();
                chartController.root.addEventListener('cfxviewportrequest',event=>nativeRequests.push(event.detail));
                chartController.root.addEventListener('cfxviewporterror',event=>nativeErrors.push(event.detail));
                chartController.root.addEventListener('cfxviewportchange',event=>{
                    const commit=event.detail;nativeBases.set(commit.sceneRevision,nativeCandidates.get(commit.requestId));
                    nativeCandidates.delete(commit.requestId);nativeChanges.push(commit);
                });
                chartController.connectXViewport(async(request,signal)=>{
                    const basis=nativeBases.get(request.sceneRevision);
                    if(!basis)throw new Error('No native preparation belongs to the committed browser revision.');
                    const result=await nativeHostPrepare({...request,basis});
                    if(signal.aborted)throw new DOMException('Canceled','AbortError');
                    nativeCandidates.set(request.requestId,result.token);
                    signal.addEventListener('abort',()=>nativeCandidates.delete(request.requestId),{once:true});
                    return result.html;
                });
            }
            """);
    }

    private static Task WaitForChangesAsync(IPage page, int count) => page.WaitForFunctionAsync("count=>nativeChanges.length===count", count);
    private static Task<JsonElement> SceneAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {const root=chartController.root;return {
            revision:chartController.sceneRevision,projection:JSON.parse(root.dataset.cfxPreparedChart).xProjection,
            xLabels:[...root.querySelectorAll('[data-cfx-role=axis-x-label]')].map(node=>node.textContent),
            yLabels:[...root.querySelectorAll('[data-cfx-role=axis-y-label]')].map(node=>node.textContent)
        };}
        """);
    private static async Task CaptureAsync(IPage page, string name, object? evidence = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Capture output must be absolute.");
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "native-x-" + name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, "native-x-" + name + ".json"), JsonSerializer.Serialize(evidence));
    }
    private static async Task SaveDownloadAsync(IDownload download, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) await download.SaveAsAsync(Path.Combine(directory, "native-x-" + name));
    }
}
