using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Uses real native preparation replies to qualify the managed host's committed-basis and lifetime contract.</summary>
public sealed partial class ManagedNativeConnectionBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Light)]
    public async Task ManagedConnectionCarriesOnlyCommittedBasisAcrossDeferredViewportAndAnnotationCommands(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var host = new NativeHost(width, mode);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 800);
        var page = session.Page;
        var pageErrors = new List<string>(); page.PageError += (_, error) => pageErrors.Add(error);
        await ConnectAsync(page, host);
        await page.EvaluateAsync("() => managed.select({targets:[managed.resolveTargets().find(target=>target.annotationId==='note')]})");
        await page.Locator("[data-cfx-annotation-id=note]").FocusAsync();
        await page.EvaluateAsync("() => managed.setViewport({x:{minimum:1,maximum:9}})");
        await PendingAsync(page, 1);
        await PrepareReplyAsync(page, 0);
        await page.EvaluateAsync("() => managed.setViewport({x:{minimum:2,maximum:8}})");
        await PendingAsync(page, 2);
        Assert.True(await page.EvaluateAsync<bool>("() => pending[0].signal.aborted&&!pending[1].signal.aborted"));
        Assert.Equal("initial", await RequestFieldAsync(page, 1, "basis"));
        Assert.Equal("viewport", await RequestFieldAsync(page, 1, "channel"));
        // An edit requested synchronously by a change listener must already use the accepted viewport's basis.
        await page.EvaluateAsync("() => { window.chainAnnotation=true; }");
        var windowBasis = await ResolveAsync(page, 1);
        await PendingAsync(page, 3);
        Assert.Equal(windowBasis, await RequestFieldAsync(page, 2, "basis"));
        Assert.Equal("annotation", await RequestFieldAsync(page, 2, "channel"));
        var revision = await page.EvaluateAsync<string>("() => managed.sceneRevision");
        await page.EvaluateAsync("() => pending[0].resolve(replies[0])");
        await DrainAsync(page);
        Assert.Equal(revision, await page.EvaluateAsync<string>("() => managed.sceneRevision"));
        Assert.False(await page.EvaluateAsync<bool>("() => pending[2].signal.aborted"));
        AssertWindow(await page.EvaluateAsync<JsonElement>("() => managed.getXWindow()"), 2, 8);

        var annotationBasis = await ResolveAsync(page, 2);
        await ChangesAsync(page, 2);
        AssertWindow(await page.EvaluateAsync<JsonElement>("() => managed.getXWindow()"), 2, 8);
        Assert.Equal("6.25", await page.Locator("[data-cfx-annotation-id=note]").GetAttributeAsync("data-cfx-source-x"));
        Assert.Equal("Reviewed", await page.Locator("[data-cfx-annotation-id=note]").GetAttributeAsync("data-cfx-label"));
        Assert.Equal("note", await page.EvaluateAsync<string>("() => managed.getSelection()[0].annotationId"));
        Assert.True(await page.Locator("[data-cfx-annotation-id=note]").EvaluateAsync<bool>("node => node===document.activeElement"));
        await page.EvaluateAsync("() => managed.setViewport({x:{minimum:3,maximum:7}})");
        await PendingAsync(page, 4);
        Assert.Equal(annotationBasis, await RequestFieldAsync(page, 3, "basis"));
        await ResolveAsync(page, 3); await ChangesAsync(page, 3);
        AssertWindow(await page.EvaluateAsync<JsonElement>("() => managed.getXWindow()"), 3, 7);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The committed native scene did not export.");
        var exported = XDocument.Load(path);
        var note = Assert.Single(exported.Descendants(), node => (string?)node.Attribute("data-cfx-annotation-id") == "note");
        Assert.Equal("6.25", (string?)note.Attribute("data-cfx-source-x"));
        Assert.Equal("Reviewed", (string?)note.Attribute("data-cfx-label"));
        Assert.Equal(await page.Locator("[data-cfx-role=axis-x-label]").AllTextContentsAsync(), await BrowserProofCaptions.ExportedAsync(page, exported, "axis-x-label"));
        Assert.True(await page.EvaluateAsync<bool>("() => pending.every(item=>Object.isFrozen(item.request))"));
        Assert.Empty(await page.EvaluateAsync<string[]>("() => managedErrors.map(error=>error.message)"));
        await CaptureAsync(page, "committed-" + width + "-" + mode);
        Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task ManagedConnectionRejectsUncommittedRepliesAndOwnsCrossChannelUpdateReconnectAndDisposal() {
        if (!Enabled) return;
        var host = new NativeHost(900, VisualThemeMode.Light);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, 924, 800);
        var page = session.Page;
        var pageErrors = new List<string>(); page.PageError += (_, error) => pageErrors.Add(error);
        await ConnectAsync(page, host, "x,y");
        var initial = await SceneAsync(page);
        await page.EvaluateAsync("() => managed.setViewport({x:{minimum:2,maximum:8},y:{minimum:20,maximum:80}})");
        await PendingAsync(page, 1); await PrepareReplyAsync(page, 0);
        await page.EvaluateAsync("() => pending[0].resolve({html:'<p>Not a generated chart section</p>',basis:replies[0].basis})");
        await ErrorsAsync(page, 1);
        Assert.Equal(initial.GetRawText(), (await SceneAsync(page)).GetRawText());
        await page.EvaluateAsync("() => managed.setViewport({x:{minimum:3,maximum:7},y:{minimum:10,maximum:90}})");
        await PendingAsync(page, 2);
        Assert.Equal("initial", await RequestFieldAsync(page, 1, "basis"));
        await page.EvaluateAsync("() => pending[1].reject(new Error('Native preparation was declined'))");
        await ErrorsAsync(page, 2);
        Assert.Equal(initial.GetRawText(), (await SceneAsync(page)).GetRawText());
        await page.EvaluateAsync("() => managed.setViewport({x:{minimum:1,maximum:9},y:{minimum:10,maximum:90}})");
        await PendingAsync(page, 3); await PrepareReplyAsync(page, 2);
        await page.EvaluateAsync("() => managed.applyAnnotationChange({operation:'add',id:'extra',drawing:{kind:'Note',start:{x:5,y:50},label:'Added'}})");
        await PendingAsync(page, 4);
        Assert.True(await page.EvaluateAsync<bool>("() => pending[2].signal.aborted&&!pending[3].signal.aborted"));
        Assert.Equal("initial", await RequestFieldAsync(page, 3, "basis"));
        Assert.Equal("annotation", await RequestFieldAsync(page, 3, "channel"));
        await ResolveAsync(page, 3); await ChangesAsync(page, 1);
        await page.EvaluateAsync("() => pending[2].resolve(replies[2])"); await DrainAsync(page);
        AssertWindow(await page.EvaluateAsync<JsonElement>("() => managed.getXWindow()"), 0, 10);
        AssertWindow(await page.EvaluateAsync<JsonElement>("() => managed.getYWindow()"), 0, 100);
        Assert.Equal("Added", await page.Locator("[data-cfx-annotation-id=extra]").GetAttributeAsync("data-cfx-label"));

        await page.EvaluateAsync("() => managed.applyAnnotationChange({operation:'replace',id:'extra',drawing:{kind:'Note',start:{x:6,y:50},label:'Uncommitted'}})");
        await PendingAsync(page, 5); await PrepareReplyAsync(page, 4);
        var committed = await SceneAsync(page);
        var external = host.External();
        Assert.Equal("TypeError", await page.EvaluateAsync<string>("reply => {try{managed.update(reply.html);return 'accepted';}catch(error){return error.name;}}", external));
        Assert.Equal(committed.GetRawText(), (await SceneAsync(page)).GetRawText());
        Assert.False(await page.EvaluateAsync<bool>("() => pending[4].signal.aborted"));
        await page.EvaluateAsync("reply => {window.externalReply=reply;managed.update(reply.html,{basis:reply.basis});}", external);
        Assert.True(await page.EvaluateAsync<bool>("() => pending[4].signal.aborted"));
        await page.EvaluateAsync("() => pending[4].resolve(replies[4])"); await DrainAsync(page);
        AssertWindow(await page.EvaluateAsync<JsonElement>("() => managed.getXWindow()"), 3, 9);
        Assert.Equal("External base", await page.Locator("[data-cfx-annotation-id=note]").GetAttributeAsync("data-cfx-label"));
        Assert.Equal(0, await page.Locator("[data-cfx-annotation-id=extra]").CountAsync());
        await page.EvaluateAsync("() => managed.applyAnnotationChange({operation:'replace',id:'note',drawing:{kind:'Note',start:{x:6,y:45},label:'Detached'}})");
        await PendingAsync(page, 6); await PrepareReplyAsync(page, 5);
        Assert.Equal(await page.EvaluateAsync<string>("() => externalReply.basis"), await RequestFieldAsync(page, 5, "basis"));
        await page.EvaluateAsync("() => {window.advanced=[];managed.connectAnnotations((request,signal)=>new Promise(resolve=>advanced.push({request,signal,resolve})));}");
        Assert.True(await page.EvaluateAsync<bool>("() => pending[5].signal.aborted"));
        await page.EvaluateAsync("() => pending[5].resolve(replies[5])"); await DrainAsync(page);
        Assert.Equal("TypeError", await page.EvaluateAsync<string>("() => {try{managed.setViewport({x:{minimum:4,maximum:8}});return 'accepted';}catch(error){return error.name;}}"));
        await page.EvaluateAsync("() => managed.applyAnnotationChange({operation:'replace',id:'note',drawing:{kind:'Note',start:{x:6,y:45},label:'Advanced'}})");
        await page.WaitForFunctionAsync("() => advanced.length===1");
        Assert.True(await page.EvaluateAsync<bool>("() => !Object.hasOwn(advanced[0].request,'basis')&&!Object.hasOwn(advanced[0].request,'channel')"));

        await ReconnectAsync(page);
        Assert.True(await page.EvaluateAsync<bool>("() => advanced[0].signal.aborted"));
        await page.EvaluateAsync("() => managed.setViewport({x:{minimum:4,maximum:8}})");
        await PendingAsync(page, 7); await PrepareReplyAsync(page, 6);
        Assert.Equal(await page.EvaluateAsync<string>("() => externalReply.basis"), await RequestFieldAsync(page, 6, "basis"));
        await page.EvaluateAsync("() => managed.connectNative(null)");
        Assert.True(await page.EvaluateAsync<bool>("() => pending[6].signal.aborted"));
        await page.EvaluateAsync("() => pending[6].resolve(replies[6])"); await DrainAsync(page);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        Assert.Equal("1.250", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-zoom"));
        AssertWindow(await page.EvaluateAsync<JsonElement>("() => managed.getXWindow()"), 3, 9);
        await CaptureAsync(page, "external-disconnected");
        await ReconnectAsync(page);
        await page.EvaluateAsync("() => managed.setViewport({x:{minimum:4,maximum:8}})");
        await PendingAsync(page, 8); await PrepareReplyAsync(page, 7);
        Assert.Equal(await page.EvaluateAsync<string>("() => externalReply.basis"), await RequestFieldAsync(page, 7, "basis"));
        await page.EvaluateAsync("() => managed.dispose()");
        Assert.True(await page.EvaluateAsync<bool>("() => pending[7].signal.aborted&&ChartForgeXCharts.get(managed.root)===null"));
        await page.EvaluateAsync("() => pending[7].resolve(replies[7])"); await DrainAsync(page);
        Assert.Equal(3, await page.EvaluateAsync<double>("() => JSON.parse(managed.root.dataset.cfxPreparedChart).xProjection.minimum"));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => managedChanges.length"));
        Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }

    private static async Task ConnectAsync(IPage page, NativeHost host, string? axes = null) {
        await page.ExposeFunctionAsync("managedHostPrepare", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("""
            axes => {
                window.managed=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                window.pending=[];window.replies=[];window.managedChanges=[];window.managedErrors=[];window.chainAnnotation=false;
                window.controlledNative=(request,signal)=>new Promise((resolve,reject)=>pending.push({request,signal,resolve,reject}));
                managed.root.addEventListener('cfxviewporterror',event=>managedErrors.push(event.detail));
                managed.root.addEventListener('cfxannotationerror',event=>managedErrors.push(event.detail));
                managed.root.addEventListener('cfxannotationchange',event=>managedChanges.push(event.detail));
                managed.root.addEventListener('cfxviewportchange',event=>{
                    managedChanges.push(event.detail);
                    if(chainAnnotation){chainAnnotation=false;managed.applyAnnotationChange({operation:'replace',id:'note',
                        drawing:{kind:'Note',start:{x:6.25,y:40.5},label:'Reviewed'}});}
                });
                managed.connectNative(controlledNative,{basis:'initial',...(axes?{axes:axes.split(',')}:{})});
            }
            """, axes);
    }

    private static Task ReconnectAsync(IPage page) => page.EvaluateAsync("() => managed.connectNative(controlledNative,{basis:externalReply.basis,axes:['x']})");
    private static Task PendingAsync(IPage page, int count) => page.WaitForFunctionAsync("count => pending.length===count", count);
    private static Task ChangesAsync(IPage page, int count) => page.WaitForFunctionAsync("count => managedChanges.length===count", count);
    private static Task ErrorsAsync(IPage page, int count) => page.WaitForFunctionAsync("count => managedErrors.length===count", count);
    private static Task<string> RequestFieldAsync(IPage page, int index, string field) => page.EvaluateAsync<string>("({index,field}) => pending[index].request[field]", new { index, field });
    private static Task PrepareReplyAsync(IPage page, int index) => page.EvaluateAsync("async index => {replies[index]=await managedHostPrepare(pending[index].request);}", index);
    private static Task<string> ResolveAsync(IPage page, int index) => page.EvaluateAsync<string>("async index => {replies[index]=await managedHostPrepare(pending[index].request);pending[index].resolve(replies[index]);return replies[index].basis;}", index);
    private static Task DrainAsync(IPage page) => page.EvaluateAsync("() => new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
    private static Task<JsonElement> SceneAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => ({revision:managed.sceneRevision,viewport:managed.getViewport(),annotations:[...managed.root.querySelectorAll('[data-cfx-annotation-id]')]
            .map(node=>({id:node.dataset.cfxAnnotationId,x:node.dataset.cfxSourceX,label:node.dataset.cfxLabel}))})
        """);
    private static void AssertWindow(JsonElement window, double minimum, double maximum) {
        Assert.Equal(minimum, window.GetProperty("minimum").GetDouble(), 8);
        Assert.Equal(maximum, window.GetProperty("maximum").GetDouble(), 8);
    }
    private static async Task CaptureAsync(IPage page, string state) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Capture output must be absolute.");
        Directory.CreateDirectory(directory);
        var stem = Path.Combine(directory, "managed-native-" + state);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".png", FullPage = true });
        await File.WriteAllTextAsync(stem + ".json", await page.EvaluateAsync<string>("() => JSON.stringify({requests:pending.map(item=>item.request),changes:managedChanges,errors:managedErrors},null,2)"));
    }

    private sealed class NativeHost {
        private readonly int _width;
        private readonly VisualThemeMode _mode;
        private readonly bool _typed;
        private readonly bool _navigation;
        private double _producerOffset;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        private readonly Dictionary<string, Snapshot> _snapshots = new();
        internal NativeHost(int width, VisualThemeMode mode, bool typed = false, bool navigation = false) {
            _width = width; _mode = mode; _typed = typed; _navigation = navigation;
            var chart = Model();
            _snapshots.Add("initial", new Snapshot(_renderer.PrepareSection(chart, configure: ConfigureForHost), chart.Annotations.ToArray()));
        }
        internal string Page => "<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>" + _snapshots["initial"].Section.Html
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>";
        internal HtmlChartSection Section(string basis) { lock (_snapshots) return _snapshots[basis].Section; }
        internal void ReplaceProducerOffset(double offset) { lock (_snapshots) _producerOffset = offset; }
        internal object External() {
            lock (_snapshots) {
                var chart = Model().ReplaceAnnotation(ChartAnnotation.Note(new ChartPoint(5, 45), "External base", Ink, new ChartAnnotationId("note")));
                return Store(chart, new ChartCartesianViewport(x: new ChartAxisWindow(3, 9)));
            }
        }
        internal object Prepare(JsonElement request) {
            lock (_snapshots) {
                var previous = _snapshots[request.GetProperty("basis").GetString()!];
                var chart = Model().ClearAnnotations();
                foreach (var annotation in previous.Annotations) chart.AddAnnotation(annotation);
                var xProjection = previous.Section.PreparedVisual.XProjection!;
                var yProjection = previous.Section.PreparedVisual.YProjection!;
                ChartAxisWindow? x = xProjection.Minimum != xProjection.FullMinimum || xProjection.Maximum != xProjection.FullMaximum || !xProjection.MaximumInclusive
                    ? new ChartAxisWindow(xProjection.Minimum, xProjection.Maximum, xProjection.MaximumInclusive) : null;
                ChartAxisWindow? y = yProjection.Minimum != yProjection.FullMinimum || yProjection.Maximum != yProjection.FullMaximum || !yProjection.MaximumInclusive
                    ? new ChartAxisWindow(yProjection.Minimum, yProjection.Maximum, yProjection.MaximumInclusive) : null;
                var operation = request.GetProperty("kind").GetString();
                if (request.GetProperty("channel").GetString() == "viewport") {
                    if (operation == "reset") { x = null; y = null; }
                    else {
                        var viewport = request.GetProperty("sourceViewport");
                        if (viewport.TryGetProperty("x", out var sourceX)) x = xProjection.ConstrainViewport(NativeWindowTransport.Read(sourceX));
                        if (viewport.TryGetProperty("y", out var sourceY)) y = yProjection.ConstrainViewport(NativeWindowTransport.Read(sourceY));
                    }
                } else {
                    var id = new ChartAnnotationId(request.GetProperty("annotationId").GetString()!);
                    if (operation == "remove") chart.RemoveAnnotation(id);
                    else {
                        var drawing = request.GetProperty("sourceDrawing");
                        var start = drawing.GetProperty("start");
                        var annotation = ChartAnnotation.Note(new ChartPoint(start.GetProperty("x").GetDouble(), start.GetProperty("y").GetDouble()),
                            drawing.GetProperty("label").GetString()!, Ink, id);
                        if (operation == "add") chart.AddAnnotation(annotation);
                        else chart.ReplaceAnnotation(annotation);
                    }
                }
                return Store(chart, x == null && y == null ? null : new ChartCartesianViewport(x, y));
            }
        }
        private object Store(Chart chart, ChartCartesianViewport? viewport) {
            var section = _renderer.PrepareSection(chart, viewport, ConfigureForHost);
            var basis = "native-" + _snapshots.Count;
            _snapshots.Add(basis, new Snapshot(section, chart.Annotations.ToArray()));
            return new { html = section.Html, basis };
        }
        private Chart Model() {
            var chart = Chart.Create().WithSize(_width, 480).WithTheme(VisualTheme.Graphite(), _mode)
                .WithFontFamily(BrowserProofFonts.Family)
                .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
                .WithTitle("Native host edits").WithLegend(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 100);
            var source = _typed
                ? ChartSeries.CreateRangeBar(new ChartSeriesId("readings"), "Readings", Enumerable.Range(0, 11)
                    .Select(value => new ChartInterval(value, 20 + value * 5 + _producerOffset, 28 + value * 5 + _producerOffset)))
                    .WithObservationIds(Enumerable.Range(0, 11).Select(value => new ChartObservationId("reading-" + value)))
                : new ChartSeries(new ChartSeriesId("readings"), "Readings", ChartSeriesKind.Line,
                    Enumerable.Range(0, 11).Select(value => new ChartPoint(value, 20 + value * 5 + _producerOffset)));
            return chart.AddSeries(source)
                .AddVerticalLine(new ChartAnnotationId("guide"), 4, "Review threshold")
                .AddNote(new ChartPoint(3, 40), "Review", Ink, new ChartAnnotationId("note"));
        }
        private void ConfigureForHost(HtmlChartInteractionOptions options) { Configure(options); if (_navigation) options.XNavigation.Enabled = true; }
        private static readonly ChartColor Ink = ChartColor.FromHex("#278C74");
        private static void Configure(HtmlChartInteractionOptions options) {
            options.Interaction.ChartId = "managed-native";
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Export);
            options.Tooltip.PinOnSelect = false;
        }
        private sealed record Snapshot(HtmlChartSection Section, ChartAnnotation[] Annotations);
    }
}
