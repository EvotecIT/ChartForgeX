using System;

namespace ChartForgeX.Core;

/// <summary>
/// Defines reusable visual tokens for line, area boundary, trend, and slope strokes.
/// </summary>
public sealed class ChartLineVisualStyle {
    private double _ambientHaloOpacity;
    private double _ambientHaloStrokeExtra;
    private double _haloOpacity;
    private double _haloStrokeExtra;
    private double _highlightOpacity;
    private double _highlightStrokeRatio = 0.30;

    /// <summary>
    /// Gets or sets the opacity used for the widest soft color halo.
    /// </summary>
    public double AmbientHaloOpacity {
        get => _ambientHaloOpacity;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            _ambientHaloOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets the extra width added to the line for the widest soft color halo.
    /// </summary>
    public double AmbientHaloStrokeExtra {
        get => _ambientHaloStrokeExtra;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Ambient halo stroke extra must be non-negative.");
            _ambientHaloStrokeExtra = value;
        }
    }

    /// <summary>
    /// Gets or sets the opacity used for the readable stroke halo closest to the line.
    /// </summary>
    public double HaloOpacity {
        get => _haloOpacity;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            _haloOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets the extra width added to the line for the readable stroke halo.
    /// </summary>
    public double HaloStrokeExtra {
        get => _haloStrokeExtra;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Halo stroke extra must be non-negative.");
            _haloStrokeExtra = value;
        }
    }

    /// <summary>
    /// Gets or sets the opacity used for the subtle white line highlight.
    /// </summary>
    public double HighlightOpacity {
        get => _highlightOpacity;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            _highlightOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets the highlight stroke width as a ratio of the foreground line stroke width.
    /// </summary>
    public double HighlightStrokeRatio {
        get => _highlightStrokeRatio;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Highlight stroke ratio must be greater than zero.");
            _highlightStrokeRatio = value;
        }
    }

    /// <summary>
    /// Creates a lightly decorated line style for dashboard and report charts.
    /// </summary>
    public static ChartLineVisualStyle Premium() => new() {
        AmbientHaloOpacity = 0.014,
        AmbientHaloStrokeExtra = 2.4,
        HaloOpacity = 0.055,
        HaloStrokeExtra = 1.2,
        HighlightOpacity = 0.035,
        HighlightStrokeRatio = 0.18
    };

    /// <summary>
    /// Creates a plain single-stroke line style without halo or highlight layers.
    /// </summary>
    public static ChartLineVisualStyle Plain() => new();

    /// <summary>
    /// Creates a stronger luminous line style for deliberate glow-heavy dashboard treatments.
    /// </summary>
    public static ChartLineVisualStyle Luminous() => new() {
        AmbientHaloOpacity = 0.055,
        AmbientHaloStrokeExtra = 10.0,
        HaloOpacity = 0.14,
        HaloStrokeExtra = 5.0,
        HighlightOpacity = 0.13,
        HighlightStrokeRatio = 0.30
    };

    /// <summary>
    /// Creates the previous simpler line style with only a readable color halo.
    /// </summary>
    public static ChartLineVisualStyle Classic() => new() {
        AmbientHaloOpacity = 0,
        AmbientHaloStrokeExtra = 0,
        HaloOpacity = 0.14,
        HaloStrokeExtra = 5,
        HighlightOpacity = 0,
        HighlightStrokeRatio = 0.30
    };

    /// <summary>
    /// Creates a copy that can be safely reused and customized.
    /// </summary>
    public ChartLineVisualStyle Clone() => new() {
        AmbientHaloOpacity = AmbientHaloOpacity,
        AmbientHaloStrokeExtra = AmbientHaloStrokeExtra,
        HaloOpacity = HaloOpacity,
        HaloStrokeExtra = HaloStrokeExtra,
        HighlightOpacity = HighlightOpacity,
        HighlightStrokeRatio = HighlightStrokeRatio
    };

    /// <summary>
    /// Sets the widest soft color halo opacity and extra stroke width.
    /// </summary>
    public ChartLineVisualStyle WithAmbientHalo(double opacity, double strokeExtra) { AmbientHaloOpacity = opacity; AmbientHaloStrokeExtra = strokeExtra; return this; }

    /// <summary>
    /// Sets the readable stroke halo opacity and extra stroke width.
    /// </summary>
    public ChartLineVisualStyle WithHalo(double opacity, double strokeExtra) { HaloOpacity = opacity; HaloStrokeExtra = strokeExtra; return this; }

    /// <summary>
    /// Sets the subtle white highlight opacity and stroke-width ratio.
    /// </summary>
    public ChartLineVisualStyle WithHighlight(double opacity, double strokeRatio = 0.30) { HighlightOpacity = opacity; HighlightStrokeRatio = strokeRatio; return this; }
}
