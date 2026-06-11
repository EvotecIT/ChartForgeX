using System;
using System.Globalization;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid block diagrams.
/// </summary>
public static class MermaidBlockRendering {
    /// <summary>
    /// Converts a Mermaid block document into a reusable block-layout visual block.
    /// </summary>
    public static BlockLayoutBlock ToBlockLayoutBlock(this MermaidBlockDocument document, MermaidBlockRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidBlockRenderOptions();
        var columns = document.Columns ?? options.Columns;
        var block = BlockLayoutBlock.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithPadding(options.Padding)
            .WithColumns(columns)
            .WithEdges(options.ShowEdges);

        foreach (var item in document.Items) {
            if (item.IsSpace) block.AddSpace(item.ColumnSpan);
            else block.AddItem(item.Id, item.Label, item.ColumnSpan, item.Shape);
        }

        foreach (var edge in document.Edges) block.AddEdge(edge.SourceId, edge.TargetId, edge.Label, edge.Directed);
        return block;
    }

    /// <summary>
    /// Wraps a Mermaid block document in a visual artifact envelope backed by a block-layout block.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidBlockDocument document, MermaidBlockRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidBlockRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-block" : options.Id!.Trim();
        var block = document.ToBlockLayoutBlock(options);
        var artifact = VisualArtifact.Create(id, VisualArtifactKind.Mermaid, block);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = block.Title;
        artifact.Subtitle = block.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(block.Options.Size.Width, block.Options.Size.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.blocks"] = VisibleItemCount(document).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.edges"] = document.Edges.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.columns"] = block.Columns.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(BlockLayoutBlock);
        return artifact;
    }

    /// <summary>Renders a Mermaid block document to static SVG.</summary>
    public static string ToSvg(this MermaidBlockDocument document, MermaidBlockRenderOptions? options = null) =>
        document.ToBlockLayoutBlock(options).ToSvg();

    /// <summary>Renders a Mermaid block document to static PNG.</summary>
    public static byte[] ToPng(this MermaidBlockDocument document, MermaidBlockRenderOptions? options = null) =>
        document.ToBlockLayoutBlock(options).ToPng();

    private static string ResolveTitle(MermaidBlockDocument document, MermaidBlockRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid block";
    }

    private static string ResolveSubtitle(MermaidBlockDocument document, MermaidBlockRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static int VisibleItemCount(MermaidBlockDocument document) {
        var count = 0;
        foreach (var item in document.Items) if (!item.IsSpace) count++;
        return count;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid block diagrams.
/// </summary>
public sealed class MermaidBlockRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 900;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 420;

    /// <summary>Gets or sets the inner preview padding.</summary>
    public double Padding { get; set; } = 30;

    /// <summary>Gets or sets the default column count when source uses auto columns.</summary>
    public int Columns { get; set; } = 3;

    /// <summary>Gets or sets whether parsed block edges are rendered.</summary>
    public bool ShowEdges { get; set; } = true;
}
