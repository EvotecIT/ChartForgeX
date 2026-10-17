using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Configures transient HTML emphasis without changing native SVG or PNG paint.</summary>
public sealed class HtmlChartHighlightOptions {
    /// <summary>
    /// Gets or sets the emphasis mode. Root-branch emphasis applies to Sunburst node targets;
    /// other chart families and legend targets retain their usual presentation.
    /// </summary>
    public HtmlChartHighlightMode Mode { get; set; }

    /// <summary>Gets or sets whether unselected data marks recede while the chart has selected marks.</summary>
    public bool DimOthersWhileSelected { get; set; }

    /// <summary>Gets overrides for the active item, applied after its branch style.</summary>
    public HtmlChartHighlightStyle HighlightedItem { get; } = new HtmlChartHighlightStyle();

    /// <summary>Gets overrides shared by the active Sunburst root branch.</summary>
    public HtmlChartHighlightStyle HighlightedBranch { get; } = new HtmlChartHighlightStyle();

    /// <summary>Gets overrides for other items in the active branch, applied after the branch style.</summary>
    public HtmlChartHighlightStyle UnhighlightedItem { get; } = new HtmlChartHighlightStyle();

    /// <summary>Gets overrides for items in other Sunburst root branches.</summary>
    public HtmlChartHighlightStyle UnhighlightedBranch { get; } = new HtmlChartHighlightStyle();

    internal void CopyTo(HtmlChartHighlightOptions destination) {
        destination.Mode = Mode;
        destination.DimOthersWhileSelected = DimOthersWhileSelected;
        HighlightedItem.CopyTo(destination.HighlightedItem);
        HighlightedBranch.CopyTo(destination.HighlightedBranch);
        UnhighlightedItem.CopyTo(destination.UnhighlightedItem);
        UnhighlightedBranch.CopyTo(destination.UnhighlightedBranch);
    }
}

/// <summary>Controls the unit emphasized by the HTML adapter's existing hover and focus pipeline.</summary>
public enum HtmlChartHighlightMode {
    /// <summary>Uses the chart family's existing emphasis. Style overrides have no effect.</summary>
    Default,
    /// <summary>Emphasizes a Sunburst item and its first ancestor below the central root.</summary>
    RootBranch
}

/// <summary>Sparse overrides for HTML highlight paint; null retains the preceding style.</summary>
public sealed class HtmlChartHighlightStyle {
    private double? _strokeWidth;
    private double? _opacity;

    /// <summary>
    /// Gets or sets the additional outline color. Null retains the preceding color; a fully
    /// transparent color explicitly hides the outline. Native fill, hatch and stroke remain intact.
    /// </summary>
    public ChartColor? Stroke { get; set; }

    /// <summary>
    /// Gets or sets the outline's outward width in SVG units. Values must be finite and non-negative.
    /// Zero explicitly hides the outline. A color without a width uses 2.5 units.
    /// </summary>
    public double? StrokeWidth {
        get => _strokeWidth;
        set { Validate(value, null); _strokeWidth = value; }
    }

    /// <summary>
    /// Gets or sets the transient opacity, from zero through one. Zero keeps the native mark
    /// available for pointer and keyboard acquisition; authored or host-hidden marks stay unavailable.
    /// Selection retains its existing full-strength emphasis.
    /// </summary>
    public double? Opacity {
        get => _opacity;
        set { Validate(value, 1); _opacity = value; }
    }

    internal void CopyTo(HtmlChartHighlightStyle destination) {
        destination.Stroke = Stroke;
        destination.StrokeWidth = StrokeWidth;
        destination.Opacity = Opacity;
    }

    private static void Validate(double? value, double? maximum) {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0 || maximum.HasValue && value.Value > maximum.Value))
            throw new ArgumentOutOfRangeException(nameof(value), "Highlight values must be finite and within the supported range.");
    }
}
