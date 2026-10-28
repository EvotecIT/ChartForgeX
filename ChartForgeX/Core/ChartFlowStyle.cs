using System;

namespace ChartForgeX.Core;

/// <summary>Immutable paint overrides for one authored Sankey or Chord flow, independent of its weight and endpoints.</summary>
public readonly struct ChartFlowStyle {
    /// <summary>Gets the explicit fill. Null inherits the selected endpoint, family, series, quantitative, semantic-state or palette paint.</summary>
    public ChartColor? Fill { get; }

    /// <summary>Gets the fill opacity multiplier from zero to one. Null inherits the family's ribbon opacity.</summary>
    /// <remarks>Opacity multiplies the color's alpha. Sankey patterns retain their separate .6 opacity; Chord patterns follow the fill opacity. Direction cues retain their base color alpha.</remarks>
    public double? FillOpacity { get; }

    /// <summary>Gets the flow pattern override. Null inherits the selected endpoint or series pattern; None explicitly disables it.</summary>
    public ChartFillPattern? FillPattern { get; }

    /// <summary>Gets the explicit outline color. Setting it enables a one-unit outline unless StrokeWidth overrides or disables it.</summary>
    public ChartColor? Stroke { get; }

    /// <summary>Gets the non-negative outline width in logical units. Zero disables the outline; a positive width enables it with Stroke or the resolved flow color.</summary>
    /// <remarks>Null uses one unit when Stroke is set, otherwise no outline. Outline width does not change the ribbon geometry or authored weight.</remarks>
    public double? StrokeWidth { get; }

    /// <summary>Gets the outline opacity multiplier from zero to one. Null uses one; opacity alone does not enable an outline.</summary>
    public double? StrokeOpacity { get; }

    /// <summary>Gets an immutable outline policy, including dash lengths. Its specified color/width and opacity override the corresponding legacy outline fields.</summary>
    /// <remarks>A nonnull policy enables an outline, retaining legacy color/width when present and otherwise using the resolved flow color and one-unit width. Width zero hides it. Dash null or empty selects a solid outline.</remarks>
    public ChartStrokeStyle? OutlineStroke { get; }

    /// <summary>Gets the finite logical phase applied to the outline's dash pattern. The default is zero.</summary>
    public double StrokeDashOffset { get; }

    /// <summary>Creates validated flow paint overrides. Unspecified values retain the existing rendering defaults.</summary>
    /// <param name="fill">Explicit flow fill, taking precedence over selected-endpoint, family, series, quantitative and semantic colors.</param>
    /// <param name="fillOpacity">Optional fill alpha multiplier from zero to one.</param>
    /// <param name="fillPattern">Optional pattern; None disables an inherited pattern.</param>
    /// <param name="stroke">Optional outline color; without a width it enables a one-unit outline.</param>
    /// <param name="strokeWidth">Optional non-negative outline width; zero disables the outline.</param>
    /// <param name="strokeOpacity">Optional outline alpha multiplier from zero to one.</param>
    /// <param name="outlineStroke">Optional immutable outline policy; specifies dash lengths without changing weighted geometry.</param>
    /// <param name="strokeDashOffset">Finite logical phase of the outline dash pattern.</param>
    public ChartFlowStyle(ChartColor? fill = null, double? fillOpacity = null, ChartFillPattern? fillPattern = null,
        ChartColor? stroke = null, double? strokeWidth = null, double? strokeOpacity = null,
        ChartStrokeStyle? outlineStroke = null, double strokeDashOffset = 0) {
        if (fillOpacity.HasValue) ChartGuards.UnitInterval(fillOpacity.Value, nameof(fillOpacity));
        if (fillPattern.HasValue && !Enum.IsDefined(typeof(ChartFillPattern), fillPattern.Value)) throw new ArgumentOutOfRangeException(nameof(fillPattern));
        if (strokeWidth.HasValue) {
            ChartGuards.Finite(strokeWidth.Value, nameof(strokeWidth));
            if (strokeWidth.Value < 0) throw new ArgumentOutOfRangeException(nameof(strokeWidth), strokeWidth, "Outline width must be non-negative.");
        }
        if (strokeOpacity.HasValue) ChartGuards.UnitInterval(strokeOpacity.Value, nameof(strokeOpacity));
        ChartGuards.Finite(strokeDashOffset, nameof(strokeDashOffset));
        Fill = fill; FillOpacity = fillOpacity; FillPattern = fillPattern;
        Stroke = stroke; StrokeWidth = strokeWidth; StrokeOpacity = strokeOpacity;
        OutlineStroke = outlineStroke; StrokeDashOffset = strokeDashOffset;
    }
}
