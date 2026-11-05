using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderGitGraph(SvgMarkupWriter writer, GitGraphBlock graph) {
        var options = graph.Options;
        var theme = options.ResolvedTheme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, graph, ref y, content.X, content.Width);
        var layout = VisualBlockRendering.BuildGitGraphLayout(graph, content, y, options.Padding.Bottom, options.Size.Height);

        for (var index = 0; index < layout.Branches.Count; index++) {
            var branch = layout.Branches[index];
            var laneY = layout.LaneYs[branch.Name];
            var color = VisualBlockRendering.PaletteAt(theme, index);
            var extent = VisualBlockRendering.GitBranchExtent(layout, branch.Name);
            if (graph.ShowBranchLabels) writer.StartElement("line")
                .Attribute("data-cfx-role", "git-branch-line")
                .Attribute("data-branch", branch.Name)
                .Attribute("x1", F(layout.Vertical ? laneY : extent.Start))
                .Attribute("y1", F(layout.Vertical ? extent.Start : laneY))
                .Attribute("x2", F(layout.Vertical ? laneY : extent.End))
                .Attribute("y2", F(layout.Vertical ? extent.End : laneY))
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", 2)
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement()
                .Line();
            if (graph.ShowBranchLabels && layout.Vertical) {
                var width = System.Math.Max(1, content.Width / layout.Branches.Count - 8);
                var font = VisualBlockRendering.GitFontSize(layout, theme, branch.Name, 11, width);
                var baseline = layout.Direction == GitGraphDirection.BottomToTop ? layout.PlotRight + 16 * layout.FontScale : layout.PlotX - 12 * layout.FontScale;
                WriteText(writer, branch.Name, laneY - width / 2, baseline, width, TextAlignment.Center, theme.MutedText, theme.FontFamily, font, "700", "git-branch-label");
            } else if (graph.ShowBranchLabels) {
                var width = layout.PlotX - content.X - 10;
                var font = VisualBlockRendering.GitFontSize(layout, theme, branch.Name, 11, width);
                WriteText(writer, branch.Name, content.X, laneY + font * 4 / 11, width, TextAlignment.Right, theme.MutedText, theme.FontFamily, font, "700");
            }
        }

        for (var index = 0; index < layout.Placements.Count; index++) WriteGitGraphEdges(writer, layout, layout.Placements[index], theme);
        for (var index = 0; index < layout.Placements.Count; index++) WriteGitGraphCommit(writer, graph, layout, layout.Placements[index], theme);
    }

    private static void WriteGitGraphEdges(SvgMarkupWriter writer, VisualBlockRendering.GitGraphLayout layout, VisualBlockRendering.GitGraphCommitPlacement placement, ChartForgeX.Themes.ResolvedVisualTheme theme) {
        var commit = placement.Commit;
        for (var index = 0; index < commit.ParentIds.Count; index++) {
            if (!layout.PlacementsById.TryGetValue(commit.ParentIds[index], out var parent)) continue;
            var branch = commit.Type == GitGraphCommitType.Merge && index > 0 ? parent.Commit.BranchName : commit.BranchName;
            var color = VisualBlockRendering.PaletteAt(theme, BranchIndex(layout, branch));
            writer.StartElement("path")
                .Attribute("data-cfx-role", "git-commit-edge")
                .Attribute("data-parent-id", parent.Commit.Id)
                .Attribute("data-commit-id", commit.Id)
                .Attribute("d", VisualBlockRendering.GitConnectorPath(layout, parent, placement, index > 0))
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-dasharray", index > 0 && commit.Type == GitGraphCommitType.CherryPick ? "4 3" : null)
                .Attribute("stroke-width", index == 0 ? 1.8 : 1.4)
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement()
                .Line();
        }
    }

    private static void WriteGitGraphCommit(SvgMarkupWriter writer, GitGraphBlock graph, VisualBlockRendering.GitGraphLayout layout, VisualBlockRendering.GitGraphCommitPlacement placement, ChartForgeX.Themes.ResolvedVisualTheme theme) {
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
            var width = layout.Vertical ? System.Math.Max(1, System.Math.Min(64 * layout.FontScale, layout.LaneSpacing / 2 - radius - 5 * layout.FontScale)) : 76;
            var font = VisualBlockRendering.GitFontSize(layout, theme, label, 10.5, width);
            if (graph.RotateCommitLabels) writer.StartElement("g").Attribute("transform", "rotate(-45 " + F(placement.X) + " " + F(placement.Y) + ")").EndStartElement();
            else WriteGitCommitLabelBackplate(writer, graph, layout, placement, label, font, width, radius);
            if (layout.Vertical) WriteText(writer, label, placement.X + radius + 5 * layout.FontScale, placement.Y + font * .35, width, TextAlignment.Left, theme.Text, theme.FontFamily, font, "650", "git-commit-label");
            else WriteText(writer, label, placement.X - 38, placement.Y + radius + font * 14 / 10.5, 76, TextAlignment.Center, theme.Text, theme.FontFamily, font, "650");
            if (graph.RotateCommitLabels) writer.EndElement().Line();
        }

        if (commit.Tag.Length > 0) {
            var tag = VisualBlockRendering.GitTag(layout, theme, commit.Tag, placement.X, placement.Y, radius, layout.Vertical ? System.Math.Max(1, System.Math.Min(96, layout.LaneSpacing / 2 - radius - 5 * layout.FontScale)) : 96);
            if (layout.Vertical) {
                var bounds = tag.Bounds; var x = placement.X - radius - bounds.Width - 5 * layout.FontScale;
                tag = new VisualBlockRendering.GitTagMetrics(new ChartRect(x, placement.Y - bounds.Height / 2, bounds.Width, bounds.Height), tag.FontSize, tag.Inset, placement.Y + tag.FontSize * .35);
            }
            WriteGitGraphTag(writer, tag, commit.Tag, theme);
        }
    }

    // A fork connector leaves its parent across the commit caption. The caption keeps a canvas-toned plate so the
    // connector passes behind it instead of striking through the text.
    private static void WriteGitCommitLabelBackplate(SvgMarkupWriter writer, GitGraphBlock graph, VisualBlockRendering.GitGraphLayout layout, VisualBlockRendering.GitGraphCommitPlacement placement, string label, double font, double width, double radius) {
        var background = VisualBlockRendering.SurfaceBackground(graph.Options);
        if (background.A == 0) return;
        var theme = graph.Options.ResolvedTheme;
        var fitted = VisualBlockRendering.FitMeasuredText(label, font, System.Math.Max(1, width), theme.FontFamily, 650);
        var textWidth = System.Math.Min(width, VisualBlockRendering.MeasureTextWidth(fitted, font, theme.FontFamily, 650));
        var pad = 2 * System.Math.Max(.6, layout.FontScale);
        var baseline = layout.Vertical ? placement.Y + font * .35 : placement.Y + radius + font * 14 / 10.5;
        var left = layout.Vertical ? placement.X + radius + 5 * layout.FontScale : placement.X - textWidth / 2;
        writer.StartElement("rect").Attribute("data-cfx-role", "git-commit-label-surface")
            .Attribute("x", F(left - pad)).Attribute("y", F(baseline - font * .95)).Attribute("width", F(textWidth + pad * 2)).Attribute("height", F(font * 1.25))
            .Attribute("rx", F(pad)).Attribute("fill", background.ToCss()).EndEmptyElement().Line();
    }

    private static void WriteGitGraphTag(SvgMarkupWriter writer, VisualBlockRendering.GitTagMetrics tagLayout, string tag, ChartForgeX.Themes.ResolvedVisualTheme theme) {
        var bounds = tagLayout.Bounds;
        writer.StartElement("rect").Attribute("data-cfx-role", "git-tag").Attribute("x", F(bounds.X)).Attribute("y", F(bounds.Y)).Attribute("width", F(bounds.Width)).Attribute("height", F(bounds.Height)).Attribute("rx", 7).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
        WriteText(writer, tag, bounds.X + tagLayout.Inset, tagLayout.Baseline, bounds.Width - tagLayout.Inset * 2, TextAlignment.Center, theme.MutedText, theme.FontFamily, tagLayout.FontSize, "700", "git-tag-label");
    }

    private static int BranchIndex(VisualBlockRendering.GitGraphLayout layout, string branchName) {
        for (var index = 0; index < layout.Branches.Count; index++) if (layout.Branches[index].Name == branchName) return index;
        return 0;
    }

}
