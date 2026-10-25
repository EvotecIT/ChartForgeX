using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GitGraphLayoutPolicyTests {
    [Fact]
    public void RestoringOrdinaryOptionsRestoresExactIncumbentLrSvgAndNativePng() {
        var graph = GitGraphDepthRecipe.Create(); graph.ParallelCommits = false;
        var svg = graph.ToSvg(); var png = graph.ToPng();
        graph.Direction = GitGraphDirection.BottomToTop; graph.ParallelCommits = true; graph.RotateCommitLabels = true;
        Assert.NotEqual(svg, graph.ToSvg()); Assert.NotEqual(png, graph.ToPng());
        graph.Direction = GitGraphDirection.LeftToRight; graph.ParallelCommits = false; graph.RotateCommitLabels = false;
        Assert.Equal(svg, graph.ToSvg()); Assert.Equal(png, graph.ToPng());
        graph.ShowBranchLabels = false; Assert.DoesNotContain("git-branch-line", graph.ToSvg());
        graph.ShowBranchLabels = true; Assert.Equal(svg, graph.ToSvg());
    }

    [Theory]
    [InlineData(GitGraphDirection.LeftToRight)] [InlineData(GitGraphDirection.TopToBottom)] [InlineData(GitGraphDirection.BottomToTop)]
    public void ParallelAncestryUsesFurthestParentAndKeepsSameLaneRootsDistinct(GitGraphDirection direction) {
        var graph = GitGraphBlock.Create().WithDirection(direction).WithParallelCommits().WithSize(760, 500).WithBranchLabels(false).WithCommitLabels(false)
            .AddBranch("main").AddBranch("work").AddCommit("root", "main")
            .AddCommit("work1", "work", new[] { "root" }).AddCommit("work2", "work", new[] { "work1" })
            .AddCommit("main1", "main", new[] { "root" }).AddCommit("merged", "main", new[] { "main1", "work2" }, GitGraphCommitType.Merge)
            .AddCommit("unrelated", "main");
        var xml = XDocument.Parse(graph.ToSvg()); var time = direction == GitGraphDirection.LeftToRight ? "cx" : "cy";
        double Position(string id) => MermaidGitGraphDepthTests.Number(MermaidGitGraphDepthTests.Marker(xml, id), time);
        var sign = direction == GitGraphDirection.BottomToTop ? -1 : 1;
        Assert.Equal(Position("work1"), Position("main1")); Assert.True(sign * (Position("merged") - Position("work2")) > 0);
        Assert.True(sign * (Position("unrelated") - Position("merged")) > 0);
        Assert.Equal(new[] { "root", "work1", "work2", "main1", "merged", "unrelated" }, graph.Commits.Select(commit => commit.Id));
    }

    [Fact]
    public void ParallelParentOrderGuardRejectsCyclicOrForwardInputWithoutChangingOrdinaryExports() {
        var graph = GitGraphBlock.Create().AddBranch("main").AddCommit("child", "main", new[] { "parent" }).AddCommit("parent", "main");
        var ordinary = graph.ToSvg(); graph.ParallelCommits = true;
        Assert.Contains("precede", Assert.Throws<InvalidOperationException>(() => graph.ToSvg()).Message);
        graph.ParallelCommits = false; Assert.Equal(ordinary, graph.ToSvg());
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.Direction = (GitGraphDirection)99);
    }

    [Fact]
    public void SourceConversionCapturesMutableAstOptionsAndSelectedCherryPickParentBeforeLaterEdits() {
        var document = new MermaidParser().ParseGitGraph(MermaidGitGraphDepthTests.Source("TB", true)).Document!;
        var options = new MermaidGitGraphRenderOptions { Width = 760, Height = 500 }; var graph = document.ToGitGraphBlock(options); var artifact = document.ToVisualArtifact(options);
        var svg = graph.ToSvg(); var png = graph.ToPng(); var scene = new SvgVisualBlockRenderer().RenderLabelScene(graph); var capturedScene = scene.ToSvg();
        document.Branches[0].Name = "changed"; document.Commits[0].Id = "changed"; document.Commits.Clear(); options.Width = 300;
        Assert.Equal(svg, graph.ToSvg()); Assert.Equal(png, graph.ToPng()); Assert.Equal("main-next", artifact.Metadata["mermaid.gitgraph.commit.5.sourceParent"]); Assert.Equal("joined", graph.Commits[^1].SourceCommitId);
        graph.Commits[0].Label = "Changed after capture"; graph.Direction = GitGraphDirection.LeftToRight;
        Assert.Equal(capturedScene, scene.ToSvg());
    }

    [Theory]
    [InlineData("wide-lr", VisualThemeMode.Light)] [InlineData("compact-lr", VisualThemeMode.Dark)]
    [InlineData("wide-tb", VisualThemeMode.Dark)] [InlineData("compact-tb", VisualThemeMode.Light)]
    [InlineData("wide-bt", VisualThemeMode.Light)] [InlineData("compact-bt", VisualThemeMode.Dark)]
    public void NativeAndStrictSvgRetainTypedCommitFillAndVisibleCherryPickInkWithTiltedCaptions(string variant, VisualThemeMode mode) {
        var graph = GitGraphDepthRecipe.Create(variant, mode); graph.RotateCommitLabels = true;
        var svg = graph.ToSvg(); var nativePng = graph.ToPng(); var native = PngReader.Decode(nativePng); var fromSvg = SvgRasterizer.Rasterize(svg, strict: true).Image;
        var xml = XDocument.Parse(svg); var count = 0;
        var markers = xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "git-commit").ToArray();
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task-owned directory.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "gitgraph-native-recipe-" + variant + "-" + mode);
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".native.png", nativePng); File.WriteAllBytes(stem + ".strict-svg.png", PngWriter.WriteRgba(fromSvg));
            var samples = markers.Select(marker => {
                var id = (string)marker.Attribute("data-commit-id")!;
                var cx = MermaidGitGraphDepthTests.Number(marker, "cx"); var cy = MermaidGitGraphDepthTests.Number(marker, "cy");
                var x = (int)Math.Round(cx); var y = (int)Math.Round(cy); var at = (y * native.Width + x) * 4;
                return new { id, type = graph.Commits.Single(commit => commit.Id == id).Type.ToString(), cx, cy, x, y,
                    native = native.Pixels.AsSpan(at, 4).ToArray(), strictSvg = fromSvg.Pixels.AsSpan(at, 4).ToArray() };
            }).ToArray();
            File.WriteAllText(stem + ".centers.json", System.Text.Json.JsonSerializer.Serialize(new { variant, mode = mode.ToString(), samples }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        Assert.Equal(native.Width, fromSvg.Width); Assert.Equal(native.Height, fromSvg.Height);
        foreach (var marker in markers) {
            var id = (string)marker.Attribute("data-commit-id")!;
            var commit = Assert.Single(graph.Commits, commit => commit.Id == id);
            var cx = MermaidGitGraphDepthTests.Number(marker, "cx"); var cy = MermaidGitGraphDepthTests.Number(marker, "cy");
            var radius = MermaidGitGraphDepthTests.Number(marker, "r");
            // The cherry-pick glyph intentionally overpaints its circle center. Sample its real
            // circle interior, then prove the sample excludes all placed text and the stroke.
            var x = (int)Math.Round(cx - (commit.Type == GitGraphCommitType.CherryPick ? radius * .6 : 0)); var y = (int)Math.Round(cy);
            Assert.True(PixelInsideMarkerFill(x, y, marker));
            if (commit.Type == GitGraphCommitType.CherryPick) {
                foreach (var label in xml.Descendants().Where(label => label.Attribute("data-cfx-label-x") != null && (string?)label.Attribute("display") != "none")) {
                    var left = MermaidGitGraphDepthTests.Number(label, "data-cfx-label-x"); var top = MermaidGitGraphDepthTests.Number(label, "data-cfx-label-y");
                    var right = left + MermaidGitGraphDepthTests.Number(label, "data-cfx-label-width"); var bottom = top + MermaidGitGraphDepthTests.Number(label, "data-cfx-label-height");
                    Assert.True(x + 1 <= left || x >= right || y + 1 <= top || y >= bottom,
                        "Commit " + id + " fill sample overlaps a captured label: " + (string?)label.Attribute("data-cfx-role"));
                }
            }
            var fill = ChartColor.Parse((string)marker.Attribute("fill")!); var expected = new[] { fill.R, fill.G, fill.B, fill.A };
            var at = (y * native.Width + x) * 4;
            Assert.Equal(expected, native.Pixels.AsSpan(at, 4).ToArray()); Assert.Equal(expected, fromSvg.Pixels.AsSpan(at, 4).ToArray()); count++;
            if (commit.Type == GitGraphCommitType.CherryPick) {
                var glyph = Assert.Single(xml.Descendants(), label => (string?)label.Attribute("data-cfx-role") == "git-cherry-pick");
                Assert.Equal("c", glyph.Value); Assert.Equal("placed", (string?)glyph.Attribute("data-cfx-label-status"));
                Assert.Equal(cx, MermaidGitGraphDepthTests.Number(glyph, "x"));
                Assert.True(HasCapturedCherryPickInk(native, glyph, marker)); Assert.True(HasCapturedCherryPickInk(fromSvg, glyph, marker));
            }
        }
        Assert.Equal(6, count); Assert.Contains("rotate(-45", svg);
    }

    private static bool PixelInsideMarkerFill(int x, int y, XElement marker) {
        var cx = MermaidGitGraphDepthTests.Number(marker, "cx"); var cy = MermaidGitGraphDepthTests.Number(marker, "cy");
        var innerRadius = MermaidGitGraphDepthTests.Number(marker, "r") - MermaidGitGraphDepthTests.Number(marker, "stroke-width") / 2;
        for (var dy = 0; dy <= 1; dy++) for (var dx = 0; dx <= 1; dx++)
            if ((x + dx - cx) * (x + dx - cx) + (y + dy - cy) * (y + dy - cy) >= innerRadius * innerRadius) return false;
        return true;
    }

    private static bool HasCapturedCherryPickInk(RgbaImage image, XElement glyph, XElement marker) {
        var left = MermaidGitGraphDepthTests.Number(glyph, "data-cfx-label-x"); var top = MermaidGitGraphDepthTests.Number(glyph, "data-cfx-label-y");
        var right = left + MermaidGitGraphDepthTests.Number(glyph, "data-cfx-label-width"); var bottom = top + MermaidGitGraphDepthTests.Number(glyph, "data-cfx-label-height");
        var fill = ChartColor.Parse((string)marker.Attribute("fill")!); var ink = ChartColor.Parse((string)glyph.Attribute("fill")!);
        Assert.NotEqual(fill, ink); Assert.True(right > left && bottom > top);
        bool Between(byte value, byte first, byte second) => value >= Math.Min(first, second) && value <= Math.Max(first, second);
        for (var y = Math.Max(0, (int)Math.Floor(top)); y < Math.Min(image.Height, (int)Math.Ceiling(bottom)); y++)
            for (var x = Math.Max(0, (int)Math.Floor(left)); x < Math.Min(image.Width, (int)Math.Ceiling(right)); x++) {
                if (!PixelInsideMarkerFill(x, y, marker)) continue;
                var at = (y * image.Width + x) * 4; var pixels = image.Pixels;
                if (pixels[at + 3] == 255 && (pixels[at] != fill.R || pixels[at + 1] != fill.G || pixels[at + 2] != fill.B)
                    && Between(pixels[at], fill.R, ink.R) && Between(pixels[at + 1], fill.G, ink.G) && Between(pixels[at + 2], fill.B, ink.B)) return true;
            }
        return false;
    }
}
