using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid treemap diagrams.
/// </summary>
public static class MermaidTreemapRendering {
    /// <summary>
    /// Converts a Mermaid treemap document into a renderer-independent ChartForgeX chart.
    /// </summary>
    public static Chart ToChart(this MermaidTreemapDocument document, MermaidTreemapRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTreemapRenderOptions();
        return Chart.Create()
            .WithTitle(ResolveTitle(options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .AddTreemap(string.IsNullOrWhiteSpace(options.SeriesName) ? "Treemap" : options.SeriesName!, ToItems(document));
    }

    /// <summary>
    /// Wraps a Mermaid treemap document in a visual artifact envelope backed by a ChartForgeX chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidTreemapDocument document, MermaidTreemapRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTreemapRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-treemap" : options.Id!.Trim();
        var chart = document.ToChart(options);
        var artifact = chart.ToVisualArtifact(id, VisualArtifactKind.Mermaid, VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.roots"] = document.Roots.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.nodes"] = document.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.leaves"] = LeafCount(document).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.classes"] = ClassCount(document).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(Chart);
        return artifact;
    }

    /// <summary>
    /// Renders a Mermaid treemap document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidTreemapDocument document, MermaidTreemapRenderOptions? options = null) =>
        document.ToChart(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid treemap document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidTreemapDocument document, MermaidTreemapRenderOptions? options = null) =>
        document.ToChart(options).ToPng();

    private static string ResolveTitle(MermaidTreemapRenderOptions options) =>
        string.IsNullOrWhiteSpace(options.Title) ? "Mermaid treemap" : options.Title!;

    private static string ResolveSubtitle(MermaidTreemapDocument document, MermaidTreemapRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static ChartTreemapItem[] ToItems(MermaidTreemapDocument document) {
        var items = new List<ChartTreemapItem>();
        foreach (var node in document.Nodes) {
            if (!node.Value.HasValue) continue;
            items.Add(new ChartTreemapItem(node.Path, node.Value.Value));
        }

        return items.ToArray();
    }

    private static int LeafCount(MermaidTreemapDocument document) {
        var count = 0;
        foreach (var node in document.Nodes) if (node.Value.HasValue) count++;
        return count;
    }

    private static int ClassCount(MermaidTreemapDocument document) {
        var count = 0;
        foreach (var node in document.Nodes) if (!string.IsNullOrWhiteSpace(node.ClassName)) count++;
        return count;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid treemap diagrams.
/// </summary>
public sealed class MermaidTreemapRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the chart series name.</summary>
    public string? SeriesName { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 560;
}
