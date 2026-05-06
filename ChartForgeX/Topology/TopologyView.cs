using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines an optional focused view over a topology chart.
/// </summary>
public sealed class TopologyView {
    /// <summary>
    /// Creates a focused neighbor view around a node.
    /// </summary>
    /// <param name="nodeId">The focus node id.</param>
    /// <param name="depth">The number of edge hops to include.</param>
    /// <returns>A topology view focused around the node.</returns>
    public static TopologyView AroundNode(string nodeId, int depth = 1) {
        var view = new TopologyView { NeighborDepth = depth };
        view.FocusNodeIds.Add(nodeId);
        return view;
    }

    /// <summary>Gets or sets a stable view identifier.</summary>
    public string? Id { get; set; }

    /// <summary>Gets or sets an optional title override for the view.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets an optional subtitle override for the view.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets the group ids to include. When empty, groups are inferred from visible nodes.</summary>
    public List<string> GroupIds { get; } = new();

    /// <summary>Gets the node ids to include. When empty, nodes are inferred from selected groups or the full chart.</summary>
    public List<string> NodeIds { get; } = new();

    /// <summary>Gets the edge ids to include. When empty, connected edges are inferred from visible nodes.</summary>
    public List<string> EdgeIds { get; } = new();

    /// <summary>Gets node ids that should seed a neighbor/ego view.</summary>
    public List<string> FocusNodeIds { get; } = new();

    /// <summary>Gets node kinds to include. When empty, all node kinds are eligible.</summary>
    public List<TopologyNodeKind> NodeKinds { get; } = new();

    /// <summary>Gets edge kinds to include. When empty, all edge kinds are eligible.</summary>
    public List<TopologyEdgeKind> EdgeKinds { get; } = new();

    /// <summary>Gets health statuses to include across nodes, groups, and edges. When empty, all statuses are eligible.</summary>
    public List<TopologyHealthStatus> HealthStatuses { get; } = new();

    /// <summary>Gets or sets the number of edge hops to include around focus nodes.</summary>
    public int NeighborDepth { get; set; }

    /// <summary>Gets or sets whether connected edges should be included when no explicit edge ids are supplied.</summary>
    public bool IncludeConnectedEdges { get; set; } = true;

    /// <summary>Gets or sets whether groups referenced by visible nodes should be included.</summary>
    public bool IncludeNodeGroups { get; set; } = true;

    /// <summary>Gets or sets whether incoming edges should be traversed when building a neighbor view.</summary>
    public bool IncludeIncomingEdges { get; set; } = true;

    /// <summary>Gets or sets whether outgoing edges should be traversed when building a neighbor view.</summary>
    public bool IncludeOutgoingEdges { get; set; } = true;
}
