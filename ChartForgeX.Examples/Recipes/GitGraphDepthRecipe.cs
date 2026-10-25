/// <summary>One dependency-free native block retains complete source ancestry in three physical directions.</summary>
public static class GitGraphDepthRecipe {
    /// <summary>Creates compact/wide LR, TB or BT exports; the caller may save the returned block directly as SVG or native PNG.</summary>
    public static GitGraphBlock Create(string variant = "wide-lr", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide-lr" or "compact-lr" or "wide-tb" or "compact-tb" or "wide-bt" or "compact-bt"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var direction = variant.EndsWith("-tb", StringComparison.Ordinal) ? GitGraphDirection.TopToBottom
            : variant.EndsWith("-bt", StringComparison.Ordinal) ? GitGraphDirection.BottomToTop : GitGraphDirection.LeftToRight;
        return GitGraphBlock.Create().WithTitle("Release ancestry").WithSize(variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 1100, 620)
            .WithTheme(VisualTheme.Graphite(), mode).WithDirection(direction).WithParallelCommits()
            .AddBranch("trunk", 2).AddBranch("feature", 1).AddBranch("consumer", 0)
            .AddCommit("base", "trunk", tag: "v1")
            .AddCommit("feature-1", "feature", new[] { "base" }, GitGraphCommitType.Highlight)
            .AddCommit("main-next", "trunk", new[] { "base" })
            .AddCommit("joined", "trunk", new[] { "main-next", "feature-1" }, GitGraphCommitType.Merge, tag: "v2")
            .AddCommit("consumer-1", "consumer", new[] { "joined" })
            .AddCommit("picked", "consumer", new[] { "consumer-1", "joined" }, GitGraphCommitType.CherryPick,
                label: "joined", tag: "picked", sourceCommitId: "joined");
    }
}
