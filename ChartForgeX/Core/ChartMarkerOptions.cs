using System;

namespace ChartForgeX.Core;

/// <summary>Shared numeric point-marker geometry, visibility and paint overrides.</summary>
public sealed class ChartMarkerOptions {
    private ChartMarkerShape _shape;
    private double? _radius, _strokeWidth;
    private bool? _enabled;
    private ChartColor? _fill, _stroke;

    /// <summary>Gets or sets the marker shape. The default is Circle.</summary>
    public ChartMarkerShape Shape {
        get => _shape;
        set {
            if (!Enum.IsDefined(typeof(ChartMarkerShape), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _shape = value; IsConfigured = true; HasNonRadiusConfiguration = true;
        }
    }

    /// <summary>
    /// Gets or sets the logical marker radius. Null preserves the family and theme default; zero hides glyphs.
    /// Bubble series use <see cref="ChartOptions.Bubble"/> radius bounds and reject a non-null marker radius.
    /// </summary>
    public double? Radius {
        get => _radius;
        set { NonNegative(value); _radius = value; IsConfigured = true; }
    }

    /// <summary>
    /// Gets or sets whether all markers are shown. Null preserves the family visibility policy;
    /// false hides glyphs while retaining source descriptions, lines and areas.
    /// </summary>
    public bool? Enabled { get => _enabled; set { _enabled = value; IsConfigured = true; HasNonRadiusConfiguration = true; } }

    /// <summary>Gets or sets the marker fill. Null preserves the family paint; explicit point colors take precedence.</summary>
    public ChartColor? Fill { get => _fill; set { _fill = value; IsConfigured = true; HasNonRadiusConfiguration = true; } }

    /// <summary>Gets or sets the marker outline color. Null preserves the family outline.</summary>
    public ChartColor? Stroke { get => _stroke; set { _stroke = value; IsConfigured = true; HasNonRadiusConfiguration = true; } }

    /// <summary>
    /// Gets or sets the logical outline width, independently of the series line width. Null preserves the family default.
    /// A positive explicit width creates an outline in the marker fill color when the family has none; zero removes it.
    /// </summary>
    public double? StrokeWidth {
        get => _strokeWidth;
        set { NonNegative(value); _strokeWidth = value; IsConfigured = true; HasNonRadiusConfiguration = true; }
    }

    internal bool IsConfigured { get; private set; }
    internal bool HasNonRadiusConfiguration { get; private set; }

    private static void NonNegative(double? value) {
        if (!value.HasValue) return;
        ChartGuards.Finite(value.Value, nameof(value));
        if (value.Value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Marker dimensions cannot be negative.");
    }
}
