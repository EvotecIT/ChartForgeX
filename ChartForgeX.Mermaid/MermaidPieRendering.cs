using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid pie charts.
/// </summary>
public static class MermaidPieRendering {
    /// <summary>
    /// Converts a Mermaid pie document into a renderer-independent ChartForgeX chart.
    /// </summary>
    public static Chart ToChart(this MermaidPieDocument document, MermaidPieRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidPieRenderOptions();
        var chart = Chart.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithPointLegend()
            .WithXLabels(GetLabels(document));
        if (document.ShowData) chart.WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndValue);
        else chart.WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent);
        chart.AddPie(string.IsNullOrWhiteSpace(options.SeriesName) ? "Slices" : options.SeriesName!, GetPoints(document));
        return chart;
    }

    /// <summary>
    /// Wraps a Mermaid pie document in a visual artifact envelope backed by a ChartForgeX chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidPieDocument document, MermaidPieRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidPieRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-pie" : options.Id!.Trim();
        var chart = document.ToChart(options);
        var artifact = chart.ToVisualArtifact(id, VisualArtifactKind.Mermaid, VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.showData"] = document.ShowData ? "true" : "false";
        artifact.Metadata["mermaid.slices"] = document.Slices.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(Chart);
        return artifact;
    }

    /// <summary>
    /// Renders a Mermaid pie document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidPieDocument document, MermaidPieRenderOptions? options = null) =>
        document.ToChart(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid pie document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidPieDocument document, MermaidPieRenderOptions? options = null) =>
        document.ToChart(options).ToPng();

    private static string ResolveTitle(MermaidPieDocument document, MermaidPieRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid pie";
    }

    private static string ResolveSubtitle(MermaidPieDocument document, MermaidPieRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static string[] GetLabels(MermaidPieDocument document) {
        var labels = new string[document.Slices.Count];
        for (var i = 0; i < labels.Length; i++) labels[i] = document.Slices[i].Label;
        return labels;
    }

    private static ChartPoint[] GetPoints(MermaidPieDocument document) {
        var points = new ChartPoint[document.Slices.Count];
        for (var i = 0; i < points.Length; i++) points[i] = new ChartPoint(i + 1, document.Slices[i].Value);
        return points;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid pie charts.
/// </summary>
public sealed class MermaidPieRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the chart series name.</summary>
    public string? SeriesName { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 820;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 460;
}
