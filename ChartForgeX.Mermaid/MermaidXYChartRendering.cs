using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid XY charts.
/// </summary>
public static class MermaidXYChartRendering {
    /// <summary>
    /// Converts a Mermaid XY chart document into a renderer-independent ChartForgeX chart.
    /// </summary>
    public static Chart ToChart(this MermaidXYChartDocument document, MermaidXYChartRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidXYChartRenderOptions();

        var chart = Chart.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height);

        if (!string.IsNullOrWhiteSpace(document.XAxis.Title)) chart.WithXAxis(document.XAxis.Title!);
        if (!string.IsNullOrWhiteSpace(document.YAxis.Title)) chart.WithYAxis(document.YAxis.Title!);
        if (document.XAxis.Labels.Count > 0) chart.WithXLabels(document.XAxis.Labels.ToArray());
        if (document.XAxis.HasRange) chart.WithXAxisBounds(document.XAxis.Minimum!.Value, document.XAxis.Maximum!.Value);
        if (document.YAxis.HasRange) chart.WithYAxisBounds(document.YAxis.Minimum!.Value, document.YAxis.Maximum!.Value);
        if (options.ShowDataLabels) chart.WithDataLabels();

        foreach (var series in document.Series) {
            var points = BuildPoints(document, series);
            if (series.Kind == MermaidXYChartSeriesKind.Line) chart.AddLine(series.Name, points);
            else if (document.Orientation == MermaidXYChartOrientation.Horizontal) chart.AddHorizontalBar(series.Name, points);
            else chart.AddBar(series.Name, points);
        }

        return chart;
    }

    /// <summary>
    /// Wraps a Mermaid XY chart document in a visual artifact envelope backed by a ChartForgeX chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidXYChartDocument document, MermaidXYChartRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidXYChartRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-xychart" : options.Id!.Trim();
        var chart = document.ToChart(options);
        var artifact = chart.ToVisualArtifact(id, VisualArtifactKind.Mermaid, VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.orientation"] = document.Orientation.ToString();
        artifact.Metadata["mermaid.series"] = document.Series.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.barSeries"] = CountSeries(document, MermaidXYChartSeriesKind.Bar).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.lineSeries"] = CountSeries(document, MermaidXYChartSeriesKind.Line).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.xAxisLabels"] = document.XAxis.Labels.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(Chart);
        return artifact;
    }

    /// <summary>
    /// Renders a Mermaid XY chart document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidXYChartDocument document, MermaidXYChartRenderOptions? options = null) =>
        document.ToChart(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid XY chart document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidXYChartDocument document, MermaidXYChartRenderOptions? options = null) =>
        document.ToChart(options).ToPng();

    private static string ResolveTitle(MermaidXYChartDocument document, MermaidXYChartRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid XY chart";
    }

    private static string ResolveSubtitle(MermaidXYChartDocument document, MermaidXYChartRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static ChartPoint[] BuildPoints(MermaidXYChartDocument document, MermaidXYChartSeries series) {
        var points = new ChartPoint[series.Values.Count];
        for (var i = 0; i < points.Length; i++) points[i] = new ChartPoint(ResolveX(document, i, points.Length), series.Values[i]);
        return points;
    }

    private static double ResolveX(MermaidXYChartDocument document, int index, int count) {
        if (!document.XAxis.HasRange) return index + 1;
        if (count <= 1) return document.XAxis.Minimum!.Value;
        var min = document.XAxis.Minimum!.Value;
        var max = document.XAxis.Maximum!.Value;
        return min + (max - min) * index / (count - 1);
    }

    private static int CountSeries(MermaidXYChartDocument document, MermaidXYChartSeriesKind kind) {
        var count = 0;
        foreach (var series in document.Series) if (series.Kind == kind) count++;
        return count;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid XY charts.
/// </summary>
public sealed class MermaidXYChartRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 900;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 520;

    /// <summary>Gets or sets whether static previews should show data labels.</summary>
    public bool ShowDataLabels { get; set; }
}
