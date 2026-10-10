using System;

namespace ChartForgeX.Core;

/// <summary>Controls hierarchical treemap interiors and their independent numeric color legend.</summary>
public sealed class ChartTreemapOptions {
    private double _groupPadding = 6;
    private double _gap = 3;

    /// <summary>Gets or sets the non-negative padding inside each group, in pixels. Default: six.</summary>
    public double GroupPadding {
        get => _groupPadding;
        set { NonNegative(value, nameof(value)); _groupPadding = value; }
    }
    /// <summary>Gets or sets the non-negative gap between sibling rectangles, in pixels. Small cells bound their own gap. Default: three.</summary>
    public double Gap {
        get => _gap;
        set { NonNegative(value, nameof(value)); _gap = value; }
    }
    /// <summary>Gets or sets whether measured group headers are reserved and drawn. Default: true.</summary>
    public bool ShowGroupLabels { get; set; } = true;
    /// <summary>Gets or sets the numeric color scale. When omitted, supplied color values use the theme's sequential ramp.</summary>
    public ChartColorScale? ColorScale { get; set; }
    /// <summary>Gets or sets whether the independent color legend is drawn when the chart legend is enabled. Default: true.</summary>
    public bool ShowColorScaleLegend { get; set; } = true;
    /// <summary>Gets or sets the color legend title, for example "Change (%)". Null uses the localized Color label.</summary>
    public string? ColorLegendTitle { get; set; }

    private static void NonNegative(double value, string parameter) {
        ChartGuards.Finite(value, parameter);
        if (value < 0) throw new ArgumentOutOfRangeException(parameter, value, "Treemap spacing must be non-negative.");
    }
}
