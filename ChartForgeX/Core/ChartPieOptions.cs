using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Pie and donut slices, labels and centre text. Configure it through <see cref="ChartOptions.Pie"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartPieOptions {
    internal ChartPieOptions Clone() => (ChartPieOptions)MemberwiseClone();
    private double _donutInnerRadiusRatio = 0.62;

    private string? _donutCenterValue;

    private string? _donutCenterLabel;


    /// <summary>
    /// Gets or sets a value indicating whether donut charts render the center total and series label.
    /// </summary>
    public bool ShowCenterLabel { get; set; } = true;


    /// <summary>
    /// Gets or sets the ratio of the donut hole radius to the outer radius.
    /// </summary>
    public double InnerRadiusRatio {
        get => _donutInnerRadiusRatio;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.35 || value > 0.82) throw new ArgumentOutOfRangeException(nameof(value), value, "Donut inner radius ratio must be between 0.35 and 0.82.");
            _donutInnerRadiusRatio = value;
        }
    }


    /// <summary>
    /// Gets or sets optional custom primary text rendered in the center of donut charts.
    /// </summary>
    public string? CenterValue {
        get => _donutCenterValue;
        set => _donutCenterValue = value == null || string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }


    /// <summary>
    /// Gets or sets optional custom secondary text rendered in the center of donut charts.
    /// </summary>
    public string? CenterLabel {
        get => _donutCenterLabel;
        set => _donutCenterLabel = value == null || string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private ChartPieSliceLabelContent _pieSliceLabelContent = ChartPieSliceLabelContent.Percent;

    private double _pieOutsideLabelDistanceRatio = 1.14;


    /// <summary>
    /// Gets or sets the text rendered for pie and donut slice data labels.
    /// </summary>
    public ChartPieSliceLabelContent SliceLabelContent {
        get => _pieSliceLabelContent;
        set {
            if (!Enum.IsDefined(typeof(ChartPieSliceLabelContent), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown pie slice label content.");
            _pieSliceLabelContent = value;
        }
    }


    /// <summary>
    /// Gets or sets a custom formatter for pie and donut slice data labels.
    /// </summary>
    [PowerForge.Fluent.Fluent("SliceLabelBy")]
public Func<ChartPieSliceLabelContext, string?>? SliceLabelFormatter { get; set; }


    /// <summary>
    /// Gets or sets the side-lane distance for outside pie and donut labels as a ratio of the outer radius.
    /// </summary>
    public double OutsideLabelDistanceRatio {
        get => _pieOutsideLabelDistanceRatio;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.9 || value > 1.8) throw new ArgumentOutOfRangeException(nameof(value), value, "Pie outside label distance ratio must be between 0.9 and 1.8.");
            _pieOutsideLabelDistanceRatio = value;
        }
    }

    private int _maximumPieSlices = 6;

    /// <summary>Gets or sets the maximum displayed pie or donut slices, including an aggregate Other slice.</summary>
    public int MaxSlices { get => _maximumPieSlices; set { if (value < 2) throw new ArgumentOutOfRangeException(nameof(value)); _maximumPieSlices = value; } }

    private double? _pieInsideLabelRadiusRatio;

    private double? _pieSliceOpacity;

    private ChartPieLegendContent? _pieLegendContent;


    /// <summary>Gets or sets an inside label's distance from its slice center as a fraction of the outer radius.</summary>
    /// <remarks>Null preserves the native pie 0.66 radius and donut band midpoint. Explicit values from zero through one still use native text fitting and containment; labels that cannot fit are omitted with semantic text retained.</remarks>
    public double? InsideLabelRadiusRatio {
        get => _pieInsideLabelRadiusRatio;
        set { if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value)); _pieInsideLabelRadiusRatio = value; }
    }


    /// <summary>Gets or sets an opacity multiplier for Pie/Donut slice fill, separator and pattern paint; null retains ordinary native paint.</summary>
    /// <remarks>Values from zero through one multiply authored alpha through native color math. Legend swatches and text retain their own paint. Native fill/stroke alpha composition is not a CSS group-opacity surface.</remarks>
    public double? SliceOpacity {
        get => _pieSliceOpacity;
        set { if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value)); _pieSliceOpacity = value; }
    }


    /// <summary>Gets or sets an immutable slice outline. Null retains the native surface-colored two-pixel separator.</summary>
    /// <remarks>Color, width, opacity and dash lengths use the shared native path painter. This does not change slice angles or data identity.</remarks>
    public ChartStrokeStyle? SliceOutline { get; set; }


    /// <summary>Gets or sets an immutable outer-circle outline for each pie or donut band. Null adds no outer-circle mark.</summary>
    public ChartStrokeStyle? OuterOutline { get; set; }


    /// <summary>Gets or sets whether pie and donut marks and legends keep authored observation order.</summary>
    /// <remarks>False retains native descending-value order. Aggregation still follows MaximumPieSlices; retained source points use authored order when true.</remarks>
    public bool PreserveSourceOrder { get; set; }


    /// <summary>Gets or sets optional pie and donut legend columns. Null retains native labels, values and percentages.</summary>
    public ChartPieLegendContent? LegendContent {
        get => _pieLegendContent;
        set {
            if (value.HasValue && !Enum.IsDefined(typeof(ChartPieLegendContent), value.Value)) throw new ArgumentOutOfRangeException(nameof(value));
            _pieLegendContent = value;
        }
    }
}
