using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Canonical weighted DAG geometry for every Sankey painter. Ribbon and node thickness share one exact scale.</summary>
internal static class ChartSankeyLayout {
    internal static ChartSankeyModel Build(Chart chart) {
        var series = chart.Series.FirstOrDefault(s => s.Kind == ChartSeriesKind.Sankey);
        if (series?.Relationships == null) return ChartSankeyModel.Empty;
        var facts = series.Relationships;
        var links = new List<ChartSankeyLayoutLink>();
        var count = facts.Nodes.Count;
        for (var i = 0; i < facts.FlowLinks.Count; i++)
            links.Add(new ChartSankeyLayoutLink(i, facts.FlowLinks[i].Id, facts.Source(i), facts.Target(i), facts.FlowLinks[i].Value));
        var nodes = Enumerable.Range(0, count).Select(i => new ChartSankeyNode(i, facts.Nodes[i].Id, facts.Nodes[i].Label) {
            Incoming = facts.FlowIncomingValues[i], Outgoing = facts.FlowOutgoingValues[i]
        }).ToList();
        var incoming = new int[count]; var outgoing = new List<int>[count];
        for (int i = 0; i < count; i++) outgoing[i] = new List<int>();
        foreach (var link in links) {
            outgoing[link.Source].Add(link.Target); incoming[link.Target]++;
        }
        var ready = new Queue<int>(Enumerable.Range(0, count).Where(i => incoming[i] == 0));
        var order = new List<int>(count);
        while (ready.Count > 0) {
            int source = ready.Dequeue();
            order.Add(source);
            foreach (int target in outgoing[source]) {
                nodes[target].Layer = Math.Max(nodes[target].Layer, nodes[source].Layer + 1);
                if (--incoming[target] == 0) ready.Enqueue(target);
            }
        }
        int maxLayer = Math.Max(1, nodes.Max(n => n.Layer));
        var remaining = new int[count];
        for (int i = order.Count - 1; i >= 0; i--) {
            int source = order[i];
            foreach (int target in outgoing[source]) remaining[source] = Math.Max(remaining[source], remaining[target] + 1);
        }
        foreach (var node in nodes) {
            int latest = maxLayer - remaining[node.Index];
            node.Layer = chart.Options.Sankey.Alignment switch {
                ChartSankeyAlignment.Right => latest,
                ChartSankeyAlignment.Center => (node.Layer + latest) / 2,
                ChartSankeyAlignment.Justify when outgoing[node.Index].Count == 0 => maxLayer,
                _ => node.Layer
            };
        }
        return new ChartSankeyModel(nodes, links, maxLayer);
    }

    internal static void Layout(ChartSankeyModel model, ChartRect plot, ChartSankeyOptions options, double gap) {
        if (model.Nodes.Count == 0) return;
        if (plot.Width <= 0 || plot.Height <= 0) throw new NotSupportedException("Sankey layout requires positive content dimensions.");
        var nodes = model.Nodes; var links = model.Links;
        model.NodeWidth = options.NodeWidth;
        if (!Finite(model.NodeWidth) || model.NodeWidth <= 0 || model.NodeWidth * (model.MaxLayer + 1) >= plot.Width)
            throw new NotSupportedException("Sankey columns require a wider common viewport.");
        Order(nodes, links, model.MaxLayer, options.NodeOrder);
        // Divide by a finite authored magnitude first. Raw column sums and pixels/weight
        // can overflow even when all node totals and the proportional geometry are valid.
        model.WeightReference = nodes.Max(n => n.Value);
        double scale = double.PositiveInfinity;
        for (int layer = 0; layer <= model.MaxLayer; layer++) {
            var column = nodes.Where(n => n.Layer == layer).ToArray(); if (column.Length == 0) continue;
            double total = column.Sum(n => n.Value / model.WeightReference), available = plot.Height - (column.Length - 1) * gap;
            if (available <= 0) throw new NotSupportedException("Sankey nodes require a taller common viewport.");
            if (total > 0) scale = Math.Min(scale, available / total);
        }
        if (!Finite(scale) || scale <= 0) throw new NotSupportedException("Sankey weights cannot be represented in the available viewport.");
        model.NormalizedWeightScale = scale;
        for (int layer = 0; layer <= model.MaxLayer; layer++) {
            var column = nodes.Where(n => n.Layer == layer).OrderBy(n => n.Order).ThenBy(n => n.Index).ToArray();
            double height = column.Sum(n => model.Thickness(n.Value)) + Math.Max(0, column.Length - 1) * gap;
            double unused = plot.Height - height;
            double y = plot.Y + (options.VerticalAlignment == ChartSankeyVerticalAlignment.Top ? 0
                : options.VerticalAlignment == ChartSankeyVerticalAlignment.Bottom ? unused : unused / 2);
            foreach (var node in column) {
                node.X = plot.X + layer / (double)model.MaxLayer * (plot.Width - model.NodeWidth);
                node.Y = y; node.Height = model.Thickness(node.Value); y += node.Height + gap;
            }
        }
        var from = new double[nodes.Count]; var to = new double[nodes.Count];
        foreach (var link in links.OrderBy(l => nodes[l.Source].Layer).ThenBy(l => nodes[l.Source].Y).ThenBy(l => nodes[l.Target].Y).ThenBy(l => l.Index)) {
            link.Width = model.Thickness(link.Value);
            link.SourceY = nodes[link.Source].Y + from[link.Source] + link.Width / 2;
            link.TargetY = nodes[link.Target].Y + to[link.Target] + link.Width / 2;
            from[link.Source] += link.Width; to[link.Target] += link.Width;
        }
    }

    internal static ChartPath Ribbon(ChartSankeyModel model, ChartSankeyLayoutLink link) {
        double x1 = model.Nodes[link.Source].X + model.NodeWidth, x2 = model.Nodes[link.Target].X, mid = x1 + (x2 - x1) * .55;
        double half = link.Width / 2;
        return new ChartPath(new[] { ChartPathCommand.MoveTo(x1, link.SourceY - half),
            ChartPathCommand.CubicTo(mid, link.SourceY - half, mid, link.TargetY - half, x2, link.TargetY - half),
            ChartPathCommand.LineTo(x2, link.TargetY + half),
            ChartPathCommand.CubicTo(mid, link.TargetY + half, mid, link.SourceY + half, x1, link.SourceY + half) });
    }

    private static void Order(List<ChartSankeyNode> nodes, List<ChartSankeyLayoutLink> links, int maxLayer, ChartSankeyNodeOrder order) {
        foreach (var node in nodes) node.Order = node.Index;
        if (order == ChartSankeyNodeOrder.Input) return;
        if (order is ChartSankeyNodeOrder.LabelAscending or ChartSankeyNodeOrder.LabelDescending) {
            for (int layer = 0; layer <= maxLayer; layer++) {
                var column = nodes.Where(node => node.Layer == layer);
                var sorted = (order == ChartSankeyNodeOrder.LabelAscending
                    ? column.OrderBy(node => node.Label, StringComparer.Ordinal)
                    : column.OrderByDescending(node => node.Label, StringComparer.Ordinal)).ThenBy(node => node.Index).ToArray();
                for (int i = 0; i < sorted.Length; i++) sorted[i].Order = i;
            }
            return;
        }
        for (int pass = 0; pass < 6; pass++) {
            bool forward = pass % 2 == 0;
            for (int step = 0; step <= maxLayer; step++) {
                int layer = forward ? step : maxLayer - step;
                var ordered = nodes.Where(n => n.Layer == layer).Select(node => {
                    var edges = links.Where(link => forward ? link.Target == node.Index : link.Source == node.Index).ToArray();
                    double total = edges.Sum(link => link.Value);
                    // Normalize before multiplication so a finite flow cannot overflow a barycentre.
                    double mean = total == 0 ? node.Order : edges.Sum(link => nodes[forward ? link.Source : link.Target].Order * (link.Value / total));
                    return new { node, mean };
                }).OrderBy(item => item.mean).ThenBy(item => item.node.Index).ToArray();
                for (int i = 0; i < ordered.Length; i++) ordered[i].node.Order = i;
            }
        }
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

internal sealed class ChartSankeyModel {
    internal ChartSankeyModel(List<ChartSankeyNode> nodes, List<ChartSankeyLayoutLink> links, int maxLayer) { Nodes = nodes; Links = links; MaxLayer = maxLayer; }
    internal static ChartSankeyModel Empty => new(new List<ChartSankeyNode>(), new List<ChartSankeyLayoutLink>(), 0);
    internal List<ChartSankeyNode> Nodes { get; }
    internal List<ChartSankeyLayoutLink> Links { get; }
    internal int MaxLayer { get; }
    internal double NodeWidth { get; set; }
    internal double WeightReference { get; set; }
    internal double NormalizedWeightScale { get; set; }
    internal double Thickness(double value) => value / WeightReference * NormalizedWeightScale;
}
internal sealed class ChartSankeyNode {
    internal ChartSankeyNode(int index, string id, string label) { Index = index; Id = id; Label = label; }
    internal int Index { get; }
    internal string Id { get; }
    internal string Label { get; }
    internal double Incoming { get; set; }
    internal double Outgoing { get; set; }
    internal double Value => Math.Max(Incoming, Outgoing);
    internal int Layer { get; set; }
    internal double Order { get; set; }
    internal double X { get; set; }
    internal double Y { get; set; }
    internal double Height { get; set; }
}
internal sealed class ChartSankeyLayoutLink {
    internal ChartSankeyLayoutLink(int index, string id, int source, int target, double value) { Index = index; Id = id; Source = source; Target = target; Value = value; }
    internal string Id { get; }
    internal int Index { get; }
    internal int Source { get; }
    internal int Target { get; }
    internal double Value { get; }
    internal double Width { get; set; }
    internal double SourceY { get; set; }
    internal double TargetY { get; set; }
}
