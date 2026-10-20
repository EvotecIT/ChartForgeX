using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGitGraphValidationTests {
    [Theory]
    [InlineData("commit banana")]
    [InlineData("commit idd: \"lost\"")]
    [InlineData("commit type: BANANA")]
    [InlineData("commit type: normal")]
    [InlineData("commit type: \"NORMAL\"")]
    [InlineData("commit id: bare")]
    [InlineData("commit id: \"unfinished")]
    [InlineData("commit id: \"complete\"tail")]
    [InlineData("branch work order: -1")]
    [InlineData("branch work order: 1.5")]
    [InlineData("branch work order:")]
    [InlineData("branch work extra: \"lost\"")]
    [InlineData("checkout main trailing")]
    [InlineData("merge work type: BANANA")]
    [InlineData("cherry-pick id: \"work\" type: REVERSE")]
    [InlineData("unknown-command")]
    public void InvalidArgumentsCannotSilentlyProduceAnExportableGraph(string statement) {
        var source = "gitGraph\ncommit id: \"base\"\nbranch work\ncommit id: \"work\"\ncheckout main\n  " + statement;
        AssertInvalidSource(source, 6, 3);
    }

    [Theory]
    [InlineData("gitgraph-missing-merge-parent.mmd", 9)]
    [InlineData("gitgraph-invalid-merge-parent.mmd", 9)]
    [InlineData("gitgraph-same-head-merge.mmd", 5)]
    [InlineData("gitgraph-empty-merge-destination.mmd", 5)]
    [InlineData("gitgraph-unknown-attribute.mmd", 3)]
    [InlineData("gitgraph-invalid-order.mmd", 3)]
    public void ReferenceRejectedGraphOperationsFailThroughDirectAndFencePaths(string fixture, int line) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "invalid", fixture));
        AssertInvalidSource(source, line, 3);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" type: NORMAL")]
    [InlineData(" type: HIGHLIGHT")]
    [InlineData(" type: REVERSE")]
    public void MergeParentRequirementUsesAncestryIndependentlyFromDisplayedType(string style) {
        var source = MergeSource(style);
        AssertInvalidSource(source + "  cherry-pick id: \"merged\"", 9, 3);
        var valid = new MermaidParser().ParseGitGraph(source + "  cherry-pick id: \"merged\" parent: \"work\"");
        Assert.Empty(valid.Diagnostics);
        var picked = valid.Document!.Commits[^1];
        Assert.Equal("work", picked.SourceParentCommitId);
        Assert.Equal(new[] { "consumer", "merged" }, picked.ParentIds);
    }

    [Fact]
    public void ChosenSourceParentAndGraphEdgesSurviveSourceAndArtifactHandoff() {
        var path = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "gitgraph-cherry-pick-parent.mmd");
        var source = File.ReadAllText(path);
        var parsed = new MermaidParser().ParseGitGraph(source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        var picked = document.Commits[^1];
        Assert.Equal(GitGraphCommitType.CherryPick, picked.Type);
        Assert.Equal("merged", picked.SourceCommitId);
        Assert.Equal("base", picked.SourceParentCommitId);
        Assert.Equal(new[] { "consumer", "merged" }, picked.ParentIds);
        Assert.Equal("cherry-pick:merged|parent:base", picked.Tag);

        // The source-preserving AST can be serialized without losing selected-parent meaning and reparsed.
        using var serialized = JsonDocument.Parse(JsonSerializer.Serialize(document));
        Assert.Equal("base", serialized.RootElement.GetProperty("Commits")[4].GetProperty("SourceParentCommitId").GetString());
        var reparsed = new MermaidParser().ParseGitGraph(serialized.RootElement.GetProperty("SourceText").GetString()!);
        Assert.Empty(reparsed.Diagnostics);
        Assert.Equal(picked.SourceParentCommitId, reparsed.Document!.Commits[^1].SourceParentCommitId);

        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        Assert.Equal("base", artifact.Metadata["mermaid.gitgraph.commit.4.sourceParent"]);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Contains(restored.Extensions, item => item.Value == "base");
        var block = Assert.IsType<GitGraphBlock>(artifact.Model);
        Assert.Equal(picked.ParentIds, block.Commits[^1].ParentIds);
        var svg = XDocument.Parse(artifact.ToSvg());
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-parent-id") == "merged"
            && (string?)node.Attribute("data-commit-id") == picked.Id);
        Assert.Equal(picked.Tag, block.Commits[^1].Tag);
        var fittedTag = Assert.Single(svg.Descendants(), node => node.Name.LocalName == "text"
            && (string?)node.Attribute("data-cfx-role") == "git-tag-label");
        Assert.StartsWith("cherry-pick:", fittedTag.Value);
        Assert.NotEmpty(artifact.ToPng());

        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fencedArtifact = Assert.Single(fenced.Artifacts);
        Assert.Equal("base", fencedArtifact.Metadata["mermaid.gitgraph.commit.4.sourceParent"]);
    }

    [Fact]
    public void ValidValuesKeepNormalTypesMessagesEscapesAndAllTagCaptions() {
        const string source = "gitGraph\ncommit id:\"base\" type:NORMAL msg:\"Initial \\\"release\\\"\" tag:\"v1\" tag:'stable'\n"
            + "branch 'work branch' order:1\ncommit id:\"work\" type:HIGHLIGHT\ncheckout main\n"
            + "cherry-pick id:\"work\" parent:\"base\" tag:\"Reviewed\"";
        var parsed = new MermaidParser().ParseGitGraph(source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal(GitGraphCommitType.Normal, document.Commits[0].Type);
        Assert.Equal("Initial \"release\"", document.Commits[0].Label);
        Assert.Equal("v1, stable", document.Commits[0].Tag);
        Assert.Equal("work branch", document.Branches[1].Name);
        Assert.Equal("base", document.Commits[^1].SourceParentCommitId);
        Assert.Equal("Reviewed", document.Commits[^1].Tag);
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal("base", rendered.Artifact!.Metadata["mermaid.gitgraph.commit.2.sourceParent"]);
    }

    [Fact]
    public void ParentSelectionAlsoValidatesOrdinarySourceAncestry() {
        var source = "gitGraph\ncommit id: \"base\"\nbranch work\ncommit id: \"work\"\ncheckout main\n  cherry-pick id: \"work\" parent: \"work\"";
        AssertInvalidSource(source, 6, 3);
    }

    [Fact]
    public void SemicolonCommandsRetainQuotedBoundariesAndExactLocations() {
        const string source = "gitGraph\n  commit id:\"base;one\"; branch work; commit id:\"work\" msg:\"Quoted; message\";\n  checkout main; cherry-pick id:\"work\";";
        var parsed = new MermaidParser().ParseGitGraph(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "base;one", "work" }, parsed.Document!.Commits.Take(2).Select(commit => commit.Id));
        Assert.Equal("Quoted; message", parsed.Document.Commits[1].Label);
        Assert.Equal(5, parsed.Document.Statements.Count);
        Assert.Equal(25, parsed.Document.Statements[1].Span.Column);
        AssertInvalidSource("gitGraph\n  commit; commit type: WRONG;", 2, 11);
    }

    private static string MergeSource(string style) => "gitGraph\n  commit id: \"base\"\n  branch work\n  commit id: \"work\"\n"
        + "  checkout main\n  merge work id: \"merged\"" + style + "\n  branch consumer\n  commit id: \"consumer\"\n";

    private static void AssertInvalidSource(string source, int line, int column) {
        var parsed = new MermaidParser().ParseGitGraph(source);
        Assert.True(parsed.HasErrors);
        var error = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code);
        Assert.Equal(MermaidDiagnosticSeverity.Error, error.Severity);
        Assert.Equal((line, column), (error.Span.Line, error.Span.Column));
        var direct = MermaidRenderer.Render(source);
        Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, diagnostic => diagnostic.Code == error.Code && diagnostic.Span.Equals(error.Span));
        var fenced = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Artifacts);
        var fencedError = Assert.Single(fenced.Diagnostics);
        Assert.Equal(MarkupDiagnosticSeverity.Error, fencedError.Severity);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, fencedError.Code);
        Assert.Equal(line + 3, fencedError.Line);
    }
}
