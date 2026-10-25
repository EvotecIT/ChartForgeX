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
        var source = document.Configuration.GitGraph;
        var block = GitGraphBlock.Create()
            .WithTitle(MermaidHeading.Title(document, options.Title, null, "Mermaid git graph"))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithSize(options.Width, options.Height)
            .WithPadding(options.Padding)
            .WithBranchLabels(options.ShowBranchLabels ?? source?.ShowBranches ?? true)
            .WithCommitLabels(options.ShowCommitLabels ?? source?.ShowCommitLabels ?? true)
            .WithDirection(options.Direction ?? GitGraphDirectionFromSource(document.Direction))
            .WithParallelCommits(options.ParallelCommits ?? source?.ParallelCommits ?? false)
            .WithRotatedCommitLabels(options.RotateCommitLabels ?? source?.RotateCommitLabels ?? false);

        var ordered = new System.Collections.Generic.List<(MermaidGitGraphBranch Branch, int Index, double Order)>();
        for (var index = 0; index < document.Branches.Count; index++) {
            var branch = document.Branches[index];
            var order = branch.Order.HasValue ? branch.Order.Value : double.Parse("0." + index.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            ordered.Add((branch, index, order));
        }
        ordered.Sort((left, right) => { var comparison = left.Order.CompareTo(right.Order); return comparison == 0 ? left.Index.CompareTo(right.Index) : comparison; });
        var ranks = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < ordered.Count; index++) ranks[ordered[index].Branch.Name] = index;
        foreach (var branch in document.Branches) block.AddBranch(branch.Name, ranks[branch.Name]);
        foreach (var commit in document.Commits) block.AddCommit(commit.Id, commit.BranchName, commit.ParentIds, commit.Type, commit.Label, commit.Tag, commit.SourceCommitId);
        return MermaidPresentation.Apply(block, document, options.Presentation);
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
        for (var index = 0; index < document.Commits.Count; index++) {
            var parent = document.Commits[index].SourceParentCommitId;
            if (parent.Length > 0) artifact.Metadata["mermaid.gitgraph.commit." + index.ToString(CultureInfo.InvariantCulture) + ".sourceParent"] = parent;
        }
        artifact.Metadata["render.model"] = nameof(GitGraphBlock);
        return MermaidPresentation.Apply(artifact, document);
    }

    private static GitGraphDirection GitGraphDirectionFromSource(string direction) => direction switch {
        "" => GitGraphDirection.LeftToRight, "LR" => GitGraphDirection.LeftToRight,
        "TB" => GitGraphDirection.TopToBottom, "BT" => GitGraphDirection.BottomToTop,
        _ => throw new ArgumentException("Git graph direction must be LR, TB or BT.", nameof(direction))
    };

    /// <summary>Renders a Mermaid git graph document to static SVG.</summary>
    public static string ToSvg(this MermaidGitGraphDocument document, MermaidGitGraphRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToSvg();

    /// <summary>Renders a Mermaid git graph document to static PNG.</summary>
    public static byte[] ToPng(this MermaidGitGraphDocument document, MermaidGitGraphRenderOptions? options = null) =>
        document.ToVisualArtifact(options).ToPng();
}

/// <summary>
/// Defines conversion and rendering defaults for Mermaid git graph diagrams.
/// </summary>
public sealed class MermaidGitGraphRenderOptions : MermaidDiagramRenderOptions {
    /// <summary>Gets or sets the natural preview width.</summary>
    public int Width { get; set; } = 980;

    /// <summary>Gets or sets the natural preview height.</summary>
    public int Height { get; set; } = 460;

    /// <summary>Gets or sets the inner preview padding.</summary>
    public double Padding { get; set; } = 30;

    /// <summary>Gets or sets whether branch labels and lane lines are rendered; null uses source configuration, then true.</summary>
    public bool? ShowBranchLabels { get; set; }

    /// <summary>Gets or sets whether commit labels are rendered; null uses source configuration, then true.</summary>
    public bool? ShowCommitLabels { get; set; }

    /// <summary>Gets or sets a host direction override; null uses the source header, then LR.</summary>
    public GitGraphDirection? Direction { get; set; }
    /// <summary>Gets or sets a host parallel-placement override; null uses source configuration, then false.</summary>
    public bool? ParallelCommits { get; set; }
    /// <summary>Gets or sets a host caption-rotation override; null uses source configuration, then false.</summary>
    public bool? RotateCommitLabels { get; set; }
}
