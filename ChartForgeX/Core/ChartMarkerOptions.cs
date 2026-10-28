using System;

namespace ChartForgeX.Core;

/// <summary>Shared numeric point-marker geometry, visibility and paint overrides.</summary>
public sealed class ChartMarkerOptions {
    private ChartMarkerShape _shape;
    private double? _radius, _strokeWidth;
    private bool? _enabled;
    private ChartColor? _fill, _stroke;
    private ChartMarkerPath? _customPath;
    private ChartStrokeStyle? _outlineStroke;
    private double _strokeDashOffset;
    private bool _hasShape, _hasCustomPath, _hasStrokeDashOffset;

    /// <summary>Gets or sets the marker shape. The default is Circle.</summary>
    public ChartMarkerShape Shape {
        get => _shape;
        set {
            if (!Enum.IsDefined(typeof(ChartMarkerShape), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _shape = value; _hasShape = true; IsConfigured = true; HasNonRadiusConfiguration = true;
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

    /// <summary>Gets or sets an immutable unit path overriding <see cref="Shape"/>. Null restores the built-in shape.</summary>
    /// <remarks>Every contour closes automatically. Unit coordinates preserve radius bounds; bubble Area encoding
    /// normalizes actual even-odd fill area to its reference circle and can extend farther than that circle.</remarks>
    public ChartMarkerPath? CustomPath {
        get => _customPath;
        set { _customPath = value; _hasCustomPath = true; IsConfigured = true; HasNonRadiusConfiguration = true; }
    }

    /// <summary>Gets or sets immutable outline overrides, including dashes. Its explicit color/width override
    /// <see cref="Stroke"/> and <see cref="StrokeWidth"/>; null preserves the existing outline policy.</summary>
    public ChartStrokeStyle? OutlineStroke {
        get => _outlineStroke;
        set { _outlineStroke = value; IsConfigured = true; HasNonRadiusConfiguration = true; }
    }

    /// <summary>Gets or sets the finite logical dash phase. It affects an authored dashed outline only.</summary>
    public double StrokeDashOffset {
        get => _strokeDashOffset;
        set { ChartGuards.Finite(value, nameof(value)); _strokeDashOffset = value; _hasStrokeDashOffset = true; IsConfigured = true; HasNonRadiusConfiguration = true; }
    }

    internal bool IsConfigured { get; private set; }
    internal bool HasNonRadiusConfiguration { get; private set; }

    /// <summary>Copies fields directly so configuration tracking remains unchanged.</summary>
    internal ChartMarkerOptions Clone() => (ChartMarkerOptions)MemberwiseClone();

    /// <summary>Overlays authored endpoint settings on detached series markers without replacing their unconfigured geometry.</summary>
    internal ChartMarkerOptions Overlay(ChartMarkerOptions overrides) {
        var copy = Clone();
        copy._shape = overrides._hasShape ? overrides._shape : _shape;
        copy._customPath = overrides._hasCustomPath ? overrides._customPath : _customPath;
        copy._strokeDashOffset = overrides._hasStrokeDashOffset ? overrides._strokeDashOffset : _strokeDashOffset;
        copy._radius = overrides._radius ?? _radius; copy._enabled = overrides._enabled ?? _enabled;
        copy._fill = overrides._fill ?? _fill; copy._stroke = overrides._stroke ?? _stroke;
        copy._strokeWidth = overrides._strokeWidth ?? _strokeWidth; copy._outlineStroke = overrides._outlineStroke ?? _outlineStroke;
        copy._hasShape |= overrides._hasShape; copy._hasCustomPath |= overrides._hasCustomPath;
        copy._hasStrokeDashOffset |= overrides._hasStrokeDashOffset;
        copy.IsConfigured |= overrides.IsConfigured; copy.HasNonRadiusConfiguration |= overrides.HasNonRadiusConfiguration;
        return copy;
    }

    private static void NonNegative(double? value) {
        if (!value.HasValue) return;
        ChartGuards.Finite(value.Value, nameof(value));
        if (value.Value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Marker dimensions cannot be negative.");
    }
}
