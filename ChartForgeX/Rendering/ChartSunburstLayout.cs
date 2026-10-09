using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed class ChartSunburstNode {
    public ChartSunburstNode(int index, string id, string label) {
        Index = index;
        Id = id;
        Label = label;
    }

    public int Index { get; }
    public string Id { get; }
    public string Label { get; }
    public int Parent { get; set; } = -1;
    public List<int> Children { get; } = new();
    public double IncomingValue { get; set; }
    public double Value { get; set; }
    public int Depth { get; set; }
    public double StartAngle { get; set; }
    public double EndAngle { get; set; }
    public double InnerRadius { get; set; }
    public double OuterRadius { get; set; }
}

internal sealed class ChartSunburstModel {
    public ChartSunburstModel(List<ChartSunburstNode> nodes, int root, int maxDepth, double centerX, double centerY) {
        Nodes = nodes;
        Root = root;
        MaxDepth = maxDepth;
        CenterX = centerX;
        CenterY = centerY;
    }

    public static ChartSunburstModel Empty { get; } = new(new List<ChartSunburstNode>(), -1, 0, 0, 0);
    public List<ChartSunburstNode> Nodes { get; }
    public int Root { get; }
    public int MaxDepth { get; }
    public double CenterX { get; }
    public double CenterY { get; }
}

internal static class ChartSunburstLayout {
    public static ChartSunburstModel Compute(Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Sunburst);
        if (series?.Relationships == null) return ChartSunburstModel.Empty;
        var facts = series.Relationships;
        var nodes = new List<ChartSunburstNode>();
        for (var i = 0; i < facts.Nodes.Count; i++)
            nodes.Add(new ChartSunburstNode(i, facts.Nodes[i].Id, facts.Nodes[i].Label) { Value = facts.HierarchyValues[i] });
        for (var i = 0; i < facts.TreeLinks.Count; i++) {
            var parent = facts.Source(i);
            var child = facts.Target(i);
            nodes[parent].Children.Add(child);
            nodes[child].Parent = parent;
            nodes[child].IncomingValue = facts.TreeLinks[i].Value;
        }
        var root = facts.Root;
        AssignDepths(nodes, root, 0);
        var maxDepth = Math.Max(0, nodes.Max(node => node.Depth));
        var radius = Math.Max(1, Math.Min(plot.Width, plot.Height) * 0.46);
        var ringWidth = radius / Math.Max(1, maxDepth + 1);
        var centerX = plot.Left + plot.Width / 2;
        var centerY = plot.Top + plot.Height / 2;
        AssignAngles(nodes, root, -Math.PI / 2, Math.PI * 3 / 2);
        foreach (var node in nodes) {
            node.InnerRadius = node.Depth * ringWidth;
            node.OuterRadius = Math.Max(node.InnerRadius + 1, (node.Depth + 1) * ringWidth);
        }

        return new ChartSunburstModel(nodes, root, maxDepth, centerX, centerY);
    }

    private static void AssignDepths(List<ChartSunburstNode> nodes, int node, int depth) {
        nodes[node].Depth = depth;
        foreach (var child in nodes[node].Children) AssignDepths(nodes, child, depth + 1);
    }

    private static void AssignAngles(List<ChartSunburstNode> nodes, int node, double start, double end) {
        nodes[node].StartAngle = start;
        nodes[node].EndAngle = end;
        if (nodes[node].Children.Count == 0) return;
        var childStart = start;
        var total = nodes[node].Children.Sum(child => nodes[child].Value);
        foreach (var child in nodes[node].Children) {
            var sweep = total <= 0 ? 0 : (nodes[child].Value / total) * (end - start);
            AssignAngles(nodes, child, childStart, childStart + sweep);
            childStart += sweep;
        }
    }


}
