using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGitGraphDepthTests {
    [Theory]
    [InlineData("LR", false, 390)] [InlineData("LR", true, 390)] [InlineData("LR", false, 1100)] [InlineData("LR", true, 1100)]
    [InlineData("TB", false, 390)] [InlineData("TB", true, 390)] [InlineData("TB", false, 1100)] [InlineData("TB", true, 1100)]
    [InlineData("BT", false, 390)] [InlineData("BT", true, 390)] [InlineData("BT", false, 1100)] [InlineData("BT", true, 1100)]
    public void NativeDirectionsKeepSourceAncestryBranchOrderSelectedParentAndBothStaticExports(string direction, bool dark, int width) {
        var source = Source(direction, true); var options = Options(width, dark);
        var result = MermaidRenderer.Render(source, options); Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; var block = Assert.IsType<GitGraphBlock>(artifact.Model);
        var xml = XDocument.Parse(artifact.ToSvg()); var png = artifact.ToPng(); Capture("gitgraph-" + direction + "-" + width + (dark ? "-dark" : "-light"), artifact);
        Assert.Equal(new[] { "trunk", "feature", "consumer" }, block.Branches.Select(branch => branch.Name));
        Assert.Equal(new[] { 2, 1, 0 }, block.Branches.Select(branch => branch.Order!.Value));
        Assert.Equal(new[] { "base", "feature-1", "main-next", "joined", "consumer-1", "cherry-pick-6" }, block.Commits.Select(commit => commit.Id));
        Assert.Equal(new[] { "main-next", "feature-1" }, block.Commits[3].ParentIds);
        var picked = block.Commits[^1]; Assert.Equal("joined", picked.SourceCommitId);
        Assert.Equal(new[] { "consumer-1", "joined" }, picked.ParentIds); Assert.Equal(GitGraphCommitType.CherryPick, picked.Type);
        Assert.Equal("main-next", artifact.Metadata["mermaid.gitgraph.commit.5.sourceParent"]);
        var markers = xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "git-commit").ToArray(); Assert.Equal(6, markers.Length);
        var first = Marker(xml, "base"); var feature = Marker(xml, "feature-1"); var main = Marker(xml, "main-next"); var joined = Marker(xml, "joined");
        var time = direction == "LR" ? "cx" : "cy"; var lane = direction == "LR" ? "cy" : "cx";
        Assert.Equal(Number(feature, time), Number(main, time)); Assert.Equal(Number(first, lane), Number(main, lane));
        Assert.Equal(direction != "BT", Number(joined, time) > Number(main, time));
        Assert.Equal(7, xml.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "git-commit-edge"));
        var image = PngReader.Decode(png); Assert.Equal(width, image.Width); Assert.Equal(620, image.Height);
        Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "script" or "image" or "foreignObject");
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fenced.Diagnostics);
        Assert.Equal(artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg()); Assert.Equal(png, fenced.Artifacts[0].ToPng());
        Assert.Contains(artifact.ToInterchangeEnvelope().Extensions, item => item.Value == "main-next");
    }

    [Fact]
    public void ExplicitHostFalseOverridesTrueSourceSettingsAndBranchVisibilityHidesBothLaneAndCaption() {
        var source = Source("TB", true).Replace("\"showBranches\":true", "\"showBranches\":false").Replace("\"showCommitLabel\":true", "\"showCommitLabel\":false").Replace("\"rotateCommitLabel\":false", "\"rotateCommitLabel\":true");
        var parsed = new MermaidParser().ParseGitGraph(source); Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidGitGraphDocument>(parsed.Document);
        var fromSource = document.ToGitGraphBlock(); Assert.False(fromSource.ShowBranchLabels); Assert.False(fromSource.ShowCommitLabels); Assert.True(fromSource.RotateCommitLabels);
        var hidden = XDocument.Parse(fromSource.ToSvg()); Assert.DoesNotContain(hidden.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "git-branch-line");
        var host = document.ToGitGraphBlock(new MermaidGitGraphRenderOptions { Direction = GitGraphDirection.BottomToTop,
            ShowBranchLabels = true, ShowCommitLabels = true, ParallelCommits = false, RotateCommitLabels = false });
        Assert.Equal(GitGraphDirection.BottomToTop, host.Direction); Assert.True(host.ShowBranchLabels); Assert.True(host.ShowCommitLabels);
        Assert.False(host.ParallelCommits); Assert.False(host.RotateCommitLabels);
        var xml = XDocument.Parse(host.ToSvg()); Assert.Equal(3, xml.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "git-branch-line"));
    }

    [Theory]
    [InlineData("mainBranchName", "null")] [InlineData("mainBranchName", "false")] [InlineData("mainBranchName", "\"\"")]
    [InlineData("mainBranchOrder", "-1")] [InlineData("mainBranchOrder", "1.5")] [InlineData("mainBranchOrder", "2147483648")]
    [InlineData("parallelCommits", "\"true\"")] [InlineData("showBranches", "{}")] [InlineData("rotateCommitLabel", "7")]
    public void InvalidSupportedConfigurationIsLocatedAndCannotBecomeAnExportableFallback(string key, string value) {
        var source = "%%{init: {\"gitGraph\":{\"" + key + "\":" + value + "}}}%%\ngitGraph\ncommit";
        var result = MermaidRenderer.Render(source); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Span.Line == 1 && diagnostic.Severity == MermaidDiagnosticSeverity.Error);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fenced.Artifacts); Assert.True(fenced.HasErrors);
    }

    [Theory]
    [InlineData("RL")] [InlineData("TD")] [InlineData("sideways")]
    public void UnsupportedHeaderDirectionIsARealLocatedSourceError(string direction) {
        var result = MermaidRenderer.Render("gitGraph " + direction + "\ncommit"); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == 1);
    }

    [Fact]
    public void DirectiveOverridesFrontmatterBeforeMainBranchIntakeWithoutMutatingRetainedDeclarations() {
        const string source = """
            ---
            config:
              gitGraph:
                mainBranchName: legacy
                mainBranchOrder: 2
                showCommitLabel: false
            ---
            %%{init: {"gitGraph":{"mainBranchName":"trunk","mainBranchOrder":0,"showCommitLabel":true}}}%%
            gitGraph
            commit id:"base"
            checkout trunk
            commit id:"next"
            """;
        var parsed = new MermaidParser().ParseGitGraph(source); Assert.Empty(parsed.Diagnostics); var document = parsed.Document!;
        Assert.Equal("trunk", document.Branches[0].Name); Assert.Equal(0, document.Branches[0].Order); Assert.Equal(6, document.Configuration.Settings.Count);
        Assert.Equal("legacy", document.Configuration.Settings[0].Value); Assert.True(document.ToGitGraphBlock().ShowCommitLabels);
        Assert.Equal(source, document.SourceText);
    }

    internal static string Source(string direction, bool parallel) => "%%{init: {\"gitGraph\":{\"mainBranchName\":\"trunk\",\"mainBranchOrder\":3,\"showBranches\":true,\"showCommitLabel\":true,\"rotateCommitLabel\":false,\"parallelCommits\":" + (parallel ? "true" : "false") + "}}}%%\ngitGraph " + direction
        + "\ncommit id:\"base\" tag:\"v1\"\nbranch feature order:2\ncommit id:\"feature-1\" type:HIGHLIGHT\ncheckout trunk\ncommit id:\"main-next\"\nmerge feature id:\"joined\" tag:\"v2\"\nbranch consumer order:1\ncommit id:\"consumer-1\"\ncherry-pick id:\"joined\" parent:\"main-next\" tag:\"picked\"";
    internal static MermaidRenderOptions Options(int width, bool dark) => new() { WarningPolicy = MermaidWarningPolicy.Reject,
        GitGraph = new MermaidGitGraphRenderOptions { Width = width, Height = 620, Title = "Native commit ancestry", Presentation = new MermaidPresentationOptions { Theme = VisualTheme.Graphite(), ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light } } };
    internal static XElement Marker(XDocument xml, string id) => xml.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "git-commit" && (string?)node.Attribute("data-commit-id") == id);
    internal static double Number(XElement node, string attribute) => double.Parse(node.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
    internal static void Capture(string name, VisualArtifact artifact) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Captures require an absolute task-owned directory.");
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), artifact.ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".native.png"), artifact.ToPng());
        File.WriteAllText(Path.Combine(output, name + ".json"), artifact.ToInterchangeJson());
    }
}
