using System.Collections.Generic;
using ChartForgeX.Topology;

namespace ChartForgeX.Markup;

/// <summary>
/// Represents a Markdown-friendly topology diagram document.
/// </summary>
public sealed class MarkupTopologyDocument {
    /// <summary>Gets or sets the optional chart id.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the layout mode.</summary>
    public TopologyLayoutMode LayoutMode { get; set; } = TopologyLayoutMode.Layered;

    /// <summary>Gets or sets the layout direction.</summary>
    public TopologyLayoutDirection LayoutDirection { get; set; } = TopologyLayoutDirection.LeftToRight;

    /// <summary>Gets or sets the viewport width.</summary>
    public double Width { get; set; } = 1200;

    /// <summary>Gets or sets the viewport height.</summary>
    public double Height { get; set; } = 700;

    /// <summary>Gets or sets the viewport padding.</summary>
    public double Padding { get; set; } = 24;

    /// <summary>Gets declared groups.</summary>
    public List<MarkupTopologyGroup> Groups { get; } = new();

    /// <summary>Gets declared nodes.</summary>
    public List<MarkupTopologyNode> Nodes { get; } = new();

    /// <summary>Gets declared edges.</summary>
    public List<MarkupTopologyEdge> Edges { get; } = new();

    /// <summary>
    /// Converts the markup document into a ChartForgeX topology chart.
    /// </summary>
    /// <returns>The topology chart.</returns>
    public TopologyChart ToTopologyChart() {
        var chart = TopologyChart.Create()
            .WithViewport(Width, Height, Padding)
            .WithLayout(LayoutMode, LayoutDirection);

        if (!string.IsNullOrWhiteSpace(Id)) chart.WithId(Id!);
        if (!string.IsNullOrWhiteSpace(Title)) chart.WithTitle(Title!);
        if (!string.IsNullOrWhiteSpace(Subtitle)) chart.WithSubtitle(Subtitle!);

        foreach (var group in Groups) {
            chart.AddGroup(group.Id, group.Label, 0, 0, group.Width, group.Height, group.Status, group.Subtitle, color: group.Color, iconId: group.Icon);
        }

        foreach (var node in Nodes) {
            chart.AddAutoNode(node.Id, node.Label, node.Kind, node.Status, node.Group, node.Subtitle, width: node.Width, height: node.Height, symbol: node.Symbol, color: node.Color, iconId: node.Icon);
            if (!string.IsNullOrWhiteSpace(node.Badge)) chart.WithNodeBadge(node.Id, node.Badge);
            if (node.Display.HasValue) chart.WithNodeDisplay(node.Id, node.Display.Value);
        }

        foreach (var edge in Edges) {
            chart.AddEdge(edge.Id, edge.Source, edge.Target, edge.Label, edge.Kind, edge.Status, edge.Direction, edge.Routing);
        }

        return chart;
    }
}

/// <summary>
/// Represents a topology group parsed from markup.
/// </summary>
public sealed class MarkupTopologyGroup {
    /// <summary>Gets or sets the group id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the group label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the group status.</summary>
    public TopologyHealthStatus Status { get; set; } = TopologyHealthStatus.Unknown;

    /// <summary>Gets or sets the optional subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the optional accent color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the optional icon id.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the group width.</summary>
    public double Width { get; set; } = 260;

    /// <summary>Gets or sets the group height.</summary>
    public double Height { get; set; } = 160;
}

/// <summary>
/// Represents a topology node parsed from markup.
/// </summary>
public sealed class MarkupTopologyNode {
    /// <summary>Gets or sets the node id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the node label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the node kind.</summary>
    public TopologyNodeKind Kind { get; set; } = TopologyNodeKind.Generic;

    /// <summary>Gets or sets the node status.</summary>
    public TopologyHealthStatus Status { get; set; } = TopologyHealthStatus.Unknown;

    /// <summary>Gets or sets the optional group id.</summary>
    public string? Group { get; set; }

    /// <summary>Gets or sets the optional subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the optional icon id.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the optional symbol.</summary>
    public string? Symbol { get; set; }

    /// <summary>Gets or sets the optional accent color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the optional badge.</summary>
    public string? Badge { get; set; }

    /// <summary>Gets or sets the optional display mode.</summary>
    public TopologyNodeDisplayMode? Display { get; set; }

    /// <summary>Gets or sets the node width.</summary>
    public double Width { get; set; } = 120;

    /// <summary>Gets or sets the node height.</summary>
    public double Height { get; set; } = 64;
}

/// <summary>
/// Represents a topology edge parsed from markup.
/// </summary>
public sealed class MarkupTopologyEdge {
    /// <summary>Gets or sets the edge id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the source node id.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the target node id.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the edge kind.</summary>
    public TopologyEdgeKind Kind { get; set; } = TopologyEdgeKind.Dependency;

    /// <summary>Gets or sets the edge status.</summary>
    public TopologyHealthStatus Status { get; set; } = TopologyHealthStatus.Unknown;

    /// <summary>Gets or sets the edge direction.</summary>
    public TopologyDirection Direction { get; set; } = TopologyDirection.None;

    /// <summary>Gets or sets the edge routing.</summary>
    public TopologyEdgeRouting Routing { get; set; } = TopologyEdgeRouting.Orthogonal;
}
