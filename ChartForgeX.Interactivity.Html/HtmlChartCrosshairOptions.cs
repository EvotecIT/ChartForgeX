using System;
using ChartForgeX.Interactivity;
using ChartForgeX.Primitives;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Configures HTML crosshair presentation independently of tooltip acquisition and palette.</summary>
public sealed class HtmlChartCrosshairOptions {
    private double? _strokeWidth;
    private HtmlChartCrosshairLineStyle? _lineStyle;

    /// <summary>
    /// Gets or sets whether the crosshair includes a target label. The default is true for every palette.
    /// The <see cref="ChartInteractionFeatures.Crosshair"/> feature controls whether the guide is enabled.
    /// </summary>
    public bool ShowLabel { get; set; } = true;

    /// <summary>
    /// Gets or sets the color of both guide lines. Null retains the palette's line color.
    /// A transparent color hides the lines while retaining the label and guide identity.
    /// Explicit line styling uses a crisp stroke without glow.
    /// </summary>
    public ChartColor? Stroke { get; set; }

    /// <summary>
    /// Gets or sets the finite, non-negative width of both guide lines in CSS pixels.
    /// Null retains the default width. Zero hides the lines without disabling the guide or its label.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The width is negative or non-finite.</exception>
    public double? StrokeWidth {
        get => _strokeWidth;
        set {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0))
                throw new ArgumentOutOfRangeException(nameof(StrokeWidth), "Crosshair widths must be finite and non-negative.");
            _strokeWidth = value;
        }
    }

    /// <summary>Gets or sets the pattern of both guide lines. Null retains the palette's line pattern.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The line style is not a defined enum value.</exception>
    public HtmlChartCrosshairLineStyle? LineStyle {
        get => _lineStyle;
        set {
            if (value.HasValue && !Enum.IsDefined(typeof(HtmlChartCrosshairLineStyle), value.Value))
                throw new ArgumentOutOfRangeException(nameof(LineStyle));
            _lineStyle = value;
        }
    }

    internal bool HasLineOverrides => Stroke.HasValue || StrokeWidth.HasValue || LineStyle.HasValue;

    internal void CopyTo(HtmlChartCrosshairOptions destination) {
        destination.ShowLabel = ShowLabel;
        destination.Stroke = Stroke;
        destination.StrokeWidth = StrokeWidth;
        destination.LineStyle = LineStyle;
    }
}

/// <summary>Controls the painted pattern of HTML crosshair guide lines.</summary>
public enum HtmlChartCrosshairLineStyle {
    /// <summary>Draws a continuous stroke.</summary>
    Solid,
    /// <summary>Draws a dashed stroke.</summary>
    Dashed,
    /// <summary>Draws a dotted stroke.</summary>
    Dotted
}
