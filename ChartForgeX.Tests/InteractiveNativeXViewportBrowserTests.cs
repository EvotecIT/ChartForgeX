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
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var pngPath = await pngDownload.PathAsync() ?? throw new InvalidOperationException("No committed PNG export was downloaded.");
        var png = await File.ReadAllBytesAsync(pngPath);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8).ToArray());
        Assert.Equal(800, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal(440, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
        await SaveDownloadAsync(pngDownload, "committed-" + width + ".png");
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

    [BrowserTheory]
    [InlineData(false, 980, ChartSeriesKind.Line)]
    [InlineData(true, 380, ChartSeriesKind.StepLine)]
    public async Task NativeWindowWithoutObservationsKeepsCrossingPaintWithoutInventingTargets(bool dark, int width, ChartSeriesKind kind) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(800, 440).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("A native window between observations").WithSubtitle("The connected path crosses this window; source observations remain outside")
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddSeries(new ChartSeries(new ChartSeriesId("crossing"), "Crossing", kind, new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) })
                .WithObservationId(0, new ChartObservationId("outside-left")).WithObservationId(1, new ChartObservationId("outside-right")));
        chart.Series[0].Markers.Enabled = false;
        var renderer = new HtmlInteractiveChartRenderer();
        var window = renderer.PrepareSection(chart, new ChartCartesianViewport(x: new ChartAxisWindow(4, 6)), Configure);
        var initial = renderer.PrepareSection(chart, configure: Configure);
        var html = "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body style='margin:12px'>" + initial.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        await using var session = await OpenAsync(html, width, 680);
        var page = session.Page; await ControlledConnectionAsync(page);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await page.WaitForFunctionAsync("()=>controlled.length===1");
        await page.EvaluateAsync("html=>controlled[0].resolve(html)", window.Html); await WaitForChangesAsync(page, 1);
        Assert.Equal(2, await page.Locator("g[data-cfx-role=point][data-cfx-visible=false]").CountAsync());
        Assert.Equal(0, await page.Locator("g[data-cfx-role=point] [data-cfx-browser-hit-area],g[data-cfx-role=point] .cfx-prepared-point-marker").CountAsync());
        Assert.Equal(0, await page.Locator("g[data-cfx-role=point][tabindex='0']").CountAsync());
        Assert.NotEmpty(await page.Locator("[data-cfx-role=line]").GetAttributeAsync("d") ?? string.Empty);
        var plot = window.PreparedVisual.XProjection!.PlotBounds;
        await page.EvaluateAsync("""
            ({x,y})=>{const svg=chartController.root.querySelector('svg'),point=new DOMPoint(x,y).matrixTransform(svg.getScreenCTM());
                window.emptyWindowPointer={x:point.x,y:point.y};}
            """, new { x = plot.Left + plot.Width / 2, y = plot.Top + plot.Height / 2 });
        var pointer = await page.EvaluateAsync<JsonElement>("()=>emptyWindowPointer");
        await page.Mouse.MoveAsync((float)pointer.GetProperty("x").GetDouble(), (float)pointer.GetProperty("y").GetDouble());
        Assert.Equal(0, await page.Locator("g[data-cfx-role=point].cfx-hovered").CountAsync());
        await CaptureAsync(page, "crossing-" + width, await SceneAsync(page));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            await File.WriteAllTextAsync(Path.Combine(directory, "native-x-crossing-native-" + width + ".svg"), window.PreparedVisual.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, "native-x-crossing-native-" + width + ".png"), window.PreparedVisual.ToPng());
        }
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
        private readonly ChartSeriesKind _kind;
        private readonly bool _time;
        private readonly int _width;
        private readonly string? _group;
        private readonly bool _scenarios, _navigation, _adaptive, _sessions, _automaticBounds, _yReversed;
        private readonly ChartScaleKind _yScale;
        private readonly Action<Chart>? _configureChart;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        internal NativeHost(bool dark = false, bool reversed = false, ChartSeriesKind kind = ChartSeriesKind.Line,
            bool time = false, int width = 800, string id = "native-readings", string? group = null, bool scenarios = false,
            bool navigation = false, bool adaptive = false, bool sessions = false, bool automaticBounds = false, ChartScaleKind yScale = ChartScaleKind.Linear, bool yReversed = false, Action<Chart>? configureChart = null) {
            _dark = dark; _reversed = reversed; _kind = kind; _time = time || sessions; _width = width; Id = id; _group = group; _scenarios = scenarios;
            _navigation = navigation; _adaptive = adaptive; _sessions = sessions;
            _automaticBounds = automaticBounds; _yScale = yScale; _yReversed = yReversed; _configureChart = configureChart;
            _sections.Add("initial", _renderer.PrepareSection(Chart(), configure: ConfigureSection));
        }
        internal string Id { get; }
        internal double Origin => _sessions ? new DateTime(2026, 1, 30, 9, 0, 0, DateTimeKind.Utc).ToOADate()
            : _time ? new DateTime(2026, 1, 28, 0, 0, 0, DateTimeKind.Utc).ToOADate() : 0;
        internal double Maximum => Coordinate(12);
        private double Coordinate(int index) => !_sessions ? Origin + index : index <= 6 ? Origin + index / 24d : Origin + 3 + (index - 7) / 24d;
        internal int PreparedCount { get { lock (_sections) return _sections.Count; } }
        internal string Initial => _sections["initial"].Html;
        internal string BookmarkReplacement(string capability) {
            var chart = Chart();
            if (capability is "xy-missing-projections" or "legacy-unsupported" or "legacy-empty") {
                chart = ChartForgeX.Core.Chart.Create().WithSize(_width, 440).AddSeries(new ChartSeries(new ChartSeriesId("samples"), "Measurements",
                    capability == "legacy-empty" ? ChartSeriesKind.Line : ChartSeriesKind.Pie,
                    capability == "legacy-empty" ? Array.Empty<ChartPoint>() : new[] { new ChartPoint(0, 3), new ChartPoint(12, 8) }));
            }
            return _renderer.PrepareSection(chart, configure: ConfigureSection).Html;
        }
        internal string Window(double minimum, double maximum) => _renderer.PrepareSection(Chart(), new ChartCartesianViewport(x: new ChartAxisWindow(minimum, maximum)), ConfigureSection).Html;
        internal string Page(params NativeHost[] peers) => "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body style='margin:12px'>" + Initial + string.Concat(peers.Select(peer => peer.Initial))
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        private void ConfigureSection(HtmlChartInteractionOptions options) {
            Configure(options); options.Interaction.ChartId = Id;
            options.XNavigation.Enabled = _navigation;
            if (_navigation) {
                if (_sessions) {
                    options.XNavigation.RangePresets.Add(HtmlChartXRangePreset.FromDuration("Last day", TimeSpan.FromDays(1)));
                    options.XNavigation.RangePresets.Add(HtmlChartXRangePreset.FromDuration("Last 4 hours", TimeSpan.FromHours(4)));
                } else {
                    options.XNavigation.RangePresets.Add(_time ? HtmlChartXRangePreset.FromDuration("Last 4 days", TimeSpan.FromDays(4))
                        : new HtmlChartXRangePreset("Latest four", 4));
                    options.XNavigation.RangePresets.Add(new HtmlChartXRangePreset("Latest eight", 8));
                }
            }
            if (_adaptive) options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            if (_scenarios) options.Interaction.AddScenario("review", "Review measurements",
                scenario => scenario.AddSeriesStep(new ChartSeriesId("samples"), "Measurements"));
            if (_group != null) {
                options.Interaction.GroupName = _group;
                options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
            }
        }
        internal object Prepare(JsonElement request) {
            lock (_sections) {
                var basis = _sections[request.GetProperty("basis").GetString()!];
                var kind = request.GetProperty("kind").GetString();
                var basisScene = request.TryGetProperty("drawing", out var drawing) && drawing.GetString() == "narrow"
                    ? basis.NarrowPreparedVisual! : basis.PreparedVisual;
                ChartAxisWindow? x = null; ChartAxisWindow? y = null;
                if (kind != "reset") {
                    var projection = basisScene.XProjection!;
                    x = projection.Minimum != projection.FullMinimum || projection.Maximum != projection.FullMaximum || !projection.MaximumInclusive ? new ChartAxisWindow(projection.Minimum, projection.Maximum, projection.MaximumInclusive) : null;
                    var primary = basisScene.YProjection;
                    if (primary != null && (primary.Minimum != primary.FullMinimum || primary.Maximum != primary.FullMaximum || !primary.MaximumInclusive)) y = new ChartAxisWindow(primary.Minimum, primary.Maximum, primary.MaximumInclusive);
                    if (request.TryGetProperty("sourceWindow", out var sourceWindow))
                        x = projection.ConstrainViewport(NativeWindowTransport.Read(sourceWindow));
                    else if (request.TryGetProperty("sourceViewport", out var sourceViewport)) {
                        if (sourceViewport.TryGetProperty("x", out var sourceX)) x = projection.ConstrainViewport(NativeWindowTransport.Read(sourceX));
                        if (sourceViewport.TryGetProperty("y", out var sourceY)) y = primary!.ConstrainViewport(NativeWindowTransport.Read(sourceY));
                    } else {
                        var axes = request.TryGetProperty("axes", out var configuredAxes) ? configuredAxes.EnumerateArray().Select(axis => axis.GetString()!).ToArray() : new[] { "x" };
                        if (axes.Contains("x")) x = projection.ConstrainViewport(request.GetProperty("left").GetDouble(), request.GetProperty("right").GetDouble());
                        if (axes.Contains("y")) y = primary!.ConstrainViewport(request.GetProperty("top").GetDouble(), request.GetProperty("bottom").GetDouble());
                    }
                }
                var section = x == null && y == null ? _renderer.PrepareSection(Chart(), configure: ConfigureSection)
                    : _renderer.PrepareSection(Chart(), new ChartCartesianViewport(x, y), ConfigureSection);
                var token = "prepared-" + _sections.Count; _sections.Add(token, section);
                return new { token, html = section.Html };
            }
        }
        private Chart Chart() {
            var points = Enumerable.Range(0, 13).Select(index => new ChartPoint(Coordinate(index), index is 0 or 12 ? 95 : 20 + index * 4)).ToArray();
            var id = new ChartSeriesId("samples");
            var financial = points.Select(point => new ChartCandlestick(point.X, point.Y, point.Y + 4, point.Y - 8, point.Y + 2));
            var ranges = points.Select(point => new ChartRangeBand(point.X, point.Y - 8, point.Y + 4));
            var series = _kind switch {
                ChartSeriesKind.Bubble => ChartSeries.CreateBubble(id, "Measurements", points.Select((point, index) => new ChartBubble(point.X, point.Y, 10 + index * 3))),
                ChartSeriesKind.Candlestick => ChartSeries.CreateCandlestick(id, "Measurements", financial),
                ChartSeriesKind.Ohlc => ChartSeries.CreateOhlc(id, "Measurements", financial),
                ChartSeriesKind.RangeBand => ChartSeries.CreateRangeBand(id, "Measurements", ranges),
                ChartSeriesKind.RangeArea => ChartSeries.CreateRangeArea(id, "Measurements", ranges),
                ChartSeriesKind.DifferenceArea => ChartSeries.CreateDifferenceArea(id, "Measurements", points.Select(point => new ChartDifferencePoint(point.X, point.Y + 4, point.Y - 8))),
                _ => new ChartSeries(id, "Measurements", _kind, points)
            };
            if (_kind == ChartSeriesKind.Area) series.WithSmooth();
            for (var index = 0; index < 13; index++) series.WithObservationId(index, new ChartObservationId("sample-" + index));
            if (_kind is not (ChartSeriesKind.Candlestick or ChartSeriesKind.Ohlc))
                series.ConfigureMarkers(markers => { markers.Enabled = true; if (_kind != ChartSeriesKind.Bubble) markers.Radius = _automaticBounds ? 18 : 4; });
            var chart = ChartForgeX.Core.Chart.Create().WithSize(_width, 440).WithTheme(VisualTheme.Graphite(), _dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
                .WithTitle("Inspect a native X window").WithSubtitle("Native ticks and fixed Y bounds follow the committed window")
                .WithLegend().AddSeries(series)
                .AddSeries(new ChartSeries(new ChartSeriesId("reference"), "Reference", ChartSeriesKind.StepLine, new[] { new ChartPoint(Origin, 30), new ChartPoint(Maximum, 30) }));
            if (!_automaticBounds) chart.WithXAxisBounds(Origin, Maximum).WithYAxisBounds(_yScale == ChartScaleKind.Logarithmic ? 1 : 0, _yScale == ChartScaleKind.Logarithmic ? 1000 : 100);
            chart.WithYAxisScale(_yScale); chart.Options.YAxis.Reversed = _yReversed;
            if (_time) chart.WithXAxisScale(ChartScaleKind.Time);
            if (_sessions) {
                chart.ConfigureXAxis(axis => axis.WithTimeSessions(new ChartTimeSessionDomain(new[] {
                    new ChartTimeSession(Origin, Coordinate(6)), new ChartTimeSession(Coordinate(7), Maximum)
                })));
                chart.AddSeries(new ChartSeries(new ChartSeriesId("companion"), "Companion", ChartSeriesKind.Line, points.Select(point => new ChartPoint(point.X, point.Y / 2))));
                chart.AddSeries(ChartSeries.CreateRangeArea(new ChartSeriesId("envelope"), "Envelope", points.Select(point => new ChartRangeBand(point.X, point.Y / 2 - 6, point.Y / 2 + 6))));
            }
            chart.Options.XAxis.Reversed = _reversed;
            _configureChart?.Invoke(chart);
            return chart;
        }
    }

    private static async Task ConnectAsync(IPage page, params NativeHost[] hosts) {
        await page.ExposeFunctionAsync("nativeHostPrepare", (JsonElement request) => hosts.Single(host => host.Id == request.GetProperty("chartId").GetString()).Prepare(request));
        await page.EvaluateAsync("""
            () => {
                window.chartControllers=[...document.querySelectorAll('.cfx-interactive-chart')].map(root=>ChartForgeXCharts.mount(root));
                window.chartController=chartControllers[0];
                window.nativeRequests=[];window.nativeChanges=[];window.nativeErrors=[];window.nativeCallbackFrozen=[];window.nativeCallbacks=new Map();
                window.nativeBases=new Map(chartControllers.map(controller=>[controller.sceneRevision,'initial']));window.nativeCandidates=new Map();
                chartControllers.forEach(controller=>{
                controller.root.addEventListener('cfxviewportrequest',event=>nativeRequests.push(event.detail));
                controller.root.addEventListener('cfxlayoutchange',event=>{
                    const layout=event.detail;nativeBases.set(layout.sceneRevision,nativeBases.get(layout.previousSceneRevision));
                });
                controller.root.addEventListener('cfxviewporterror',event=>nativeErrors.push(event.detail));
                controller.root.addEventListener('cfxviewportchange',event=>{
                    const commit=event.detail;nativeBases.set(commit.sceneRevision,nativeCandidates.get(commit.requestId));
                    nativeCandidates.delete(commit.requestId);nativeChanges.push(commit);
                });
                const callback=async(request,signal)=>{
                    nativeCallbackFrozen.push(Object.isFrozen(request)&&(!request.sourceWindow||Object.isFrozen(request.sourceWindow))
                        &&(!request.sourceViewport||Object.isFrozen(request.sourceViewport)&&Object.values(request.sourceViewport).every(Object.isFrozen)));
                    const basis=nativeBases.get(request.sceneRevision);
                    if(!basis)throw new Error('No native preparation belongs to the committed browser revision.');
                    const result=await nativeHostPrepare({...request,basis});
                    if(signal.aborted)throw new DOMException('Canceled','AbortError');
                    nativeCandidates.set(request.requestId,result.token);
                    signal.addEventListener('abort',()=>nativeCandidates.delete(request.requestId),{once:true});
                    return result.html;
                };
                nativeCallbacks.set(controller,callback);controller.connectXViewport(callback);
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
        const string layoutScript = """
            ()=>{
                const root=chartController.root,stage=root.querySelector('.cfx-stage'),svg=stage?.querySelector('svg'),title=svg?.querySelector('text');
                const rect=node=>{if(!node)return null;const b=node.getBoundingClientRect(),s=getComputedStyle(node);return{
                    x:b.x,y:b.y,width:b.width,height:b.height,scrollTop:node.scrollTop,scrollLeft:node.scrollLeft,
                    scrollWidth:node.scrollWidth,scrollHeight:node.scrollHeight,clientWidth:node.clientWidth,clientHeight:node.clientHeight,
                    overflowX:s.overflowX,overflowY:s.overflowY,transform:s.transform};};
                const matrix=svg?.getScreenCTM(),focus=root.getRootNode().activeElement;
                let titleBBox=null;try{const b=title?.getBBox();if(b)titleBBox={x:b.x,y:b.y,width:b.width,height:b.height};}catch{}
                return{viewport:{width:innerWidth,height:innerHeight,scrollX,scrollY},root:rect(root),stage:rect(stage),svg:rect(svg),
                    document:rect(document.scrollingElement),body:rect(document.body),toolbar:rect(root.querySelector('.cfx-toolbar')),
                    title:{text:title?.textContent,client:rect(title),native:titleBBox},
                    screenMatrix:matrix?{a:matrix.a,b:matrix.b,c:matrix.c,d:matrix.d,e:matrix.e,f:matrix.f}:null,
                    drawing:root.dataset.cfxAdaptiveDrawing,state:{zoom:root.dataset.cfxZoom,panX:root.dataset.cfxPanX,panY:root.dataset.cfxPanY},
                    focus:focus?{tag:focus.tagName,id:focus.id,role:focus.getAttribute('data-cfx-role'),owned:root.contains(focus)}:null};
            }
            """;
        var before = await page.EvaluateAsync<JsonElement>(layoutScript);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "native-x-" + name + ".png"), FullPage = true });
        var after = await page.EvaluateAsync<JsonElement>(layoutScript);
        await File.WriteAllTextAsync(Path.Combine(directory, "native-x-" + name + ".json"), JsonSerializer.Serialize(evidence));
        await File.WriteAllTextAsync(Path.Combine(directory, "native-x-" + name + ".layout.json"), JsonSerializer.Serialize(new { before, after }));
    }
    private static async Task SaveDownloadAsync(IDownload download, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) await download.SaveAsAsync(Path.Combine(directory, "native-x-" + name));
    }
}
