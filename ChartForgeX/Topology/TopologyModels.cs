using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>
/// Represents a renderer-independent topology diagram.
/// </summary>
public sealed class TopologyChart {
    /// <summary>Gets or sets a stable chart identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the chart title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the chart subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the deterministic layout mode.</summary>
    public TopologyLayoutMode LayoutMode { get; set; } = TopologyLayoutMode.Manual;

    /// <summary>Gets the viewport used by renderers.</summary>
    public TopologyViewport Viewport { get; set; } = new();

    /// <summary>Gets the topology groups.</summary>
    public List<TopologyGroup> Groups { get; } = new();

    /// <summary>Gets the topology nodes.</summary>
    public List<TopologyNode> Nodes { get; } = new();

    /// <summary>Gets the topology edges.</summary>
    public List<TopologyEdge> Edges { get; } = new();

    /// <summary>Gets or sets an optional legend.</summary>
    public TopologyLegend? Legend { get; set; }

    /// <summary>Gets or sets an optional topology theme.</summary>
    public TopologyTheme? Theme { get; set; }
}

/// <summary>
/// Defines the topology rendering viewport.
/// </summary>
public sealed class TopologyViewport {
    /// <summary>Gets or sets the viewport width.</summary>
    public double Width { get; set; } = 1200;

    /// <summary>Gets or sets the viewport height.</summary>
    public double Height { get; set; } = 700;

    /// <summary>Gets or sets the viewport padding.</summary>
    public double Padding { get; set; } = 24;
}

/// <summary>
/// Represents a logical topology region or cluster.
/// </summary>
public sealed class TopologyGroup {
    /// <summary>Gets or sets the stable group identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the group label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional group subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the group status.</summary>
    public TopologyHealthStatus Status { get; set; } = TopologyHealthStatus.Unknown;

    /// <summary>Gets or sets the group x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the group y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the group width.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the group height.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets an optional drill-down link.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets an optional SVG tooltip.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Gets arbitrary metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>
/// Represents a topology node.
/// </summary>
public sealed class TopologyNode {
    /// <summary>Gets or sets the stable node identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the node label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional node subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the node kind.</summary>
    public TopologyNodeKind Kind { get; set; } = TopologyNodeKind.Generic;

    /// <summary>Gets or sets the node status.</summary>
    public TopologyHealthStatus Status { get; set; } = TopologyHealthStatus.Unknown;

    /// <summary>Gets or sets the optional parent group id.</summary>
    public string? GroupId { get; set; }

    /// <summary>Gets or sets the node x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the node y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the node width.</summary>
    public double Width { get; set; } = 120;

    /// <summary>Gets or sets the node height.</summary>
    public double Height { get; set; } = 64;

    /// <summary>Gets or sets an optional drill-down link.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets an optional SVG tooltip.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Gets node metrics for host adapters.</summary>
    public Dictionary<string, string> Metrics { get; } = new();

    /// <summary>Gets arbitrary node metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>
/// Represents a topology edge between two nodes.
/// </summary>
public sealed class TopologyEdge {
    /// <summary>Gets or sets the stable edge identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the source node id.</summary>
    public string SourceNodeId { get; set; } = string.Empty;

    /// <summary>Gets or sets the target node id.</summary>
    public string TargetNodeId { get; set; } = string.Empty;

    /// <summary>Gets or sets the edge kind.</summary>
    public TopologyEdgeKind Kind { get; set; } = TopologyEdgeKind.Generic;

    /// <summary>Gets or sets the edge status.</summary>
    public TopologyHealthStatus Status { get; set; } = TopologyHealthStatus.Unknown;

    /// <summary>Gets or sets the edge direction.</summary>
    public TopologyDirection Direction { get; set; } = TopologyDirection.None;

    /// <summary>Gets or sets the edge routing mode.</summary>
    public TopologyEdgeRouting Routing { get; set; } = TopologyEdgeRouting.Orthogonal;

    /// <summary>Gets or sets the primary edge label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the secondary edge label.</summary>
    public string? SecondaryLabel { get; set; }

    /// <summary>Gets or sets an optional drill-down link.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets an optional SVG tooltip.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Gets edge metrics for host adapters.</summary>
    public Dictionary<string, string> Metrics { get; } = new();

    /// <summary>Gets arbitrary edge metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}
