using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid journey diagrams.
/// </summary>
public static class MermaidJourneyRendering {
    /// <summary>
    /// Converts a Mermaid journey document into a renderer-independent ChartForgeX score chart.
    /// </summary>
    public static Chart ToChart(this MermaidJourneyDocument document, MermaidJourneyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidJourneyRenderOptions();
        var points = document.Tasks.Select((task, index) => new ChartPoint(index + 1, task.Score)).ToArray();
        var labels = document.Tasks.Select(task => string.IsNullOrWhiteSpace(task.Section) ? task.Text : task.Section + " - " + task.Text).ToArray();
        var chart = Chart.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithXAxis("Journey task")
            .WithYAxis("Score")
            .WithYAxisBounds(0, Math.Max(5, document.Tasks.Count == 0 ? 5 : document.Tasks.Max(task => task.Score)))
            .WithXLabels(labels)
            .WithDataLabels()
            .AddBar(options.SeriesName, points);

        var series = chart.Series[0];
        series.SemanticRole = "mermaid-journey-score";
        for (var i = 0; i < document.Tasks.Count; i++) {
            var actors = document.Tasks[i].Actors.Count == 0 ? string.Empty : " [" + string.Join(", ", document.Tasks[i].Actors) + "]";
            series.WithPointLabel(i, document.Tasks[i].Text + actors);
        }

        return chart;
    }

    /// <summary>
    /// Wraps a Mermaid journey document in a visual artifact envelope backed by a ChartForgeX chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidJourneyDocument document, MermaidJourneyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidJourneyRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-journey" : options.Id!.Trim();
        var chart = document.ToChart(options);
        var artifact = chart.ToVisualArtifact(id, VisualArtifactKind.Mermaid, VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.sections"] = document.Sections.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.tasks"] = document.Tasks.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.actors"] = CountActors(document).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(Chart);
        return artifact;
    }

    /// <summary>
    /// Renders a Mermaid journey document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidJourneyDocument document, MermaidJourneyRenderOptions? options = null) =>
        document.ToChart(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid journey document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidJourneyDocument document, MermaidJourneyRenderOptions? options = null) =>
        document.ToChart(options).ToPng();

    private static string ResolveTitle(MermaidJourneyDocument document, MermaidJourneyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid journey";
    }

    private static string ResolveSubtitle(MermaidJourneyDocument document, MermaidJourneyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static int CountActors(MermaidJourneyDocument document) =>
        document.Tasks.SelectMany(task => task.Actors).Distinct(StringComparer.Ordinal).Count();
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid journey diagrams.
/// </summary>
public sealed class MermaidJourneyRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the ChartForgeX series name.</summary>
    public string SeriesName { get; set; } = "Journey score";

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 560;
}
