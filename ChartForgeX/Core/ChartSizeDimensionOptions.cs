using System;

namespace ChartForgeX.Core;

/// <summary>Controls the name, number display and optional numeric key of a numeric marker-size measurement.</summary>
public sealed class ChartSizeDimensionOptions {
    private string? _name;

    /// <summary>Gets or sets the size measurement name. Null uses <see cref="ChartLabels.Size"/>; nonnull names must not be blank.</summary>
    public string? Name {
        get => _name;
        set {
            if (value != null && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Size dimension name must not be blank.", nameof(value));
            _name = value;
        }
    }

    /// <summary>Gets or sets the immutable number display policy. Null uses the chart value format without changing Y or color measurements.</summary>
    /// <remarks>Preparation captures the policy and formats each distinct finite source or key value once. Empty custom output remains empty.</remarks>
    public ChartValueFormat? ValueFormat { get; set; }

    /// <summary>Gets or sets whether to draw a numeric size key when the chart/frame legend is enabled. Default: false.</summary>
    /// <remarks>The key explains logical marker radii; it does not claim proportional painted areas or replace categorical series entries.</remarks>
    public bool ShowLegend { get; set; }

    internal ChartSizeDimensionOptions Clone() => (ChartSizeDimensionOptions)MemberwiseClone();
}
