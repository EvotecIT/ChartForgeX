using System;
using System.Linq;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    internal static (double Start, double End) GitBranchExtent(GitGraphLayout layout, string branch) {
        var own = layout.Placements.Where(item => item.Commit.BranchName == branch).OrderBy(item => item.Index).ToArray();
        if (own.Length == 0) return (layout.PlotX, layout.PlotX);
        var first = own[0]; var last = own[own.Length - 1];
        var birth = first;
        foreach (var parentId in first.Commit.ParentIds)
            if (layout.PlacementsById.TryGetValue(parentId, out var parent) && parent.Index < birth.Index) birth = parent;
        var merge = layout.Placements.FirstOrDefault(item => item.Index > last.Index && item.Commit.BranchName != branch
            && item.Commit.ParentIds.Contains(last.Commit.Id));
        var end = merge.Index > last.Index ? merge : last;
        return layout.Vertical ? (birth.Y, end.Y) : (birth.X, end.X);
    }
}
