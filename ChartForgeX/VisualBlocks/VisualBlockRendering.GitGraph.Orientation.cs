using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    private static int[] GitGraphColumns(GitGraphBlock graph, out int lastColumn) {
        var result = new int[graph.Commits.Count]; lastColumn = 0;
        var byId = new Dictionary<string, int>(StringComparer.Ordinal);
        var byLane = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < graph.Commits.Count; index++) {
            var commit = graph.Commits[index]; var column = graph.ParallelCommits ? 0 : index;
            if (graph.ParallelCommits) {
                foreach (var parent in commit.ParentIds) {
                    if (!byId.TryGetValue(parent, out var parentColumn))
                        throw new InvalidOperationException("Parallel git graph parents must precede their children in source order: " + parent + ".");
                    column = Math.Max(column, parentColumn + 1);
                }
                if (byLane.TryGetValue(commit.BranchName, out var previous)) column = Math.Max(column, previous + 1);
            }
            result[index] = column; byId[commit.Id] = column; byLane[commit.BranchName] = column;
            lastColumn = Math.Max(lastColumn, column);
        }
        return result;
    }

    private static GitGraphLayout BuildVerticalGitGraphLayout(GitGraphBlock graph, ChartRect content, double y, double bottomPadding, double totalHeight) {
        var branches = OrderedGitGraphBranches(graph); var height = Math.Max(1, totalHeight - bottomPadding - y);
        var scale = Math.Min(graph.Options.ResolvedTheme.DataLabelFontSize / Themes.ResolvedVisualTheme.DefaultDataLabelFontSize,
            Math.Min(content.Width / Math.Max(1, branches.Count * 112), height / 96));
        var reserve = Math.Max(12, (graph.RotateCommitLabels ? 64 : 14) * scale);
        var laneGap = Math.Max(1, content.Width - reserve * 2) / branches.Count;
        var lanes = new Dictionary<string, double>(StringComparer.Ordinal);
        for (var index = 0; index < branches.Count; index++) lanes[branches[index].Name] = content.X + reserve + (index + .5) * laneGap;
        var top = y + reserve + (graph.ShowBranchLabels && graph.Direction == GitGraphDirection.TopToBottom ? 22 * scale : 0);
        var bottom = y + height - reserve - (graph.ShowBranchLabels && graph.Direction == GitGraphDirection.BottomToTop ? 22 * scale : 0);
        var columns = GitGraphColumns(graph, out var lastColumn); var step = lastColumn == 0 ? 0 : Math.Max(1, bottom - top) / lastColumn;
        var placements = new List<GitGraphCommitPlacement>(graph.Commits.Count);
        var byId = new Dictionary<string, GitGraphCommitPlacement>(StringComparer.Ordinal);
        for (var index = 0; index < graph.Commits.Count; index++) {
            var commit = graph.Commits[index];
            var time = graph.Direction == GitGraphDirection.BottomToTop ? bottom - columns[index] * step : top + columns[index] * step;
            var placement = new GitGraphCommitPlacement(commit, index, lanes[commit.BranchName], time);
            placements.Add(placement); byId[commit.Id] = placement;
        }
        return new GitGraphLayout(branches, lanes, placements, byId, top, bottom, scale, graph.Direction, laneGap);
    }

}
