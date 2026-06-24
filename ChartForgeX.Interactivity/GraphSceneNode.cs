using System.Collections.Generic;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes a graph node independently from a particular renderer or browser runtime.
/// </summary>
public sealed class GraphSceneNode {
    private string _id = string.Empty;
    private string _label = string.Empty;
    private string? _kind;
    private string? _groupId;
    private string? _clusterId;
    private string? _imageUrl;
    private string? _imageAlt;
    private string? _iconText;
    private double _x;
    private double _y;
    private bool _hasExplicitPosition;

    /// <summary>Gets or sets the stable node identifier.</summary>
    public string Id { get => _id; set => _id = ChartInteractionText.RequiredToken(value, nameof(value), "Graph node ids"); }

    /// <summary>Gets or sets the node label shown by adapters.</summary>
    public string Label { get => _label; set => _label = ChartInteractionText.RequiredText(value, nameof(value), "Graph node labels"); }

    /// <summary>Gets or sets an optional product-neutral node kind such as service, identity, endpoint, database, or person.</summary>
    public string? Kind { get => _kind; set => _kind = ChartInteractionText.OptionalToken(value, nameof(value), "Graph node kinds"); }

    /// <summary>Gets or sets an optional logical group id used for filtering, color, or layout gravity.</summary>
    public string? GroupId { get => _groupId; set => _groupId = ChartInteractionText.OptionalToken(value, nameof(value), "Graph node group ids"); }

    /// <summary>Gets or sets an optional cluster id used for collapsed large-graph summaries.</summary>
    public string? ClusterId { get => _clusterId; set => _clusterId = ChartInteractionText.OptionalToken(value, nameof(value), "Graph node cluster ids"); }

    /// <summary>Gets or sets an optional semantic status such as healthy, warning, critical, unknown, or muted.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets the preferred node shape used by adapters that support richer graph marks.</summary>
    public GraphNodeShape Shape { get; set; } = GraphNodeShape.Circle;

    /// <summary>Gets or sets an optional image URI for image-backed graph nodes. Data URIs keep static output self-contained.</summary>
    public string? ImageUrl { get => _imageUrl; set => _imageUrl = ChartInteractionText.OptionalText(value, nameof(value), "Graph node image URLs"); }

    /// <summary>Gets or sets optional alternative text for image-backed graph nodes.</summary>
    public string? ImageAlt { get => _imageAlt; set => _imageAlt = ChartInteractionText.OptionalText(value, nameof(value), "Graph node image alternative text"); }

    /// <summary>Gets or sets an optional short icon glyph or text shown inside the node mark.</summary>
    public string? IconText { get => _iconText; set => _iconText = ChartInteractionText.OptionalText(value, nameof(value), "Graph node icon text"); }

    /// <summary>Gets or sets the current x-coordinate in graph scene space.</summary>
    public double X {
        get => _x;
        set {
            _x = value;
            _hasExplicitPosition = true;
        }
    }

    /// <summary>Gets or sets the current y-coordinate in graph scene space.</summary>
    public double Y {
        get => _y;
        set {
            _y = value;
            _hasExplicitPosition = true;
        }
    }

    /// <summary>Gets whether a caller explicitly supplied scene coordinates, including the origin.</summary>
    public bool HasExplicitPosition => _hasExplicitPosition;

    /// <summary>Gets or sets the node radius or half-size used by lightweight adapters.</summary>
    public double Size { get; set; } = 8;

    /// <summary>Gets or sets whether physics adapters should keep the node at its current position.</summary>
    public bool Fixed { get; set; }

    /// <summary>Gets arbitrary node metadata for search, tooltips, inspectors, and host event payloads.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>
/// Describes adapter-neutral graph node mark preferences.
/// </summary>
public enum GraphNodeShape {
    /// <summary>Use a circular node mark.</summary>
    Circle,

    /// <summary>Use a rounded rectangular node mark.</summary>
    Box,

    /// <summary>Use an image-backed node mark when an image URI is available.</summary>
    Image
}
