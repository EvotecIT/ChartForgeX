using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Existing public API probes for source settings and layouts that were previously retained only.</summary>
public sealed class MermaidGitGraphPolicyCausalTests {
    [Fact]
    public void SourceMainBranchNameMustBeEffectiveBeforeCheckoutAndCommitIntake() {
        var parsed = new MermaidParser().ParseGitGraph("""
            %%{init: {"gitGraph":{"mainBranchName":"trunk","mainBranchOrder":3}}}%%
            gitGraph
            commit id:"base"
            branch work order:1
            commit id:"work"
            checkout trunk
            merge work id:"joined"
            """);
        Assert.Empty(parsed.Diagnostics); Assert.Equal("trunk", parsed.Document!.Branches[0].Name);
        Assert.Equal(3, parsed.Document.Branches[0].Order); Assert.Equal("trunk", parsed.Document.Commits[^1].BranchName);
    }

    [Theory]
    [InlineData("TB", true)] [InlineData("BT", false)]
    public void HeaderDirectionChangesNativeAncestryAxisWithoutChangingParentIdentity(string direction, bool increasing) {
        var result = MermaidRenderer.Render("gitGraph " + direction + "\ncommit id:\"first\"\ncommit id:\"second\""); Assert.Empty(result.Diagnostics);
        var xml = XDocument.Parse(result.Artifact!.ToSvg());
        var first = Commit(xml, "first"); var second = Commit(xml, "second");
        Assert.Equal(Number(first, "cx"), Number(second, "cx"));
        Assert.Equal(increasing, Number(second, "cy") > Number(first, "cy"));
        Assert.Contains(xml.Descendants(), node => (string?)node.Attribute("data-parent-id") == "first" && (string?)node.Attribute("data-commit-id") == "second");
    }

    [Fact]
    public void SourceParallelCommitsUseFurthestParentDepthRatherThanStatementPosition() {
        const string source = """
            %%{init: {"gitGraph":{"parallelCommits":true}}}%%
            gitGraph
            commit id:"base"
            branch work
            commit id:"work"
            checkout main
            commit id:"main-next"
            merge work id:"joined"
            """;
        var result = MermaidRenderer.Render(source); Assert.Empty(result.Diagnostics); var xml = XDocument.Parse(result.Artifact!.ToSvg());
        Assert.Equal(Number(Commit(xml, "work"), "cx"), Number(Commit(xml, "main-next"), "cx"));
        Assert.True(Number(Commit(xml, "joined"), "cx") > Number(Commit(xml, "work"), "cx"));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OrdinaryUnconfiguredGitGraphKeepsItsActualDefaultNativeExports(bool mermaid) {
        const string source = "gitGraph\ncommit id:\"base\" tag:\"v1\"\nbranch work\ncommit id:\"work\"\ncheckout main\nmerge work id:\"joined\"";
        var graph = mermaid ? new MermaidParser().ParseGitGraph(source).Document!.ToGitGraphBlock()
            : GitGraphBlock.Create().WithSize(960, 420).AddBranch("main", 0).AddBranch("work")
                .AddCommit("base", "main", tag: "v1").AddCommit("work", "work", new[] { "base" })
                .AddCommit("joined", "main", new[] { "base", "work" }, GitGraphCommitType.Merge);
        var svg = graph.ToSvg(); var png = graph.ToPng();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "gitgraph-unconfigured-default-" + (mermaid ? "source" : "core"));
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".png", png);
        }
        var xml = XDocument.Parse(svg); Assert.True(Number(Commit(xml, "joined"), "cx") > Number(Commit(xml, "base"), "cx"));
        Assert.Equal(3, xml.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "git-commit-edge")); Assert.NotEmpty(png);
    }

    private static XElement Commit(XDocument svg, string id) => svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "git-commit" && (string?)node.Attribute("data-commit-id") == id);
    private static double Number(XElement node, string attribute) => double.Parse(node.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
