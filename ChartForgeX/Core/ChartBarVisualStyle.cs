using System;

namespace ChartForgeX.Core;

/// <summary>
/// Defines reusable visual tokens for bar and horizontal-bar marks.
/// </summary>
public sealed class ChartBarVisualStyle {
    private ChartBarStyle _kind;
    private double _bodyOpacity = 0.18;
    private double _capOpacity = 1.0;
    private double _capThickness = 7.0;
    private double _capInset = 2.0;
    private double _cornerRadius;
    private double _capShadowOpacity = 0.18;
    private double _capShadowOffset = 2.0;
    private double _capShadowSpread = 3.0;
    private double _capHighlightOpacity = 0.0;

    /// <summary>
    /// Gets or sets the bar visual treatment.
    /// </summary>
    public ChartBarStyle Kind {
        get => _kind;
        set {
            if (!Enum.IsDefined(typeof(ChartBarStyle), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown bar style.");
            _kind = value;
        }
    }

    /// <summary>
    /// Gets or sets the opacity used for translucent segment bodies.
    /// </summary>
    public double BodyOpacity {
        get => _bodyOpacity;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            _bodyOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets the opacity used for value-edge caps.
    /// </summary>
    public double CapOpacity {
        get => _capOpacity;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            _capOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets the cap thickness in pixels.
    /// </summary>
    public double CapThickness {
        get => _capThickness;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Cap thickness must be greater than zero.");
            _capThickness = value;
        }
    }

    /// <summary>
    /// Gets or sets the inset from the segment edge before a rounded cap starts.
    /// </summary>
    public double CapInset {
        get => _capInset;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Cap inset must be non-negative.");
            _capInset = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum translucent segment body corner radius in pixels.
    /// </summary>
    public double CornerRadius {
        get => _cornerRadius;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Corner radius must be non-negative.");
            _cornerRadius = value;
        }
    }

    /// <summary>
    /// Gets or sets the soft cap shadow opacity.
    /// </summary>
    public double CapShadowOpacity {
        get => _capShadowOpacity;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            _capShadowOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets the cap shadow offset in pixels.
    /// </summary>
    public double CapShadowOffset {
        get => _capShadowOffset;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Cap shadow offset must be non-negative.");
            _capShadowOffset = value;
        }
    }

    /// <summary>
    /// Gets or sets the extra soft-shadow thickness around value-edge caps.
    /// </summary>
    public double CapShadowSpread {
        get => _capShadowSpread;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Cap shadow spread must be non-negative.");
            _capShadowSpread = value;
        }
    }

    /// <summary>
    /// Gets or sets the white highlight opacity drawn over value-edge caps.
    /// </summary>
    public double CapHighlightOpacity {
        get => _capHighlightOpacity;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            _capHighlightOpacity = value;
        }
    }

    /// <summary>
    /// Creates the default solid bar style.
    /// </summary>
    public static ChartBarVisualStyle Solid() => new() { Kind = ChartBarStyle.Solid };

    /// <summary>
    /// Creates a soft dashboard capsule style for stacked or grouped bars.
    /// </summary>
    public static ChartBarVisualStyle DashboardCapsule() => new() {
        Kind = ChartBarStyle.SegmentedCapsule,
        BodyOpacity = 0.22,
        CapOpacity = 0.96,
        CapThickness = 7.0,
        CapInset = 2.2,
        CornerRadius = 0.0,
        CapShadowOpacity = 0.20,
        CapShadowOffset = 2.0,
        CapShadowSpread = 3.0,
        CapHighlightOpacity = 0.26
    };

    /// <summary>
    /// Creates a copy that can be safely reused and customized.
    /// </summary>
    public ChartBarVisualStyle Clone() => new() {
        Kind = Kind,
        BodyOpacity = BodyOpacity,
        CapOpacity = CapOpacity,
        CapThickness = CapThickness,
        CapInset = CapInset,
        CornerRadius = CornerRadius,
        CapShadowOpacity = CapShadowOpacity,
        CapShadowOffset = CapShadowOffset,
        CapShadowSpread = CapShadowSpread,
        CapHighlightOpacity = CapHighlightOpacity
    };

    /// <summary>
    /// Sets the bar visual treatment.
    /// </summary>
    public ChartBarVisualStyle WithKind(ChartBarStyle kind) { Kind = kind; return this; }

    /// <summary>
    /// Sets the opacity used for translucent segment bodies.
    /// </summary>
    public ChartBarVisualStyle WithBodyOpacity(double opacity) { BodyOpacity = opacity; return this; }

    /// <summary>
    /// Sets the opacity used for value-edge caps.
    /// </summary>
    public ChartBarVisualStyle WithCapOpacity(double opacity) { CapOpacity = opacity; return this; }

    /// <summary>
    /// Sets the cap thickness in pixels.
    /// </summary>
    public ChartBarVisualStyle WithCapThickness(double thickness) { CapThickness = thickness; return this; }

    /// <summary>
    /// Sets the inset from the segment edge before a rounded cap starts.
    /// </summary>
    public ChartBarVisualStyle WithCapInset(double inset) { CapInset = inset; return this; }

    /// <summary>
    /// Sets the maximum translucent segment body corner radius in pixels.
    /// </summary>
    public ChartBarVisualStyle WithCornerRadius(double radius) { CornerRadius = radius; return this; }

    /// <summary>
    /// Sets the soft cap shadow opacity and offset in pixels.
    /// </summary>
    public ChartBarVisualStyle WithCapShadow(double opacity, double offset) { CapShadowOpacity = opacity; CapShadowOffset = offset; return this; }

    /// <summary>
    /// Sets the soft cap shadow opacity, offset, and spread in pixels.
    /// </summary>
    public ChartBarVisualStyle WithCapShadow(double opacity, double offset, double spread) { CapShadowOpacity = opacity; CapShadowOffset = offset; CapShadowSpread = spread; return this; }

    /// <summary>
    /// Sets the white highlight opacity drawn over value-edge caps.
    /// </summary>
    public ChartBarVisualStyle WithCapHighlight(double opacity) { CapHighlightOpacity = opacity; return this; }
}
