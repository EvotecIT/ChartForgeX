using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines an optional focused view over a topology chart.
/// </summary>
public sealed class TopologyView {
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

    /// <summary>Gets or sets whether connected edges should be included when no explicit edge ids are supplied.</summary>
    public bool IncludeConnectedEdges { get; set; } = true;

    /// <summary>Gets or sets whether groups referenced by visible nodes should be included.</summary>
    public bool IncludeNodeGroups { get; set; } = true;
}
