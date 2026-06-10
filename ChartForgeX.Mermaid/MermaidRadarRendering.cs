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
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithLegend(document.ShowLegend)
            .WithXLabels(AxisLabels(document));
        if (document.Ticks.HasValue) chart.WithTickCount(document.Ticks.Value);
        if (document.Minimum.HasValue || document.Maximum.HasValue) chart.WithYAxisBounds(document.Minimum ?? 0, document.Maximum ?? ResolveMaximum(document));
        foreach (var curve in document.Curves) chart.AddRadar(curve.Label, Points(document, curve));
        return chart;
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
        return artifact;
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
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid radar";
    }

    private static string ResolveSubtitle(MermaidRadarDocument document, MermaidRadarRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static string[] AxisLabels(MermaidRadarDocument document) {
        var labels = new string[document.Axes.Count];
        for (var i = 0; i < labels.Length; i++) labels[i] = document.Axes[i].Label;
        return labels;
    }

    private static ChartPoint[] Points(MermaidRadarDocument document, MermaidRadarCurve curve) {
        var points = new ChartPoint[document.Axes.Count];
        for (var i = 0; i < points.Length; i++) {
            var value = curve.ValuesByAxisId.Count > 0
                ? curve.ValuesByAxisId.TryGetValue(document.Axes[i].Id, out var keyed) ? keyed : 0
                : curve.OrderedValues[i];
            points[i] = new ChartPoint(i + 1, value);
        }

        return points;
    }

    private static double ResolveMaximum(MermaidRadarDocument document) {
        var max = 0.0;
        foreach (var curve in document.Curves) {
            foreach (var value in curve.OrderedValues) max = Math.Max(max, value);
            foreach (var value in curve.ValuesByAxisId.Values) max = Math.Max(max, value);
        }

        return max <= (document.Minimum ?? 0) ? (document.Minimum ?? 0) + 1 : max;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid radar diagrams.
/// </summary>
public sealed class MermaidRadarRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 760;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 620;
}
