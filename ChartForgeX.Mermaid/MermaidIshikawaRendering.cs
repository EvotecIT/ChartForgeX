using System;
using System.Globalization;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid Ishikawa diagrams.
/// </summary>
public static class MermaidIshikawaRendering {
    /// <summary>
    /// Converts a Mermaid Ishikawa document into a reusable fishbone visual block.
    /// </summary>
    public static FishboneDiagramBlock ToFishboneDiagramBlock(this MermaidIshikawaDocument document, MermaidIshikawaRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (document.Root == null) throw new InvalidOperationException("Mermaid Ishikawa diagrams require a root effect line.");
        options ??= new MermaidIshikawaRenderOptions();
        var block = FishboneDiagramBlock.Create(document.Root.Text)
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithPadding(options.Padding);

        foreach (var child in document.Root.Children) CopyCause(child, block.AddCause(child.Text));
        return block;
    }

    /// <summary>
    /// Wraps a Mermaid Ishikawa document in a visual artifact envelope.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidIshikawaDocument document, MermaidIshikawaRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidIshikawaRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-ishikawa" : options.Id!.Trim();
        var block = document.ToFishboneDiagramBlock(options);
        var artifact = VisualArtifact.Create(id, VisualArtifactKind.Mermaid, block);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = block.Title;
        artifact.Subtitle = block.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(block.Options.Size.Width, block.Options.Size.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.causes"] = block.Causes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.nodes"] = CountNodes(document.Root).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(FishboneDiagramBlock);
        artifact.Metadata["render.note"] = "Deterministic fishbone preview; Mermaid hand-drawn and exact browser layout are not runtime dependencies.";
        return artifact;
    }

    /// <summary>Renders a Mermaid Ishikawa document to static SVG.</summary>
    public static string ToSvg(this MermaidIshikawaDocument document, MermaidIshikawaRenderOptions? options = null) =>
        document.ToFishboneDiagramBlock(options).ToSvg();

    /// <summary>Renders a Mermaid Ishikawa document to static PNG.</summary>
    public static byte[] ToPng(this MermaidIshikawaDocument document, MermaidIshikawaRenderOptions? options = null) =>
        document.ToFishboneDiagramBlock(options).ToPng();

    private static void CopyCause(MermaidIshikawaNode source, FishboneCause target) {
        foreach (var child in source.Children) CopyCause(child, target.AddChild(child.Text));
    }

    private static int CountNodes(MermaidIshikawaNode? node) {
        if (node == null) return 0;
        var count = 1;
        foreach (var child in node.Children) count += CountNodes(child);
        return count;
    }

    private static string ResolveTitle(MermaidIshikawaDocument document, MermaidIshikawaRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        return "Mermaid Ishikawa";
    }

    private static string ResolveSubtitle(MermaidIshikawaDocument document, MermaidIshikawaRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid Ishikawa diagrams.
/// </summary>
public sealed class MermaidIshikawaRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 900;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 520;

    /// <summary>Gets or sets the inner preview padding.</summary>
    public double Padding { get; set; } = 30;
}
