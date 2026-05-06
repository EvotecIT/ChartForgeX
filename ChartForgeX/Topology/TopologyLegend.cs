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

        foreach (var item in chart.Nodes
            .Select(node => new { node.Kind, Symbol = string.IsNullOrWhiteSpace(node.Symbol) ? null : node.Symbol!.Trim() })
            .Distinct()
            .OrderBy(item => item.Kind.ToString())
            .ThenBy(item => item.Symbol)) {
            legend.AddNodeKind(NodeLegendLabel(item.Kind, item.Symbol), item.Kind, symbol: item.Symbol);
        }

        foreach (var kind in chart.Edges.Select(edge => edge.Kind).Distinct().OrderBy(kind => kind.ToString())) {
            legend.AddEdgeKind(kind.ToString(), kind);
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
    /// <returns>The current legend.</returns>
    public TopologyLegend AddNodeKind(string label, TopologyNodeKind nodeKind, string? color = null, string? symbol = null) {
        Items.Add(new TopologyLegendItem { Label = label, NodeKind = nodeKind, Kind = TopologyLegendItemKind.Node, Color = color, Symbol = symbol });
        return this;
    }

    /// <summary>
    /// Adds an edge-kind legend item.
    /// </summary>
    /// <param name="label">The item label.</param>
    /// <param name="edgeKind">The represented edge kind.</param>
    /// <param name="color">An optional color override.</param>
    /// <returns>The current legend.</returns>
    public TopologyLegend AddEdgeKind(string label, TopologyEdgeKind edgeKind, string? color = null) {
        Items.Add(new TopologyLegendItem { Label = label, EdgeKind = edgeKind, Kind = TopologyLegendItemKind.Edge, Color = color });
        return this;
    }

    private static TopologyLegend Merge(TopologyLegend? explicitLegend, TopologyLegend inferredLegend) {
        if (explicitLegend == null) return inferredLegend;
        var merged = Clone(explicitLegend);
        foreach (var item in inferredLegend.Items) {
            if (merged.Items.Any(existing => SameLegendItem(existing, item))) continue;
            merged.Items.Add(Clone(item));
        }

        return merged;
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
            Color = item.Color
        };
    }

    private static bool SameLegendItem(TopologyLegendItem left, TopologyLegendItem right) {
        return left.Kind == right.Kind
            && left.Status == right.Status
            && left.NodeKind == right.NodeKind
            && left.EdgeKind == right.EdgeKind
            && string.Equals(left.Symbol, right.Symbol, System.StringComparison.Ordinal);
    }

    private static string NodeLegendLabel(TopologyNodeKind kind, string? symbol) {
        return string.IsNullOrWhiteSpace(symbol) ? kind.ToString() : symbol + " " + kind;
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

    /// <summary>Gets or sets the optional color override.</summary>
    public string? Color { get; set; }
}
