using System;

namespace ChartForgeX.Core;

/// <summary>Controls the name, number format and key presentation of an existing numeric color measurement.</summary>
public sealed class ChartColorDimensionOptions {
    private string? _name;
    private ChartColorLegendStyle _legendStyle;

    /// <summary>Creates color presentation options using the family's default name, format and visible automatic key.</summary>
    public ChartColorDimensionOptions() { }

    /// <summary>Gets or sets the color measurement name. Null restores the family default; nonnull names must not be blank.</summary>
    public string? Name {
        get => _name;
        set {
            if (value != null && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Color dimension name must not be blank.", nameof(value));
            _name = value;
        }
    }

    /// <summary>Gets or sets the immutable number display policy. Null restores the family default without changing other dimensions.</summary>
    /// <remarks>Preparation captures the policy and calls a custom formatter once per distinct finite color value. Missing values bypass it; empty output remains empty.</remarks>
    public ChartValueFormat? ValueFormat { get; set; }

    /// <summary>Gets or sets whether the numeric key is drawn when the chart/frame legend is enabled. Independent of the categorical series entry. Default: true.</summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>Gets or sets the numeric key presentation without changing value-to-color mapping. Default: Auto.</summary>
    /// <remarks>Explicit styles require a resolved numeric color scale. Gradient requires a continuous scale; discrete bands retain exact intervals under Swatches.</remarks>
    public ChartColorLegendStyle LegendStyle {
        get => _legendStyle;
        set {
            if (!Enum.IsDefined(typeof(ChartColorLegendStyle), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _legendStyle = value;
        }
    }

    internal bool HasOverrides => Name != null || ValueFormat != null || !ShowLegend || LegendStyle != ChartColorLegendStyle.Auto;

    internal void ValidateLegend(ChartColorScale? scale, bool inferredContinuousScale = false) {
        if (LegendStyle == ChartColorLegendStyle.Auto) return;
        if (scale == null && !inferredContinuousScale) throw new InvalidOperationException("An explicit color legend style requires a resolved ChartColorScale.");
        if (LegendStyle == ChartColorLegendStyle.Gradient && scale?.Mode == ChartColorScaleMode.Discrete)
            throw new InvalidOperationException("A gradient color legend requires a continuous ChartColorScale; discrete bands use Swatches.");
    }
}
