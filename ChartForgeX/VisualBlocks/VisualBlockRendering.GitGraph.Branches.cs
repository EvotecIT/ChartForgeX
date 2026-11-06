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
        // A lane continues to the merge that takes its head. A branch forked from the head, or a cherry-pick of
        // it, starts its own lane there instead of stretching this one.
        var merge = layout.Placements.FirstOrDefault(item => item.Index > last.Index && item.Commit.BranchName != branch
            && item.Commit.Type == GitGraphCommitType.Merge && item.Commit.ParentIds.Skip(1).Contains(last.Commit.Id));
        var end = merge.Index > last.Index ? merge : last;
        return layout.Vertical ? (birth.Y, end.Y) : (birth.X, end.X);
    }

    // Commits on one lane join with a straight segment. A connector between lanes turns once with a rounded
    // corner: a merged or cherry-picked parent runs along its own lane and turns into the receiving commit; a
    // fork leaves its parent across the lanes and runs along the new lane. No connector crosses lanes diagonally.
    internal static string GitConnectorPath(GitGraphLayout layout, GitGraphCommitPlacement parent, GitGraphCommitPlacement child, bool merge) {
        var path = new Svg.SvgPathDataBuilder().MoveTo(parent.X, parent.Y);
        var parentLane = layout.Vertical ? parent.X : parent.Y; var childLane = layout.Vertical ? child.X : child.Y;
        var parentTime = layout.Vertical ? parent.Y : parent.X; var childTime = layout.Vertical ? child.Y : child.X;
        if (Math.Abs(parentLane - childLane) < .01 || Math.Abs(parentTime - childTime) < .01) return path.LineTo(child.X, child.Y).Build();
        var cornerTime = merge ? childTime : parentTime; var cornerLane = merge ? parentLane : childLane;
        var corner = layout.Vertical ? new Primitives.ChartPoint(cornerLane, cornerTime) : new Primitives.ChartPoint(cornerTime, cornerLane);
        var radius = Math.Min(10 * Math.Max(.6, layout.FontScale), Math.Min(Math.Abs(parentLane - childLane), Math.Abs(parentTime - childTime)) / 2);
        var before = Toward(corner, parent.X, parent.Y, radius); var after = Toward(corner, child.X, child.Y, radius);
        return path.LineTo(before.X, before.Y).QuadraticTo(corner.X, corner.Y, after.X, after.Y).LineTo(child.X, child.Y).Build();

        static Primitives.ChartPoint Toward(Primitives.ChartPoint from, double x, double y, double distance) {
            var dx = x - from.X; var dy = y - from.Y; var length = Math.Sqrt(dx * dx + dy * dy);
            return length < .0001 ? from : new Primitives.ChartPoint(from.X + dx / length * distance, from.Y + dy / length * distance);
        }
    }
}
