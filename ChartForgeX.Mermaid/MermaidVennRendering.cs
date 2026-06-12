using System;
using System.Globalization;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid Venn diagrams.
/// </summary>
public static class MermaidVennRendering {
    /// <summary>
    /// Converts a Mermaid Venn document into a reusable Venn visual block.
    /// </summary>
    public static VennDiagramBlock ToVennDiagramBlock(this MermaidVennDocument document, MermaidVennRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidVennRenderOptions();
        var block = VennDiagramBlock.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithPadding(options.Padding);

        foreach (var item in document.Sets) {
            block.AddSet(item.Id, item.Label, item.Size);
            var set = (VennSet)block.Sets[block.Sets.Count - 1];
            set.Fill = item.Fill;
            set.Stroke = item.Stroke;
            set.TextColor = item.TextColor;
        }

        foreach (var item in document.Intersections) {
            block.AddIntersection(item.SetIds, item.Label, item.Size);
            var intersection = (VennIntersection)block.Intersections[block.Intersections.Count - 1];
            intersection.Fill = item.Fill;
            intersection.Stroke = item.Stroke;
            intersection.TextColor = item.TextColor;
        }

        foreach (var textNode in document.TextNodes) {
            block.AddText(textNode.SetIds, textNode.Id, textNode.Label);
            var target = (VennTextNode)block.TextNodes[block.TextNodes.Count - 1];
            target.TextColor = textNode.TextColor;
        }
        return block;
    }

    /// <summary>
    /// Wraps a Mermaid Venn document in a visual artifact envelope.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidVennDocument document, MermaidVennRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidVennRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-venn" : options.Id!.Trim();
        var block = document.ToVennDiagramBlock(options);
        var artifact = VisualArtifact.Create(id, VisualArtifactKind.Mermaid, block);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = block.Title;
        artifact.Subtitle = block.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(block.Options.Size.Width, block.Options.Size.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.sets"] = document.Sets.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.intersections"] = document.Intersections.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.textNodes"] = document.TextNodes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(VennDiagramBlock);
        artifact.Metadata["render.note"] = "Deterministic one-to-three-set Venn preview; source sizes are retained but not area-proportional.";
        return artifact;
    }

    /// <summary>Renders a Mermaid Venn document to static SVG.</summary>
    public static string ToSvg(this MermaidVennDocument document, MermaidVennRenderOptions? options = null) =>
        document.ToVennDiagramBlock(options).ToSvg();

    /// <summary>Renders a Mermaid Venn document to static PNG.</summary>
    public static byte[] ToPng(this MermaidVennDocument document, MermaidVennRenderOptions? options = null) =>
        document.ToVennDiagramBlock(options).ToPng();

    private static string ResolveTitle(MermaidVennDocument document, MermaidVennRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid Venn";
    }

    private static string ResolveSubtitle(MermaidVennDocument document, MermaidVennRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid Venn diagrams.
/// </summary>
public sealed class MermaidVennRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 780;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 460;

    /// <summary>Gets or sets the inner preview padding.</summary>
    public double Padding { get; set; } = 30;
}
