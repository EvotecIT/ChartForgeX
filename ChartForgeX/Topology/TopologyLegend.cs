using System.Collections.Generic;

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
    /// Creates a default health and kind legend.
    /// </summary>
    /// <returns>A default topology legend.</returns>
    public static TopologyLegend Default() {
        var legend = new TopologyLegend();
        legend.Items.Add(new TopologyLegendItem { Label = "Healthy", Status = TopologyHealthStatus.Healthy, Kind = "status" });
        legend.Items.Add(new TopologyLegendItem { Label = "Warning", Status = TopologyHealthStatus.Warning, Kind = "status" });
        legend.Items.Add(new TopologyLegendItem { Label = "Critical", Status = TopologyHealthStatus.Critical, Kind = "status" });
        legend.Items.Add(new TopologyLegendItem { Label = "Unknown", Status = TopologyHealthStatus.Unknown, Kind = "status" });
        legend.Items.Add(new TopologyLegendItem { Label = "Hub site", NodeKind = TopologyNodeKind.HubSite, Kind = "node" });
        legend.Items.Add(new TopologyLegendItem { Label = "Branch site", NodeKind = TopologyNodeKind.BranchSite, Kind = "node" });
        legend.Items.Add(new TopologyLegendItem { Label = "Bridgehead", NodeKind = TopologyNodeKind.BridgeheadServer, Kind = "node" });
        legend.Items.Add(new TopologyLegendItem { Label = "Site link", EdgeKind = TopologyEdgeKind.SiteLink, Kind = "edge" });
        return legend;
    }
}

/// <summary>
/// Represents a topology legend item.
/// </summary>
public sealed class TopologyLegendItem {
    /// <summary>Gets or sets the item label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the item kind token.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional status represented by this item.</summary>
    public TopologyHealthStatus? Status { get; set; }

    /// <summary>Gets or sets the optional node kind represented by this item.</summary>
    public TopologyNodeKind? NodeKind { get; set; }

    /// <summary>Gets or sets the optional edge kind represented by this item.</summary>
    public TopologyEdgeKind? EdgeKind { get; set; }

    /// <summary>Gets or sets the optional color override.</summary>
    public string? Color { get; set; }
}
