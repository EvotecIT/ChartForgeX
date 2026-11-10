using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Themes;

/// <summary>Theme defaults for one series kind, such as every line or every bar in charts that use the theme.</summary>
/// <remarks>
/// Each unset value inherits the global theme and family default. An explicit series setting always wins:
/// <see cref="ChartSeries.StrokeWidth"/>, <see cref="ChartSeries.StrokeStyle"/>, <see cref="ChartSeries.Markers"/>,
/// <see cref="ChartSeries.Interpolation"/> and <see cref="ChartSeries.DataLabelStyle"/> fields replace the matching default.
/// Chart-level data-label styling and an authored bar corner radius also take precedence over these defaults.
/// A theme snapshots its defaults, so later edits to this instance do not change an existing theme.
/// </remarks>
public sealed class VisualSeriesDefaults {
    private double? _strokeWidth, _cornerRadius;
    private ChartInterpolation? _interpolation;
    private double[]? _dash;

    /// <summary>Gets or sets the stroke width for this kind's lines, area edges and outlines. Null keeps the theme width.</summary>
    public double? StrokeWidth {
        get => _strokeWidth;
        set {
            if (value.HasValue) {
                ChartGuards.Finite(value.Value, nameof(value));
                if (value.Value <= 0 || value.Value > 1024) throw new ArgumentOutOfRangeException(nameof(value), value, "Stroke width must be greater than zero and at most 1024.");
            }
            _strokeWidth = value;
        }
    }

    /// <summary>Gets or sets alternating dash/gap lengths for this kind's connected strokes. Null keeps the family pattern; empty is solid.</summary>
    public IReadOnlyList<double>? Dash {
        get => _dash == null ? null : Array.AsReadOnly(_dash);
        set => _dash = value == null ? null : new ChartStrokeStyle(dash: value).Dash!.ToArray();
    }

    /// <summary>Gets or sets connected-path interpolation for line and area kinds. Null keeps the family default.</summary>
    public ChartInterpolation? Interpolation {
        get => _interpolation;
        set {
            if (value.HasValue && !Enum.IsDefined(typeof(ChartInterpolation), value.Value)) throw new ArgumentOutOfRangeException(nameof(value));
            _interpolation = value;
        }
    }

    /// <summary>Gets or sets the corner radius for bar, horizontal bar, range bar and waterfall marks. Null keeps the theme bar radius.</summary>
    public double? CornerRadius {
        get => _cornerRadius;
        set {
            if (value.HasValue) {
                ChartGuards.Finite(value.Value, nameof(value));
                if (value.Value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Corner radius cannot be negative.");
            }
            _cornerRadius = value;
        }
    }

    /// <summary>Gets the marker shape, radius, visibility and paint defaults for kinds that draw point markers.</summary>
    public ChartMarkerOptions Markers { get; private set; } = new();

    /// <summary>Gets the data-label text defaults for this kind.</summary>
    public TextStyleOverride DataLabelStyle { get; private set; } = new();

    /// <summary>Configures marker defaults.</summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The current defaults.</returns>
    public VisualSeriesDefaults ConfigureMarkers(Action<ChartMarkerOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Markers = ChartOptionConfiguration.Configure(Markers, configure, options => options.Clone());
        return this;
    }

    /// <summary>Configures data-label text defaults.</summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The current defaults.</returns>
    public VisualSeriesDefaults ConfigureDataLabelStyle(Action<TextStyleOverride> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        DataLabelStyle = ChartOptionConfiguration.Configure(DataLabelStyle, configure, style => style.Clone());
        return this;
    }

    internal bool IsEmpty => !_strokeWidth.HasValue && _dash == null && !_interpolation.HasValue && !_cornerRadius.HasValue
        && !Markers.IsConfigured && !DataLabelStyle.HasOverrides;

    internal VisualSeriesDefaults Clone() {
        var copy = (VisualSeriesDefaults)MemberwiseClone();
        copy.Markers = Markers.Clone(); copy.DataLabelStyle = DataLabelStyle.Clone();
        return copy;
    }

    /// <summary>Rejects settings the kind never draws, so a misplaced theme value fails instead of being ignored.</summary>
    internal void Validate(ChartSeriesKind kind) {
        var name = "Theme defaults for " + kind;
        if (Markers.IsConfigured && !ChartSeriesKindTraits.SupportsMarkers(kind)) throw new ArgumentException(name + " cannot set markers; the kind has no point markers.");
        if (kind == ChartSeriesKind.Bubble && Markers.Radius.HasValue) throw new ArgumentException(name + " cannot set a marker radius; bubbles use the bubble size scale.");
        if (_interpolation.HasValue && !ChartSeriesKindTraits.SupportsInterpolation(kind)) throw new ArgumentException(name + " cannot set a curve; the kind has no connected path.");
        if (_cornerRadius.HasValue && !ChartSeriesKindTraits.UsesBarSurface(kind)) throw new ArgumentException(name + " cannot set a corner radius; the kind has no bar marks.");
    }
}
