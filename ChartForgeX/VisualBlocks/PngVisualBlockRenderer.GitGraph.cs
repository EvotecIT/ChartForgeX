using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawGitGraph(RgbaCanvas canvas, GitGraphBlock graph) {
        var options = graph.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, graph, ref y, content.X, content.Width);
        var layout = VisualBlockRendering.BuildGitGraphLayout(graph, content, y, options.Padding.Bottom, options.Size.Height);

        for (var index = 0; index < layout.Branches.Count; index++) {
            var branch = layout.Branches[index];
            var laneY = layout.LaneYs[branch.Name];
            var color = VisualBlockRendering.PaletteAt(theme, index);
            canvas.DrawLine(layout.PlotX, laneY, layout.PlotRight, laneY, color.WithAlpha(115), 2);
            if (graph.ShowBranchLabels) DrawAlignedText(canvas, branch.Name, content.X, laneY - 7, layout.PlotX - content.X - 10, VisualTextAlignment.Right, theme.MutedText, 11, true);
        }

        for (var index = 0; index < layout.Placements.Count; index++) DrawGitGraphEdges(canvas, layout, layout.Placements[index], theme.Axis);
        for (var index = 0; index < layout.Placements.Count; index++) DrawGitGraphCommit(canvas, graph, layout, layout.Placements[index], theme);
    }

    private static void DrawGitGraphEdges(RgbaCanvas canvas, VisualBlockRendering.GitGraphLayout layout, VisualBlockRendering.GitGraphCommitPlacement placement, ChartColor lineColor) {
        var commit = placement.Commit;
        for (var index = 0; index < commit.ParentIds.Count; index++) {
            if (!layout.PlacementsById.TryGetValue(commit.ParentIds[index], out var parent)) continue;
            canvas.DrawLine(parent.X, parent.Y, placement.X, placement.Y, lineColor.WithAlpha(index == 0 ? (byte)160 : (byte)120), index == 0 ? 1.8 : 1.4);
        }
    }

    private static void DrawGitGraphCommit(RgbaCanvas canvas, GitGraphBlock graph, VisualBlockRendering.GitGraphLayout layout, VisualBlockRendering.GitGraphCommitPlacement placement, ChartForgeX.Themes.ChartTheme theme) {
        var commit = placement.Commit;
        var branchIndex = BranchIndex(layout, commit.BranchName);
        var color = VisualBlockRendering.PaletteAt(theme, branchIndex);
        var radius = CommitRadius(commit);
        var fill = commit.Type == GitGraphCommitType.Reverse ? theme.CardBackground : color;
        canvas.DrawCircle(placement.X, placement.Y, radius, fill);
        canvas.DrawCircleOutline(placement.X, placement.Y, radius, color, commit.Type == GitGraphCommitType.Highlight ? 2.6 : 1.5);
        if (commit.Type == GitGraphCommitType.Merge) canvas.DrawCircleOutline(placement.X, placement.Y, radius + 4, color, 1.3);
        if (commit.Type == GitGraphCommitType.CherryPick) DrawCenteredTextMiddle(canvas, "c", placement.X, placement.Y, 10, theme.CardBackground, true, 12);

        if (graph.ShowCommitLabels) {
            var label = commit.Label.Length == 0 ? commit.Id : commit.Label;
            DrawCenteredTextMiddle(canvas, label, placement.X, placement.Y + radius + 17, 10.5, theme.Text, true, 76);
        }

        if (commit.Tag.Length > 0) DrawGitGraphTag(canvas, placement.X, placement.Y - radius - 24, commit.Tag, theme);
    }

    private static void DrawGitGraphTag(RgbaCanvas canvas, double centerX, double y, string tag, ChartForgeX.Themes.ChartTheme theme) {
        var width = System.Math.Min(96, System.Math.Max(38, tag.Length * 6.2 + 18));
        var x = centerX - width / 2;
        canvas.FillRoundedRect(x, y, width, 18, 7, theme.PlotBackground);
        canvas.StrokeRoundedRect(x, y, width, 18, 7, theme.PlotBorder, 1);
        DrawCenteredTextMiddle(canvas, tag, centerX, y + 9, 9.5, theme.MutedText, true, width - 10);
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
