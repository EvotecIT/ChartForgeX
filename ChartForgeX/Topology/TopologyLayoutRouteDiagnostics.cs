using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>Identifies what a prepared edge route runs through.</summary>
public enum TopologyLayoutRouteCrossingKind {
    /// <summary>The card of a node that is not one of the edge's endpoints.</summary>
    Node,
    /// <summary>The caption drawn below a tile node, including the captions of the edge's own endpoints.</summary>
    NodeCaption,
    /// <summary>The label block drawn at the top of a group.</summary>
    GroupHeader
}

/// <summary>Describes one prepared group, with the header block drawn at its top.</summary>
public sealed class TopologyLayoutGroupDiagnostic : TopologyLayoutBoundsDiagnostic {
    internal TopologyLayoutGroupDiagnostic(string id, ChartRect bounds, ChartRect? headerBounds) : base(id, bounds) { HeaderBounds = headerBounds; }
    /// <summary>Gets the bounds of the group symbol, label, and subtitle, or null when group labels are not drawn.</summary>
    public ChartRect? HeaderBounds { get; }
}

/// <summary>Describes one edge route that runs through a node card, a node caption, or a group header.</summary>
public sealed class TopologyLayoutRouteCrossingDiagnostic {
    internal TopologyLayoutRouteCrossingDiagnostic(TopologyLayoutRouteCrossingKind kind, string edgeId, string obstacleId) { Kind = kind; EdgeId = edgeId; ObstacleId = obstacleId; }
    /// <summary>Gets what the route runs through.</summary>
    public TopologyLayoutRouteCrossingKind Kind { get; }
    /// <summary>Gets the edge id.</summary>
    public string EdgeId { get; }
    /// <summary>Gets the id of the crossed node (for a card or caption) or group (for a header).</summary>
    public string ObstacleId { get; }
}

/// <summary>Describes two edge routes whose horizontal or vertical runs are drawn on top of each other.</summary>
public sealed class TopologyLayoutRouteOverlapDiagnostic {
    internal TopologyLayoutRouteOverlapDiagnostic(string firstEdgeId, string secondEdgeId, double length) { FirstEdgeId = firstEdgeId; SecondEdgeId = secondEdgeId; Length = length; }
    /// <summary>Gets the first edge id.</summary>
    public string FirstEdgeId { get; }
    /// <summary>Gets the second edge id.</summary>
    public string SecondEdgeId { get; }
    /// <summary>Gets the total length, in pixels, along which the two routes run less than 2 px apart; pairs sharing less than 6 px are not reported.</summary>
    public double Length { get; }
}

/// <summary>Describes one placed edge label and how far it sits from the route it names.</summary>
public sealed class TopologyLayoutEdgeLabelDiagnostic {
    internal TopologyLayoutEdgeLabelDiagnostic(string edgeId, ChartRect bounds, double routeDistance) { EdgeId = edgeId; Bounds = bounds; RouteDistance = routeDistance; }
    /// <summary>Gets the edge id.</summary>
    public string EdgeId { get; }
    /// <summary>Gets the label bounds.</summary>
    public ChartRect Bounds { get; }
    /// <summary>Gets the shortest distance, in pixels, from the label to its route; zero when the label sits on it.</summary>
    public double RouteDistance { get; }
}
