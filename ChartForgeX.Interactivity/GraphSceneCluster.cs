using System.Collections.Generic;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes a semantic or computed group of graph nodes that an adapter may collapse, expand, or summarize.
/// </summary>
public sealed class GraphSceneCluster {
    private string _id = string.Empty;
    private string _label = string.Empty;

    /// <summary>Gets or sets the stable cluster identifier.</summary>
    public string Id { get => _id; set => _id = ChartInteractionText.RequiredToken(value, nameof(value), "Graph cluster ids"); }

    /// <summary>Gets or sets the cluster label shown by host controls and inspectors.</summary>
    public string Label { get => _label; set => _label = ChartInteractionText.RequiredText(value, nameof(value), "Graph cluster labels"); }

    /// <summary>Gets or sets an optional cluster kind such as hub, community, group, path, or outlier.</summary>
    public string? Kind { get; set; }

    /// <summary>Gets node ids contained by this cluster.</summary>
    public List<string> NodeIds { get; } = new();

    /// <summary>Gets or sets whether adapters should render this cluster collapsed by default.</summary>
    public bool Collapsed { get; set; }

    /// <summary>Gets arbitrary cluster metadata for search, tooltips, inspectors, and host event payloads.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}
