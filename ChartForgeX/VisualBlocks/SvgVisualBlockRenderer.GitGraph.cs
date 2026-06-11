using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderGitGraph(SvgMarkupWriter writer, GitGraphBlock graph) {
        var options = graph.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, graph, ref y, content.X, content.Width);
        var layout = VisualBlockRendering.BuildGitGraphLayout(graph, content, y, options.Padding.Bottom, options.Size.Height);

        for (var index = 0; index < layout.Branches.Count; index++) {
            var branch = layout.Branches[index];
            var laneY = layout.LaneYs[branch.Name];
            var color = VisualBlockRendering.PaletteAt(theme, index);
            writer.StartElement("line")
                .Attribute("data-cfx-role", "git-branch-line")
                .Attribute("data-branch", branch.Name)
                .Attribute("x1", F(layout.PlotX))
                .Attribute("y1", F(laneY))
                .Attribute("x2", F(layout.PlotRight))
                .Attribute("y2", F(laneY))
                .Attribute("stroke", color.WithAlpha(115).ToCss())
                .Attribute("stroke-width", 2)
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement()
                .Line();
            if (graph.ShowBranchLabels) WriteText(writer, branch.Name, content.X, laneY + 4, layout.PlotX - content.X - 10, VisualTextAlignment.Right, theme.MutedText, theme.FontFamily, 11, "700");
        }

        for (var index = 0; index < layout.Placements.Count; index++) WriteGitGraphEdges(writer, layout, layout.Placements[index], theme);
        for (var index = 0; index < layout.Placements.Count; index++) WriteGitGraphCommit(writer, graph, layout, layout.Placements[index], theme);
    }

    private static void WriteGitGraphEdges(SvgMarkupWriter writer, VisualBlockRendering.GitGraphLayout layout, VisualBlockRendering.GitGraphCommitPlacement placement, ChartForgeX.Themes.ChartTheme theme) {
        var commit = placement.Commit;
        for (var index = 0; index < commit.ParentIds.Count; index++) {
            if (!layout.PlacementsById.TryGetValue(commit.ParentIds[index], out var parent)) continue;
            var color = theme.Axis.WithAlpha(index == 0 ? (byte)160 : (byte)120);
            writer.StartElement("line")
                .Attribute("data-cfx-role", "git-commit-edge")
                .Attribute("data-parent-id", parent.Commit.Id)
                .Attribute("data-commit-id", commit.Id)
                .Attribute("x1", F(parent.X))
                .Attribute("y1", F(parent.Y))
                .Attribute("x2", F(placement.X))
                .Attribute("y2", F(placement.Y))
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", index == 0 ? 1.8 : 1.4)
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement()
                .Line();
        }
    }

    private static void WriteGitGraphCommit(SvgMarkupWriter writer, GitGraphBlock graph, VisualBlockRendering.GitGraphLayout layout, VisualBlockRendering.GitGraphCommitPlacement placement, ChartForgeX.Themes.ChartTheme theme) {
        var commit = placement.Commit;
        var branchIndex = BranchIndex(layout, commit.BranchName);
        var color = VisualBlockRendering.PaletteAt(theme, branchIndex);
        var radius = CommitRadius(commit);
        var fill = commit.Type == GitGraphCommitType.Reverse ? theme.CardBackground : color;
        var stroke = color;
        writer.StartElement("circle")
            .Attribute("data-cfx-role", "git-commit")
            .Attribute("data-commit-id", commit.Id)
            .Attribute("data-branch", commit.BranchName)
            .Attribute("cx", F(placement.X))
            .Attribute("cy", F(placement.Y))
            .Attribute("r", F(radius))
            .Attribute("fill", fill.ToCss())
            .Attribute("stroke", stroke.ToCss())
            .Attribute("stroke-width", commit.Type == GitGraphCommitType.Highlight ? 2.6 : 1.5)
            .EndEmptyElement()
            .Line();
        if (commit.Type == GitGraphCommitType.Merge) {
            writer.StartElement("circle").Attribute("data-cfx-role", "git-merge-ring").Attribute("cx", F(placement.X)).Attribute("cy", F(placement.Y)).Attribute("r", F(radius + 4)).Attribute("fill", "none").Attribute("stroke", stroke.ToCss()).Attribute("stroke-width", 1.3).EndEmptyElement().Line();
        }

        if (commit.Type == GitGraphCommitType.CherryPick) {
            writer.StartElement("text").Attribute("data-cfx-role", "git-cherry-pick").Attribute("x", F(placement.X)).Attribute("y", F(placement.Y + 4)).Attribute("text-anchor", "middle").Attribute("fill", theme.CardBackground.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", 10).Attribute("font-weight", "800").Text("c").EndElement().Line();
        }

        if (graph.ShowCommitLabels) {
            var label = commit.Label.Length == 0 ? commit.Id : commit.Label;
            WriteText(writer, label, placement.X - 38, placement.Y + radius + 14, 76, VisualTextAlignment.Center, theme.Text, theme.FontFamily, 10.5, "650");
        }

        if (commit.Tag.Length > 0) WriteGitGraphTag(writer, placement.X, placement.Y - radius - 24, commit.Tag, theme);
    }

    private static void WriteGitGraphTag(SvgMarkupWriter writer, double centerX, double y, string tag, ChartForgeX.Themes.ChartTheme theme) {
        var width = System.Math.Min(96, System.Math.Max(38, tag.Length * 6.2 + 18));
        var x = centerX - width / 2;
        writer.StartElement("rect").Attribute("data-cfx-role", "git-tag").Attribute("x", F(x)).Attribute("y", F(y)).Attribute("width", F(width)).Attribute("height", 18).Attribute("rx", 7).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
        WriteText(writer, tag, x + 5, y + 12.5, width - 10, VisualTextAlignment.Center, theme.MutedText, theme.FontFamily, 9.5, "700");
    }

    private static int BranchIndex(VisualBlockRendering.GitGraphLayout layout, string branchName) {
        for (var index = 0; index < layout.Branches.Count; index++) if (layout.Branches[index].Name == branchName) return index;
        return 0;
    }

    private static double CommitRadius(GitGraphCommit commit) {
        if (commit.Type == GitGraphCommitType.Highlight) return 8.5;
        if (commit.Type == GitGraphCommitType.Merge) return 7.5;
        if (commit.Type == GitGraphCommitType.CherryPick) return 7.5;
        return 6.5;
    }
}
