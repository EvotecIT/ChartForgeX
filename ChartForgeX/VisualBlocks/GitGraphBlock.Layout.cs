namespace ChartForgeX.VisualBlocks;

/// <summary>The physical direction in which commit ancestry progresses.</summary>
public enum GitGraphDirection {
    /// <summary>Earlier commits appear to the left.</summary>
    LeftToRight,
    /// <summary>Earlier commits appear above their descendants.</summary>
    TopToBottom,
    /// <summary>Earlier commits appear below their descendants.</summary>
    BottomToTop
}

public sealed partial class GitGraphBlock {
    private GitGraphDirection _direction;
    /// <summary>Gets or sets commit progression. Branch and commit source order remains unchanged.</summary>
    public GitGraphDirection Direction { get => _direction; set { VisualBlockGuards.EnumDefined(value, nameof(value)); _direction = value; } }
    /// <summary>Gets or sets whether independent branches share ancestry-depth columns. Parents must precede children in source order; commits on one lane remain distinct.</summary>
    public bool ParallelCommits { get; set; }
    /// <summary>Gets or sets whether commit captions are tilted 45 degrees. Tags and branch captions stay upright.</summary>
    public bool RotateCommitLabels { get; set; }
    /// <summary>Sets physical ancestry progression without changing source or parent identities.</summary>
    public GitGraphBlock WithDirection(GitGraphDirection direction) { Direction = direction; return this; }
    /// <summary>Sets ancestry-depth placement for independent branches.</summary>
    public GitGraphBlock WithParallelCommits(bool parallel = true) { ParallelCommits = parallel; return this; }
    /// <summary>Sets tilted commit captions without rotating the graph geometry.</summary>
    public GitGraphBlock WithRotatedCommitLabels(bool rotated = true) { RotateCommitLabels = rotated; return this; }
}
