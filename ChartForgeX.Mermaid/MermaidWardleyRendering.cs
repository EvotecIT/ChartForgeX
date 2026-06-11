using System;
using System.Globalization;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid Wardley maps.
/// </summary>
public static class MermaidWardleyRendering {
    /// <summary>
    /// Converts a Mermaid Wardley document into a reusable Wardley map block.
    /// </summary>
    public static WardleyMapBlock ToWardleyMapBlock(this MermaidWardleyDocument document, MermaidWardleyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidWardleyRenderOptions();
        var block = WardleyMapBlock.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(ResolveWidth(document, options), ResolveHeight(document, options))
            .WithPadding(options.Padding);

        block.SetStages(document.Stages);
        foreach (var node in document.Nodes) {
            var mapped = block.AddNode(node.Id, node.Id, node.Visibility, node.Evolution, node.Kind);
            mapped.Inertia = node.Inertia;
            mapped.Strategy = node.Strategy;
            mapped.LabelOffsetX = node.LabelOffsetX;
            mapped.LabelOffsetY = node.LabelOffsetY;
        }

        foreach (var link in document.Links) block.AddLink(link.From, link.To, link.Label, link.Dashed, link.Flow);
        foreach (var evolution in document.Evolutions) block.AddEvolution(evolution.NodeId, evolution.TargetEvolution);
        foreach (var note in document.Notes) block.AddNote(note.Text, note.Visibility, note.Evolution);
        foreach (var annotation in document.Annotations) block.AddAnnotation(annotation.Number, annotation.Text, annotation.Visibility, annotation.Evolution);
        foreach (var marker in document.Markers) block.AddMarker(marker.Label, marker.Visibility, marker.Evolution, marker.Kind);
        foreach (var pipeline in document.Pipelines) {
            var mapped = block.AddPipeline(pipeline.ParentId);
            foreach (var component in pipeline.Components) mapped.AddComponent(component.Label, component.Evolution);
        }

        return block;
    }

    /// <summary>
    /// Wraps a Mermaid Wardley document in a visual artifact envelope.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidWardleyDocument document, MermaidWardleyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidWardleyRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-wardley" : options.Id!.Trim();
        var block = document.ToWardleyMapBlock(options);
        var artifact = VisualArtifact.Create(id, VisualArtifactKind.Mermaid, block);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = block.Title;
        artifact.Subtitle = block.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(block.Options.Size.Width, block.Options.Size.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.nodes"] = document.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.links"] = document.Links.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.evolves"] = document.Evolutions.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.pipelines"] = document.Pipelines.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(WardleyMapBlock);
        artifact.Metadata["render.note"] = "Deterministic Wardley map preview; browser-exact Mermaid styling and all advanced grammar are not runtime dependencies.";
        return artifact;
    }

    /// <summary>Renders a Mermaid Wardley document to static SVG.</summary>
    public static string ToSvg(this MermaidWardleyDocument document, MermaidWardleyRenderOptions? options = null) =>
        document.ToWardleyMapBlock(options).ToSvg();

    /// <summary>Renders a Mermaid Wardley document to static PNG.</summary>
    public static byte[] ToPng(this MermaidWardleyDocument document, MermaidWardleyRenderOptions? options = null) =>
        document.ToWardleyMapBlock(options).ToPng();

    private static string ResolveTitle(MermaidWardleyDocument document, MermaidWardleyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid Wardley";
    }

    private static string ResolveSubtitle(MermaidWardleyDocument document, MermaidWardleyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static int ResolveWidth(MermaidWardleyDocument document, MermaidWardleyRenderOptions options) => options.Width > 0 ? options.Width : document.CanvasWidth ?? 900;

    private static int ResolveHeight(MermaidWardleyDocument document, MermaidWardleyRenderOptions options) => options.Height > 0 ? options.Height : document.CanvasHeight ?? 600;
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid Wardley maps.
/// </summary>
public sealed class MermaidWardleyRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width. Zero uses the Mermaid size statement or renderer default.</summary>
    public int Width { get; set; }

    /// <summary>Gets or sets the natural preview height. Zero uses the Mermaid size statement or renderer default.</summary>
    public int Height { get; set; }

    /// <summary>Gets or sets the inner preview padding.</summary>
    public double Padding { get; set; } = 30;
}
