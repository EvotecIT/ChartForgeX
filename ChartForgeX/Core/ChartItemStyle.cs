namespace ChartForgeX.Core;

/// <summary>Immutable per-observation paint and marker overrides, evaluated once while preparing a chart.</summary>
public sealed class ChartItemStyle {
    private readonly ChartMarkerOptions? _markers;
    /// <summary>Creates item overrides. Null fields retain the series, ordinal override or family default.</summary>
    /// <param name="color">A solid base color, overriding series and ordinal colors.</param>
    /// <param name="fill">An authored gradient, pattern or embedded-image fill.</param>
    /// <param name="stroke">Outline color, width, opacity and dash overrides.</param>
    /// <param name="markers">Marker overrides, copied immediately so later edits cannot change this style.</param>
    public ChartItemStyle(ChartColor? color = null, ChartFill? fill = null, ChartStrokeStyle? stroke = null, ChartMarkerOptions? markers = null) {
        Color = color; Fill = fill; Stroke = stroke; _markers = markers?.Clone();
    }
    /// <summary>Gets the optional solid base color.</summary>
    public ChartColor? Color { get; }
    /// <summary>Gets the optional authored fill.</summary>
    public ChartFill? Fill { get; }
    /// <summary>Gets immutable outline overrides.</summary>
    public ChartStrokeStyle? Stroke { get; }
    /// <summary>Gets an independent copy of marker overrides.</summary>
    public ChartMarkerOptions? Markers => _markers?.Clone();
    internal ChartMarkerOptions? MarkerSnapshot => _markers;
}
