using System;

namespace ChartForgeX.Core;

/// <summary>Chooses the numeric columns accompanying native pie and donut legend labels.</summary>
public enum ChartPieLegendContent {
    /// <summary>Shows the category label without numeric columns.</summary>
    Label,
    /// <summary>Shows the category label and its authored numeric value.</summary>
    LabelAndValue
}

public sealed partial class ChartOptions {
    private double? _pieInsideLabelRadiusRatio;
    private double? _pieSliceOpacity;
    private ChartPieLegendContent? _pieLegendContent;

    /// <summary>Gets or sets an inside label's distance from its slice center as a fraction of the outer radius.</summary>
    /// <remarks>Null preserves the native pie 0.66 radius and donut band midpoint. Explicit values from zero through one still use native text fitting and containment; labels that cannot fit are omitted with semantic text retained.</remarks>
    public double? PieInsideLabelRadiusRatio {
        get => _pieInsideLabelRadiusRatio;
        set { if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value)); _pieInsideLabelRadiusRatio = value; }
    }

    /// <summary>Gets or sets an opacity multiplier for Pie/Donut slice fill, separator and pattern paint; null retains ordinary native paint.</summary>
    /// <remarks>Values from zero through one multiply authored alpha through native color math. Legend swatches and text retain their own paint. Native fill/stroke alpha composition is not a CSS group-opacity surface.</remarks>
    public double? PieSliceOpacity {
        get => _pieSliceOpacity;
        set { if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value)); _pieSliceOpacity = value; }
    }

    /// <summary>Gets or sets an immutable slice outline. Null retains the native surface-colored two-pixel separator.</summary>
    /// <remarks>Color, width, opacity and dash lengths use the shared native path painter. This does not change slice angles or data identity.</remarks>
    public ChartStrokeStyle? PieSliceOutline { get; set; }

    /// <summary>Gets or sets an immutable outer-circle outline for each pie or donut band. Null adds no outer-circle mark.</summary>
    public ChartStrokeStyle? PieOuterOutline { get; set; }

    /// <summary>Gets or sets whether pie and donut marks and legends keep authored observation order.</summary>
    /// <remarks>False retains native descending-value order. Aggregation still follows MaximumPieSlices; retained source points use authored order when true.</remarks>
    public bool PiePreserveSourceOrder { get; set; }

    /// <summary>Gets or sets optional pie and donut legend columns. Null retains native labels, values and percentages.</summary>
    public ChartPieLegendContent? PieLegendContent {
        get => _pieLegendContent;
        set {
            if (value.HasValue && !Enum.IsDefined(typeof(ChartPieLegendContent), value.Value)) throw new ArgumentOutOfRangeException(nameof(value));
            _pieLegendContent = value;
        }
    }
}
