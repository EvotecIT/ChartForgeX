using System;
using System.Globalization;
using ChartForgeX;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid packet diagrams.
/// </summary>
public static class MermaidPacketRendering {
    /// <summary>
    /// Converts a Mermaid packet document into a reusable packet-layout visual block.
    /// </summary>
    public static PacketLayoutBlock ToPacketLayoutBlock(this MermaidPacketDocument document, MermaidPacketRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidPacketRenderOptions();
        var block = PacketLayoutBlock.Create()
            .WithTitle(ResolveTitle(document, options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithPadding(options.Padding)
            .WithBitsPerRow(options.BitsPerRow)
            .WithBitNumbers(options.ShowBitNumbers);

        foreach (var field in document.Fields) block.AddField(field.StartBit, field.EndBit, field.Label);
        return block;
    }

    /// <summary>
    /// Wraps a Mermaid packet document in a visual artifact envelope backed by a packet-layout block.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidPacketDocument document, MermaidPacketRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidPacketRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-packet" : options.Id!.Trim();
        var block = document.ToPacketLayoutBlock(options);
        var artifact = VisualArtifact.Create(id, VisualArtifactKind.Mermaid, block);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = block.Title;
        artifact.Subtitle = block.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(block.Options.Size.Width, block.Options.Size.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.fields"] = document.Fields.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.bits"] = TotalBits(document).ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(PacketLayoutBlock);
        return artifact;
    }

    /// <summary>Renders a Mermaid packet document to static SVG.</summary>
    public static string ToSvg(this MermaidPacketDocument document, MermaidPacketRenderOptions? options = null) =>
        document.ToPacketLayoutBlock(options).ToSvg();

    /// <summary>Renders a Mermaid packet document to static PNG.</summary>
    public static byte[] ToPng(this MermaidPacketDocument document, MermaidPacketRenderOptions? options = null) =>
        document.ToPacketLayoutBlock(options).ToPng();

    private static string ResolveTitle(MermaidPacketDocument document, MermaidPacketRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        if (!string.IsNullOrWhiteSpace(document.Title)) return document.Title!;
        return "Mermaid packet";
    }

    private static string ResolveSubtitle(MermaidPacketDocument document, MermaidPacketRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Header;
    }

    private static int TotalBits(MermaidPacketDocument document) =>
        document.Fields.Count == 0 ? 0 : document.Fields[document.Fields.Count - 1].EndBit + 1;
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid packet diagrams.
/// </summary>
public sealed class MermaidPacketRenderOptions {
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

    /// <summary>Gets or sets how many bits are rendered per visual row.</summary>
    public int BitsPerRow { get; set; } = 32;

    /// <summary>Gets or sets whether bit numbers are rendered above fields.</summary>
    public bool ShowBitNumbers { get; set; } = true;
}
