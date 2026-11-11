using ChartForgeX.Themes;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets the theme's bar corner radius for this kind, captured with the preparation view.</summary>
    internal double? ThemeCornerRadius { get; private set; }

    /// <summary>
    /// Resolves the theme's defaults for this kind into a preparation view. Every authored series value wins; chart-level
    /// data-label styling sits between the series and the kind default. The authored series is never changed.
    /// </summary>
    internal ChartSeries WithThemeDefaults(VisualTheme theme, ChartOptions options) {
        var defaults = theme.SeriesDefaultsSnapshot(Kind);
        if (defaults == null) return this;
        var view = (ChartSeries)MemberwiseClone();
        if (defaults.StrokeWidth is { } width && !HasExplicitStrokeWidth) { view._strokeWidth = width; view.HasExplicitStrokeWidth = true; }
        if (defaults.Dash is { } dash && StrokeStyle?.Dash == null)
            view.StrokeStyle = new ChartStrokeStyle(StrokeStyle?.Color, StrokeStyle?.Width, StrokeStyle?.Opacity ?? 1, dash);
        if (defaults.Markers.IsConfigured) view.Markers = defaults.Markers.Overlay(Markers);
        // An authored step position is a curve choice too, so it keeps the family interpolation.
        if (defaults.Interpolation is { } curve && !HasExplicitInterpolation && StepPosition == ChartStepPosition.End) view._interpolation = curve;
        if (defaults.DataLabelStyle.HasOverrides) view.DataLabelStyle = defaults.DataLabelStyle.Overlay(options.DataLabelStyle).Overlay(DataLabelStyle);
        view.ThemeCornerRadius = defaults.CornerRadius;
        return view;
    }
}
