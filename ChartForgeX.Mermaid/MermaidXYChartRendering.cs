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

        var settings = document.Configuration.XYChart;
        var orientation = document.HasExplicitOrientation ? document.Orientation : settings?.Orientation ?? document.Orientation;
        var chart = Chart.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithSize(options.Width ?? settings?.Width ?? 900, options.Height ?? settings?.Height ?? 520);

        var horizontal = orientation == ChartOrientation.Horizontal;
        chart.Options.LineOrientation = orientation;
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
        if (options.ShowDataLabels.HasValue) chart.WithDataLabels(options.ShowDataLabels.Value);
        var showLegend = options.ShowLegend ?? settings?.ShowLegend;
        if (showLegend.HasValue) chart.WithLegend(showLegend.Value);

        foreach (var series in document.Series) {
            var points = BuildPoints(document, series);
            if (series.Kind == MermaidXYChartSeriesKind.Line) chart.AddLine(series.Name, points);
            else if (horizontal) chart.AddHorizontalBar(series.Name, points);
            else chart.AddBar(series.Name, points);
            ApplyLabels(chart.Series[chart.Series.Count - 1], series, options, settings);
        }

        MermaidPresentation.Apply(chart, document, options.Presentation);
        settings?.Apply(chart, horizontal, options.Presentation);
        var showTitle = options.ShowTitle ?? (options.Title != null ? true : settings?.ShowTitle ?? true);
        if (!showTitle) chart.WithTitle(string.Empty);
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
        artifact.Metadata["mermaid.orientation"] = chart.Options.LineOrientation.ToString();
        artifact.Metadata["mermaid.series"] = document.Series.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.barSeries"] = CountSeries(document, MermaidXYChartSeriesKind.Bar).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.lineSeries"] = CountSeries(document, MermaidXYChartSeriesKind.Line).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.xAxisLabels"] = document.XAxis.Labels.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.pointLabels"] = CountPointLabels(document).ToString(CultureInfo.InvariantCulture);
        if (document.Configuration.XYChart?.UseMaxWidth is bool fit)
            artifact.HtmlSizing = fit ? VisualArtifactHtmlSizing.FitToWidth : VisualArtifactHtmlSizing.PreserveSize;
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

    private static void ApplyLabels(ChartSeries native, MermaidXYChartSeries source,
        MermaidXYChartRenderOptions host, MermaidXYChartSettings? settings) {
        if (source.Kind == MermaidXYChartSeriesKind.Bar) {
            if (!host.ShowDataLabels.HasValue && settings?.ShowDataLabel is bool visible) native.WithDataLabels(visible);
            if (settings?.ShowDataLabelOutsideBar is bool outside)
                native.WithDataLabelPlacement(outside ? ChartDataLabelPlacement.Outside : ChartDataLabelPlacement.Inside);
            return;
        }
        if (source.PointLabels.Count == 0) return;
        // Unlabelled line points stay unlabelled unless the host explicitly asks for numeric labels.
        if (host.ShowDataLabels != true)
            for (var index = 0; index < source.Values.Count; index++) native.WithPointLabel(index, string.Empty);
        foreach (var label in source.PointLabels) {
            if (label.PointIndex >= source.Values.Count) throw new ArgumentException("An XY point caption must reference a datum in its series.", nameof(source));
            native.WithPointLabel(label.PointIndex, MermaidLabelText.DisplayCaption(label.Text));
        }
        if (host.ShowDataLabels != false) native.WithDataLabels();
    }

    private static int CountPointLabels(MermaidXYChartDocument document) {
        var count = 0; foreach (var series in document.Series) count += series.PointLabels.Count; return count;
    }

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
    /// <summary>Gets or sets a host width override. Null uses source configuration, then 900 pixels.</summary>
    public int? Width { get; set; }

    /// <summary>Gets or sets a host height override. Null uses source configuration, then 520 pixels.</summary>
    public int? Height { get; set; }

    /// <summary>Gets or sets an explicit host data-label policy. Null retains source captions and native defaults.</summary>
    public bool? ShowDataLabels { get; set; }

    /// <summary>Gets or sets a host legend override. Null retains source configuration and the existing visible native legend.</summary>
    public bool? ShowLegend { get; set; }

    /// <summary>Gets or sets a host heading override. Null retains source showTitle; an explicit Title is visible by default.</summary>
    public bool? ShowTitle { get; set; }
}
