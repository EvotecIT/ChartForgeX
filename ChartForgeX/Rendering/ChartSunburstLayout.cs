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
    public double Value { get; set; }
    public double RemainderValue { get; set; }
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
        var values = facts.ResolveHierarchyValues(chart.Options.Sunburst.ParentValuePolicy);
        var nodes = new List<ChartSunburstNode>();
        for (var i = 0; i < facts.Nodes.Count; i++)
            nodes.Add(new ChartSunburstNode(i, facts.Nodes[i].Id, facts.Nodes[i].Label) { Value = values[i], Parent = facts.Parent(i), Depth = facts.Depths[i] });
        for (var i = 0; i < nodes.Count; i++) {
            nodes[i].Children.AddRange(facts.Children(i));
            var childTotal = 0d;
            foreach (var child in nodes[i].Children) childTotal += values[child];
            if (nodes[i].Children.Count > 0) nodes[i].RemainderValue = Math.Max(0, values[i] - childTotal);
        }
        var root = facts.Root;
        var maxDepth = Math.Max(0, nodes.Max(node => node.Depth));
        var radius = Math.Max(1, Math.Min(plot.Width, plot.Height) * 0.46);
        var ringWidth = radius / Math.Max(1, maxDepth + 1);
        var centerX = plot.Left + plot.Width / 2;
        var centerY = plot.Top + plot.Height / 2;
        AssignAngles(nodes, root, -Math.PI / 2, nodes[root].Value > 0 ? Math.PI * 3 / 2 : -Math.PI / 2);
        foreach (var node in nodes) {
            node.InnerRadius = node.Depth * ringWidth;
            node.OuterRadius = (node.Depth + 1) * ringWidth;
        }

        return new ChartSunburstModel(nodes, root, maxDepth, centerX, centerY);
    }

    private static void AssignAngles(List<ChartSunburstNode> nodes, int node, double start, double end) {
        nodes[node].StartAngle = start;
        nodes[node].EndAngle = end;
        if (nodes[node].Children.Count == 0) return;
        var childStart = start;
        var total = nodes[node].Value;
        var children = nodes[node].Children;
        var childTotal = children.Sum(child => nodes[child].Value);
        var finalPositive = children.LastOrDefault(child => nodes[child].Value > 0);
        foreach (var child in children) {
            var sweep = total <= 0 ? 0 : (nodes[child].Value / total) * (end - start);
            var childEnd = Math.Min(end, childStart + sweep);
            // Only equal totals or an accepted representational overrun close the final endpoint.
            // A positive authored remainder, however small, is never normalized away.
            if (child == finalPositive && nodes[child].Value > 0 && childTotal >= total) childEnd = end;
            AssignAngles(nodes, child, childStart, childEnd);
            childStart = childEnd;
        }
    }
}
