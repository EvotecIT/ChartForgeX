using System;
using System.Collections.Generic;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid git graph diagram.
/// </summary>
public sealed class MermaidGitGraphDocument : MermaidDocument {
    /// <summary>Gets git graph statements retained in source order.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets parsed git graph branches.</summary>
    public List<MermaidGitGraphBranch> Branches { get; } = new();

    /// <summary>Gets parsed git graph commits.</summary>
    public List<MermaidGitGraphCommit> Commits { get; } = new();

    /// <summary>Gets configuration, theme, or unsupported statements retained for future support.</summary>
    public List<MermaidRawStatement> RetainedStatements { get; } = new();

    /// <summary>Gets or sets the diagram orientation token from the header.</summary>
    public string Direction { get; set; } = string.Empty;
}

/// <summary>
/// Describes one Mermaid git graph branch.
/// </summary>
public sealed class MermaidGitGraphBranch {
    private string _name;

    /// <summary>Initializes a Mermaid git graph branch.</summary>
    public MermaidGitGraphBranch(string name, int? order, MermaidSourceSpan span) {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        Order = order;
        Span = span;
    }

    /// <summary>Gets or sets the branch name.</summary>
    public string Name { get => _name; set => _name = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets the optional branch order.</summary>
    public int? Order { get; }

    /// <summary>Gets the source span for the branch statement.</summary>
    public MermaidSourceSpan Span { get; }
}

/// <summary>
/// Describes one Mermaid git graph commit.
/// </summary>
public sealed class MermaidGitGraphCommit {
    private string _id;
    private string _branchName;
    private string _label;
    private string _tag;
    private string _sourceCommitId;
    private readonly List<string> _parentIds = new();

    /// <summary>Initializes a Mermaid git graph commit.</summary>
    public MermaidGitGraphCommit(string id, string branchName, IEnumerable<string>? parentIds, GitGraphCommitType type, string label, string tag, string sourceCommitId, MermaidSourceSpan span) {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _branchName = branchName ?? throw new ArgumentNullException(nameof(branchName));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _tag = tag ?? throw new ArgumentNullException(nameof(tag));
        _sourceCommitId = sourceCommitId ?? throw new ArgumentNullException(nameof(sourceCommitId));
        if (parentIds != null) foreach (var parentId in parentIds) _parentIds.Add(parentId ?? throw new ArgumentNullException(nameof(parentIds)));
        Type = type;
        Span = span;
    }

    /// <summary>Gets or sets the commit id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the branch name.</summary>
    public string BranchName { get => _branchName; set => _branchName = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets parent commit ids.</summary>
    public IReadOnlyList<string> ParentIds => _parentIds;

    /// <summary>Gets the commit type.</summary>
    public GitGraphCommitType Type { get; }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the optional tag label.</summary>
    public string Tag { get => _tag; set => _tag = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the cherry-pick source commit id.</summary>
    public string SourceCommitId { get => _sourceCommitId; set => _sourceCommitId = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets the source span for the commit statement.</summary>
    public MermaidSourceSpan Span { get; }
}
