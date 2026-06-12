using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid quadrant charts.
/// </summary>
public static class MermaidQuadrantRendering {
    /// <summary>
    /// Converts a Mermaid quadrant document into a renderer-independent ChartForgeX scatter chart.
    /// </summary>
    public static Chart ToChart(this MermaidQuadrantDocument document, MermaidQuadrantRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidQuadrantRenderOptions();
        var points = document.Points.Select(point => new ChartPoint(point.X, point.Y)).ToArray();
        var chart = Chart.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithXAxis(FormatAxis(document.XAxisStart, document.XAxisEnd, "X"))
            .WithYAxis(FormatAxis(document.YAxisStart, document.YAxisEnd, "Y"))
            .WithXAxisBounds(0, 1)
            .WithYAxisBounds(0, 1)
            .WithDataLabels()
            .AddScatter(options.SeriesName, points);

        var series = chart.Series[0];
        series.SemanticRole = "mermaid-quadrant-point";
        for (var i = 0; i < document.Points.Count; i++) series.WithPointLabel(i, document.Points[i].Label);
        return chart;
    }

    /// <summary>
    /// Wraps a Mermaid quadrant document in a visual artifact envelope backed by a ChartForgeX scatter chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidQuadrantDocument document, MermaidQuadrantRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidQuadrantRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-quadrant" : options.Id!.Trim();
        var chart = document.ToChart(options);
        var artifact = chart.ToVisualArtifact(id, VisualArtifactKind.Mermaid, VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.points"] = document.Points.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.quadrants"] = document.QuadrantLabels.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(Chart);
        return artifact;
    }

    /// <summary>
    /// Renders a Mermaid quadrant document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidQuadrantDocument document, MermaidQuadrantRenderOptions? options = null) =>
        document.ToChart(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid quadrant document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidQuadrantDocument document, MermaidQuadrantRenderOptions? options = null) =>
        document.ToChart(options).ToPng();

    private static string ResolveTitle(MermaidQuadrantDocument document, MermaidQuadrantRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid quadrant";
    }

    private static string ResolveSubtitle(MermaidQuadrantDocument document, MermaidQuadrantRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        if (document.QuadrantLabels.Count == 0) return document.Header;
        var labels = document.QuadrantLabels
            .OrderBy(item => item.Key)
            .Select(item => "Q" + item.Key.ToString(CultureInfo.InvariantCulture) + ": " + item.Value);
        return document.Header + " | " + string.Join(" | ", labels);
    }

    private static string FormatAxis(string? start, string? end, string fallback) {
        if (!string.IsNullOrWhiteSpace(start) && !string.IsNullOrWhiteSpace(end)) return start + " -> " + end;
        if (!string.IsNullOrWhiteSpace(start)) return start!;
        if (!string.IsNullOrWhiteSpace(end)) return end!;
        return fallback;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid quadrant charts.
/// </summary>
public sealed class MermaidQuadrantRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the ChartForgeX series name.</summary>
    public string SeriesName { get; set; } = "Quadrant point";

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 560;
}
