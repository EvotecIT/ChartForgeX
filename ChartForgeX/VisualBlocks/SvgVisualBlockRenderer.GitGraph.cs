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
            if (graph.ShowBranchLabels) {
                var width = layout.PlotX - content.X - 10;
                var font = VisualBlockRendering.GitFontSize(layout, theme, branch.Name, 11, width);
                WriteText(writer, branch.Name, content.X, laneY + font * 4 / 11, width, TextAlignment.Right, theme.MutedText, theme.FontFamily, font, "700");
            }
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
        var radius = VisualBlockRendering.GitCommitRadius(commit, layout.FontScale);
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
            writer.StartElement("text").Attribute("data-cfx-role", "git-cherry-pick").Attribute("x", F(placement.X)).Attribute("y", F(placement.Y + 4 * layout.FontScale)).Attribute("text-anchor", "middle").Attribute("fill", theme.CardBackground.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", F(10 * layout.FontScale)).Attribute("font-weight", "800").Text("c").EndElement().Line();
        }

        if (graph.ShowCommitLabels) {
            var label = commit.Label.Length == 0 ? commit.Id : commit.Label;
            var font = VisualBlockRendering.GitFontSize(layout, theme, label, 10.5, 76);
            WriteText(writer, label, placement.X - 38, placement.Y + radius + font * 14 / 10.5, 76, TextAlignment.Center, theme.Text, theme.FontFamily, font, "650");
        }

        if (commit.Tag.Length > 0) WriteGitGraphTag(writer, VisualBlockRendering.GitTag(layout, theme, commit.Tag, placement.X, placement.Y, radius), commit.Tag, theme);
    }

    private static void WriteGitGraphTag(SvgMarkupWriter writer, VisualBlockRendering.GitTagMetrics tagLayout, string tag, ChartForgeX.Themes.ChartTheme theme) {
        var bounds = tagLayout.Bounds;
        writer.StartElement("rect").Attribute("data-cfx-role", "git-tag").Attribute("x", F(bounds.X)).Attribute("y", F(bounds.Y)).Attribute("width", F(bounds.Width)).Attribute("height", F(bounds.Height)).Attribute("rx", 7).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
        WriteText(writer, tag, bounds.X + tagLayout.Inset, tagLayout.Baseline, bounds.Width - tagLayout.Inset * 2, TextAlignment.Center, theme.MutedText, theme.FontFamily, tagLayout.FontSize, "700", "git-tag-label");
    }

    private static int BranchIndex(VisualBlockRendering.GitGraphLayout layout, string branchName) {
        for (var index = 0; index < layout.Branches.Count; index++) if (layout.Branches[index].Name == branchName) return index;
        return 0;
    }

}
