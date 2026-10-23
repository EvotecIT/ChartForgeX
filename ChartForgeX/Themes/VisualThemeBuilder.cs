using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>Builds one immutable paired theme. Mutable values are never retained by the resulting theme.</summary>
public sealed class VisualThemeBuilder {
    /// <summary>Creates a builder from the canonical theme.</summary>
    public VisualThemeBuilder() : this(VisualTheme.Graphite()) { }

    /// <summary>Creates an independent editable copy of an existing theme.</summary>
    public VisualThemeBuilder(VisualTheme theme) {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        Light = theme.Resolve(VisualThemeMode.Light).ToTokens(); Dark = theme.Resolve(VisualThemeMode.Dark).ToTokens();
        Typography = theme.Typography; Spacing = theme.Spacing; SeriesStrokeWidth = theme.SeriesStrokeWidth;
        MarkerRadius = theme.MarkerRadius; AreaOpacity = theme.AreaOpacity; BarRadius = theme.BarRadius;
        GridStrokeWidth = theme.GridStrokeWidth; AxisStrokeWidth = theme.AxisStrokeWidth; CardRadius = theme.CardRadius;
        PlotRadius = theme.PlotRadius; CardShadowOpacity = theme.CardShadowOpacity; CardShadowColor = theme.CardShadowColor;
        GaugeStrokeWidth = theme.GaugeStrokeWidth; GaugeBandWidth = theme.GaugeBandWidth;
        MaximumBarWidth = theme.MaximumBarWidth;
    }

    /// <summary>Gets the independently editable light color palette.</summary>
    public VisualDesignTokens Light { get; }
    /// <summary>Gets the independently editable dark color palette.</summary>
    public VisualDesignTokens Dark { get; }
    /// <summary>Gets or sets the complete shared font scale.</summary>
    public VisualTypography Typography { get; set; }
    /// <summary>Gets or sets the standard layout gap.</summary>
    public double Spacing { get; set; }
    /// <summary>Gets or sets the data-line width.</summary>
    public double SeriesStrokeWidth { get; set; }
    /// <summary>Gets or sets the point radius.</summary>
    public double MarkerRadius { get; set; }
    /// <summary>Gets or sets the area fill opacity.</summary>
    public double AreaOpacity { get; set; }
    /// <summary>Gets or sets the bar corner radius.</summary>
    public double BarRadius { get; set; }
    /// <summary>Gets or sets the maximum regular bar thickness in drawing pixels, default 64.</summary>
    public double MaximumBarWidth { get; set; }
    /// <summary>Gets or sets the plot-guide width.</summary>
    public double GridStrokeWidth { get; set; }
    /// <summary>Gets or sets the axis width.</summary>
    public double AxisStrokeWidth { get; set; }
    /// <summary>Gets or sets the outer card corner radius.</summary>
    public double CardRadius { get; set; }
    /// <summary>Gets or sets the plot surface corner radius.</summary>
    public double PlotRadius { get; set; }
    /// <summary>Gets or sets the card shadow opacity.</summary>
    public double CardShadowOpacity { get; set; }
    /// <summary>Gets or sets the card shadow color, retaining alpha.</summary>
    public ChartColor CardShadowColor { get; set; }
    /// <summary>Gets or sets the gauge value and track width.</summary>
    public double GaugeStrokeWidth { get; set; }
    /// <summary>Gets or sets the gauge threshold band width.</summary>
    public double GaugeBandWidth { get; set; }

    /// <summary>Gets the editable palette for an explicit mode.</summary>
    public VisualDesignTokens Colors(VisualThemeMode mode) => mode switch {
        VisualThemeMode.Light => Light, VisualThemeMode.Dark => Dark,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    /// <summary>Validates and snapshots all colors, typography, geometry and effects.</summary>
    public VisualTheme Build() => new(Light, Dark, Typography ?? throw new ArgumentNullException(nameof(Typography)),
        Spacing, SeriesStrokeWidth, MarkerRadius, AreaOpacity, BarRadius, GridStrokeWidth, AxisStrokeWidth,
        CardRadius, CardShadowOpacity, CardShadowColor, GaugeStrokeWidth, GaugeBandWidth, PlotRadius, MaximumBarWidth);
}
