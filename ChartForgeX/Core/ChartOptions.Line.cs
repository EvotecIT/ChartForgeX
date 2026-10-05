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
}
