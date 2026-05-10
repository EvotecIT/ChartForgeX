using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Describes a non-data map layer rendered behind or above a region map.
/// </summary>
public sealed class ChartMapLayer {
    /// <summary>
    /// Gets the map geometry definition rendered by this layer.
    /// </summary>
    public ChartMapDefinition Definition { get; }

    /// <summary>
    /// Gets the optional fill color. Null renders the layer without fill.
    /// </summary>
    public ChartColor? FillColor { get; }

    /// <summary>
    /// Gets the optional stroke color. Null renders the layer without stroke.
    /// </summary>
    public ChartColor? StrokeColor { get; }

    /// <summary>
    /// Gets the stroke width in rendered pixels.
    /// </summary>
    public double StrokeWidth { get; }

    /// <summary>
    /// Gets the semantic role used in SVG metadata.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartMapLayer"/> class.
    /// </summary>
    /// <param name="definition">The map geometry definition.</param>
    /// <param name="fillColor">The optional fill color.</param>
    /// <param name="strokeColor">The optional stroke color.</param>
    /// <param name="strokeWidth">The stroke width in rendered pixels.</param>
    /// <param name="role">The semantic role used in SVG metadata.</param>
    public ChartMapLayer(ChartMapDefinition definition, ChartColor? fillColor, ChartColor? strokeColor, double strokeWidth, string role) {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        if (strokeWidth < 0 || double.IsNaN(strokeWidth) || double.IsInfinity(strokeWidth)) throw new ArgumentOutOfRangeException(nameof(strokeWidth), strokeWidth, "Map layer stroke width must be non-negative and finite.");
        if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Map layer role must not be empty.", nameof(role));
        FillColor = fillColor;
        StrokeColor = strokeColor;
        StrokeWidth = strokeWidth;
        Role = role.Trim();
    }
}
