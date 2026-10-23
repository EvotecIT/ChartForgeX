using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>Measures an actual adaptive section call through the canonical Cartesian viewport API.</summary>
public class SectionPreparationBenchmarkFixture {
    private readonly ChartPoint[] _points;
    private readonly ChartCartesianViewport? _xViewport;
    private readonly double? _authoredXMinimum, _authoredXMaximum, _authoredYMinimum, _authoredYMaximum;
    private HtmlChartSection? _first, _last;
    private string? _expectedDigest;
    private bool _validated;

    /// <summary>Creates the shared full/X fixture without preparing or exporting a scene.</summary>
    public SectionPreparationBenchmarkFixture(string caseName, string tokenPath, string fontPath, string boldFontPath)
        : this(caseName, tokenPath, fontPath, boldFontPath, cartesian: false) { }

    /// <summary>Initializes deterministic source data and explicit measurement policy outside timing.</summary>
    protected SectionPreparationBenchmarkFixture(string caseName, string tokenPath, string fontPath, string boldFontPath, bool cartesian) {
        var parts = caseName.Split('-');
        if (parts.Length != 3 || parts[0] is not ("small" or "large") || parts[1] is not ("explicit" or "auto")
            || parts[2] is not ("full" or "x" or "y" or "xy") || (!cartesian && parts[2] is ("y" or "xy")))
            throw new ArgumentException("Unknown section-preparation case.", nameof(caseName));
        CaseName = caseName; Window = parts[2];
        var count = parts[0] == "small" ? 3 : 1000;
        var radius = count == 3 ? 18 : 4;
        _points = Enumerable.Range(0, count).Select(index => new ChartPoint(12d * index / (count - 1), 10 + 20d * index / (count - 1))).ToArray();
        FontRegistry.Register("CFX Section Proof Carlito", fontPath, 400);
        FontRegistry.Register("CFX Section Proof Carlito", boldFontPath, 700);
        var tokens = File.ReadAllText(tokenPath);
        var theme = VisualTheme.FromPaletteJson(tokens, new VisualTypography("CFX Section Proof Carlito", 22, 13, 11, 12, 11));
        var series = ChartSeries.CreateScatter(new ChartSeriesId("samples"), "Samples", _points)
            .WithObservationIds(Enumerable.Range(0, count).Select(index => new ChartObservationId("row-" + index.ToString(CultureInfo.InvariantCulture))));
        series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = radius; });
        SourceChart = Chart.Create().WithSize(800, 440).WithPadding(24, 24, 24, 24).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithTheme(theme, VisualThemeMode.Light).AddSeries(series);
        SourceChart.Options.PngFontPath = fontPath;
        if (parts[1] == "explicit") SourceChart.WithXAxisBounds(0, 12).WithYAxisBounds(0, 40);
        _authoredXMinimum = SourceChart.Options.XAxis.Minimum; _authoredXMaximum = SourceChart.Options.XAxis.Maximum;
        _authoredYMinimum = SourceChart.Options.YAxis.Minimum; _authoredYMaximum = SourceChart.Options.YAxis.Maximum;
        _xViewport = Window == "x" ? new ChartCartesianViewport(x: new ChartAxisWindow(2, 10)) : null;
        var source = new StringBuilder("section-preparation-v1|800x440|390x440|640|24|light|samples|")
            .Append(caseName).Append('|').Append(radius).Append('|').Append(Hash(tokens)).Append('|')
            .Append(Hash(File.ReadAllBytes(fontPath))).Append('|').Append(Hash(File.ReadAllBytes(boldFontPath))).Append('|')
            .Append(_authoredXMinimum?.ToString("R", CultureInfo.InvariantCulture) ?? "auto").Append(':')
            .Append(_authoredXMaximum?.ToString("R", CultureInfo.InvariantCulture) ?? "auto").Append(':')
            .Append(_authoredYMinimum?.ToString("R", CultureInfo.InvariantCulture) ?? "auto").Append(':')
            .Append(_authoredYMaximum?.ToString("R", CultureInfo.InvariantCulture) ?? "auto");
        foreach (var point in SourceChart.Series[0].Points.Select((point, index) => new { point, index })) {
            source.Append('|').Append(SourceChart.Series[0].ObservationIds[point.index].Value).Append(':')
                .Append(BitConverter.DoubleToInt64Bits(point.point.X)).Append(':').Append(BitConverter.DoubleToInt64Bits(point.point.Y));
        }
        SourceDigest = Hash(source.ToString());
    }

    /// <summary>Gets the actual source and policy digest for baseline/current pairing.</summary>
    public string SourceDigest { get; }
    /// <summary>Gets the declared deterministic case.</summary>
    public string CaseName { get; }
    /// <summary>Gets the requested window kind.</summary>
    protected string Window { get; }
    /// <summary>Gets the retained source model, prepared without mutation by each operation.</summary>
    protected Chart SourceChart { get; }
    /// <summary>Gets the stateless public HTML renderer.</summary>
    protected HtmlInteractiveChartRenderer Renderer { get; } = new();

    /// <summary>Qualifies one untimed output once and clears only the result before a sample.</summary>
    public void Reset() {
        if (_expectedDigest == null) {
            _first = PrepareSection(); ValidateSection(_first); _expectedDigest = Hash(_first.Html);
        }
        _last = null; _validated = false;
    }

    /// <summary>Performs exactly one public PrepareSection call inside PowerForge timing.</summary>
    public void Execute() { _last = PrepareSection(); }

    /// <summary>Checks complete source facts, native output and determinism outside timing.</summary>
    public void Validate() {
        if (_last == null || _expectedDigest == null) throw new InvalidOperationException("Section preparation did not run.");
        ValidateSection(_last);
        if (Hash(_last.Html) != _expectedDigest) throw new InvalidOperationException("Section output changed within a fixed lane.");
        _validated = true;
    }

    /// <summary>Gets the measured section's UTF-8 payload length after validation.</summary>
    public int HtmlBytes => Encoding.UTF8.GetByteCount(Result.Html);
    /// <summary>Gets the complete retained source count.</summary>
    public int SourceCount => _points.Length;
    /// <summary>Gets the number of exported SVG elements for the wide native drawing, outside timing.</summary>
    public int WideSvgElementCount => XDocument.Parse(Result.PreparedVisual.ToSvg()).Descendants().Count();
    /// <summary>Gets the number of exported SVG elements for the narrow native drawing, outside timing.</summary>
    public int NarrowSvgElementCount => XDocument.Parse(Result.NarrowPreparedVisual!.ToSvg()).Descendants().Count();
    private HtmlChartSection Result => _validated && _last != null ? _last : throw new InvalidOperationException("No validated section result.");

    /// <summary>Writes the first and final validated outputs after the benchmark has finished, outside timing.</summary>
    public void WriteReceipt(string directory) {
        if (_first == null) return;
        Directory.CreateDirectory(directory);
        Capture(_first, directory, "first");
        var identical = _validated && _last != null && _first.Html == _last.Html;
        if (_validated && _last != null && !identical) Capture(_last, directory, "last");
        File.WriteAllText(Path.Combine(directory, "receipt.json"), JsonSerializer.Serialize(new {
            CaseName, SourceDigest, SourceCount, validated = _validated,
            first = Receipt(_first), last = _validated && _last != null ? Receipt(_last) : null,
            lastArtifacts = identical ? "first (byte-identical native section)" : "last"
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Calls the shared full-domain or X-window public API.</summary>
    protected virtual HtmlChartSection PrepareSection() => _xViewport == null
        ? Renderer.PrepareSection(SourceChart, configure: ConfigureAdaptive)
        : Renderer.PrepareSection(SourceChart, _xViewport, ConfigureAdaptive);

    /// <summary>Applies only options present in both measured API versions.</summary>
    protected static void ConfigureAdaptive(HtmlChartInteractionOptions options) {
        options.IdScope = "section-proof"; options.Interaction.ChartId = "section-proof";
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Export;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Adaptive.NarrowWidth = 390; options.Adaptive.NarrowHeight = 440; options.Adaptive.Breakpoint = 640;
    }

    /// <summary>Validates shared public contracts while permitting historical automatic domain differences.</summary>
    protected virtual void ValidateSection(HtmlChartSection section) {
        var narrow = section.NarrowPreparedVisual ?? throw new InvalidOperationException("Adaptive preparation omitted the narrow scene.");
        if (section.PreparedVisual.Size.Width != 800 || narrow.Size.Width != 390
            || section.PreparedVisual.Size.Height != 440 || narrow.Size.Height != 440
            || !section.Html.Contains("data-cfx-adaptive-narrow=\"true\"", StringComparison.Ordinal)
            || section.Html.Contains("<script", StringComparison.OrdinalIgnoreCase) || section.Html.Contains("<style", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Adaptive native dimensions or the asset-free section boundary changed.");
        foreach (var prepared in new[] { section.PreparedVisual, narrow }) {
            var svg = XDocument.Parse(prepared.ToSvg());
            if (!svg.Descendants().Any(node => node.Name.LocalName == "ellipse" && (string?)node.Attribute("data-cfx-role") == "marker"))
                throw new InvalidOperationException("The native section contains no painted source marker.");
            var projection = prepared.XProjection ?? throw new InvalidOperationException("The native X projection is missing.");
            if ((Window is "x" or "xy") && (projection.Minimum != 2 || projection.Maximum != 10))
                throw new InvalidOperationException("The requested X window changed.");
            var points = svg.Descendants().Where(node =>
                (string?)node.Attribute("data-cfx-role") == "point" && (string?)node.Attribute("data-cfx-series-key") == "samples").ToArray();
            if (points.Length != _points.Length) throw new InvalidOperationException("Native preparation lost source observations.");
            for (var index = 0; index < points.Length; index++) {
                if ((string?)points[index].Attribute("data-cfx-observation-id") != "row-" + index.ToString(CultureInfo.InvariantCulture)
                    || double.Parse(points[index].Attribute("data-cfx-x")!.Value, CultureInfo.InvariantCulture) != _points[index].X
                    || double.Parse(points[index].Attribute("data-cfx-y")!.Value, CultureInfo.InvariantCulture) != _points[index].Y)
                    throw new InvalidOperationException("A native section changed source identity or coordinates.");
            }
        }
        if (SourceChart.Options.XAxis.Minimum != _authoredXMinimum || SourceChart.Options.XAxis.Maximum != _authoredXMaximum
            || SourceChart.Options.YAxis.Minimum != _authoredYMinimum || SourceChart.Options.YAxis.Maximum != _authoredYMaximum)
            throw new InvalidOperationException("Section preparation mutated authored domain policy.");
    }

    /// <summary>Builds an untimed receipt of actual projections; current fixtures append their Y contract separately.</summary>
    protected virtual object Receipt(HtmlChartSection section) => new {
        htmlSha256 = Hash(section.Html), htmlBytes = Encoding.UTF8.GetByteCount(section.Html),
        wide = Projection(section.PreparedVisual), narrow = Projection(section.NarrowPreparedVisual!)
    };

    private static object Projection(PreparedVisual prepared) => new {
        width = prepared.Size.Width, height = prepared.Size.Height, svgElements = XDocument.Parse(prepared.ToSvg()).Descendants().Count(),
        minimum = prepared.XProjection!.Minimum, maximum = prepared.XProjection.Maximum,
        fullMinimum = prepared.XProjection.FullMinimum, fullMaximum = prepared.XProjection.FullMaximum
    };
    private static void Capture(HtmlChartSection section, string directory, string prefix) {
        File.WriteAllText(Path.Combine(directory, prefix + ".html"), section.Html);
        foreach (var drawing in new[] { (Name: "wide", Visual: section.PreparedVisual), (Name: "narrow", Visual: section.NarrowPreparedVisual!) }) {
            File.WriteAllText(Path.Combine(directory, prefix + "-" + drawing.Name + ".svg"), drawing.Visual.ToSvg());
            var png = drawing.Visual.ToPng();
            var decoded = RasterImageDecoder.Decode(png);
            if (decoded.Width != drawing.Visual.Size.Width || decoded.Height != drawing.Visual.Size.Height)
                throw new InvalidOperationException("A native section PNG has different dimensions from its prepared scene.");
            File.WriteAllBytes(Path.Combine(directory, prefix + "-" + drawing.Name + ".png"), png);
        }
    }
    private static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
}
