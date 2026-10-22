using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using HtmlTinkerX;
using Microsoft.Playwright;

public sealed partial class SustainedUpdateBenchmarkFixture {
    private readonly List<object> _samples = new();
    private int _browserScale = 1;

    /// <summary>Validates every retained source/native/browser fact and actual SVG download outside PowerForge timing.</summary>
    public void Validate() {
        if (_source == null || _session == null || _last?.NarrowPreparedVisual == null || _committed != _updates)
            throw new InvalidOperationException("The complete declared source/preparation/browser commit batch did not run.");
        if (_source.Series.Count != _series.Length || _source.Options.XAxis.Minimum.HasValue || _source.Options.XAxis.Maximum.HasValue
            || _source.Options.YAxis.Minimum != 0 || _source.Options.YAxis.Maximum != 60
            || _secondary && (_source.Options.SecondaryYAxis.Minimum != 0 || _source.Options.SecondaryYAxis.Maximum != 500))
            throw new InvalidOperationException("The rolling update changed its source series or authored axis policy.");
        for (var series = 0; series < _series.Length; series++) {
            if (!ReferenceEquals(_source.Series[series], _series[series]) || _series[series].Points.Count != _count
                || _series[series].ObservationIds.Count != _count)
                throw new InvalidOperationException("The source series handle or bounded observation collection changed.");
            for (var ordinal = 0; ordinal < _count; ordinal++) {
                var index = ordinal + _updates; var expected = Expected(index, series, _updates);
                if (_series[series].ObservationIds[ordinal] != Id(index)
                    || _series[series].Points[ordinal].X != expected.X || _series[series].Points[ordinal].Y != expected.Y)
                    throw new InvalidOperationException("A rolling keyed transaction lost, reordered or changed a source fact.");
            }
            if (_series[series].PointColors[_count / 2 - _updates] != ChartColor.FromHex("#7c3aed"))
                throw new InvalidOperationException("The surviving observation lost its authored paint during rolling updates.");
        }
        ValidateExport(_last.PreparedVisual.ToSvg(), _updates); ValidateExport(_last.NarrowPreparedVisual.ToSvg(), _updates);
        var browser = _session.Page.EvaluateAsync<JsonElement>("""
            ()=>{const f=updateBenchmark,root=f.controller.root;
                return {updates:f.updates,roots:document.querySelectorAll('.cfx-interactive-chart').length,connected:root.isConnected,
                    current:ChartForgeXCharts.get(root)===f.controller,drawing:root.dataset.cfxAdaptiveDrawing,
                    selected:f.controller.getSelection().map(target=>({seriesKey:target.seriesKey,id:target.observationId})),
                    focused:document.activeElement?.dataset?.cfxObservationId??null,
                    points:[...root.querySelectorAll('.cfx-stage svg g[data-cfx-role=point][data-cfx-observation-id]')]
                        .map(node=>({key:node.dataset.cfxSeriesKey,id:node.dataset.cfxObservationId,x:Number(node.dataset.cfxX),y:Number(node.dataset.cfxY)}))};}
            """).GetAwaiter().GetResult();
        if (browser.GetProperty("updates").GetInt32() != _updates || browser.GetProperty("roots").GetInt32() != 1
            || !browser.GetProperty("connected").GetBoolean() || !browser.GetProperty("current").GetBoolean()
            || browser.GetProperty("drawing").GetString() != "wide" || browser.GetProperty("focused").GetString() != Id(_count / 2).Value)
            throw new InvalidOperationException("The final browser scene was not the one connected committed drawing, or surviving keyboard focus was lost.");
        var selected = browser.GetProperty("selected").EnumerateArray().ToArray();
        if (selected.Length != 1 || selected[0].GetProperty("seriesKey").GetString() != "primary"
            || selected[0].GetProperty("id").GetString() != Id(_count / 2).Value)
            throw new InvalidOperationException("The surviving source observation lost its public selection identity.");
        var facts = browser.GetProperty("points").EnumerateArray().ToArray();
        if (facts.Length != SourceCount) throw new InvalidOperationException("The committed browser scene has a different observation count.");
        for (var series = 0; series < _series.Length; series++) {
            var actual = facts.Where(point => point.GetProperty("key").GetString() == Key(series)).ToArray();
            if (actual.Length != _count) throw new InvalidOperationException("A committed browser series lost observations.");
            for (var ordinal = 0; ordinal < _count; ordinal++) {
                var index = ordinal + _updates; var expected = Expected(index, series, _updates);
                if (actual[ordinal].GetProperty("id").GetString() != Id(index).Value
                    || actual[ordinal].GetProperty("x").GetDouble() != expected.X || actual[ordinal].GetProperty("y").GetDouble() != expected.Y)
                    throw new InvalidOperationException("The committed browser scene differs from the independent complete source oracle.");
            }
        }
        _lastBrowserSvg = DownloadSvg(); ValidateExport(_lastBrowserSvg, _updates);
        _browserScale = _session.Page.EvaluateAsync<int>("()=>Math.max(1,Math.ceil(devicePixelRatio||1))").GetAwaiter().GetResult();
        if (_pageErrors.Count > 0 || _session.ConsoleLog.Any(message => message.Type == HtmlConsoleMessageType.Error))
            throw new InvalidOperationException("The browser produced a page or console error; retain its complete receipt.");
        _validated = true;
        _samples.Add(new { sample = _samples.Count + 1, commits = _committed, sourceCount = SourceCount,
            sourceIndexSum = SourceIndexSum, htmlSha256 = Hash(_last.Html), browserSvgSha256 = Hash(_lastBrowserSvg),
            retainedMinimum = _updates, retainedMaximum = _count + _updates - 1, oneConnectedRoot = true });
    }

    private void ValidateExport(string svgText, int updates) {
        var svg = XDocument.Parse(svgText);
        for (var series = 0; series < (_secondary ? 2 : 1); series++) {
            var points = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point"
                && (string?)node.Attribute("data-cfx-series-key") == Key(series)).ToArray();
            if (points.Length != _count) throw new InvalidOperationException("A native or downloaded SVG lost source observations.");
            for (var ordinal = 0; ordinal < _count; ordinal++) {
                var index = ordinal + updates; var expected = Expected(index, series, updates);
                if ((string?)points[ordinal].Attribute("data-cfx-observation-id") != Id(index).Value
                    || double.Parse(points[ordinal].Attribute("data-cfx-x")!.Value, CultureInfo.InvariantCulture) != expected.X
                    || double.Parse(points[ordinal].Attribute("data-cfx-y")!.Value, CultureInfo.InvariantCulture) != expected.Y)
                    throw new InvalidOperationException("A native or downloaded SVG changed an authored identity or coordinate.");
            }
        }
    }

    private ChartPoint Expected(int index, int series, int updates) => Point(index, series, index == _count / 2 ? updates : 0);
    private string DownloadSvg() {
        var session = _session ?? throw new InvalidOperationException("The HtmlTinkerX session is unavailable.");
        var download = session.Page.RunAndWaitForDownloadAsync(() => session.Page.Locator("[data-cfx-export=svg]").ClickAsync()).GetAwaiter().GetResult();
        var path = download.PathAsync().GetAwaiter().GetResult() ?? throw new InvalidOperationException("The actual browser SVG download is absent.");
        return File.ReadAllText(path);
    }
    private byte[] DownloadPng() {
        var session = _session ?? throw new InvalidOperationException("The HtmlTinkerX session is unavailable.");
        var download = session.Page.RunAndWaitForDownloadAsync(() => session.Page.Locator("[data-cfx-export=png]").ClickAsync()).GetAwaiter().GetResult();
        var path = download.PathAsync().GetAwaiter().GetResult() ?? throw new InvalidOperationException("The actual browser PNG download is absent.");
        return File.ReadAllBytes(path);
    }
    private void ValidatePng(byte[] png, int width, int height, bool browser) {
        if (browser) _browserScale = _session!.Page.EvaluateAsync<int>("()=>Math.max(1,Math.ceil(devicePixelRatio||1))").GetAwaiter().GetResult();
        var image = RasterImageDecoder.Decode(png); var scale = browser ? _browserScale : 1;
        if (image.Width != width * scale || image.Height != height * scale)
            throw new InvalidOperationException("A native or downloaded PNG has different dimensions from its actual committed drawing.");
    }

    /// <summary>Writes only first/final actual artifacts and every compact validation receipt after measurement.</summary>
    public void WriteReceipt(string directory) {
        if (_first == null || _session == null) return;
        Directory.CreateDirectory(directory);
        CaptureNative(_first, directory, "first");
        File.WriteAllText(Path.Combine(directory, "first.browser.svg"), _firstBrowserSvg!);
        File.WriteAllBytes(Path.Combine(directory, "first.browser.png"), _firstBrowserPng!);
        if (_last != null) {
            CaptureNative(_last, directory, "last");
            File.WriteAllText(Path.Combine(directory, "last.browser.svg"), _lastBrowserSvg ?? DownloadSvg());
            var png = DownloadPng(); ValidatePng(png, 900, 500, browser: true);
            File.WriteAllBytes(Path.Combine(directory, "last.browser.png"), png);
            _session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "last.page.png"), FullPage = true }).GetAwaiter().GetResult();
        }
        File.WriteAllText(Path.Combine(directory, "receipt.json"), JsonSerializer.Serialize(new {
            SourceDigest, pointsPerSeries = _count, series = _secondary ? 2 : 1, updatesPerOperation = _updates,
            validated = _validated, committed = _committed, SourceCount, BrowserVersion, browserScale = _browserScale,
            samples = _samples, pageErrors = _pageErrors, console = _session.ConsoleLog.Select(message => new { type = message.Type.ToString(), message.Text }),
            scope = "PowerForge times keyed changes, PrepareSection, public update round-trip and one requestAnimationFrame callback per commit. Source planning, browser setup, full verification, raster encoding and downloads are untimed. No managed-request, frame-p95 or total-memory claim."
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private void CaptureNative(ChartForgeX.Interactivity.Html.HtmlChartSection section, string directory, string prefix) {
        File.WriteAllText(Path.Combine(directory, prefix + ".section.html"), section.Html);
        foreach (var drawing in new[] { (Name: "wide", Visual: section.PreparedVisual), (Name: "narrow", Visual: section.NarrowPreparedVisual!) }) {
            File.WriteAllText(Path.Combine(directory, prefix + "." + drawing.Name + ".native.svg"), drawing.Visual.ToSvg());
            var png = drawing.Visual.ToPng(); ValidatePng(png, (int)drawing.Visual.Size.Width, (int)drawing.Visual.Size.Height, browser: false);
            File.WriteAllBytes(Path.Combine(directory, prefix + "." + drawing.Name + ".native.png"), png);
        }
    }
}
