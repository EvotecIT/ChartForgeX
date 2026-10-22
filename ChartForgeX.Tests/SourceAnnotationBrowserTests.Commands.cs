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

public sealed partial class SourceAnnotationBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Light)]
    public async Task ConnectedSourceCommandsCommitNativeFactsKeepReaderStateAndExportTheCommittedDrawing(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var host = new AnnotationHost(width, mode);
        await using var session = await BrowserProofFonts.OpenAsync(host.Initial, width + 24, 900);
        var page = session.Page; await ConnectAnnotations(page, host);
        await page.EvaluateAsync("() => drawings.select({targets:[drawings.resolveTargets().find(target=>target.annotationId==='note')]})");
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.Locator("[data-cfx-annotation-id=note]").FocusAsync();
        var zoom = await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-zoom");
        Assert.NotEqual("1.000", zoom);
        await page.EvaluateAsync("""
            () => drawings.applyAnnotationChange({operation:'add',id:'review-line',drawing:{kind:'LineSegment',start:{x:1.125,y:12.5},end:{x:8.25,y:30.125},label:'Review line'}})
            """);
        await WaitForAnnotationChanges(page, 1);
        Assert.Equal(zoom, await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-zoom"));
        await page.EvaluateAsync("""
            () => drawings.applyAnnotationChange({operation:'replace',id:'note',drawing:{kind:'Note',start:{x:6.125,y:40.25},label:'Reviewed'}})
            """);
        await WaitForAnnotationChanges(page, 2);
        Assert.Equal("note", (await page.EvaluateAsync<JsonElement>("() => drawings.getSelection()[0]")).GetProperty("annotationId").GetString());
        Assert.True(await page.Locator("[data-cfx-annotation-id=note]").EvaluateAsync<bool>("node => node === document.activeElement"));
        Assert.Equal("6.125", await page.Locator("[data-cfx-annotation-id=note]").GetAttributeAsync("data-cfx-source-x"));
        Assert.Equal(zoom, await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-zoom"));
        Assert.True(await page.EvaluateAsync<bool>("() => annotationFrozen.length===2 && annotationFrozen.every(Boolean)"));
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync(); Assert.NotNull(path);
        var svg = XDocument.Load(path!);
        var note = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-annotation-id") == "note");
        Assert.Equal("6.125", (string?)note.Attribute("data-cfx-source-x"));
        Assert.Equal("Reviewed", (string?)note.Attribute("data-cfx-label"));
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-annotation-id") == "review-line");
        await Capture(page, width, mode, "native-commands");
        await page.EvaluateAsync("() => drawings.applyAnnotationChange({operation:'remove',id:'channel'})");
        await WaitForAnnotationChanges(page, 3);
        Assert.Equal(0, await page.Locator("[data-cfx-annotation-id=channel]").CountAsync());
        await page.EvaluateAsync("() => drawings.applyAnnotationChange({operation:'remove',id:'note'})");
        await WaitForAnnotationChanges(page, 4);
        Assert.Empty((await page.EvaluateAsync<JsonElement>("() => drawings.getSelection()")).EnumerateArray());
        Assert.Empty(await page.EvaluateAsync<string[]>("() => annotationErrors.map(error => error.message)"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SourceCommandsRejectInvalidRepliesAndShareCancellationWithViewportUpdateAndDisposal() {
        if (!Enabled) return;
        var host = new AnnotationHost(800, VisualThemeMode.Light);
        await using var session = await BrowserProofFonts.OpenAsync(host.Initial, 900, 900);
        var page = session.Page; await ConnectAnnotations(page, host);
        var invalid = await page.EvaluateAsync<string[]>("""
            () => [{operation:'add',id:'note',drawing:{kind:'Note',start:{x:2,y:30}}},
                {operation:'remove',id:'absent'},
                {operation:'add',id:'bad-line',drawing:{kind:'LineSegment',start:{x:2,y:30},end:{x:2,y:30}}},
                {operation:'add',id:'bad-channel',drawing:{kind:'ParallelChannel',start:{x:2,y:30},end:{x:4,y:40},yOffset:0}}]
                .map(change=>{try{drawings.applyAnnotationChange(change);return'accepted';}catch(error){return error.name;}})
            """);
        Assert.All(invalid, error => Assert.Equal("TypeError", error));
        await page.EvaluateAsync("""
            () => { window.pending=[];
                drawings.connectViewport((request,signal)=>new Promise(resolve=>pending.push({request,signal,resolve})),{axes:['x']});
                drawings.connectAnnotations((request,signal)=>new Promise(resolve=>pending.push({request,signal,resolve})));
                window.newDrawing={operation:'add',id:'added',drawing:{kind:'Note',start:{x:4,y:30},label:'Added'}};
                drawings.setViewport({x:{minimum:2,maximum:8}}); }
            """);
        await page.WaitForFunctionAsync("() => pending.length===1");
        await page.EvaluateAsync("() => drawings.applyAnnotationChange(newDrawing)");
        await page.WaitForFunctionAsync("() => pending.length===2");
        Assert.True(await page.EvaluateAsync<bool>("() => pending[0].signal.aborted && !pending[1].signal.aborted"));
        await page.EvaluateAsync("html => pending[0].resolve(html)", host.Window());
        await page.EvaluateAsync("""
            async () => {const result=await annotationHostPrepare({...pending[1].request,basis:'initial'});
                pending[1].resolve(result.html);}
            """);
        await WaitForAnnotationChanges(page, 1);
        Assert.Equal(0, await page.EvaluateAsync<double>("() => drawings.getXWindow().minimum"));
        Assert.Equal(1, await page.Locator("[data-cfx-annotation-id=added]").CountAsync());
        var revision = await page.EvaluateAsync<string>("() => drawings.sceneRevision");
        await page.EvaluateAsync("() => drawings.applyAnnotationChange({operation:'replace',id:'added',drawing:{kind:'Note',start:{x:5,y:35},label:'Changed'}})");
        await page.WaitForFunctionAsync("() => pending.length===3");
        await page.EvaluateAsync("html => pending[2].resolve(html)", host.Fresh());
        await page.WaitForFunctionAsync("() => annotationErrors.length===1");
        Assert.Equal(revision, await page.EvaluateAsync<string>("() => drawings.sceneRevision"));
        Assert.Equal("Added", await page.Locator("[data-cfx-annotation-id=added]").GetAttributeAsync("data-cfx-label"));
        await page.EvaluateAsync("() => drawings.applyAnnotationChange({operation:'remove',id:'added'})");
        await page.WaitForFunctionAsync("() => pending.length===4");
        await page.EvaluateAsync("html => drawings.update(html)", host.Fresh());
        Assert.True(await page.EvaluateAsync<bool>("() => pending[3].signal.aborted"));
        await page.EvaluateAsync("html => pending[3].resolve(html)", host.Fresh());
        await page.EvaluateAsync("() => drawings.applyAnnotationChange(newDrawing)");
        await page.WaitForFunctionAsync("() => pending.length===5");
        await page.EvaluateAsync("() => drawings.dispose()");
        Assert.True(await page.EvaluateAsync<bool>("() => pending[4].signal.aborted && ChartForgeXCharts.get(drawings.root)===null"));
        await page.EvaluateAsync("html => pending[4].resolve(html)", host.Fresh());
        Assert.Equal(0, await page.Locator("[data-cfx-annotation-id=added]").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ExplicitSecondarySourceCommandsDoNotRequireAnAmbiguousPrimaryYInverse() {
        if (!Enabled) return;
        var host = new AnnotationHost(800, VisualThemeMode.Light, secondary: true);
        await using var session = await BrowserProofFonts.OpenAsync(host.Initial, 900, 900);
        var page = session.Page; await ConnectAnnotations(page, host);
        Assert.True(await page.EvaluateAsync<bool>("() => drawings.getYWindow()!==null&&JSON.parse(drawings.root.dataset.cfxPreparedChart).secondaryYProjection!==undefined"));
        await page.EvaluateAsync("""
            () => drawings.applyAnnotationChange({operation:'add',id:'secondary-note',drawing:{kind:'Note',start:{x:5,y:1500},label:'Secondary review',yAxis:'Secondary'}})
            """);
        await WaitForAnnotationChanges(page, 1);
        var note = page.Locator("[data-cfx-annotation-id=secondary-note]");
        Assert.Equal("Secondary", await note.GetAttributeAsync("data-cfx-y-axis"));
        Assert.Equal("1500", await note.GetAttributeAsync("data-cfx-source-y"));
        Assert.Equal("Secondary review", await note.GetAttributeAsync("data-cfx-label"));
        var inside = await note.Locator("[data-cfx-role=annotation-anchor]").EvaluateAsync<bool>("""
            node => { const projection=JSON.parse(drawings.root.dataset.cfxPreparedChart).dataProjection,plot=projection.plotBounds;
                const y=Number(node.getAttribute('cy'));return y>plot.y&&y<plot.y+plot.height; }
            """);
        Assert.True(inside);
        Assert.Empty(await page.EvaluateAsync<string[]>("() => annotationErrors.map(error=>error.message)"));
        AssertNoConsoleErrors(session);
    }

    private static Task WaitForAnnotationChanges(IPage page, int count) => page.WaitForFunctionAsync("count => annotationChanges.length===count", count);
    private static async Task ConnectAnnotations(IPage page, AnnotationHost host) {
        await page.ExposeFunctionAsync("annotationHostPrepare", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("""
            () => { window.drawings=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                window.annotationChanges=[];window.annotationErrors=[];window.annotationFrozen=[];
                drawings.root.addEventListener('cfxannotationerror',event=>annotationErrors.push(event.detail));
                drawings.root.addEventListener('cfxannotationchange',event=>annotationChanges.push(event.detail));
                drawings.connectNative(async(request,signal)=>{
                    const source=request.sourceDrawing;annotationFrozen.push(Object.isFrozen(request)&&(!source||Object.isFrozen(source)&&Object.isFrozen(source.start)&&(!source.end||Object.isFrozen(source.end))));
                    const result=await annotationHostPrepare(request);
                    if(signal.aborted)throw new DOMException('Canceled','AbortError');
                    return {html:result.html,basis:result.token};
                },{basis:'initial',axes:null}); }
            """);
    }
    private static void CommandOptions(HtmlChartInteractionOptions options) {
        Options(options); options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Export);
    }
    private sealed class AnnotationHost {
        private readonly int _width;
        private readonly VisualThemeMode _mode;
        private readonly bool _secondary;
        private readonly bool _references;
        private readonly Dictionary<string, ChartAnnotation[]> _scenes = new();
        internal AnnotationHost(int width, VisualThemeMode mode, bool secondary = false, bool references = false) {
            _width = width; _mode = mode; _secondary = secondary; _references = references; _scenes.Add("initial", Create().Annotations.ToArray());
        }
        internal string Initial => Create().ToInteractiveHtmlFragment(CommandOptions);
        internal string Fresh() => Create().ToInteractiveHtmlFragmentWithoutAssets(CommandOptions);
        internal string Window() => new HtmlInteractiveChartRenderer().PrepareSection(Create(), new ChartCartesianViewport(x: new ChartAxisWindow(2, 8)), CommandOptions).Html;
        private Chart Create() {
            var chart = Model(_width, _mode, false);
            if (_references) chart.AddHorizontalLine(new ChartAnnotationId("reference-line"), 60, "Reference line")
                .AddHorizontalBand(new ChartAnnotationId("reference-band"), 70, 80, "Reference band");
            if (_secondary) chart.WithSecondaryYAxisBounds(1000, 2000).AddSeries(ChartSeries.CreateLine(new ChartSeriesId("secondary"), "Secondary",
                new[] { new ChartPoint(0, 1000), new ChartPoint(10, 2000) }).UseSecondaryValueAxis());
            return chart;
        }
        internal object Prepare(JsonElement request) {
            lock (_scenes) {
                var chart = Create().ClearAnnotations();
                foreach (var annotation in _scenes[request.GetProperty("basis").GetString()!]) chart.AddAnnotation(annotation);
                var id = new ChartAnnotationId(request.GetProperty("annotationId").GetString()!);
                var operation = request.GetProperty("kind").GetString();
                if (operation == "remove") chart.RemoveAnnotation(id);
                else {
                    var source = request.GetProperty("sourceDrawing"); var start = Point(source.GetProperty("start"));
                    var label = source.GetProperty("label").GetString()!;
                    var axis = Enum.Parse<ChartAxisSide>(source.GetProperty("yAxis").GetString()!);
                    var color = ChartColor.FromHex("#278C74");
                    var replacement = source.GetProperty("kind").GetString() switch {
                        "Note" => ChartAnnotation.Note(start, label, color, id, axis),
                        "LineSegment" => ChartAnnotation.LineSegment(start, Point(source.GetProperty("end")), label, color, id, axis),
                        _ => ChartAnnotation.ParallelChannel(start, Point(source.GetProperty("end")), source.GetProperty("yOffset").GetDouble(), label, color, id, axis)
                    };
                    if (operation == "add") chart.AddAnnotation(replacement);
                    else if (operation == "replace") chart.ReplaceAnnotation(replacement);
                    else throw new ArgumentException("Unknown annotation operation.", nameof(request));
                }
                var html = chart.ToInteractiveHtmlFragmentWithoutAssets(CommandOptions);
                var token = "candidate-" + _scenes.Count; _scenes.Add(token, chart.Annotations.ToArray());
                return new { token, html };
            }
        }
        private static ChartPoint Point(JsonElement point) => new(point.GetProperty("x").GetDouble(), point.GetProperty("y").GetDouble());
    }
}
