using System.Collections.Generic;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes a graph edge independently from renderer-specific line geometry.
/// </summary>
public sealed class GraphSceneEdge {
    private string _id = string.Empty;
    private string _sourceNodeId = string.Empty;
    private string _targetNodeId = string.Empty;
    private string? _kind;

    /// <summary>Gets or sets the stable edge identifier.</summary>
    public string Id { get => _id; set => _id = ChartInteractionText.RequiredToken(value, nameof(value), "Graph edge ids"); }

    /// <summary>Gets or sets the source node id.</summary>
    public string SourceNodeId { get => _sourceNodeId; set => _sourceNodeId = ChartInteractionText.RequiredToken(value, nameof(value), "Graph edge source ids"); }

    /// <summary>Gets or sets the target node id.</summary>
    public string TargetNodeId { get => _targetNodeId; set => _targetNodeId = ChartInteractionText.RequiredToken(value, nameof(value), "Graph edge target ids"); }

    /// <summary>Gets or sets an optional relationship label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets an optional product-neutral edge kind such as dependency, ownership, authentication, or dataflow.</summary>
    public string? Kind { get => _kind; set => _kind = ChartInteractionText.OptionalToken(value, nameof(value), "Graph edge kinds"); }

    /// <summary>Gets or sets an optional semantic status such as healthy, warning, critical, unknown, or muted.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the relationship weight used by physics and clustering adapters.</summary>
    public double Weight { get; set; } = 1;

    /// <summary>Gets or sets the preferred spring length for physics adapters. A value of zero lets the active profile decide.</summary>
    public double Length { get; set; }

    /// <summary>Gets or sets whether adapters should render a directional arrow toward the target node.</summary>
    public bool Directed { get; set; }

    /// <summary>Gets or sets the preferred edge geometry used by adapters that support richer graph lines.</summary>
    public GraphEdgeShape Shape { get; set; } = GraphEdgeShape.Line;

    /// <summary>Gets or sets the curve offset in scene units for curved edge rendering.</summary>
    public double Curvature { get; set; }

    /// <summary>Gets or sets whether adapters should draw the edge with a dashed stroke.</summary>
    public bool Dashed { get; set; }

    /// <summary>Gets or sets whether adapters should draw the relationship label when labels are enabled.</summary>
    public bool ShowLabel { get; set; } = true;

    /// <summary>Gets arbitrary edge metadata for search, tooltips, inspectors, and host event payloads.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>
/// Describes adapter-neutral graph edge geometry preferences.
/// </summary>
public enum GraphEdgeShape {
    /// <summary>Use a straight edge.</summary>
    Line,

    /// <summary>Use a quadratic curved edge.</summary>
    Curve
}
