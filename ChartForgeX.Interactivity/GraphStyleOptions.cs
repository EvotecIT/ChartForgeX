using System;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes adapter-neutral graph node styling hints, including common vis-network node option concepts.
/// </summary>
public sealed class GraphNodeStyle {
    /// <summary>Gets or sets the node fill color.</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Gets or sets the node border color.</summary>
    public string? BorderColor { get; set; }

    /// <summary>Gets or sets the node label color.</summary>
    public string? LabelColor { get; set; }

    /// <summary>Gets or sets an optional label background color.</summary>
    public string? LabelBackgroundColor { get; set; }

    /// <summary>Gets or sets whether the adapter should render a node shadow when supported.</summary>
    public bool Shadow { get; set; }

    internal void Validate(string nodeId) {
        GraphStyleGuards.ValidateCssColor(BackgroundColor, "node background color", nodeId);
        GraphStyleGuards.ValidateCssColor(BorderColor, "node border color", nodeId);
        GraphStyleGuards.ValidateCssColor(LabelColor, "node label color", nodeId);
        GraphStyleGuards.ValidateCssColor(LabelBackgroundColor, "node label background color", nodeId);
    }
}

/// <summary>
/// Describes adapter-neutral graph edge styling hints, including common vis-network edge option concepts.
/// </summary>
public sealed class GraphEdgeStyle {
    /// <summary>Gets or sets the edge stroke color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the edge label color.</summary>
    public string? LabelColor { get; set; }

    /// <summary>Gets or sets the preferred edge stroke width.</summary>
    public double? Width { get; set; }

    /// <summary>Gets or sets whether the edge participates in runtime physics solvers.</summary>
    public bool Physics { get; set; } = true;

    /// <summary>Gets or sets whether the edge is hidden from visual output while remaining available in metadata.</summary>
    public bool Hidden { get; set; }

    internal void Validate(string edgeId) {
        GraphStyleGuards.ValidateCssColor(Color, "edge color", edgeId);
        GraphStyleGuards.ValidateCssColor(LabelColor, "edge label color", edgeId);
        if (Width.HasValue && (double.IsNaN(Width.Value) || double.IsInfinity(Width.Value) || Width.Value <= 0)) throw new InvalidOperationException("Graph scene edge width must be finite and greater than zero: " + edgeId);
    }
}

internal static class GraphStyleGuards {
    internal static void ValidateCssColor(string? value, string name, string id) {
        if (string.IsNullOrWhiteSpace(value)) return;
        var cssValue = value!;
        if (cssValue.Length > 80) throw new InvalidOperationException("Graph scene " + name + " is too long: " + id);
        foreach (var ch in cssValue) {
            if (char.IsLetterOrDigit(ch) || ch is '#' or '(' or ')' or ',' or '.' or '%' or '-' or '_' or ' ') continue;
            throw new InvalidOperationException("Graph scene " + name + " contains unsupported CSS color characters: " + id);
        }
    }
}
