using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid radar diagrams.
/// </summary>
public static class MermaidRadarRendering {
    /// <summary>
    /// Converts a Mermaid radar document into a renderer-independent ChartForgeX chart.
    /// </summary>
    public static Chart ToChart(this MermaidRadarDocument document, MermaidRadarRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidRadarRenderOptions();
        var chart = Chart.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithSize(options.Width ?? document.Configuration.Radar?.Width ?? 760, options.Height ?? document.Configuration.Radar?.Height ?? 620)
            .WithLegend(document.ShowLegend)
            .WithXLabels(AxisLabels(document))
            .WithPolarGridShape(GraticuleShape(document));
        chart.Options.PolarGridRingCount = Math.Min(document.Ticks ?? 5, 32);
        (document.Configuration.Radar ?? new MermaidRadarSettings()).Apply(chart, options);
        chart.WithYAxisBounds(document.Minimum ?? 0, document.Maximum ?? ResolveMaximum(document));
        var tension = options.CurveTension ?? (string.Equals(document.Graticule, "polygon", StringComparison.OrdinalIgnoreCase)
            ? 0 : document.Configuration.Radar?.CurveTension ?? .17);
        foreach (var curve in document.Curves) {
            chart.AddRadar(curve.Label, Points(document, curve));
            chart.Series[chart.Series.Count - 1].Radar.CurveTension = tension;
        }
        return MermaidPresentation.Apply(chart, document);
    }

    /// <summary>
    /// Wraps a Mermaid radar document in a visual artifact envelope backed by a ChartForgeX chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidRadarDocument document, MermaidRadarRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidRadarRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-radar" : options.Id!.Trim();
        var chart = document.ToChart(options);
        var artifact = chart.ToVisualArtifact(id, VisualArtifactKind.Mermaid, VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.axes"] = document.Axes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.curves"] = document.Curves.Count.ToString(CultureInfo.InvariantCulture);
        if (document.Minimum.HasValue) artifact.Metadata["mermaid.min"] = document.Minimum.Value.ToString(CultureInfo.InvariantCulture);
        if (document.Maximum.HasValue) artifact.Metadata["mermaid.max"] = document.Maximum.Value.ToString(CultureInfo.InvariantCulture);
        if (document.Ticks.HasValue) artifact.Metadata["mermaid.ticks"] = document.Ticks.Value.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(document.Graticule)) artifact.Metadata["mermaid.graticule"] = document.Graticule!;
        artifact.Metadata["render.model"] = nameof(Chart);
        return MermaidPresentation.Apply(artifact, document);
    }

    /// <summary>
    /// Renders a Mermaid radar document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidRadarDocument document, MermaidRadarRenderOptions? options = null) =>
        document.ToChart(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid radar document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidRadarDocument document, MermaidRadarRenderOptions? options = null) =>
        document.ToChart(options).ToPng();

    private static string ResolveTitle(MermaidRadarDocument document, MermaidRadarRenderOptions options) {
        return MermaidHeading.Title(document, options.Title, document.Title, "Mermaid radar");
    }

    private static string[] AxisLabels(MermaidRadarDocument document) {
        var labels = new string[document.Axes.Count];
        for (var i = 0; i < labels.Length; i++) labels[i] = document.Axes[i].Label;
        return labels;
    }

    private static ChartPoint[] Points(MermaidRadarDocument document, MermaidRadarCurve curve) {
        var points = new ChartPoint[document.Axes.Count];
        for (var i = 0; i < points.Length; i++) {
            points[i] = new ChartPoint(i + 1, CurveValue(document, curve, i));
        }

        return points;
    }

    private static double ResolveMaximum(MermaidRadarDocument document) {
        var max = double.NegativeInfinity;
        foreach (var curve in document.Curves) {
            for (var i = 0; i < document.Axes.Count; i++) max = Math.Max(max, CurveValue(document, curve, i));
        }

        return max <= (document.Minimum ?? 0) ? (document.Minimum ?? 0) + 1 : max;
    }

    private static double CurveValue(MermaidRadarDocument document, MermaidRadarCurve curve, int axisIndex) =>
        curve.ValuesByAxisId.Count > 0
            ? curve.ValuesByAxisId.TryGetValue(document.Axes[axisIndex].Id, out var value) ? value : 0
            : curve.OrderedValues[axisIndex];

    private static ChartPolarGridShape GraticuleShape(MermaidRadarDocument document) {
        if (string.IsNullOrWhiteSpace(document.Graticule) || document.Graticule == "circle") return ChartPolarGridShape.Circle;
        if (document.Graticule == "polygon") return ChartPolarGridShape.Polygon;
        throw new ArgumentException("Radar graticule must be circle or polygon.", nameof(document));
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid radar diagrams.
/// </summary>
public sealed class MermaidRadarRenderOptions {
    private double? _curveTension;
    private double? _axisScaleFactor;
    private double? _axisLabelFactor;

    /// <summary>Gets or sets the host spoke-radius factor. Null uses source configuration or one. Finite nonnegative values override the source without changing observations or value bounds.</summary>
    public double? AxisScaleFactor {
        get => _axisScaleFactor;
        set { ValidateFactor(value); _axisScaleFactor = value; }
    }

    /// <summary>Gets or sets the host category-label radius factor. Null uses source configuration or 1.05. Finite nonnegative values override the source; the native theme still supplies the label gap.</summary>
    public double? AxisLabelFactor {
        get => _axisLabelFactor;
        set { ValidateFactor(value); _axisLabelFactor = value; }
    }

    /// <summary>Gets or sets host frame padding. Null applies authored source margins over the native frame defaults; an explicit padding overrides all four source margins.</summary>
    public ChartPadding? Padding { get; set; }

    private static void ValidateFactor(double? value) {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Gets or sets host curve tension from zero through one. Null uses source configuration with circular graticules, or straight edges with polygonal graticules. An explicit value overrides either source policy.</summary>
    public double? CurveTension {
        get => _curveTension;
        set {
            if (value.HasValue && (double.IsNaN(value.Value) || value.Value < 0 || value.Value > 1)) throw new ArgumentOutOfRangeException(nameof(value));
            _curveTension = value;
        }
    }

    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the host preview width. Null uses authored source width, then 760 pixels.</summary>
    public int? Width { get; set; }

    /// <summary>Gets or sets the host preview height. Null uses authored source height, then 620 pixels.</summary>
    public int? Height { get; set; }
}
