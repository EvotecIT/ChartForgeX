using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

public sealed partial class TopologyCellLayout {
    private static (TopologyChart Chart, TopologyLayoutDiagnosticReport Report) CellGeometry(PreparedTopology prepared) {
        if (prepared.CellLayoutSource == null) return (prepared.LayoutChart, prepared.Analyze());
        var source = TopologyLayoutEngine.Clone(prepared.CellLayoutSource);
        // Reuse compound ranking so related regions are adjacent, instead of projecting
        // the large SVG heading gutters into the terminal's few available rows.
        foreach (var group in source.Groups.Where(group => group.ParentGroupId == null)) group.LayoutDirection ??= source.LayoutDirection;
        source.Title = null; source.Subtitle = null; source.Legend = null;
        // Terminal scrolling is across rows. Do not let a short SVG viewport wrap a
        // dependency rank into another column and strand a related group far away.
        if (source.LayoutDirection is TopologyLayoutDirection.LeftToRight or TopologyLayoutDirection.RightToLeft)
            source.Viewport.Height = Math.Max(source.Viewport.Height, source.Nodes.Sum(node => node.Height + 80) + source.Groups.Count * 128);
        else source.Viewport.Width = Math.Max(source.Viewport.Width, source.Nodes.Sum(node => node.Width + 80) + source.Groups.Count * 128);
        var options = source.ResolveRenderOptions(null).CloneForRendering();
        options.IncludeTitle = false; options.IncludeLegend = false;
        options.IncludeGroupLabels = false; options.IncludeEdgeLabels = false; options.FitContentToViewport = false;
        var chart = TopologyLayoutEngine.Prepare(source, null, options, compactCompoundLayout: true);
        // Packing uses the captured source; paint and arrow changes come from the
        // current prepared scene without becoming new layout input.
        return (prepared.LayoutChart, TopologyLayoutDiagnostics.AnalyzePrepared(chart, options));
    }

    private static List<TopologyCellGroup> FitCellGroups(TopologyChart chart, List<TopologyCellNode> nodes, List<TopologyCellGroup> groups) {
        var fitted = new Dictionary<string, TopologyCellGroup>(StringComparer.Ordinal);
        foreach (var group in groups.OrderByDescending(group => group.Depth)) {
            var members = nodes.Where(node => node.GroupId == group.Id).Cast<TopologyCellBox>()
                .Concat(fitted.Values.Where(child => child.ParentGroupId == group.Id)).ToArray();
            if (members.Length == 0) { fitted[group.Id] = group; continue; }
            var left = Math.Max(0, members.Min(box => box.Column) - 2);
            var top = Math.Max(0, members.Min(box => box.Row) - 2);
            var right = Math.Max(members.Max(box => box.Column + box.Width) + 1, left + Terminal.TerminalTextWidth.Measure(group.Label) + 6);
            var bottom = members.Max(box => box.Row + box.Height) + 1;
            fitted[group.Id] = new TopologyCellGroup(chart.Groups.First(item => item.Id == group.Id), left, top,
                right - left + 1, bottom - top + 1, group.Depth);
        }
        return groups.Select(group => fitted[group.Id]).ToList();
    }

    private static TopologyCellLayout FitCellCanvas(string? title, TopologyChart chart, List<TopologyCellNode> nodes,
        List<TopologyCellGroup> groups, List<TopologyCellEdge> edges) {
        var points = nodes.Cast<TopologyCellBox>().Concat(groups).SelectMany(box => new[] {
            new TopologyCellPoint(box.Column, box.Row), new TopologyCellPoint(box.Column + box.Width - 1, box.Row + box.Height - 1)
        }).Concat(edges.SelectMany(edge => edge.Points)).ToArray();
        if (points.Length == 0) return new TopologyCellLayout(title, 1, 1, nodes, groups, edges);
        var dx = 1 - points.Min(point => point.Column); var dy = 1 - points.Min(point => point.Row);
        var shiftedNodes = nodes.Select(node => new TopologyCellNode(chart.Nodes.First(model => model.Id == node.Id), node.Label, node.Subtitle,
            node.Column + dx, node.Row + dy, node.Width, node.Height)).ToList();
        var shiftedGroups = groups.Select(group => new TopologyCellGroup(chart.Groups.First(model => model.Id == group.Id),
            group.Column + dx, group.Row + dy, group.Width, group.Height, group.Depth)).ToList();
        var shiftedEdges = edges.Select(edge => new TopologyCellEdge(chart.Edges.First(model => model.Id == edge.Id), edge.Dashed,
            edge.Points.Select(point => new TopologyCellPoint(point.Column + dx, point.Row + dy)).ToList(),
            edge.LabelPosition is TopologyCellPoint at ? new TopologyCellPoint(at.Column + dx, at.Row + dy) : null, edge.LabelOnLine)).ToList();
        return new TopologyCellLayout(title, points.Max(point => point.Column) + dx + 2,
            points.Max(point => point.Row) + dy + 2, shiftedNodes, shiftedGroups, shiftedEdges);
    }

    private static bool KeepCellLabel(TopologyCellLayoutOptions options, TopologyEdge edge) => options.IncludeEdgeLabels
        && CellLabelPriority(edge.Status) >= options.MinimumLabelPriority && !string.IsNullOrWhiteSpace(edge.Label);

    private static int CellLabelPriority(TopologyHealthStatus status) => status switch {
        TopologyHealthStatus.Critical => 3,
        TopologyHealthStatus.Warning => 2,
        _ => 1
    };
}
