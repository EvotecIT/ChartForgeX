using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.Rendering;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid quadrant charts.
/// </summary>
public static partial class MermaidQuadrantRendering {
    /// <summary>
    /// Converts a Mermaid quadrant document into a scatter chart with four native background regions.
    /// </summary>
    public static Chart ToChart(this MermaidQuadrantDocument document, MermaidQuadrantRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        using var resourceScope = MermaidResourceGuard.BeginIfAbsent(document.CapturedResourceLimits);
        MermaidResourceGuard.ValidateDocument(document);
        options ??= new MermaidQuadrantRenderOptions();
        foreach (var definition in document.ClassDefinitions)
            if (string.IsNullOrWhiteSpace(definition.Name)) throw new ArgumentException("Quadrant class definitions require a nonempty name.", nameof(document));
        foreach (var point in document.Points) {
            MermaidResourceGuard.Checkpoint();
            if (point.ClassName != null && !MermaidQuadrantStyles.IsInlineClassName(point.ClassName))
                throw new ArgumentException("Quadrant inline classes require one word name.", nameof(document));
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) || point.X < 0 || point.X > 1
                || double.IsNaN(point.Y) || double.IsInfinity(point.Y) || point.Y < 0 || point.Y > 1)
                throw new ArgumentException("Quadrant point coordinates must be finite normalized values from zero through one.", nameof(document));
        }
        var points = document.Points.Select(point => new ChartPoint(point.X, point.Y)).ToArray();
        var settings = document.Configuration.Quadrant;
        var chart = Chart.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithSize(options.HasExplicitWidth ? options.Width : settings?.Width ?? options.Width,
                options.HasExplicitHeight ? options.Height : settings?.Height ?? options.Height)
            .WithXAxis(FormatAxis(document.XAxisStart, document.XAxisEnd, "X"))
            .WithYAxis(FormatAxis(document.YAxisStart, document.YAxisEnd, "Y"))
            .WithXAxisBounds(0, 1)
            .WithYAxisBounds(0, 1)
            .WithDataLabels()
            .AddScatter(options.SeriesName, points);

        var series = chart.Series[0];
        series.SemanticRole = "mermaid-quadrant-point";
        for (var i = 0; i < document.Points.Count; i++) series.WithPointLabel(i, document.Points[i].Label);
        ApplyEndpointLabels(chart.Options.XAxis, document.XAxisStart, document.XAxisEnd);
        ApplyEndpointLabels(chart.Options.YAxis, document.YAxisStart, document.YAxisEnd);
        MermaidPresentation.Apply(chart, document, options.Presentation);
        settings?.Apply(chart);
        var strokeDefault = series.Markers.Fill ?? ChartSeriesColours.Resolve(series, 0, chart.Options.Theme.Resolve(chart.Options.ThemeMode));
        var definitions = MermaidQuadrantStyles.Definitions(document);
        for (var index = 0; index < document.Points.Count; index++)
            MermaidQuadrantStyles.Apply(series, index, MermaidQuadrantStyles.Resolve(definitions, document.Points[index]), strokeDefault);
        if (options.ShowPivotRules && options.ShowPivotAxes)
            throw new ArgumentException("Choose either rule-only or complete pivot axes.", nameof(options));
        if (options.ShowPivotAxes) {
            chart.Options.XAxis.CrossAt = .5; chart.Options.YAxis.CrossAt = .5;
        }
        if (options.ShowPivotRules) {
            chart.Options.XAxis.RuleCrossAt = .5;
            chart.Options.YAxis.RuleCrossAt = .5;
        }
        for (var quadrant = 1; quadrant <= 4; quadrant++) {
            var right = quadrant == 1 || quadrant == 4;
            var top = quadrant == 1 || quadrant == 2;
            document.QuadrantLabels.TryGetValue(quadrant, out var label);
            var fill = settings?.RegionFill(quadrant);
            var region = fill.HasValue ? new ChartPlotRegion(fill.Value,
                xMinimum: right ? .5 : 0, xMaximum: right ? 1 : .5,
                yMinimum: top ? .5 : 0, yMaximum: top ? 1 : .5, label: label ?? string.Empty, opacity: 1)
                : new ChartPlotRegion(quadrant - 1,
                    xMinimum: right ? .5 : 0, xMaximum: right ? 1 : .5,
                    yMinimum: top ? .5 : 0, yMaximum: top ? 1 : .5, label: label ?? string.Empty);
            chart.AddPlotRegion(region.WithBorder(options.RegionBorder).WithLabelPosition(options.RegionLabelPosition));
        }
        return chart;
    }

    /// <summary>
    /// Wraps a Mermaid quadrant document in an artifact backed by native scatter points and quadrant regions.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidQuadrantDocument document, MermaidQuadrantRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidQuadrantRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-quadrant" : options.Id!.Trim();
        using var resourceScope = MermaidResourceGuard.BeginIfAbsent(document.CapturedResourceLimits);
        var chart = document.ToChart(options);
        var artifact = chart.ToVisualArtifact(id, VisualArtifactKind.Mermaid, VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.points"] = document.Points.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.quadrants"] = document.QuadrantLabels.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(Chart);
        AttachSourceFacts(artifact, document);
        return MermaidPresentation.Apply(artifact, document);
    }

    /// <summary>
    /// Renders a Mermaid quadrant document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidQuadrantDocument document, MermaidQuadrantRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid quadrant document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidQuadrantDocument document, MermaidQuadrantRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToPng();

    private static string ResolveTitle(MermaidQuadrantDocument document, MermaidQuadrantRenderOptions options) {
        return MermaidHeading.Title(document, options.Title, document.Title, "Mermaid quadrant");
    }

    private static string FormatAxis(string? start, string? end, string fallback) =>
        string.IsNullOrWhiteSpace(start) && string.IsNullOrWhiteSpace(end) ? fallback : string.Empty;

    private static void ApplyEndpointLabels(ChartAxis axis, string? start, string? end) {
        var hasEnd = !string.IsNullOrWhiteSpace(end);
        if (!string.IsNullOrWhiteSpace(start)) axis.Labels.Add(new ChartAxisLabel(hasEnd ? .25 : 0, start!));
        if (hasEnd) axis.Labels.Add(new ChartAxisLabel(.75, end!));
        if (!string.IsNullOrWhiteSpace(start) || hasEnd) axis.TickCount = 2;
    }

}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid quadrant charts.
/// </summary>
public sealed class MermaidQuadrantRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the ChartForgeX series name.</summary>
    public string SeriesName { get; set; } = "Quadrant point";

    /// <summary>Gets or sets an immutable border for all four native background regions. Null preserves borderless regions.</summary>
    public ChartStrokeStyle? RegionBorder { get; set; }

    /// <summary>Gets or sets one screen-relative caption anchor for the four native regions. Auto retains collision-aware placement.</summary>
    public ChartPlotRegionLabelPosition RegionLabelPosition { get; set; }

    /// <summary>Gets or sets whether the native X and Y rules cross at the normalized 0.5 pivot. Captions and titles retain edge gutters.</summary>
    public bool ShowPivotRules { get; set; }

    /// <summary>Gets or sets whether complete native axes cross at normalized 0.5, including measured tick captions and titles. Mutually exclusive with ShowPivotRules.</summary>
    public bool ShowPivotAxes { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get => _width; set { _width = value; HasExplicitWidth = true; } }
    private int _width = 960;
    internal bool HasExplicitWidth { get; private set; }

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get => _height; set { _height = value; HasExplicitHeight = true; } }
    private int _height = 560;
    internal bool HasExplicitHeight { get; private set; }
}
