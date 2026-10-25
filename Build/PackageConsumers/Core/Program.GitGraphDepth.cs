using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.VisualBlocks;

internal static partial class Program {
    private static void GitGraphDepthContracts() {
        foreach (var direction in new[] { GitGraphDirection.LeftToRight, GitGraphDirection.TopToBottom, GitGraphDirection.BottomToTop }) {
            var graph = GitGraphBlock.Create().WithSize(390, 460).WithDirection(direction).WithParallelCommits()
                .AddBranch("main").AddBranch("work").AddCommit("base", "main")
                .AddCommit("work", "work", new[] { "base" }).AddCommit("next", "main", new[] { "base" })
                .AddCommit("joined", "main", new[] { "next", "work" }, GitGraphCommitType.Merge, tag: "v2");
            var xml = XDocument.Parse(graph.ToSvg()); var time = direction == GitGraphDirection.LeftToRight ? "cx" : "cy";
            double Position(string id) => double.Parse(xml.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "git-commit" && (string?)node.Attribute("data-commit-id") == id).Attribute(time)!.Value, CultureInfo.InvariantCulture);
            PackageAssertions.Require(Position("work") == Position("next") && (direction == GitGraphDirection.BottomToTop ? Position("joined") < Position("next") : Position("joined") > Position("next")),
                "Packed Git graph lost physical direction or furthest-parent placement.");
            PackageAssertions.Require(graph.Commits[3].ParentIds.SequenceEqual(new[] { "next", "work" }) && graph.Commits.Count == 4,
                "Packed Git graph rewrote source ancestry.");
            graph.WithRotatedCommitLabels(); PackageAssertions.Require(PackageAssertions.Contains(graph.ToSvg(), "rotate(-45"), "Packed Git graph caption policy is absent.");
            PackageAssertions.Png(graph.ToPng());
        }
        Console.WriteLine("Packed native Git graph direction, parent-depth placement and static SVG/PNG passed.");
    }
}
