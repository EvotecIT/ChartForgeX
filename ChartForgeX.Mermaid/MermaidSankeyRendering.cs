using System;
using System.Globalization;
using System.Collections.Generic;
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
        var chart = Chart.Create()
            .WithTitle(MermaidHeading.Title(document, options.Title, null, "Mermaid Sankey"))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithSize(options.Width ?? document.Configuration.Sankey?.Width ?? 960,
                options.Height ?? document.Configuration.Sankey?.Height ?? 560)
            .AddSankey(string.IsNullOrWhiteSpace(options.SeriesName) ? "Flow" : options.SeriesName!, ToNodes(document), ToLinks(document));
        document.Configuration.Sankey?.Apply(chart);
        return MermaidPresentation.Apply(chart, document);
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
        return MermaidPresentation.Apply(artifact, document);
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
    // Sankey CSV uses endpoint text as identity. Make that language mapping explicit at this boundary.
    private static IReadOnlyList<ChartNode> ToNodes(MermaidSankeyDocument document) {
        var nodes = new List<ChartNode>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in document.Links) {
            if (ids.Add(link.Source)) nodes.Add(new ChartNode(link.Source, link.Source));
            if (ids.Add(link.Target)) nodes.Add(new ChartNode(link.Target, link.Target));
        }
        return nodes;
    }

    private static ChartFlowLink[] ToLinks(MermaidSankeyDocument document) {
        var links = new ChartFlowLink[document.Links.Count];
        for (var i = 0; i < links.Length; i++) links[i] = new ChartFlowLink("flow-" + i.ToString(CultureInfo.InvariantCulture), document.Links[i].Source, document.Links[i].Target, document.Links[i].Value);
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

    /// <summary>Gets or sets a host width override. Null uses source configuration or the 960-pixel fallback.</summary>
    public int? Width { get; set; }

    /// <summary>Gets or sets a host height override. Null uses source configuration or the 560-pixel fallback.</summary>
    public int? Height { get; set; }
}
