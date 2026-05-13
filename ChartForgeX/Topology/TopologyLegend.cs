using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Represents an optional topology legend.
/// </summary>
public sealed class TopologyLegend {
    /// <summary>Gets or sets the legend title.</summary>
    public string? Title { get; set; } = "Topology Legend";

    /// <summary>Gets the legend items.</summary>
    public List<TopologyLegendItem> Items { get; } = new();

    /// <summary>
    /// Creates a new topology legend.
    /// </summary>
    /// <param name="title">The optional legend title.</param>
    /// <returns>A topology legend.</returns>
    public static TopologyLegend Create(string? title = "Topology Legend") => new() { Title = title };

    /// <summary>
    /// Creates a generic default health legend.
    /// </summary>
    /// <returns>A default topology legend.</returns>
    public static TopologyLegend Default() {
        return Create()
            .AddStatus("Healthy", TopologyHealthStatus.Healthy)
            .AddStatus("Warning", TopologyHealthStatus.Warning)
            .AddStatus("Critical", TopologyHealthStatus.Critical)
            .AddStatus("Unknown", TopologyHealthStatus.Unknown)
            .AddStatus("Disabled", TopologyHealthStatus.Disabled);
    }

    /// <summary>
    /// Resolves the legend according to the requested legend mode.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="mode">The legend mode.</param>
    /// <returns>The resolved legend, if any.</returns>
    public static TopologyLegend? Resolve(TopologyChart chart, TopologyLegendMode mode) {
        if (chart == null) return null;
        return mode switch {
            TopologyLegendMode.Auto => Infer(chart),
            TopologyLegendMode.AutoWhenMissing => chart.Legend ?? Infer(chart),
            TopologyLegendMode.Enrich => Enrich(chart.Legend, Infer(chart)),
            TopologyLegendMode.Merge => Merge(chart.Legend, Infer(chart)),
            _ => chart.Legend
        };
    }

    /// <summary>
    /// Infers a generic legend from statuses, node kinds, node symbols, and edge kinds used by the chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <returns>An inferred topology legend.</returns>
    public static TopologyLegend Infer(TopologyChart chart) {
        var legend = Create();
        foreach (var status in chart.Groups.Select(group => group.Status).Concat(chart.Nodes.Select(node => node.Status)).Concat(chart.Edges.Select(edge => edge.Status)).Distinct().OrderBy(status => status)) {
            legend.AddStatus(status.ToString(), status);
        }

        foreach (var group in chart.Nodes
            .GroupBy(node => new { node.Kind, Symbol = OptionalLegendText(node.Symbol), IconId = OptionalLegendText(node.IconId) })
            .OrderBy(group => group.Key.Kind.ToString())
            .ThenBy(group => group.Key.Symbol)
            .ThenBy(group => group.Key.IconId)) {
            legend.AddNodeKind(NodeLegendLabel(group.Key.Kind, group.Key.Symbol), group.Key.Kind, SharedNodeColor(group), group.Key.Symbol, SharedNodeBackgroundColor(group), group.Key.IconId);
        }

        foreach (var group in chart.Edges.GroupBy(edge => edge.Kind).OrderBy(group => group.Key.ToString())) {
            legend.AddEdgeKind(group.Key.ToString(), group.Key, SharedEdgeColor(group), SharedEdgeLineStyle(group));
        }

        return legend;
    }

    /// <summary>
    /// Adds a status legend item.
    /// </summary>
    /// <param name="label">The item label.</param>
    /// <param name="status">The represented status.</param>
    /// <param name="color">An optional color override.</param>
    /// <returns>The current legend.</returns>
    public TopologyLegend AddStatus(string label, TopologyHealthStatus status, string? color = null) {
        Items.Add(new TopologyLegendItem { Label = label, Status = status, Kind = TopologyLegendItemKind.Status, Color = color });
        return this;
    }

    /// <summary>
    /// Adds a node-kind legend item.
    /// </summary>
    /// <param name="label">The item label.</param>
    /// <param name="nodeKind">The represented node kind.</param>
    /// <param name="color">An optional color override.</param>
    /// <param name="symbol">An optional short symbol override.</param>
    /// <param name="backgroundColor">An optional node marker background color override.</param>
    /// <param name="iconId">An optional topology icon id for the legend marker.</param>
    /// <returns>The current legend.</returns>
    public TopologyLegend AddNodeKind(string label, TopologyNodeKind nodeKind, string? color = null, string? symbol = null, string? backgroundColor = null, string? iconId = null) {
        Items.Add(new TopologyLegendItem { Label = label, NodeKind = nodeKind, Kind = TopologyLegendItemKind.Node, Color = color, Symbol = symbol, BackgroundColor = backgroundColor, IconId = iconId });
        return this;
    }

    /// <summary>
    /// Adds an edge-kind legend item.
    /// </summary>
    /// <param name="label">The item label.</param>
    /// <param name="edgeKind">The represented edge kind.</param>
    /// <param name="color">An optional color override.</param>
    /// <param name="lineStyle">An optional edge line style override.</param>
    /// <returns>The current legend.</returns>
    public TopologyLegend AddEdgeKind(string label, TopologyEdgeKind edgeKind, string? color = null, TopologyEdgeLineStyle lineStyle = TopologyEdgeLineStyle.Auto) {
        Items.Add(new TopologyLegendItem { Label = label, EdgeKind = edgeKind, Kind = TopologyLegendItemKind.Edge, Color = color, LineStyle = lineStyle });
        return this;
    }

    private static TopologyLegend Merge(TopologyLegend? explicitLegend, TopologyLegend inferredLegend) {
        if (explicitLegend == null) return inferredLegend;
        var merged = Clone(explicitLegend);
        foreach (var item in inferredLegend.Items) {
            var existing = merged.Items.FirstOrDefault(candidate => SameLegendItem(candidate, item));
            if (existing != null) {
                FillMissingDetails(existing, item);
                continue;
            }

            merged.Items.Add(Clone(item));
        }

        return merged;
    }

    private static TopologyLegend Enrich(TopologyLegend? explicitLegend, TopologyLegend inferredLegend) {
        if (explicitLegend == null) return inferredLegend;
        var enriched = Clone(explicitLegend);
        foreach (var item in inferredLegend.Items) {
            var existing = enriched.Items.FirstOrDefault(candidate => SameLegendItem(candidate, item));
            if (existing != null) FillMissingDetails(existing, item);
        }

        return enriched;
    }

    private static TopologyLegend Clone(TopologyLegend legend) {
        var copy = Create(legend.Title);
        foreach (var item in legend.Items) copy.Items.Add(Clone(item));
        return copy;
    }

    private static TopologyLegendItem Clone(TopologyLegendItem item) {
        return new TopologyLegendItem {
            Label = item.Label,
            Kind = item.Kind,
            Status = item.Status,
            NodeKind = item.NodeKind,
            EdgeKind = item.EdgeKind,
            Symbol = item.Symbol,
            IconId = item.IconId,
            Color = item.Color,
            BackgroundColor = item.BackgroundColor,
            LineStyle = item.LineStyle
        };
    }

    private static bool SameLegendItem(TopologyLegendItem left, TopologyLegendItem right) {
        return left.Kind == right.Kind
            && left.Status == right.Status
            && left.NodeKind == right.NodeKind
            && left.EdgeKind == right.EdgeKind
            && CompatibleText(left.Symbol, right.Symbol)
            && CompatibleText(left.IconId, right.IconId);
    }

    private static void FillMissingDetails(TopologyLegendItem target, TopologyLegendItem source) {
        if (string.IsNullOrWhiteSpace(target.Symbol)) target.Symbol = source.Symbol;
        if (string.IsNullOrWhiteSpace(target.IconId)) target.IconId = source.IconId;
        if (string.IsNullOrWhiteSpace(target.Color)) target.Color = source.Color;
        if (string.IsNullOrWhiteSpace(target.BackgroundColor)) target.BackgroundColor = source.BackgroundColor;
        if (target.LineStyle == TopologyEdgeLineStyle.Auto && source.LineStyle != TopologyEdgeLineStyle.Auto) target.LineStyle = source.LineStyle;
    }

    private static bool CompatibleText(string? left, string? right) {
        return string.IsNullOrWhiteSpace(left)
            || string.IsNullOrWhiteSpace(right)
            || string.Equals(left, right, StringComparison.Ordinal);
    }

    private static string NodeLegendLabel(TopologyNodeKind kind, string? symbol) {
        return string.IsNullOrWhiteSpace(symbol) ? kind.ToString() : symbol + " " + kind;
    }

    private static string? OptionalLegendText(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
    }

    private static string? SharedEdgeColor(IEnumerable<TopologyEdge> edges) {
        string? color = null;
        var hasColor = false;
        foreach (var edge in edges) {
            if (string.IsNullOrWhiteSpace(edge.Color)) return null;
            var candidate = edge.Color!.Trim();
            if (!hasColor) {
                color = candidate;
                hasColor = true;
                continue;
            }

            if (!string.Equals(color, candidate, StringComparison.OrdinalIgnoreCase)) return null;
        }

        return hasColor ? color : null;
    }

    private static string? SharedNodeColor(IEnumerable<TopologyNode> nodes) {
        string? color = null;
        var hasColor = false;
        foreach (var node in nodes) {
            if (string.IsNullOrWhiteSpace(node.Color)) return null;
            var candidate = node.Color!.Trim();
            if (!hasColor) {
                color = candidate;
                hasColor = true;
                continue;
            }

            if (!string.Equals(color, candidate, StringComparison.OrdinalIgnoreCase)) return null;
        }

        return hasColor ? color : null;
    }

    private static string? SharedNodeBackgroundColor(IEnumerable<TopologyNode> nodes) {
        string? color = null;
        var hasColor = false;
        foreach (var node in nodes) {
            if (string.IsNullOrWhiteSpace(node.BackgroundColor)) return null;
            var candidate = node.BackgroundColor!.Trim();
            if (!hasColor) {
                color = candidate;
                hasColor = true;
                continue;
            }

            if (!string.Equals(color, candidate, StringComparison.OrdinalIgnoreCase)) return null;
        }

        return hasColor ? color : null;
    }

    private static TopologyEdgeLineStyle SharedEdgeLineStyle(IEnumerable<TopologyEdge> edges) {
        TopologyEdgeLineStyle? lineStyle = null;
        foreach (var edge in edges) {
            if (edge.LineStyle == TopologyEdgeLineStyle.Auto) return TopologyEdgeLineStyle.Auto;
            if (!lineStyle.HasValue) {
                lineStyle = edge.LineStyle;
                continue;
            }

            if (lineStyle.Value != edge.LineStyle) return TopologyEdgeLineStyle.Auto;
        }

        return lineStyle ?? TopologyEdgeLineStyle.Auto;
    }
}

/// <summary>
/// Represents a topology legend item.
/// </summary>
public sealed class TopologyLegendItem {
    /// <summary>Gets or sets the item label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the legend item kind.</summary>
    public TopologyLegendItemKind Kind { get; set; } = TopologyLegendItemKind.Status;

    /// <summary>Gets or sets the optional status represented by this item.</summary>
    public TopologyHealthStatus? Status { get; set; }

    /// <summary>Gets or sets the optional node kind represented by this item.</summary>
    public TopologyNodeKind? NodeKind { get; set; }

    /// <summary>Gets or sets the optional edge kind represented by this item.</summary>
    public TopologyEdgeKind? EdgeKind { get; set; }

    /// <summary>Gets or sets an optional short symbol shown for node legend items.</summary>
    public string? Symbol { get; set; }

    /// <summary>Gets or sets an optional topology icon id shown for node legend items.</summary>
    public string? IconId { get; set; }

    /// <summary>Gets or sets the optional color override.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the optional node marker background color override.</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Gets or sets the optional line style used when rendering edge legend items.</summary>
    public TopologyEdgeLineStyle LineStyle { get; set; } = TopologyEdgeLineStyle.Auto;
}
