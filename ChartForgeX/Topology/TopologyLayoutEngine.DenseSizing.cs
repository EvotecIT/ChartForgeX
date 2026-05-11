using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
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

    private static int DenseCollapsedDotColumns(int count) {
        if (count <= 0) return 1;
        return Math.Max(5, (int)Math.Ceiling(Math.Sqrt(count * 1.3)));
    }

    private static double DenseGroupWidth(IList<TopologyNode> nodes, TopologyGroupLayoutPolicy policy) {
        if (nodes.Count == 0) return 190;
        if (policy == TopologyGroupLayoutPolicy.CollapsedDots) {
            const double dotSize = 22;
            const double dotGap = 12;
            var dotColumns = DenseCollapsedDotColumns(nodes.Count);
            return Math.Max(190, 36 + dotColumns * dotSize + Math.Max(0, dotColumns - 1) * dotGap);
        }

        if (policy == TopologyGroupLayoutPolicy.PairRows) {
            var pairMaxNodeWidth = nodes.Select(node => node.Width).DefaultIfEmpty(90).Max();
            return Math.Max(190, 36 + 2 * Math.Max(70, pairMaxNodeWidth + 18));
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

    private static double DenseGroupHeight(IList<TopologyNode> nodes, TopologyGroupLayoutPolicy policy) {
        if (nodes.Count == 0) return 170;
        if (policy == TopologyGroupLayoutPolicy.CollapsedDots) {
            const double dotSize = 22;
            const double dotGap = 12;
            var dotColumns = DenseCollapsedDotColumns(nodes.Count);
            var dotRows = (int)Math.Ceiling(nodes.Count / (double)dotColumns);
            return Math.Max(170, 92 + dotRows * dotSize + Math.Max(0, dotRows - 1) * dotGap);
        }

        if (policy == TopologyGroupLayoutPolicy.PairRows) {
            var pairMaxNodeHeight = nodes.Select(node => node.Height).DefaultIfEmpty(46).Max();
            var pairRows = (int)Math.Ceiling(nodes.Count / 2.0);
            return Math.Max(170, 98 + pairRows * (pairMaxNodeHeight + 34));
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
