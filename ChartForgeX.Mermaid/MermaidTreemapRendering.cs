using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid treemap diagrams.
/// </summary>
public static partial class MermaidTreemapRendering {
    /// <summary>
    /// Converts a Mermaid treemap document into a renderer-independent ChartForgeX chart.
    /// </summary>
    public static Chart ToChart(this MermaidTreemapDocument document, MermaidTreemapRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTreemapRenderOptions();
        var chart = Chart.Create()
            .WithTitle(MermaidHeading.Title(document, options.Title, null, "Mermaid treemap"))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithSize(options.Width, options.Height)
            .WithDataLabels()
            .AddTreemap(string.IsNullOrWhiteSpace(options.SeriesName) ? "Treemap" : options.SeriesName!, ToItems(document));
        MermaidPresentation.Apply(chart, document, options.Presentation);
        document.Configuration.Treemap?.Apply(chart);
        if (options.ShowValues.HasValue) chart.Options.Treemap.ShowValues = options.ShowValues.Value;
        if (options.ValueFormat != null) chart.Options.ValueFormat = options.ValueFormat;
        ApplyStyles(chart.Series[0], document);
        return chart;
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
        RetainStyles(artifact, document);
        var sourceFormat = document.Configuration.Last("treemap.valueFormat");
        if (sourceFormat?.Value != null) artifact.Metadata["mermaid.treemap.source.valueFormat"] = sourceFormat.Value;
        return MermaidPresentation.Apply(artifact, document);
    }

    /// <summary>
    /// Renders a Mermaid treemap document to static SVG through ChartForgeX chart rendering.
    /// </summary>
    public static string ToSvg(this MermaidTreemapDocument document, MermaidTreemapRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToSvg();

    /// <summary>
    /// Renders a Mermaid treemap document to static PNG through ChartForgeX chart rendering.
    /// </summary>
    public static byte[] ToPng(this MermaidTreemapDocument document, MermaidTreemapRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToPng();
    private static ChartHierarchyItem[] ToItems(MermaidTreemapDocument document) {
        // Mermaid labels have no authored ID syntax; source-order IDs preserve distinct repeated labels.
        var ids = new Dictionary<MermaidTreemapNode, string>();
        for (var index = 0; index < document.Nodes.Count; index++) ids.Add(document.Nodes[index], "node-" + index.ToString(CultureInfo.InvariantCulture));
        var items = new List<ChartHierarchyItem>();
        foreach (var node in document.Nodes) {
            if (node.Parent != null && !ids.ContainsKey(node.Parent)) throw new InvalidOperationException("Treemap parents must belong to the document.");
            items.Add(new ChartHierarchyItem(ids[node], node.Label, node.Parent == null ? null : ids[node.Parent], node.Value));
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
public sealed class MermaidTreemapRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the chart series name.</summary>
    public string? SeriesName { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 960;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 560;
    /// <summary>Gets or sets an explicit host override for value captions. Null uses source configuration or the native default.</summary>
    public bool? ShowValues { get; set; }
    /// <summary>Gets or sets an immutable native value format overriding source valueFormat; null preserves source/native policy.</summary>
    public ChartValueFormat? ValueFormat { get; set; }
}
