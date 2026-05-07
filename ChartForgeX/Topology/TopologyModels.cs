using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Represents a renderer-independent topology diagram.
/// </summary>
public sealed class TopologyChart {
    /// <summary>
    /// Creates a new topology chart.
    /// </summary>
    /// <returns>A new topology chart.</returns>
    public static TopologyChart Create() => new();

    /// <summary>Gets or sets a stable chart identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets the chart title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the chart subtitle.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the deterministic layout mode.</summary>
    public TopologyLayoutMode LayoutMode { get; set; } = TopologyLayoutMode.Manual;

    /// <summary>Gets or sets the deterministic layout flow direction.</summary>
    public TopologyLayoutDirection LayoutDirection { get; set; } = TopologyLayoutDirection.TopToBottom;

    /// <summary>Gets the viewport used by renderers.</summary>
    public TopologyViewport Viewport { get; set; } = new();

    /// <summary>Gets or sets the longitude/latitude window used by geographic topology layouts.</summary>
    public ChartMapViewport MapViewport { get; set; } = ChartMapViewport.World();

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

    /// <summary>Gets or sets the optional group longitude in degrees.</summary>
    public double? Longitude { get; set; }

    /// <summary>Gets or sets the optional group latitude in degrees.</summary>
    public double? Latitude { get; set; }

    /// <summary>Gets or sets the group width.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the group height.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets an optional drill-down link.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets an optional SVG tooltip.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Gets or sets optional caller-provided CSS class tokens for SVG/HTML hosts.</summary>
    public string? CssClass { get; set; }

    /// <summary>Gets or sets an optional short group symbol used by renderers.</summary>
    public string? Symbol { get; set; }

    /// <summary>Gets or sets an optional group accent color. When set, this colors the group shell independently from health status.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the preferred node arrangement policy for dense grouped layouts.</summary>
    public TopologyGroupLayoutPolicy LayoutPolicy { get; set; } = TopologyGroupLayoutPolicy.Auto;

    /// <summary>Gets or sets the resolved node arrangement policy applied by layout preparation.</summary>
    public TopologyGroupLayoutPolicy AppliedLayoutPolicy { get; set; } = TopologyGroupLayoutPolicy.Auto;

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

    /// <summary>Gets or sets an optional short visual symbol used inside the node icon.</summary>
    public string? Symbol { get; set; }

    /// <summary>Gets or sets an optional node-specific display mode. When null, render options decide.</summary>
    public TopologyNodeDisplayMode? DisplayMode { get; set; }

    /// <summary>Gets or sets optional short badge text such as a count, role, or collapsed-item marker.</summary>
    public string? Badge { get; set; }

    /// <summary>Gets or sets the node status.</summary>
    public TopologyHealthStatus Status { get; set; } = TopologyHealthStatus.Unknown;

    /// <summary>Gets or sets the optional parent group id.</summary>
    public string? GroupId { get; set; }

    /// <summary>Gets or sets the node x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the node y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the optional node longitude in degrees.</summary>
    public double? Longitude { get; set; }

    /// <summary>Gets or sets the optional node latitude in degrees.</summary>
    public double? Latitude { get; set; }

    /// <summary>Gets or sets the node width.</summary>
    public double Width { get; set; } = 120;

    /// <summary>Gets or sets the node height.</summary>
    public double Height { get; set; } = 64;

    /// <summary>Gets or sets an optional drill-down link.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets an optional SVG tooltip.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Gets or sets optional caller-provided CSS class tokens for SVG/HTML hosts.</summary>
    public string? CssClass { get; set; }

    /// <summary>Gets or sets an optional node accent color. When set, this colors the node shell independently from health status.</summary>
    public string? Color { get; set; }

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

    /// <summary>Gets or sets explicit edge line styling. Auto derives styling from health status.</summary>
    public TopologyEdgeLineStyle LineStyle { get; set; } = TopologyEdgeLineStyle.Auto;

    /// <summary>Gets or sets the preferred source attachment side.</summary>
    public TopologyEdgePort SourcePort { get; set; } = TopologyEdgePort.Auto;

    /// <summary>Gets or sets the preferred target attachment side.</summary>
    public TopologyEdgePort TargetPort { get; set; } = TopologyEdgePort.Auto;

    /// <summary>Gets or sets the deterministic orthogonal route lane offset in pixels.</summary>
    public double RouteLane { get; set; }

    /// <summary>Gets or sets which edge layout values were inferred during layout preparation.</summary>
    public TopologyEdgeLayoutInference LayoutInference { get; set; } = TopologyEdgeLayoutInference.None;

    /// <summary>Gets explicit route bend points used between the source and target nodes.</summary>
    public List<ChartPoint> Waypoints { get; } = new();

    /// <summary>Gets or sets the primary edge label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the secondary edge label.</summary>
    public string? SecondaryLabel { get; set; }

    /// <summary>Gets or sets the tertiary edge label.</summary>
    public string? TertiaryLabel { get; set; }

    /// <summary>Gets or sets an optional drill-down link.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets an optional SVG tooltip.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Gets or sets optional caller-provided CSS class tokens for SVG/HTML hosts.</summary>
    public string? CssClass { get; set; }

    /// <summary>Gets or sets whether the edge should render as a quiet structural relationship instead of a status route.</summary>
    public bool IsMuted { get; set; }

    /// <summary>Gets edge metrics for host adapters.</summary>
    public Dictionary<string, string> Metrics { get; } = new();

    /// <summary>Gets arbitrary edge metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}
