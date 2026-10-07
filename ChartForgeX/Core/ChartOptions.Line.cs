using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartLineVisualStyle _lineVisualStyle = ChartLineVisualStyle.Premium();
    private readonly ChartLineVisualStyle _plainLineVisualStyle = ChartLineVisualStyle.Plain();
    private bool _hasExplicitLineStyle;

    /// <summary>
    /// Gets or sets reusable visual tokens for line, area boundary, trend, and slope strokes.
    /// </summary>
    public ChartLineVisualStyle LineVisualStyle {
        get => !_hasExplicitLineStyle && Theme.FlatMarks ? _plainLineVisualStyle : _lineVisualStyle;
        set { _lineVisualStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone(); _hasExplicitLineStyle = true; }
    }

    /// <summary>Distinguishes authored line treatment from the untouched legacy theme defaults.</summary>
    /// <remarks>As with grid styles, getter mutations equal to the original tokens cannot express distinct intent; assign a complete style in that case.</remarks>
    internal bool HasPreparedLineVisualStyle => _hasExplicitLineStyle
        || !SameLineTokens(_lineVisualStyle, ChartLineVisualStyle.Premium())
        || !SameLineTokens(_plainLineVisualStyle, ChartLineVisualStyle.Plain());

    private static bool SameLineTokens(ChartLineVisualStyle left, ChartLineVisualStyle right) =>
        left.AmbientHaloOpacity == right.AmbientHaloOpacity && left.AmbientHaloStrokeExtra == right.AmbientHaloStrokeExtra
        && left.HaloOpacity == right.HaloOpacity && left.HaloStrokeExtra == right.HaloStrokeExtra
        && left.HighlightOpacity == right.HighlightOpacity && left.HighlightStrokeRatio == right.HighlightStrokeRatio;
}
