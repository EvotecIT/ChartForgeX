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
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithSize(options.Width, options.Height);

        var horizontal = document.Orientation == ChartOrientation.Horizontal;
        chart.Options.LineOrientation = document.Orientation;
        var categoryAxis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        var valueAxis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        if (!string.IsNullOrWhiteSpace(document.XAxis.Title)) {
            if (horizontal) chart.WithYAxis(document.XAxis.Title!);
            else chart.WithXAxis(document.XAxis.Title!);
        }
        if (!string.IsNullOrWhiteSpace(document.YAxis.Title)) {
            if (horizontal) chart.WithXAxis(document.YAxis.Title!);
            else chart.WithYAxis(document.YAxis.Title!);
        }
        for (var index = 0; index < document.XAxis.Labels.Count; index++)
            categoryAxis.Labels.Add(new ChartAxisLabel(index + 1, document.XAxis.Labels[index]));
        if (document.XAxis.HasRange) { categoryAxis.Minimum = document.XAxis.Minimum; categoryAxis.Maximum = document.XAxis.Maximum; }
        if (document.YAxis.HasRange) { valueAxis.Minimum = document.YAxis.Minimum; valueAxis.Maximum = document.YAxis.Maximum; }
        if (options.ShowDataLabels) chart.WithDataLabels();

        foreach (var series in document.Series) {
            var points = BuildPoints(document, series);
            if (series.Kind == MermaidXYChartSeriesKind.Line) chart.AddLine(series.Name, points);
            else if (document.Orientation == ChartOrientation.Horizontal) chart.AddHorizontalBar(series.Name, points);
            else chart.AddBar(series.Name, points);
        }

        return MermaidPresentation.Apply(chart, document, options.Presentation);
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
        return MermaidPresentation.Apply(artifact, document);
    }

    /// <summary>
    /// Renders a Mermaid XY chart document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidXYChartDocument document, MermaidXYChartRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid XY chart document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidXYChartDocument document, MermaidXYChartRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToPng();

    private static string ResolveTitle(MermaidXYChartDocument document, MermaidXYChartRenderOptions options) {
        return MermaidHeading.Title(document, options.Title, document.Title, "Mermaid XY chart");
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
public sealed class MermaidXYChartRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 900;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 520;

    /// <summary>Gets or sets whether static previews should show data labels.</summary>
    public bool ShowDataLabels { get; set; }
}
