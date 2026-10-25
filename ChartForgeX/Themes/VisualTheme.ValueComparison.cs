namespace ChartForgeX.Themes;

public sealed partial class VisualTheme {
    // Native value comparison does not serialize or apply wire-only array/text budgets during compilation.
    internal bool HasTopologyDefaultExtras() {
        var ordinary = Graphite();
        if (ReferenceEquals(this, ordinary)) return true;
        return Spacing.Equals(ordinary.Spacing)
            && SeriesStrokeWidth.Equals(ordinary.SeriesStrokeWidth)
            && MarkerRadius.Equals(ordinary.MarkerRadius)
            && AreaOpacity.Equals(ordinary.AreaOpacity)
            && BarRadius.Equals(ordinary.BarRadius)
            && MaximumBarWidth.Equals(ordinary.MaximumBarWidth)
            && GridStrokeWidth.Equals(ordinary.GridStrokeWidth)
            && AxisStrokeWidth.Equals(ordinary.AxisStrokeWidth)
            && CardRadius.Equals(ordinary.CardRadius)
            && PlotRadius.Equals(ordinary.PlotRadius)
            && CardShadowOpacity.Equals(ordinary.CardShadowOpacity)
            && CardShadowColor.Equals(ordinary.CardShadowColor)
            && GaugeStrokeWidth.Equals(ordinary.GaugeStrokeWidth)
            && GaugeBandWidth.Equals(ordinary.GaugeBandWidth)
            && Typography.MonospaceFamily.Equals(ordinary.Typography.MonospaceFamily)
            && Typography.AxisTitleSize.Equals(ordinary.Typography.AxisTitleSize)
            && Typography.AxisSize.Equals(ordinary.Typography.AxisSize)
            && Typography.ScalarValueSize.Equals(ordinary.Typography.ScalarValueSize)
            && Typography.CenterValueSize.Equals(ordinary.Typography.CenterValueSize)
            && Typography.TileLabelSize.Equals(ordinary.Typography.TileLabelSize)
            && Typography.TileValueSize.Equals(ordinary.Typography.TileValueSize)
            && Typography.TileDetailSize.Equals(ordinary.Typography.TileDetailSize)
            && _light.SameValues(ordinary._light) && _dark.SameValues(ordinary._dark);
    }
}

public sealed partial class VisualThemeColors {
    internal bool SameValues(VisualThemeColors other) => _tokens.SameValues(other._tokens);
}
