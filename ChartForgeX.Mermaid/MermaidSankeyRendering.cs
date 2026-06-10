using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid Sankey diagrams.
/// </summary>
public static class MermaidSankeyRendering {
    /// <summary>
    /// Converts a Mermaid Sankey document into a renderer-independent ChartForgeX chart.
    /// </summary>
    public static Chart ToChart(this MermaidSankeyDocument document, MermaidSankeyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidSankeyRenderOptions();
        return Chart.Create()
            .WithTitle(ResolveTitle(options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .AddSankey(string.IsNullOrWhiteSpace(options.SeriesName) ? "Flow" : options.SeriesName!, ToLinks(document));
    }

    /// <summary>
    /// Wraps a Mermaid Sankey document in a visual artifact envelope backed by a ChartForgeX chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidSankeyDocument document, MermaidSankeyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidSankeyRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-sankey" : options.Id!.Trim();
        var chart = document.ToChart(options);
        var artifact = chart.ToVisualArtifact(id, VisualArtifactKind.Mermaid, VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.links"] = document.Links.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(Chart);
        return artifact;
    }

    /// <summary>
    /// Renders a Mermaid Sankey document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidSankeyDocument document, MermaidSankeyRenderOptions? options = null) =>
        document.ToChart(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid Sankey document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidSankeyDocument document, MermaidSankeyRenderOptions? options = null) =>
        document.ToChart(options).ToPng();

    private static string ResolveTitle(MermaidSankeyRenderOptions options) =>
        string.IsNullOrWhiteSpace(options.Title) ? "Mermaid Sankey" : options.Title!;

    private static string ResolveSubtitle(MermaidSankeyDocument document, MermaidSankeyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static ChartSankeyLink[] ToLinks(MermaidSankeyDocument document) {
        var links = new ChartSankeyLink[document.Links.Count];
        for (var i = 0; i < links.Length; i++) links[i] = new ChartSankeyLink(document.Links[i].Source, document.Links[i].Target, document.Links[i].Value);
        return links;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid Sankey diagrams.
/// </summary>
public sealed class MermaidSankeyRenderOptions {
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
