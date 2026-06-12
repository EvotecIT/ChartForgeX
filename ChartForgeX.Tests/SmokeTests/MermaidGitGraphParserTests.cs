using System;
using System.Text;
using ChartForgeX;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesGitGraphBranchesMergesAndCherryPicks() {
        const string source = @"gitGraph LR:
  commit id: ""base"" tag: ""v1""
  branch develop order: 1
  checkout develop
  commit id: ""work"" type: HIGHLIGHT
  checkout main
  merge develop id: ""merge"" tag: ""v2""
  branch hotfix order: 2
  checkout hotfix
  cherry-pick id: ""work""
  checkout main
  merge hotfix";

        var result = new MermaidParser().ParseGitGraph(source);

        Assert(!result.HasErrors, "Mermaid gitGraph parser should parse branches, merges, and cherry-picks: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid gitGraph parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.GitGraph, "Mermaid gitGraph parser should produce a git graph document.");
        Assert(document.Direction == "LR", "Mermaid gitGraph parser should preserve header orientation.");
        Assert(document.Branches.Count == 3, "Mermaid gitGraph parser should include main and declared branches.");
        Assert(document.Commits.Count == 5, "Mermaid gitGraph parser should parse commits, merges, and cherry-picks.");
        Assert(document.Commits[1].Type == GitGraphCommitType.Highlight, "Mermaid gitGraph parser should preserve commit type attributes.");
        Assert(document.Commits[2].ParentIds.Count == 2 && document.Commits[2].Tag == "v2", "Mermaid gitGraph merge commits should link both parents and preserve tags.");
        Assert(document.Commits[3].Type == GitGraphCommitType.CherryPick && document.Commits[3].SourceCommitId == "work", "Mermaid gitGraph cherry-pick commits should retain the source commit id.");
        Assert(document.Statements.Count == 11, "Mermaid gitGraph parser should retain raw body statements.");
    }

    private static void MermaidGitGraphConvertsToVisualBlockArtifactAndRenders() {
        const string source = @"gitGraph
  commit id: ""base""
  branch develop
  checkout develop
  commit id: ""work""
  checkout main
  merge develop id: ""merge""";

        var result = new MermaidParser().ParseGitGraph(source);
        Assert(!result.HasErrors, "Mermaid gitGraph parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid gitGraph parser should produce a document.");
        var block = document.ToGitGraphBlock(new MermaidGitGraphRenderOptions { Id = "release-history", Title = "Release History", Width = 760, Height = 340 });
        Assert(block.Title == "Release History", "Mermaid gitGraph conversion should preserve caller-provided titles.");
        Assert(block.Branches.Count == 2 && block.Commits.Count == 3, "Mermaid gitGraph conversion should map branches and commits into a reusable git graph block.");

        var artifact = document.ToVisualArtifact(new MermaidGitGraphRenderOptions { Id = "release-history" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid gitGraph visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is GitGraphBlock, "Mermaid gitGraph visual artifact should carry the git graph model.");
        Assert(artifact.Metadata["mermaid.branches"] == "2" && artifact.Metadata["mermaid.commits"] == "3", "Mermaid gitGraph artifacts should expose model counts.");
        Assert(artifact.Metadata["render.model"] == nameof(GitGraphBlock), "Mermaid gitGraph artifacts should expose the visual block render model.");

        var svg = document.ToSvg(new MermaidGitGraphRenderOptions { Id = "release-history" });
        var png = document.ToPng(new MermaidGitGraphRenderOptions { Id = "release-history" });
        Assert(svg.Contains("data-cfx-role=\"git-commit\"", StringComparison.Ordinal), "Mermaid gitGraph SVG rendering should include commits.");
        Assert(svg.Contains("data-cfx-role=\"git-commit-edge\"", StringComparison.Ordinal), "Mermaid gitGraph SVG rendering should include commit edges.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid gitGraph PNG rendering should emit a valid PNG.");
    }

    private static void MermaidGitGraphRejectsUnknownCheckout() {
        const string source = @"gitGraph
  checkout missing
  commit";

        var result = new MermaidParser().ParseGitGraph(source);

        Assert(result.HasErrors, "Mermaid gitGraph parser should reject unknown checkouts.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("unknown branch", StringComparison.Ordinal)), "Invalid gitGraph checkout diagnostics should name the missing branch contract.");
    }

    private static void MermaidGitGraphSupportsSwitchAlias() {
        const string source = @"gitGraph
  commit id: ""base""
  branch develop
  switch develop
  commit id: ""work""";

        var result = new MermaidParser().ParseGitGraph(source);

        Assert(!result.HasErrors, "Mermaid gitGraph parser should treat switch as a checkout alias: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid gitGraph parser should produce a document.");
        Assert(document.Commits.Count == 2 && document.Commits[1].BranchName == "develop", "Mermaid gitGraph switch statements should move the current branch before later commits.");
    }

    private static void MermaidGitGraphRejectsSameBranchCherryPicks() {
        const string source = @"gitGraph
  commit id: ""base""
  cherry-pick id: ""base""";

        var result = new MermaidParser().ParseGitGraph(source);

        Assert(result.HasErrors, "Mermaid gitGraph parser should reject cherry-picks from the current branch.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("different branch", StringComparison.Ordinal)), "Invalid gitGraph cherry-pick diagnostics should explain the source branch contract.");
    }

    private static void MermaidGitGraphRejectsRenderLimitOverflowDuringParsing() {
        var branches = new StringBuilder("gitGraph\n  commit id: \"base\"\n");
        for (var index = 0; index < 128; index++) branches.Append("  branch b").Append(index).Append('\n');

        var branchResult = new MermaidParser().ParseGitGraph(branches.ToString());

        Assert(branchResult.HasErrors, "Mermaid gitGraph parser should reject branch counts beyond the renderer limit.");
        Assert(branchResult.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("128", StringComparison.Ordinal) && diagnostic.Message.Contains("branches", StringComparison.Ordinal)), "Oversized gitGraph branch diagnostics should name the renderable branch cap.");

        var commits = new StringBuilder("gitGraph\n");
        for (var index = 0; index <= 10000; index++) commits.Append("  commit id: \"c").Append(index).Append("\"\n");

        var commitResult = new MermaidParser().ParseGitGraph(commits.ToString());

        Assert(commitResult.HasErrors, "Mermaid gitGraph parser should reject commit counts beyond the renderer limit.");
        Assert(commitResult.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("10000", StringComparison.Ordinal) && diagnostic.Message.Contains("commits", StringComparison.Ordinal)), "Oversized gitGraph commit diagnostics should name the renderable commit cap.");
    }
}
