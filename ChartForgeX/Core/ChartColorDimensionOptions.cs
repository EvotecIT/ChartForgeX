using System;

namespace ChartForgeX.Core;

/// <summary>Names and formats an existing numeric color measurement independently of other dimensions.</summary>
public sealed class ChartColorDimensionOptions {
    private string? _name;

    /// <summary>Creates color presentation options using the chart family's default name and number format.</summary>
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

    internal bool HasOverrides => Name != null || ValueFormat != null;
}
