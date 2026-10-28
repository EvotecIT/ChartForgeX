using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.SvgRaster;
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
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithSize(options.Width, options.Height)
            .WithPadding(options.Padding)
            .WithColumns(columns)
            .WithEdges(options.ShowEdges);

        block.AutoColumns = document.AutoColumns;
        foreach (var item in document.Items) {
            if (item.IsComposite) block.AddComposite(item.Id, item.Label, item.ColumnSpan, item.Columns, item.ParentId, item.AutoColumns);
            else if (item.IsSpace) { if (item.ParentId == null) block.AddSpace(item.ColumnSpan); else block.AddSpace(item.ParentId, item.ColumnSpan); }
            else if (item.ParentId == null) block.AddItem(item.Id, item.Label, item.ColumnSpan, item.Shape);
            else block.AddItem(item.Id, item.Label, item.ParentId, item.ColumnSpan, item.Shape);
            var target = block.Items[block.Items.Count - 1]; target.ArrowDirections = item.ArrowDirections;
            target.Metadata["mermaid.source.line"] = item.Span.Line.ToString(CultureInfo.InvariantCulture);
            target.Metadata["mermaid.source.column"] = item.Span.Column.ToString(CultureInfo.InvariantCulture);
            if (item.Classes.Count > 0) target.Metadata["mermaid.classes"] = string.Join(" ", item.Classes);
            ChartForgeX.Primitives.ChartColor? strokeColor = null; double? strokeWidth = null; double[]? strokeDash = null;
            foreach (var style in item.Styles) {
                target.Metadata["mermaid.style." + style.Key] = style.Value;
                if (SvgRasterColor.TryParse(style.Value, out var color)) {
                    if (style.Key == "fill") target.Fill = color;
                    else if (style.Key == "stroke") strokeColor = color;
                    else if (style.Key == "color") target.TextStyle.Color = color;
                }
                if (style.Key == "stroke-width" && MermaidBlockParser.BlockNumber(style.Value, out var width) && width >= 0 && width <= 1024) strokeWidth = width;
                else if (style.Key == "stroke-dasharray" && MermaidBlockParser.BlockDash(style.Value, out var dash)) strokeDash = dash;
                else if (style.Key == "font-size" && MermaidBlockParser.BlockNumber(style.Value, out var size) && size > 0) target.TextStyle.FontSize = size;
                else if (style.Key == "font-weight" && MermaidBlockParser.BlockNativeStyle(style.Key, style.Value)) target.TextStyle.FontWeight = style.Value;
                else if (style.Key == "font-style" && MermaidBlockParser.BlockNativeStyle(style.Key, style.Value)) target.TextStyle.Italic = style.Value == "italic";
            }
            if (strokeColor.HasValue || strokeWidth.HasValue || strokeDash != null) target.Stroke = new ChartStrokeStyle(strokeColor, strokeWidth, dash: strokeDash);
        }

        foreach (var edge in document.Edges) {
            block.AddEdge(edge.SourceId, edge.TargetId, edge.Label, edge.Directed);
            block.Edges[block.Edges.Count - 1].Visible = edge.Visible;
            block.Edges[block.Edges.Count - 1].Stroke = edge.Stroke;
        }
        MermaidPresentation.Apply(block, document, options.Presentation);
        MermaidNativeStyles.ResolveLetterSpacing(block); return block;
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
        return MermaidPresentation.Apply(artifact, document);
    }

    /// <summary>Renders a Mermaid block document to static SVG.</summary>
    public static string ToSvg(this MermaidBlockDocument document, MermaidBlockRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToSvg();

    /// <summary>Renders a Mermaid block document to static PNG.</summary>
    public static byte[] ToPng(this MermaidBlockDocument document, MermaidBlockRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToPng();

    private static string ResolveTitle(MermaidBlockDocument document, MermaidBlockRenderOptions options) {
        return MermaidHeading.Title(document, options.Title, document.Title, "Mermaid block");
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
public sealed class MermaidBlockRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 900;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 420;

    /// <summary>Gets or sets the inner preview padding.</summary>
    public double Padding { get; set; } = 30;

    /// <summary>Gets or sets the default column count when source omits a column declaration.</summary>
    public int Columns { get; set; } = 3;

    /// <summary>Gets or sets whether parsed block edges are rendered.</summary>
    public bool ShowEdges { get; set; } = true;
}
