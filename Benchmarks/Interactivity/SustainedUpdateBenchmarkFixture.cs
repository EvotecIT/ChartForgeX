using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using HtmlTinkerX;

/// <summary>Times public keyed source changes, native adaptive preparation and the committed browser update as one operation.</summary>
public sealed partial class SustainedUpdateBenchmarkFixture : IDisposable {
    private readonly int _count, _updates;
    private readonly bool _secondary;
    private readonly VisualTheme _theme;
    private readonly string _font, _fontCss;
    private readonly HtmlInteractiveChartRenderer _renderer = new();
    private readonly List<string> _pageErrors = new();
    private HtmlBrowserSession? _session;
    private Chart? _source;
    private ChartSeries[] _series = Array.Empty<ChartSeries>();
    private ChartObservationChange<ChartPoint>[][][] _plans = Array.Empty<ChartObservationChange<ChartPoint>[][]>();
    private HtmlChartSection? _first, _last;
    private byte[]? _firstBrowserPng;
    private string? _firstBrowserSvg, _lastBrowserSvg;
    private int _committed;
    private bool _validated;

    /// <summary>Captures deterministic inputs and policy without opening a browser or preparing a chart.</summary>
    public SustainedUpdateBenchmarkFixture(int count, int updates, bool secondary, string tokenPath, string fontPath, string boldFontPath) {
        if (count is not (32 or 1000 or 5000)) throw new ArgumentOutOfRangeException(nameof(count));
        if (updates < 1 || updates > 10) throw new ArgumentOutOfRangeException(nameof(updates));
        _count = count; _updates = updates; _secondary = secondary; _font = fontPath;
        var regular = File.ReadAllBytes(fontPath); var bold = File.ReadAllBytes(boldFontPath); var tokens = File.ReadAllText(tokenPath);
        FontRegistry.Register("CFX Update Proof Carlito", fontPath, 400);
        FontRegistry.Register("CFX Update Proof Carlito", boldFontPath, 700);
        _theme = VisualTheme.FromPaletteJson(tokens, new VisualTypography("CFX Update Proof Carlito", 22, 13, 11, 12, 11));
        _fontCss = "@font-face{font-family:'CFX Update Proof Carlito';font-weight:400;src:url(data:font/ttf;base64,"
            + Convert.ToBase64String(regular) + ")}@font-face{font-family:'CFX Update Proof Carlito';font-weight:700;src:url(data:font/ttf;base64,"
            + Convert.ToBase64String(bold) + ")}";
        var source = new StringBuilder("sustained-update-v1|900x500|390x440|640|light|primary-0:60|secondary-0:500|one-raf|")
            .Append(count).Append('|').Append(updates).Append('|').Append(secondary).Append('|').Append(Hash(tokens)).Append('|')
            .Append(Hash(regular)).Append('|').Append(Hash(bold));
        for (var series = 0; series < (secondary ? 2 : 1); series++) for (var index = 0; index < count; index++) {
            var point = Point(index, series);
            source.Append('|').Append(Key(series)).Append(':').Append(Id(index).Value).Append(':')
                .Append(BitConverter.DoubleToInt64Bits(point.X)).Append(':').Append(BitConverter.DoubleToInt64Bits(point.Y));
        }
        SourceDigest = Hash(source.ToString());
    }

    /// <summary>Gets the exact initial data, batch, font, theme and pose contract for equivalent-source comparisons.</summary>
    public string SourceDigest { get; }
    /// <summary>Gets the verified count across all retained series, not merely one series.</summary>
    public int SourceCount => _count * (_secondary ? 2 : 1);
    /// <summary>Gets the number of commits observed during the most recent complete measured operation.</summary>
    public int CommittedUpdates => _validated ? _committed : throw new InvalidOperationException("The update batch has not been validated.");
    /// <summary>Gets the final observation-index checksum across all series after full fact validation.</summary>
    public int SourceIndexSum => _validated ? Enumerable.Range(_updates, _count).Sum() * (_secondary ? 2 : 1) : 0;
    /// <summary>Gets the actual browser version, without starting an uninitialized session.</summary>
    public string BrowserVersion => _session?.Browser?.Version ?? "Not started";
    /// <summary>Gets the final asset-free HTML payload size outside measurement.</summary>
    public int HtmlBytes => Encoding.UTF8.GetByteCount(Result.Html);
    private HtmlChartSection Result => _validated && _last != null ? _last : throw new InvalidOperationException("No validated update result.");

    /// <summary>Recreates source/input plans and installs the untimed starting scene; the browser remains owned by HtmlTinkerX.</summary>
    public void Reset() {
        _last = null; _lastBrowserSvg = null; _committed = 0; _validated = false;
        var source = Chart.Create().WithSize(900, 500).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithPadding(24, 24, 24, 24).WithTheme(_theme, VisualThemeMode.Light).WithYAxisBounds(0, 60);
        _source = source; source.Options.PngFontPath = _font;
        _series = Enumerable.Range(0, _secondary ? 2 : 1).Select(series => {
            var data = ChartSeries.CreateLine(new ChartSeriesId(Key(series)), series == 0 ? "Measurements" : "Companion",
                Enumerable.Range(0, _count).Select(index => Point(index, series)))
                .WithObservationIds(Enumerable.Range(0, _count).Select(Id)).WithPointColor(_count / 2, "#7c3aed");
            data.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = _count == 32 ? 3 : 2; });
            if (series > 0) data.UseSecondaryValueAxis();
            source.AddSeries(data); return data;
        }).ToArray();
        if (_secondary) source.WithSecondaryYAxisBounds(0, 500);
        _plans = Enumerable.Range(0, _updates).Select(step => Enumerable.Range(0, _series.Length).Select(series => new[] {
            ChartObservationChange<ChartPoint>.Remove(Id(step)),
            ChartObservationChange<ChartPoint>.Replace(Id(_count / 2), Point(_count / 2, series, step + 1)),
            ChartObservationChange<ChartPoint>.Append(Id(_count + step), Point(_count + step, series))
        }).ToArray()).ToArray();
        var initial = Prepare();
        if (_session == null) {
            var opened = HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
                Browser = HtmlBrowserEngine.Chromium, Headless = true, Timeout = 30000, ViewportWidth = 980, ViewportHeight = 760
            }).GetAwaiter().GetResult();
            _session = opened; opened.Page.PageError += (_, error) => _pageErrors.Add(error);
            var page = "<!doctype html><html><head><style>" + _fontCss + HtmlInteractiveChartRenderer.BuildFragmentStyle()
                + "</style></head><body style='margin:12px'>" + initial.Html + "<script>"
                + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
            try {
                opened.Page.SetContentAsync(page).GetAwaiter().GetResult();
                opened.Page.EvaluateAsync("""
                    async()=>{await document.fonts.ready;window.updateBenchmark={controller:ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart')),updates:0};
                        updateBenchmark.controller.root.addEventListener('cfxupdated',()=>updateBenchmark.updates++);}
                    """).GetAwaiter().GetResult();
            } catch { Dispose(); throw; }
        } else {
            _session.Page.EvaluateAsync("html=>updateBenchmark.controller.update(html)", initial.Html).GetAwaiter().GetResult();
        }
        var session = _session ?? throw new InvalidOperationException("The HtmlTinkerX session was not initialized.");
        session.Page.EvaluateAsync("""
            id=>{const f=updateBenchmark;f.updates=0;f.controller.select({observationIds:[id],seriesKey:'primary'});}
            """, Id(_count / 2).Value).GetAwaiter().GetResult();
        if (_first == null) {
            _first = initial; _firstBrowserSvg = DownloadSvg(); _firstBrowserPng = DownloadPng();
            ValidateExport(_firstBrowserSvg, updates: 0); ValidatePng(_firstBrowserPng, 900, 500, browser: true);
        }
        session.Page.EvaluateAsync("id=>updateBenchmark.controller.root.querySelector('[data-cfx-series-key=primary][data-cfx-observation-id=\"'+id+'\"]').focus()",
            Id(_count / 2).Value).GetAwaiter().GetResult();
    }

    /// <summary>Runs the declared transaction/preparation/DOM-commit batch inside PowerForge's single elapsed measurement.</summary>
    public void Execute() {
        if (_source == null || _session == null || _last != null) throw new InvalidOperationException("Reset the benchmark before executing.");
        foreach (var plan in _plans) {
            for (var series = 0; series < _series.Length; series++)
                _series[series].ApplyPointChanges(plan[series], maximumObservations: _count);
            _last = Prepare();
            _committed = _session.Page.EvaluateAsync<int>("""
                async html=>{
                    const f=updateBenchmark,before=f.controller.sceneRevision,updates=f.updates;
                    f.controller.update(html);
                    if(f.updates!==updates+1||f.controller.sceneRevision===before)throw new Error('The public update did not commit once.');
                    await new Promise(resolve=>requestAnimationFrame(resolve));
                    if(!f.controller.root.isConnected||ChartForgeXCharts.get(f.controller.root)!==f.controller)throw new Error('The committed controller was replaced or detached.');
                    return f.updates;
                }
                """, _last.Html).GetAwaiter().GetResult();
        }
    }

    private HtmlChartSection Prepare() => _renderer.PrepareSection(_source!, configure: options => {
        options.IdScope = "update-proof"; options.Interaction.ChartId = "update-proof";
        options.Interaction.Features = ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Export;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Adaptive.NarrowWidth = 390; options.Adaptive.NarrowHeight = 440; options.Adaptive.Breakpoint = 640;
    });
    private static ChartObservationId Id(int index) => new("row-" + index.ToString(CultureInfo.InvariantCulture));
    private static string Key(int series) => series == 0 ? "primary" : "companion";
    private static ChartPoint Point(int index, int series, int replacement = 0) => new(index,
        series == 0 ? 20 + Math.Sin(index * .19) * 10 + index % 7 + replacement * .25
            : 100 + Math.Sin(index * .13) * 30 + index % 31 * 8 + replacement * 2.5);
    private static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));

    /// <summary>Disposes only this fixture's HtmlTinkerX session; it does not stop shared build/browser processes.</summary>
    public void Dispose() { _session?.DisposeAsync().AsTask().GetAwaiter().GetResult(); _session = null; }
}
