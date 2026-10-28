using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class NativePhysicalViewportBrowserTests {
    /// <summary>Proves discrete dates through physical input and accepted native scenes, without timing or a browser-side projection oracle.</summary>
    [BrowserTheory]
    [InlineData(true, false, 390, VisualThemeMode.Dark)]
    [InlineData(true, true, 900, VisualThemeMode.Light)]
    [InlineData(false, false, 390, VisualThemeMode.Dark)]
    [InlineData(false, true, 900, VisualThemeMode.Light)]
    public async Task DiscreteTimeSpacingKeepsCalendarAndOrdinalFactsThroughBrushNavigationExportsAndReset(
        bool unit, bool reversed, int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var host = new TimeSpacingHost(unit, reversed, width, mode);
        await using var session = await BrowserProofFonts.OpenAsync(host.Page, width + 24, 1000);
        var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.ExposeFunctionAsync("prepareDiscreteTime", (JsonElement request) => host.Prepare(request));
        await page.EvaluateAsync("""
            ()=>{
                window.timeController=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                window.timeRequests=[];window.timeChanges=[];window.timeErrors=[];window.timeBrush=[];
                timeController.root.addEventListener('cfxviewportchange',event=>timeChanges.push(event.detail));
                timeController.root.addEventListener('cfxviewporterror',event=>timeErrors.push(event.detail));
                timeController.root.addEventListener('cfxbrush',event=>timeBrush.push(event.detail));
                timeController.connectNative(async(request,signal)=>{
                    timeRequests.push(request);const reply=await prepareDiscreteTime(request);
                    if(signal.aborted)throw new DOMException('Canceled','AbortError');return reply;
                },{basis:'initial',axes:['x']});
            }
            """);
        var initial = await TimeSpacingScene(page);
        Assert.Equal(unit ? "unit" : "ordinal", initial.GetProperty("projection").GetProperty("timeSpacing").GetProperty("kind").GetString());
        Assert.Equal(host.RawInstants, initial.GetProperty("rawInstants").EnumerateArray().Select(value => value.GetDouble()));
        var centers = await page.EvaluateAsync<double[]>("""
            ()=>[...timeController.root.querySelectorAll('[data-cfx-role=point][data-cfx-series-key=dated]')].map(node=>{
                const bounds=node.getBBox();return bounds.x+bounds.width/2;
            })
            """);
        Assert.Equal(host.RawInstants.Length, centers.Length);
        if (unit) {
            // January -> March crosses the deliberately empty February band; March -> April is one band.
            Assert.InRange(Math.Abs(Math.Abs(centers[1]-centers[0])-2*Math.Abs(centers[2]-centers[1])), 0, .01);
            Assert.Contains("Feb 01", initial.GetProperty("xLabels").EnumerateArray().Select(value => value.GetString()));
        } else {
            // Jan 1, Jan 2, Jan 12 and Jan 31 remain equally spaced despite their different elapsed gaps.
            Assert.InRange(Math.Abs(Math.Abs(centers[1]-centers[0])-Math.Abs(centers[2]-centers[1])), 0, .01);
            Assert.InRange(Math.Abs(Math.Abs(centers[2]-centers[1])-Math.Abs(centers[3]-centers[2])), 0, .01);
        }
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        var pointer = await page.EvaluateAsync<double[]>("""
            unit=>{
                const root=timeController.root,svg=root.querySelector('.cfx-stage svg'),matrix=svg.getScreenCTM();
                const marks=[...root.querySelectorAll('[data-cfx-role=point][data-cfx-series-key=dated]')];
                const center=node=>{const box=node.getBBox();return box.x+box.width/2;};
                const first=center(marks[unit?0:1]),last=center(marks[unit?1:2]);
                const plot=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
                const from=new DOMPoint(first+(last-first)*.4,plot.y+plot.height*.3).matrixTransform(matrix);
                const to=new DOMPoint(first+(last-first)*.6,plot.y+plot.height*.7).matrixTransform(matrix);
                return[from.x,from.y,to.x,to.y];
            }
            """, unit);
        await page.Mouse.MoveAsync((float)pointer[0], (float)pointer[1]); await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)pointer[2], (float)pointer[3], new MouseMoveOptions { Steps = 4 }); await page.Mouse.UpAsync();
        await page.WaitForFunctionAsync("()=>timeBrush.length===1");
        var brush = await page.EvaluateAsync<JsonElement>("()=>timeBrush[0]");
        var range = brush.GetProperty("range").GetProperty("x").EnumerateArray()
            .Select(value => DateTimeOffset.Parse(value.GetString()!, CultureInfo.InvariantCulture).UtcDateTime).ToArray();
        Assert.Equal(unit ? new[] { TimeSpacingHost.February, TimeSpacingHost.February }
            : new[] { TimeSpacingHost.January.AddDays(1), TimeSpacingHost.January.AddDays(11) }, range);
        Assert.Equal(host.RawInstants, (await TimeSpacingScene(page)).GetProperty("rawInstants").EnumerateArray().Select(value => value.GetDouble()));
        await CaptureTimeSpacing(page, host, "inverse");
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();

        await page.EvaluateAsync("bounds=>timeController.setViewport({x:bounds})", new { minimum = host.WindowMinimum, maximum = host.WindowMaximum });
        await TimeSpacingChanges(page, 1);
        Window((await page.EvaluateAsync<JsonElement>("()=>timeController.getViewport()")).GetProperty("x"), host.WindowMinimum, host.WindowMaximum);
        var narrowed = await TimeSpacingScene(page);
        Assert.Equal(initial.GetProperty("yLabels").GetRawText(), narrowed.GetProperty("yLabels").GetRawText());
        Assert.Equal(initial.GetProperty("secondaryYLabels").GetRawText(), narrowed.GetProperty("secondaryYLabels").GetRawText());
        Assert.NotEqual(initial.GetProperty("xLabels").GetRawText(), narrowed.GetProperty("xLabels").GetRawText());
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>timeRequests[0].basis"));
        var span = ProjectionSpan(narrowed);
        var position = page.Locator("[data-cfx-x-range=position]");
        Assert.False(await position.IsDisabledAsync());
        await position.FocusAsync(); await page.Keyboard.PressAsync("Home"); await TimeSpacingChanges(page, 2);
        var moved = await TimeSpacingScene(page);
        Assert.Equal(host.FullMinimum, moved.GetProperty("projection").GetProperty("minimum").GetDouble(), 8);
        Assert.Equal(span, ProjectionSpan(moved), 8); // Native accepted navigation preserves bands/ordinal steps, not elapsed days.
        Assert.Equal(host.FullMinimum, await page.EvaluateAsync<double>("()=>timeRequests[1].sourceViewport.x.minimum"), 8);
        Assert.Equal("time-spacing-1", await page.EvaluateAsync<string>("()=>timeRequests[1].basis"));
        Assert.True(await position.EvaluateAsync<bool>("input=>input===input.getRootNode().activeElement"));
        Assert.Equal(initial.GetProperty("yLabels").GetRawText(), moved.GetProperty("yLabels").GetRawText());
        Assert.Equal(initial.GetProperty("secondaryYLabels").GetRawText(), moved.GetProperty("secondaryYLabels").GetRawText());

        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svg = XDocument.Load(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No discrete-time SVG download."));
        Assert.Equal(moved.GetProperty("xLabels").EnumerateArray().Select(value => value.GetString()), await BrowserProofCaptions.ExportedAsync(page, svg, "axis-x-label"));
        Assert.Equal(host.RawInstants, svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point"
            && (string?)node.Attribute("data-cfx-series-key") == "dated").Select(node => double.Parse(node.Attribute("data-cfx-x")!.Value, CultureInfo.InvariantCulture)));
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var png = await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No discrete-time PNG download."));
        var scale = await page.EvaluateAsync<int>("()=>Math.max(1,Math.ceil(devicePixelRatio||1))");
        Assert.Equal((int)host.Last.PreparedVisual.Size.Width * scale, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)));
        Assert.Equal((int)host.Last.PreparedVisual.Size.Height * scale, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
        await CaptureTimeSpacing(page, host, "committed", svgDownload, pngDownload);

        await page.Locator("[data-cfx-x-full]").ClickAsync(); await TimeSpacingChanges(page, 3);
        Window((await page.EvaluateAsync<JsonElement>("()=>timeController.getViewport()")).GetProperty("x"), host.FullMinimum, host.FullMaximum);
        var reset = await TimeSpacingScene(page);
        Assert.Equal(initial.GetProperty("xLabels").GetRawText(), reset.GetProperty("xLabels").GetRawText());
        Assert.Equal(initial.GetProperty("rawInstants").GetRawText(), reset.GetProperty("rawInstants").GetRawText());
        Assert.Equal(host.FullSvg, host.Last.PreparedVisual.ToSvg());
        Assert.Equal("true", await page.Locator("[data-cfx-x-full]").GetAttributeAsync("aria-pressed"));
        Assert.True(await page.Locator("[data-cfx-x-range=position]").IsDisabledAsync());
        await CaptureTimeSpacing(page, host, "reset");
        await page.EvaluateAsync("()=>timeController.dispose()");
        Assert.True(await page.EvaluateAsync<bool>("()=>ChartForgeXCharts.get(timeController.root)===null"));
        Assert.Empty(errors); Assert.Empty(await page.EvaluateAsync<JsonElement[]>("()=>timeErrors")); AssertNoConsoleErrors(session);
    }

    private static Task TimeSpacingChanges(IPage page, int count) => page.WaitForFunctionAsync("count=>timeChanges.length===count", count);
    private static double ProjectionSpan(JsonElement scene) {
        var projection = scene.GetProperty("projection");
        return projection.GetProperty("transformedMaximum").GetDouble() - projection.GetProperty("transformedMinimum").GetDouble();
    }
    private static async Task<JsonElement> TimeSpacingScene(IPage page) {
        await BrowserProofCaptions.InstallNativeAsync(page);
        return await page.EvaluateAsync<JsonElement>("""
        ()=>{const root=timeController.root;return{
            projection:JSON.parse(root.dataset.cfxPreparedChart).xProjection,
            xLabels:[...root.querySelectorAll('[data-cfx-role=axis-x-label]')].map(node=>window.cfxCaptionText(node)),
            yLabels:[...root.querySelectorAll('[data-cfx-role=axis-y-label]')].map(node=>window.cfxCaptionText(node)),
            secondaryYLabels:[...root.querySelectorAll('[data-cfx-role=axis-secondary-y-label]')].map(node=>window.cfxCaptionText(node)),
            rawInstants:[...root.querySelectorAll('[data-cfx-role=point][data-cfx-series-key=dated]')].map(node=>Number(node.dataset.cfxX)),
            requests:timeRequests,changes:timeChanges,brush:timeBrush,errors:timeErrors};}
        """);
    }
    private static async Task CaptureTimeSpacing(IPage page, TimeSpacingHost host, string state, IDownload? svg = null, IDownload? png = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Discrete-time captures require an absolute task-owned directory.");
        Directory.CreateDirectory(directory);
        var stem = Path.Combine(directory, $"native-time-{host.Name}-{host.Width}-{state}");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".png", FullPage = true });
        await File.WriteAllTextAsync(stem + ".json", (await TimeSpacingScene(page)).GetRawText());
        await File.WriteAllTextAsync(stem + ".native.svg", host.Last.PreparedVisual.ToSvg());
        await File.WriteAllBytesAsync(stem + ".native.png", host.Last.PreparedVisual.ToPng());
        if (svg != null) await svg.SaveAsAsync(stem + ".export.svg");
        if (png != null) await png.SaveAsAsync(stem + ".export.png");
    }

    /// <summary>Owns source storage and native preparation; all date transforms remain in the existing projection owner.</summary>
    private sealed class TimeSpacingHost {
        internal static readonly DateTime January = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal static readonly DateTime February = new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly HtmlInteractiveChartRenderer _renderer = new();
        private readonly Dictionary<string, HtmlChartSection> _sections = new();
        private readonly Chart _source;
        internal TimeSpacingHost(bool unit, bool reversed, int width, VisualThemeMode mode) {
            Name = unit ? "unit" : "ordinal"; Width = width;
            var dates = unit ? new[] { January.AddDays(9), January.AddMonths(2).AddDays(19), January.AddMonths(3).AddDays(9) }
                : new[] { January, January.AddDays(1), January.AddDays(11), January.AddDays(30) };
            RawInstants = dates.Select(date => date.ToOADate()).ToArray();
            WindowMinimum = unit ? February.ToOADate() : RawInstants[1];
            WindowMaximum = unit ? January.AddMonths(2).AddDays(19).ToOADate() : RawInstants[2];
            var primary = new ChartSeries(new ChartSeriesId("dated"), "Dated values", ChartSeriesKind.Line,
                RawInstants.Select((date, index) => new ChartPoint(date, 2 + index * 2)));
            var secondary = new ChartSeries(new ChartSeriesId("companion"), "Secondary values", ChartSeriesKind.Line,
                RawInstants.Select((date, index) => new ChartPoint(date, 100 + index * 200))).UseSecondaryValueAxis();
            for (var index = 0; index < dates.Length; index++) {
                primary.WithObservationId(index, new ChartObservationId("dated-" + index));
                secondary.WithObservationId(index, new ChartObservationId("companion-" + index));
            }
            _source = Chart.Create().WithSize(width, 460).WithTitle(unit ? "Calendar month bands" : "Ordinal observation dates")
                .WithFontFamily(BrowserProofFonts.Family).WithTheme(VisualTheme.Graphite(), mode).WithLegend(false).WithDataLabels(false)
                .WithLineMarkers(ChartLineMarkerMode.All).WithYAxisBounds(0, 10).WithSecondaryYAxisBounds(0, 1000)
                .AddSeries(primary).AddSeries(secondary);
            _source.Options.XAxis.WithTimeSpacing(unit ? ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)) : ChartTimeSpacing.Ordinal)
                .WithLabelFormatter(value => DateTime.FromOADate(value).ToString("MMM dd", CultureInfo.InvariantCulture));
            _source.Options.XAxis.Reversed = reversed;
            if (!unit) _source.WithXAxisBounds(RawInstants[0], RawInstants[RawInstants.Length - 1]);
            Last = _renderer.PrepareSection(_source, configure: Configure);
            _sections.Add("initial", Last);
            FullMinimum = Last.PreparedVisual.XProjection!.FullMinimum; FullMaximum = Last.PreparedVisual.XProjection.FullMaximum;
            FullSvg = Last.PreparedVisual.ToSvg();
        }
        internal string Name { get; }
        internal int Width { get; }
        internal double[] RawInstants { get; }
        internal double WindowMinimum { get; }
        internal double WindowMaximum { get; }
        internal double FullMinimum { get; }
        internal double FullMaximum { get; }
        internal string FullSvg { get; }
        internal HtmlChartSection Last { get; private set; }
        internal string Page => "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body style='margin:12px'>" + Last.Html
            + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        internal object Prepare(JsonElement request) {
            lock (_sections) {
                var basis = _sections[request.GetProperty("basis").GetString()!].PreparedVisual;
                ChartAxisWindow? window = null;
                if (request.GetProperty("kind").GetString() != "reset") {
                    if (request.TryGetProperty("sourceViewport", out var viewport)) {
                        var bounds = viewport.GetProperty("x");
                        window = basis.XProjection!.ConstrainViewport(NativeWindowTransport.Read(bounds));
                    } else window = basis.XProjection!.ConstrainViewport(request.GetProperty("left").GetDouble(), request.GetProperty("right").GetDouble());
                }
                Last = _renderer.PrepareSection(_source, window == null ? null : new ChartCartesianViewport(x: window), Configure);
                var token = "time-spacing-" + _sections.Count;
                _sections.Add(token, Last);
                return new { html = Last.Html, basis = token };
            }
        }
        private static void Configure(HtmlChartInteractionOptions options) {
            options.IdScope = "native-discrete-time"; options.Interaction.ChartId = "discrete-time";
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Enable(ChartInteractionFeatures.Brush | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.StateBookmarks | ChartInteractionFeatures.Export);
            options.Interaction.BrushAxis = ChartBrushAxis.X;
            options.XNavigation.Enabled = true; options.XNavigation.IncludeNavigator = false;
        }
    }
}
