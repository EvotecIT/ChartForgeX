using System;
using System.Globalization;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid git graph diagrams.
/// </summary>
public static class MermaidGitGraphRendering {
    /// <summary>
    /// Converts a Mermaid git graph document into a reusable git graph visual block.
    /// </summary>
    public static GitGraphBlock ToGitGraphBlock(this MermaidGitGraphDocument document, MermaidGitGraphRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidGitGraphRenderOptions();
        var block = GitGraphBlock.Create()
            .WithTitle(ResolveTitle(options))
            .WithSubtitle(ResolveSubtitle(document, options))
            .WithSize(options.Width, options.Height)
            .WithPadding(options.Padding)
            .WithBranchLabels(options.ShowBranchLabels)
            .WithCommitLabels(options.ShowCommitLabels);

        foreach (var branch in document.Branches) block.AddBranch(branch.Name, branch.Order);
        foreach (var commit in document.Commits) block.AddCommit(commit.Id, commit.BranchName, commit.ParentIds, commit.Type, commit.Label, commit.Tag, commit.SourceCommitId);
        return block;
    }

    /// <summary>
    /// Wraps a Mermaid git graph document in a visual artifact envelope backed by a git graph block.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidGitGraphDocument document, MermaidGitGraphRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidGitGraphRenderOptions();
        var id = string.IsNullOrWhiteSpace(options.Id) ? "mermaid-gitgraph" : options.Id!.Trim();
        var block = document.ToGitGraphBlock(options);
        var artifact = VisualArtifact.Create(id, VisualArtifactKind.Mermaid, block);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = block.Title;
        artifact.Subtitle = block.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(block.Options.Size.Width, block.Options.Size.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.branches"] = document.Branches.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.commits"] = document.Commits.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(GitGraphBlock);
        return artifact;
    }

    /// <summary>Renders a Mermaid git graph document to static SVG.</summary>
    public static string ToSvg(this MermaidGitGraphDocument document, MermaidGitGraphRenderOptions? options = null) =>
        document.ToGitGraphBlock(options).ToSvg();

    /// <summary>Renders a Mermaid git graph document to static PNG.</summary>
    public static byte[] ToPng(this MermaidGitGraphDocument document, MermaidGitGraphRenderOptions? options = null) =>
        document.ToGitGraphBlock(options).ToPng();

    private static string ResolveTitle(MermaidGitGraphRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Title)) return options.Title!;
        return "Mermaid git graph";
    }

    private static string ResolveSubtitle(MermaidGitGraphDocument document, MermaidGitGraphRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(options.Subtitle)) return options.Subtitle!;
        return document.Direction.Length == 0 ? document.Header : document.Header + " " + document.Direction;
    }
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid git graph diagrams.
/// </summary>
public sealed class MermaidGitGraphRenderOptions {
    /// <summary>Gets or sets the artifact id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the artifact title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the artifact subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 980;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 460;

    /// <summary>Gets or sets the inner preview padding.</summary>
    public double Padding { get; set; } = 30;

    /// <summary>Gets or sets whether branch labels and lane lines are rendered.</summary>
    public bool ShowBranchLabels { get; set; } = true;

    /// <summary>Gets or sets whether commit labels are rendered.</summary>
    public bool ShowCommitLabels { get; set; } = true;
}
