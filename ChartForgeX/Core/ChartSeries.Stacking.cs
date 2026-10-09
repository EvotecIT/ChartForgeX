using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private string? _stackGroup;
    private double? _normalizedTo;

    /// <summary>
    /// Gets or sets the ordinal key of an independent Cartesian/radial bar, radial column, or stacked-area stack.
    /// A named bar stack participates in stacking even when the chart's bar mode is grouped.
    /// Null uses the chart's default bar arrangement or the default stacked-area group.
    /// Stacks on different axes and of different series kinds remain independent.
    /// </summary>
    public string? StackGroup {
        get => _stackGroup;
        set {
            if (value != null) {
                RequireStackingKind();
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stack group must have a nonempty key.", nameof(value));
            }
            _stackGroup = value;
        }
    }

    /// <summary>
    /// Gets or sets the positive total used to normalize each positive and negative stack separately.
    /// Null retains source units. Every series in the same stack must use the same target.
    /// A zero-only stack remains at the baseline and produces a preparation diagnostic.
    /// Source values and their formatted labels are retained independently of normalized geometry.
    /// </summary>
    public double? NormalizedTo {
        get => _normalizedTo;
        set {
            if (value.HasValue) {
                RequireStackingKind();
                ChartGuards.Finite(value.Value, nameof(value));
                if (value.Value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "A stack normalization target must be greater than zero.");
            }
            _normalizedTo = value;
        }
    }

    /// <summary>Assigns an independent stack key; null restores the chart's default arrangement.</summary>
    /// <param name="group">An ordinal key shared by the series in one stack, or null.</param>
    public ChartSeries WithStackGroup(string? group) { StackGroup = group; return this; }

    /// <summary>Normalizes positive and negative stack totals separately while retaining source values.</summary>
    /// <param name="target">The finite positive total, normally 100 for percentage stacks; null retains source units.</param>
    public ChartSeries WithNormalization(double? target = 100) { NormalizedTo = target; return this; }

    private void RequireStackingKind() {
        if (Kind != ChartSeriesKind.Bar && Kind != ChartSeriesKind.HorizontalBar && Kind != ChartSeriesKind.StackedArea
            && Kind != ChartSeriesKind.RadialBar && Kind != ChartSeriesKind.RadialColumn)
            throw new InvalidOperationException("Stack configuration is supported only by Cartesian/radial bars, radial columns, and stacked areas.");
    }
}
