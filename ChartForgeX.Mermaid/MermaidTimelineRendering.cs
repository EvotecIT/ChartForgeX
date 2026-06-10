using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid timeline diagrams.
/// </summary>
public static class MermaidTimelineRendering {
    /// <summary>
    /// Converts a Mermaid timeline document into a renderer-independent ChartForgeX timeline chart.
    /// </summary>
    public static Chart ToChart(this MermaidTimelineDocument document, MermaidTimelineRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTimelineRenderOptions();
        var chart = Chart.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithXAxis("Timeline")
            .WithYAxis("Events")
            .WithXLabels(GetPeriodLabels(document));

        for (var periodIndex = 0; periodIndex < document.Periods.Count; periodIndex++) {
            var period = document.Periods[periodIndex];
            for (var itemIndex = 0; itemIndex < period.Events.Count; itemIndex++) {
                var label = string.IsNullOrWhiteSpace(period.Section)
                    ? period.Events[itemIndex].Text
                    : period.Section + " - " + period.Events[itemIndex].Text;
                chart.AddTimelineRange(label, periodIndex + 1, periodIndex + 1.72);
                var series = chart.Series[chart.Series.Count - 1];
                series.SemanticRole = "mermaid-timeline-event";
                series.WithLegendEntry(false);
                if (options.ShowEventDurations) series.WithDataLabels();
                series.WithPointLabel(0, period.Text);
            }
        }

        return chart;
    }

    /// <summary>
    /// Wraps a Mermaid timeline document in a visual artifact envelope backed by a ChartForgeX timeline chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidTimelineDocument document, MermaidTimelineRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTimelineRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-timeline" : options.Id!.Trim();
        var chart = document.ToChart(options);
        var artifact = chart.ToVisualArtifact(id, VisualArtifactKind.Mermaid, VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.direction"] = document.Direction.ToString();
        artifact.Metadata["mermaid.sections"] = document.Sections.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.periods"] = document.Periods.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.events"] = CountEvents(document).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(Chart);
        return artifact;
    }

    /// <summary>
    /// Renders a Mermaid timeline document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidTimelineDocument document, MermaidTimelineRenderOptions? options = null) =>
        document.ToChart(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid timeline document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidTimelineDocument document, MermaidTimelineRenderOptions? options = null) =>
        document.ToChart(options).ToPng();

    private static string ResolveTitle(MermaidTimelineDocument document, MermaidTimelineRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid timeline";
    }

    private static string ResolveSubtitle(MermaidTimelineDocument document, MermaidTimelineRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static string[] GetPeriodLabels(MermaidTimelineDocument document) {
        var labels = new string[document.Periods.Count];
        for (var i = 0; i < labels.Length; i++) labels[i] = document.Periods[i].Text;
        return labels;
    }

    private static int CountEvents(MermaidTimelineDocument document) {
        var count = 0;
        foreach (var period in document.Periods) count += period.Events.Count;
        return count;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid timeline diagrams.
/// </summary>
public sealed class MermaidTimelineRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 560;

    /// <summary>Gets or sets whether CFX should render duration labels on mapped event ranges.</summary>
    public bool ShowEventDurations { get; set; }
}
