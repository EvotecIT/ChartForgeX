using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A deterministic visual block for product-neutral commit and branch graphs.
/// </summary>
public sealed class GitGraphBlock : VisualBlock<GitGraphBlock> {
    private readonly List<GitGraphBranch> _branches = new();
    private readonly List<GitGraphCommit> _commits = new();

    /// <summary>Gets branches in declaration order.</summary>
    public IReadOnlyList<GitGraphBranch> Branches => _branches;

    /// <summary>Gets commits in source order.</summary>
    public IReadOnlyList<GitGraphCommit> Commits => _commits;

    /// <summary>Gets or sets whether branch labels and lane lines are rendered.</summary>
    public bool ShowBranchLabels { get; set; } = true;

    /// <summary>Gets or sets whether commit labels are rendered.</summary>
    public bool ShowCommitLabels { get; set; } = true;

    /// <summary>Gets a concise accessibility label.</summary>
    public override string AccessibleName => Title.Length == 0 ? "Git graph" : Title;

    /// <summary>Creates a git graph block.</summary>
    public static GitGraphBlock Create() => new();

    /// <summary>Adds a branch lane.</summary>
    public GitGraphBlock AddBranch(string name, int? order = null) {
        _branches.Add(new GitGraphBranch(name, order));
        return this;
    }

    /// <summary>Adds a commit on a branch.</summary>
    public GitGraphBlock AddCommit(string id, string branchName, IEnumerable<string>? parentIds = null, GitGraphCommitType type = GitGraphCommitType.Normal, string label = "", string tag = "", string sourceCommitId = "") {
        _commits.Add(new GitGraphCommit(id, branchName, parentIds, type, label, tag, sourceCommitId));
        return this;
    }

    /// <summary>Sets whether branch labels are rendered.</summary>
    public GitGraphBlock WithBranchLabels(bool visible = true) { ShowBranchLabels = visible; return this; }

    /// <summary>Sets whether commit labels are rendered.</summary>
    public GitGraphBlock WithCommitLabels(bool visible = true) { ShowCommitLabels = visible; return this; }
}

/// <summary>
/// Describes one lane in a git graph visual block.
/// </summary>
public sealed class GitGraphBranch {
    private string _name;

    /// <summary>Initializes a git graph branch.</summary>
    public GitGraphBranch(string name, int? order = null) {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        Order = order;
    }

    /// <summary>Gets or sets the branch name.</summary>
    public string Name { get => _name; set => _name = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional rendering order.</summary>
    public int? Order { get; set; }
}

/// <summary>
/// Describes one commit in a git graph visual block.
/// </summary>
public sealed class GitGraphCommit {
    private string _id;
    private string _branchName;
    private string _label;
    private string _tag;
    private string _sourceCommitId;
    private readonly List<string> _parentIds = new();

    /// <summary>Initializes a git graph commit.</summary>
    public GitGraphCommit(string id, string branchName, IEnumerable<string>? parentIds = null, GitGraphCommitType type = GitGraphCommitType.Normal, string label = "", string tag = "", string sourceCommitId = "") {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _branchName = branchName ?? throw new ArgumentNullException(nameof(branchName));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _tag = tag ?? throw new ArgumentNullException(nameof(tag));
        _sourceCommitId = sourceCommitId ?? throw new ArgumentNullException(nameof(sourceCommitId));
        Type = type;
        if (parentIds != null) foreach (var parentId in parentIds) _parentIds.Add(parentId ?? throw new ArgumentNullException(nameof(parentIds)));
    }

    /// <summary>Gets or sets the stable commit id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the branch lane name.</summary>
    public string BranchName { get => _branchName; set => _branchName = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets parent commit ids.</summary>
    public IReadOnlyList<string> ParentIds => _parentIds;

    /// <summary>Gets or sets the commit type.</summary>
    public GitGraphCommitType Type { get; set; }

    /// <summary>Gets or sets the optional display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional tag label.</summary>
    public string Tag { get => _tag; set => _tag = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the source commit id for cherry-pick commits.</summary>
    public string SourceCommitId { get => _sourceCommitId; set => _sourceCommitId = value ?? throw new ArgumentNullException(nameof(value)); }
}

/// <summary>
/// Identifies the visual role of a git graph commit.
/// </summary>
public enum GitGraphCommitType {
    /// <summary>A normal commit.</summary>
    Normal,
    /// <summary>A highlighted commit.</summary>
    Highlight,
    /// <summary>A reverse-style commit.</summary>
    Reverse,
    /// <summary>A merge commit.</summary>
    Merge,
    /// <summary>A cherry-pick commit.</summary>
    CherryPick
}
