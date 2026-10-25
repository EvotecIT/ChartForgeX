using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>Maps source tree notation to the reusable native directory-row owner.</summary>
public static class MermaidTreeViewRendering {
    /// <summary>Creates measured rows in source order without turning filenames into network nodes.</summary>
    public static TreeViewBlock ToTreeViewBlock(this MermaidTreeViewDocument document, MermaidTreeViewRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document)); options ??= new MermaidTreeViewRenderOptions();
        var fallback = document.Roots.Count == 1 ? document.Roots[0].Label : "Mermaid tree view";
        var block = TreeViewBlock.Create().WithTitle(MermaidHeading.Title(document, options.Title, null, fallback))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle));
        block.ShowIcons = options.ShowIcons ?? document.Configuration.TreeView?.ShowIcons ?? false;
        var seen = new HashSet<MermaidTreeViewNode>();
        foreach (var node in document.Nodes) {
            if (!seen.Add(node)) throw new ArgumentException("Tree view rows must be distinct.", nameof(document));
            if (node.Parent != null && !seen.Contains(node.Parent)) throw new ArgumentException("Tree view parents must precede their children in the document.", nameof(document));
            var icon = node.IconReference == null ? TreeViewIcon.Auto : node.IconReference == "" || node.IconReference == "none" ? TreeViewIcon.None
                : node.IconReference == "file" ? TreeViewIcon.File : node.IconReference == "folder" ? TreeViewIcon.Folder : TreeViewIcon.Unknown;
            var highlight = false; foreach (var name in node.Classes) if (name == "highlight") highlight = true;
            block.AddItem(new TreeViewItem(node.Id, node.Label, node.Parent?.Id, node.Description,
                node.IsDirectory ? TreeViewItemKind.Directory : TreeViewItemKind.Auto, icon, highlight, node.IconReference, node.Classes));
        }
        MermaidPresentation.Apply(block, document, options.Presentation);
        return block.WithNaturalSize(options.Width, options.Height);
    }
    /// <summary>Wraps native rows in the existing bounded Mermaid export policy.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidTreeViewDocument document, MermaidTreeViewRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document)); options ??= new MermaidTreeViewRenderOptions();
        var block = document.ToTreeViewBlock(options);
        var artifact = VisualArtifact.Create(string.IsNullOrWhiteSpace(options.Id) ? "mermaid-treeview" : options.Id!.Trim(), VisualArtifactKind.Mermaid, block);
        artifact.Title = block.Title; artifact.Subtitle = block.Subtitle; artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.NaturalSize = new VisualArtifactSize(block.Options.Size.Width, block.Options.Size.Height);
        artifact.HtmlSizing = VisualArtifactHtmlSizing.PreserveSize;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString(); artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.nodes"] = document.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.roots"] = document.Roots.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TreeViewBlock);
        return MermaidPresentation.Apply(artifact, document);
    }
    /// <summary>Renders the shared static SVG row scene.</summary>
    public static string ToSvg(this MermaidTreeViewDocument document, MermaidTreeViewRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();
    /// <summary>Renders native PNG through the same mark and positioned-text scene.</summary>
    public static byte[] ToPng(this MermaidTreeViewDocument document, MermaidTreeViewRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();
}

/// <summary>Defines the fixed native width and minimum natural height of a directory tree.</summary>
public sealed class MermaidTreeViewRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the width in logical pixels, from one through 8192.</summary>
    public int Width { get; set; } = 720;
    /// <summary>Gets or sets the minimum natural height, from zero through 8192. Readable rows can require more.</summary>
    public int Height { get; set; } = 320;
    /// <summary>Gets or sets the host override for automatic file/folder icons. Explicit per-row icons are independent.</summary>
    public bool? ShowIcons { get; set; }
}
