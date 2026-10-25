using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>Solves authored global alignment constraints inside the existing Layered preparation.</summary>
internal sealed partial class TopologyAlignmentLayout {
    private readonly TopologyChart chart;
    private readonly TopologyRenderOptions options;
    private readonly TopologyAlignmentBudget budget;
    private readonly TopologyAlignmentAxis x, y;
    private readonly Dictionary<string, Item> nodes = new(StringComparer.Ordinal), groups = new(StringComparer.Ordinal);
    private readonly List<Item> items = new();
    private readonly List<List<Item>> siblings = new();
    private readonly int origin;
    private const double Padding = TopologyLayoutNormalizer.GroupPadding;

    private sealed class Item {
        internal int Low, High, Order;
        internal double Left, Top, Right, Bottom, PreferredX, PreferredY;
        internal string? Parent;
        internal TopologyNode? Node;
        internal TopologyGroup? Group;
        internal bool Visible;
    }

    internal static void Apply(TopologyChart chart, TopologyRenderOptions? options) =>
        new TopologyAlignmentLayout(chart, options ?? new TopologyRenderOptions()).Solve();

    private TopologyAlignmentLayout(TopologyChart chart, TopologyRenderOptions options) {
        this.chart = chart; this.options = options;
        budget = new TopologyAlignmentBudget(chart.Alignments[0].Id);
        budget.Work((long)chart.Nodes.Count + (long)chart.Groups.Count * 2 + 1);
        origin = checked(chart.Nodes.Count + chart.Groups.Count * 2);
        x = new TopologyAlignmentAxis(origin + 1, budget); y = new TopologyAlignmentAxis(origin + 1, budget);
        foreach (var node in chart.Nodes) {
            budget.Work(); var bounds = TopologyNodeFootprint.RenderedBounds(node, options);
            var cx = CenterX(node); var cy = CenterY(node);
            var item = new Item {
                Low = items.Count, High = items.Count, Order = items.Count, Node = node, Parent = node.GroupId,
                PreferredX = cx, PreferredY = cy, Visible = bounds.HasValue,
                Left = (bounds?.X ?? node.X) - cx, Top = (bounds?.Y ?? node.Y) - cy,
                Right = (bounds?.Right ?? node.X + node.Width) - cx, Bottom = (bounds?.Bottom ?? node.Y + node.Height) - cy
            };
            nodes.Add(node.Id, item); items.Add(item);
        }
        foreach (var group in chart.Groups) {
            budget.Work(); var index = chart.Nodes.Count + groups.Count * 2;
            var item = new Item { Low = index, High = index + 1, Group = group, Parent = group.ParentGroupId,
                Order = items.Count, PreferredX = group.X + group.Width / 2, PreferredY = group.Y + group.Height / 2, Visible = true };
            groups.Add(group.Id, item); items.Add(item);
        }
        foreach (var alignment in chart.Alignments) {
            budget.Work(alignment.NodeIds.Count);
            var axis = alignment.Kind == TopologyAlignmentKind.Row ? y : x;
            var first = nodes[alignment.NodeIds[0]];
            foreach (var id in alignment.NodeIds) axis.Equal(first.Low, nodes[id].Low);
        }
        foreach (var item in items) AddContainment(item);
        foreach (var alignment in chart.Alignments) {
            for (var i = 1; i < alignment.NodeIds.Count; i++) {
                budget.Work(); var a = nodes[alignment.NodeIds[i - 1]]; var b = nodes[alignment.NodeIds[i]];
                Separate(a, b, alignment.Kind == TopologyAlignmentKind.Row ? 0 : 2, alignment.Id);
            }
        }
        var sets = new Dictionary<string, List<Item>>(StringComparer.Ordinal);
        foreach (var item in items) {
            budget.Work(); if (!item.Visible) continue;
            var key = item.Parent ?? string.Empty;
            if (!sets.TryGetValue(key, out var set)) { budget.Work(); sets.Add(key, set = new List<Item>()); siblings.Add(set); }
            budget.Work(); set.Add(item);
        }
        AddCompartmentOrder();
    }

    private void AddContainment(Item item) {
        budget.Work();
        var parent = item.Parent != null ? groups[item.Parent] : null;
        if (item.Group != null) {
            var group = item.Group;
            // Preferred shell width is a lower bound: the shared measured header can
            // wrap less after growth, but cannot acquire a taller unreserved header.
            if (!(group.Width > 0) || !(group.Height > 0)) throw new InvalidOperationException("Layered preparation must measure each real group shell before alignment.");
            x.Add(item.Low, item.High, group.Width);
            y.Add(item.Low, item.High, group.Height);
        }
        if (parent == null) {
            x.Add(origin, item.Low, Math.Max(0, -item.Left));
            y.Add(origin, item.Low, Math.Max(0, -item.Top));
        } else {
            var header = ParentInset(parent);
            x.Add(parent.Low, item.Low, Padding - item.Left);
            x.Add(item.High, parent.High, Padding + item.Right);
            y.Add(parent.Low, item.Low, header - item.Top);
            y.Add(item.High, parent.High, Padding + item.Bottom);
        }
    }

    private double ParentInset(Item parent) {
        var group = parent.Group!;
        if (group.Presentation == TopologyGroupPresentation.Compartment || !TopologyGroupHeader.IsDrawn(options)) return Padding;
        return Math.Max(Padding, TopologyGroupHeader.ReservedBounds(chart, group, options).Bottom - group.Y + TopologyLayoutNormalizer.GroupHeaderBottomGap);
    }

    private void AddCompartmentOrder() {
        foreach (var set in siblings) {
            budget.Work(set.Count);
            if (set.Count == 0 || set[0].Parent == null || !set.Exists(item => item.Group?.Presentation == TopologyGroupPresentation.Compartment)) continue;
            var parent = groups[set[0].Parent!].Group!;
            var direction = TopologyGroupHierarchy.ResolveDirection(parent, id => { budget.Work(); return groups[id].Group!; }, chart.LayoutDirection);
            var horizontalFlow = direction is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft;
            // Existing ordered compartments progress across the flow axis with their
            // positive native clearance; authored alignments do not reverse that order.
            var cross = horizontalFlow ? x : y;
            for (var i = 1; i < set.Count; i++) {
                cross.Equal(set[0].Low, set[i].Low); cross.Equal(set[0].High, set[i].High);
                var a = set[i - 1]; var b = set[i];
                var gap = horizontalFlow ? b.Group!.Y - a.Group!.Y - a.Group!.Height : b.Group!.X - a.Group!.X - a.Group!.Width;
                (horizontalFlow ? y : x).Add(a.High, b.Low, Math.Max(0, gap));
            }
        }
    }

    private void Separate(Item first, Item second, int direction, string? declarationId = null) {
        if (direction == 1 || direction == 3) { var swap = first; first = second; second = swap; }
        var horizontal = direction < 2;
        (horizontal ? x : y).Add(first.High, second.Low,
            (horizontal ? first.Right - second.Left : first.Bottom - second.Top) + TopologyLayoutNormalizer.NodeGap, declarationId);
    }

    private static TopologyValidationException Conflict(string id) {
        var result = new TopologyValidationResult();
        result.Errors.Add(new TopologyValidationIssue { Code = "alignment-conflict", ItemId = id,
            Message = "Authored alignments cannot satisfy ordered members, native footprints and group containment." });
        return new TopologyValidationException(result);
    }
}
