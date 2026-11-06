namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    /// <summary>Gets or sets the node's accessible name, for example a localized label, role and health summary.</summary>
    /// <remarks>When unset, renderers combine the full label, subtitle, badge and health state; visual truncation does not shorten this name.</remarks>
    public string? AccessibleName { get; set; }
}
