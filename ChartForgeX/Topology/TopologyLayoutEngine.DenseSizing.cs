using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    // Left-to-right dense layouts keep one row of site panels up to this many groups, then wrap into rows.
    private const int DenseSingleRowGroupLimit = 6;
    // Gutter between site panels and between dense cards; wider than twice the router's obstacle padding so routes fit.
    private const double DenseWrappedGroupGap = 44;
    private const double DenseCardColumnGutter = 30;
    // Groups with more nodes than this collapse to dots; smaller groups keep readable cards.
    private const int DenseCollapsedDotThreshold = 24;

    private static bool UsesWrappedDenseRows(TopologyChart chart) =>
        chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft && chart.Groups.Count > DenseSingleRowGroupLimit;

    /// <summary>
    /// Packs site panels left to right into rows no wider than the viewport content width (or the widest panel), keeping
    /// the declared group order so related sites stay together.
    /// </summary>
    private static void PlaceWrappedDenseGroups(TopologyChart chart, IReadOnlyDictionary<string, (double X, double Y)> explicitGroupPositions, double pad, double titleOffset) {
        var sized = new List<(TopologyGroup Group, List<TopologyNode> Nodes)>(chart.Groups.Count);
        foreach (var group in chart.Groups) {
            var nodes = chart.Nodes.Where(node => string.Equals(node.GroupId, group.Id, StringComparison.Ordinal)).ToList();
            var policy = ResolveDenseGroupPolicy(group, nodes);
            if (group.Width <= 0) group.Width = DenseGroupWidth(chart, nodes, policy);
            if (group.Height <= 0) group.Height = DenseGroupHeight(chart, nodes, policy);
            sized.Add((group, nodes));
        }

        var rowWidth = Math.Max(chart.Viewport.Width - pad * 2, sized.Max(item => item.Group.Width));
        var x = pad;
        var y = pad + titleOffset;
        var rowHeight = 0.0;
        foreach (var (group, nodes) in sized) {
            if (x > pad && x + group.Width > pad + rowWidth) {
                x = pad;
                y += rowHeight + DenseWrappedGroupGap;
                rowHeight = 0;
            }

            if (!explicitGroupPositions.ContainsKey(group.Id)) {
                group.X = x;
                group.Y = y;
            }

            x += group.Width + DenseWrappedGroupGap;
            rowHeight = Math.Max(rowHeight, group.Height);
            PlaceDenseNodesInGroup(chart, nodes, group);
        }
    }

    private static int DenseGroupColumns(TopologyChart chart) {
        if (chart.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft) return Math.Max(1, chart.Groups.Count);
        if (chart.LayoutDirection == TopologyLayoutDirection.BottomToTop && chart.Groups.Count <= 4) return 1;
        if (chart.Groups.Count <= 4) return chart.Groups.Count;
        return Math.Max(1, Math.Min(4, (int)Math.Ceiling(Math.Sqrt(chart.Groups.Count))));
    }

    private static int DenseNodeColumns(int count) {
        if (count <= 0) return 1;
        return Math.Max(1, Math.Min(4, (int)Math.Ceiling(Math.Sqrt(count))));
    }

    private static double DenseCaptionHeight(TopologyChart chart, IList<TopologyNode> nodes) =>
        nodes.Select(node => TopologyNodeFootprint.Caption(chart, node).Height).DefaultIfEmpty(0).Max();

    private static int DenseGridColumns(int count) => Math.Max(1, Math.Min(4, (int)Math.Ceiling(Math.Sqrt(Math.Max(1, count)))));

    private static int DenseCollapsedDotColumns(int count) {
        if (count <= 0) return 1;
        return Math.Max(5, (int)Math.Ceiling(Math.Sqrt(count * 1.3)));
    }

    private static double DenseGroupWidth(TopologyChart chart, IList<TopologyNode> nodes, TopologyGroupLayoutPolicy policy) {
        if (nodes.Count == 0) return 190;
        if (policy == TopologyGroupLayoutPolicy.CollapsedDots) {
            const double dotSize = 22;
            const double dotGap = 12;
            var dotColumns = DenseCollapsedDotColumns(nodes.Count);
            return Math.Max(190, 36 + dotColumns * dotSize + Math.Max(0, dotColumns - 1) * dotGap);
        }

        if (policy == TopologyGroupLayoutPolicy.PairRows) {
            var pairMaxNodeWidth = nodes.Select(node => TopologyNodeFootprint.Width(chart, node)).DefaultIfEmpty(90).Max();
            return Math.Max(190, 36 + 2 * Math.Max(70, pairMaxNodeWidth + DenseCardColumnGutter));
        }

        if (policy == TopologyGroupLayoutPolicy.Grid) {
            var gridMaxNodeWidth = nodes.Select(node => TopologyNodeFootprint.Width(chart, node)).DefaultIfEmpty(90).Max();
            return Math.Max(190, 36 + DenseGridColumns(nodes.Count) * Math.Max(70, gridMaxNodeWidth + DenseCardColumnGutter));
        }

        if (policy == TopologyGroupLayoutPolicy.MiniMesh) {
            var meshColumns = Math.Max(1, (int)Math.Ceiling(nodes.Count / 2.0));
            var meshMaxNodeWidth = nodes.Select(node => node.Width).DefaultIfEmpty(90).Max();
            return Math.Max(190, 36 + meshColumns * Math.Max(70, meshMaxNodeWidth + 18));
        }

        var remaining = Math.Max(0, nodes.Count - 1);
        var branchColumns = DenseNodeColumns(remaining);
        var branchMaxNodeWidth = nodes.Select(node => node.Width).DefaultIfEmpty(90).Max();
        return Math.Max(190, 36 + branchColumns * Math.Max(70, branchMaxNodeWidth + 18));
    }

    private static double DenseGroupHeight(TopologyChart chart, IList<TopologyNode> nodes, TopologyGroupLayoutPolicy policy) {
        if (nodes.Count == 0) return 170;
        if (policy == TopologyGroupLayoutPolicy.CollapsedDots) {
            const double dotSize = 22;
            const double dotGap = 12;
            var dotColumns = DenseCollapsedDotColumns(nodes.Count);
            var dotRows = (int)Math.Ceiling(nodes.Count / (double)dotColumns);
            return Math.Max(170, 92 + dotRows * dotSize + Math.Max(0, dotRows - 1) * dotGap);
        }

        if (policy == TopologyGroupLayoutPolicy.PairRows) {
            var pairMaxNodeHeight = nodes.Select(node => TopologyNodeFootprint.Height(chart, node)).DefaultIfEmpty(46).Max();
            var pairRows = (int)Math.Ceiling(nodes.Count / 2.0);
            return Math.Max(170, 98 + pairRows * (pairMaxNodeHeight + 34));
        }

        if (policy == TopologyGroupLayoutPolicy.Grid) {
            var gridMaxNodeHeight = nodes.Select(node => TopologyNodeFootprint.Height(chart, node)).DefaultIfEmpty(46).Max();
            var gridRows = (int)Math.Ceiling(nodes.Count / (double)DenseGridColumns(nodes.Count));
            return Math.Max(170, 98 + gridRows * (gridMaxNodeHeight + 34));
        }

        if (policy == TopologyGroupLayoutPolicy.MiniMesh) {
            var meshMaxNodeHeight = nodes.Select(node => node.Height).DefaultIfEmpty(46).Max();
            return Math.Max(170, 98 + Math.Min(2, nodes.Count) * (meshMaxNodeHeight + 44));
        }

        var hub = FindDenseHub(nodes);
        var remaining = nodes.Count(node => !ReferenceEquals(node, hub));
        var branchColumns = DenseNodeColumns(remaining);
        var branchRows = remaining == 0 ? 0 : (int)Math.Ceiling(remaining / (double)branchColumns);
        var branchMaxNodeHeight = nodes.Select(node => node.Height).DefaultIfEmpty(46).Max();
        return 98 + (hub?.Height ?? 0) + (branchRows == 0 ? 0 : 40 + branchRows * (branchMaxNodeHeight + 34));
    }
}
