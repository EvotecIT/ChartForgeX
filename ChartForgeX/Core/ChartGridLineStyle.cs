using System;

namespace ChartForgeX.Core;

/// <summary>
/// Defines reusable visual tokens for cartesian grid and guide lines.
/// </summary>
public sealed class ChartGridLineStyle {
    private double _horizontalOpacity = 1.0;
    private double _verticalOpacity = 0.42;
    private double _strokeWidth = 1.0;
    private double _dash;
    private double _gap;

    /// <summary>
    /// Gets or sets whether horizontal value grid lines are drawn.
    /// </summary>
    public bool ShowHorizontalLines { get; set; } = true;

    /// <summary>
    /// Gets or sets whether vertical category grid lines are drawn.
    /// </summary>
    public bool ShowVerticalLines { get; set; } = true;

    /// <summary>
    /// Gets or sets horizontal grid line opacity.
    /// </summary>
    public double HorizontalOpacity {
        get => _horizontalOpacity;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            _horizontalOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets vertical grid line opacity.
    /// </summary>
    public double VerticalOpacity {
        get => _verticalOpacity;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            _verticalOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets grid line stroke width.
    /// </summary>
    public double StrokeWidth {
        get => _strokeWidth;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Grid stroke width must be greater than zero.");
            _strokeWidth = value;
        }
    }

    /// <summary>
    /// Gets or sets the dash length. Zero renders solid lines.
    /// </summary>
    public double Dash {
        get => _dash;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Grid dash length must be non-negative.");
            _dash = value;
        }
    }

    /// <summary>
    /// Gets or sets the dash gap length.
    /// </summary>
    public double Gap {
        get => _gap;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Grid dash gap must be non-negative.");
            _gap = value;
        }
    }

    /// <summary>
    /// Creates the default cartesian grid style.
    /// </summary>
    public static ChartGridLineStyle Default() => new();

    /// <summary>
    /// Creates a dashboard guide style with vertical dashed category guides.
    /// </summary>
    public static ChartGridLineStyle DashboardVerticalGuides() => new() {
        ShowHorizontalLines = false,
        ShowVerticalLines = true,
        HorizontalOpacity = 0.0,
        VerticalOpacity = 0.34,
        StrokeWidth = 1.0,
        Dash = 4.0,
        Gap = 6.0
    };

    /// <summary>
    /// Creates a copy that can be safely reused and customized.
    /// </summary>
    public ChartGridLineStyle Clone() => new() {
        ShowHorizontalLines = ShowHorizontalLines,
        ShowVerticalLines = ShowVerticalLines,
        HorizontalOpacity = HorizontalOpacity,
        VerticalOpacity = VerticalOpacity,
        StrokeWidth = StrokeWidth,
        Dash = Dash,
        Gap = Gap
    };

    /// <summary>
    /// Sets whether horizontal value grid lines are drawn.
    /// </summary>
    public ChartGridLineStyle WithHorizontalLines(bool visible = true) { ShowHorizontalLines = visible; return this; }

    /// <summary>
    /// Sets whether vertical category grid lines are drawn.
    /// </summary>
    public ChartGridLineStyle WithVerticalLines(bool visible = true) { ShowVerticalLines = visible; return this; }

    /// <summary>
    /// Sets horizontal grid line opacity.
    /// </summary>
    public ChartGridLineStyle WithHorizontalOpacity(double opacity) { HorizontalOpacity = opacity; return this; }

    /// <summary>
    /// Sets vertical grid line opacity.
    /// </summary>
    public ChartGridLineStyle WithVerticalOpacity(double opacity) { VerticalOpacity = opacity; return this; }

    /// <summary>
    /// Sets grid line stroke width.
    /// </summary>
    public ChartGridLineStyle WithStrokeWidth(double width) { StrokeWidth = width; return this; }

    /// <summary>
    /// Sets the dash and gap lengths. Use zero dash for solid lines.
    /// </summary>
    public ChartGridLineStyle WithDash(double dash, double gap) { Dash = dash; Gap = gap; return this; }
}
