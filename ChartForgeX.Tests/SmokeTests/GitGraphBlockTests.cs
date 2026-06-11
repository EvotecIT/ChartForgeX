using System;
using ChartForgeX;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GitGraphBlockRendersStaticSvgAndPng() {
        var block = GitGraphBlock.Create()
            .WithTitle("Release History")
            .WithSize(760, 340)
            .AddBranch("main", 0)
            .AddBranch("develop", 1)
            .AddCommit("base", "main", type: GitGraphCommitType.Normal, tag: "v1")
            .AddCommit("work", "develop", new[] { "base" }, GitGraphCommitType.Highlight)
            .AddCommit("merge", "main", new[] { "base", "work" }, GitGraphCommitType.Merge, tag: "v2");

        var svg = block.ToSvg();
        var png = block.ToPng();

        Assert(svg.Contains("data-cfx-role=\"git-branch-line\"", StringComparison.Ordinal), "Git graph SVG should expose branch line roles.");
        Assert(svg.Contains("data-cfx-role=\"git-commit\"", StringComparison.Ordinal), "Git graph SVG should expose commit roles.");
        Assert(svg.Contains("data-commit-id=\"merge\"", StringComparison.Ordinal), "Git graph SVG should expose commit ids.");
        Assert(svg.Contains("data-cfx-role=\"git-tag\"", StringComparison.Ordinal), "Git graph SVG should render tag roles.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Git graph PNG rendering should emit a valid PNG.");
    }

    private static void GitGraphBlockRejectsUnknownParents() {
        var block = GitGraphBlock.Create()
            .AddBranch("main")
            .AddCommit("bad", "main", new[] { "missing" });

        AssertThrows<InvalidOperationException>(() => block.ToSvg(), "Git graph blocks should reject unknown parent ids instead of silently dropping edges.");
    }
}
