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

/// <summary>Qualifies exact physical-axis requests through actual native replies and the existing accepted-basis lifetime.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed partial class NativePhysicalViewportBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390, VisualThemeMode.Dark)]
    [InlineData(true, 900, VisualThemeMode.Light)]
    public async Task SecondaryPhysicalWindowsKeepPrimaryAxesBookmarksExportsAndAcceptedBasis(bool horizontal, int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var host = new Host(horizontal, width, mode);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 1000);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.ExposeFunctionAsync("physicalPrepare", (JsonElement request) => host.Prepare(request));
        var secondary = horizontal ? "secondaryX" : "secondaryY";
        await page.EvaluateAsync("""
            ({horizontal,secondary})=>{
                window.physical=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                window.pendingPhysical=[];window.physicalReplies=[];window.physicalChanges=[];window.physicalErrors=[];
                physical.root.addEventListener('cfxviewportchange',event=>physicalChanges.push(event.detail));
                physical.root.addEventListener('cfxviewporterror',event=>physicalErrors.push(event.detail));
                physical.connectNative((request,signal)=>new Promise((resolve,reject)=>pendingPhysical.push({request,signal,resolve,reject})),
                    {basis:'initial',axes:[horizontal?'y':'x',secondary]});
                window.setPhysical=minimum=>physical.setViewport({[secondary]:{minimum,maximum:1000-minimum}});
                setPhysical(200);
            }
            """, new { horizontal, secondary });
        await Pending(page, 1);
        await page.EvaluateAsync("()=>setPhysical(300)");
        await Pending(page, 2);
        Assert.True(await page.EvaluateAsync<bool>("()=>pendingPhysical[0].signal.aborted&&!pendingPhysical[1].signal.aborted"));
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>pendingPhysical[1].request.basis"));
        Assert.True(await page.EvaluateAsync<bool>("()=>Object.isFrozen(pendingPhysical[1].request.axes)&&Object.isFrozen(pendingPhysical[1].request.sourceViewport)"));
        await Resolve(page, 1); await Changes(page, 1);
        var window = await page.EvaluateAsync<JsonElement>("()=>physical.getViewport()");
        Window(window.GetProperty(secondary), 300, 700);
        Window(window.GetProperty(horizontal ? "x" : "y"), 0, 100);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync(); await Pending(page, 3);
        Assert.Equal(new[] { horizontal ? "y" : "x", secondary }, (await page.EvaluateAsync<JsonElement>("()=>pendingPhysical[2].request.axes"))
            .EnumerateArray().Select(axis => axis.GetString()));
        var acceptedBasis = await page.EvaluateAsync<string>("()=>physicalReplies[1].basis");
        Assert.Equal(acceptedBasis, await page.EvaluateAsync<string>("()=>pendingPhysical[2].request.basis"));
        await page.EvaluateAsync("()=>pendingPhysical[2].reject(new Error('Retain the accepted physical domains'))");
        await page.WaitForFunctionAsync("()=>physicalErrors.length===1");
        await page.EvaluateAsync("()=>setPhysical(400)"); await Pending(page, 4);
        Assert.Equal(acceptedBasis, await page.EvaluateAsync<string>("()=>pendingPhysical[3].request.basis"));
        await Resolve(page, 3); await Changes(page, 2);
        Window((await page.EvaluateAsync<JsonElement>("()=>physical.getViewport()")).GetProperty(secondary), 400, 600);
        await page.EvaluateAsync("""
            ()=>{physical.root.dispatchEvent(new CustomEvent('cfx-capture-state',{detail:{sync:false}}));
                window.physicalBookmark=JSON.parse(physical.root.dataset.cfxInteractionSnapshot);}
            """);
        var bookmark = await page.EvaluateAsync<JsonElement>("()=>physicalBookmark");
        Assert.Equal(2, bookmark.GetProperty("version").GetInt32()); Window(bookmark.GetProperty("nativeViewport").GetProperty(secondary), 400, 600);
        await page.EvaluateAsync("()=>setPhysical(200)"); await Pending(page, 5); await Resolve(page, 4); await Changes(page, 3);
        await page.EvaluateAsync("()=>physical.root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:physicalBookmark,sync:false}}))");
        await Pending(page, 6); await Resolve(page, 5); await Changes(page, 4);
        Window((await page.EvaluateAsync<JsonElement>("()=>physical.getViewport()")).GetProperty(secondary), 400, 600);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No physical viewport SVG download.");
        var svg = XDocument.Load(path);
        var role = horizontal ? "axis-secondary-x-label" : "axis-secondary-y-label";
        Assert.Equal(await page.Locator("[data-cfx-role=" + role + "]").AllTextContentsAsync(), svg.Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == role).Select(node => node.Value));
        Assert.Equal(3, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-series-key") == "secondary"
            && (string?)node.Attribute("data-cfx-role") == "point"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            if (!Path.IsPathRooted(captures)) throw new InvalidOperationException("Browser captures require an absolute task-owned directory.");
            Directory.CreateDirectory(captures);
            var capture = Path.Combine(captures, "native-physical-" + secondary + "-" + width);
            await download.SaveAsAsync(capture + ".svg");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = capture + ".png", FullPage = true });
            await File.WriteAllTextAsync(capture + ".html", await page.ContentAsync());
            await File.WriteAllTextAsync(capture + ".json", JsonSerializer.Serialize(bookmark));
        }
        await page.EvaluateAsync("()=>setPhysical(250)"); await Pending(page, 7);
        var revision = await page.EvaluateAsync<string>("()=>physical.sceneRevision");
        await page.EvaluateAsync("()=>physical.dispose()");
        Assert.True(await page.EvaluateAsync<bool>("()=>pendingPhysical[6].signal.aborted&&ChartForgeXCharts.get(physical.root)===null"));
        await page.EvaluateAsync("()=>pendingPhysical[6].resolve(physicalReplies[1])");
        Assert.Equal(revision, await page.EvaluateAsync<string>("()=>physical.sceneRevision"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Task Pending(IPage page, int count) => page.WaitForFunctionAsync("count=>pendingPhysical.length===count", count);
    private static Task Changes(IPage page, int count) => page.WaitForFunctionAsync("count=>physicalChanges.length===count", count);
    private static Task Resolve(IPage page, int index) => page.EvaluateAsync("async index=>{physicalReplies[index]=await physicalPrepare(pendingPhysical[index].request);pendingPhysical[index].resolve(physicalReplies[index]);}", index);
    private static void Window(JsonElement value, double minimum, double maximum) {
        Assert.Equal(minimum, value.GetProperty("minimum").GetDouble(), 8); Assert.Equal(maximum, value.GetProperty("maximum").GetDouble(), 8);
    }

    private sealed class Host {
        private readonly bool _horizontal; private readonly int _width; private readonly VisualThemeMode _mode;
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        private readonly Dictionary<string, HtmlChartSection> _sections = new();
        internal Host(bool horizontal, int width, VisualThemeMode mode) {
            _horizontal = horizontal; _width = width; _mode = mode;
            _sections.Add("initial", _renderer.PrepareSection(Model(), configure: Configure));
        }
        internal string Page => "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body style='margin:12px'>" + _sections["initial"].Html
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        internal object Prepare(JsonElement request) {
            lock (_sections) {
                var basis = _sections[request.GetProperty("basis").GetString()!].PreparedVisual;
                var previous = basis.CartesianViewport;
                ChartAxisWindow? x = previous?.X, y = previous?.Y, secondaryX = previous?.SecondaryX, secondaryY = previous?.SecondaryY;
                var policy = request.TryGetProperty("valueDomain", out var value) && value.GetString() == "visibleCategories"
                    ? ChartValueDomain.VisibleCategories : ChartValueDomain.FullData;
                if (request.GetProperty("kind").GetString() == "reset") x = y = secondaryX = secondaryY = null;
                else foreach (var entry in request.GetProperty("axes").EnumerateArray()) {
                    var name = entry.GetString()!;
                    var projection = name == "x" ? basis.XProjection : name == "y" ? basis.YProjection
                        : name == "secondaryX" ? basis.SecondaryXProjection : basis.SecondaryYProjection;
                    ChartAxisWindow? window;
                    if (request.TryGetProperty("sourceViewport", out var source)) {
                        var bounds = source.GetProperty(name);
                        window = projection!.ConstrainViewport(NativeWindowTransport.Read(bounds));
                    } else window = projection!.ConstrainViewport(request.GetProperty(name is "x" or "secondaryX" ? "left" : "top").GetDouble(),
                        request.GetProperty(name is "x" or "secondaryX" ? "right" : "bottom").GetDouble());
                    switch (name) { case "x": x = window; break; case "y": y = window; break; case "secondaryX": secondaryX = window; break; default: secondaryY = window; break; }
                }
                var viewport = x == null && y == null && secondaryX == null && secondaryY == null ? null : new ChartCartesianViewport(x,y,secondaryX,secondaryY,policy);
                var section = _renderer.PrepareSection(Model(), viewport, Configure);
                var token = "physical-" + _sections.Count; _sections.Add(token, section);
                return new { html = section.Html, basis = token };
            }
        }
        private Chart Model() {
            var primary = new ChartSeries(new ChartSeriesId("primary"), "Counts", _horizontal ? ChartSeriesKind.HorizontalBar : ChartSeriesKind.Line,
                new[] { new ChartPoint(1, 20), new ChartPoint(2, 40), new ChartPoint(3, 80) });
            var secondary = new ChartSeries(new ChartSeriesId("secondary"), "Duration", ChartSeriesKind.Line,
                new[] { new ChartPoint(1, 100), new ChartPoint(2, 500), new ChartPoint(3, 900) }).UseSecondaryValueAxis();
            for (var item = 0; item < 3; item++) secondary.WithObservationId(item, new ChartObservationId("secondary-" + item));
            var chart = Chart.Create().WithSize(_width, 440).WithFontFamily(BrowserProofFonts.Family).WithTheme(VisualTheme.Graphite(), _mode)
                .WithLegend(false).AddSeries(primary).AddSeries(secondary);
            if (_horizontal) { chart.Options.LineOrientation = ChartOrientation.Horizontal; chart.WithXAxisBounds(0,100).WithYAxisBounds(0,4).WithSecondaryXAxisBounds(0,1000); }
            else chart.WithXAxisBounds(0,4).WithYAxisBounds(0,100).WithSecondaryYAxisBounds(0,1000);
            return chart;
        }
        private void Configure(HtmlChartInteractionOptions options) {
            options.IdScope = "native-physical-axis"; options.Interaction.ChartId = "physical-axes";
            options.IncludeContextMenu = true;
            options.Interaction.Enable(ChartInteractionFeatures.StateBookmarks);
        }
    }
}
