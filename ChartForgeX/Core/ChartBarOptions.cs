using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Bar layout, stacking and styling. Configure it through <see cref="ChartOptions.Bars"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartBarOptions {
    internal ChartBarOptions Clone() {
        var copy = (ChartBarOptions)MemberwiseClone();
        copy._barVisualStyle = _barVisualStyle.Clone();
        return copy;
    }
    private ChartBarMode _barMode = ChartBarMode.Grouped;

    private ChartBarVisualStyle _barVisualStyle = ChartBarVisualStyle.Flat();

    private bool _hasExplicitBarStyle;


    /// <summary>
    /// Gets or sets how multiple bar series are arranged within each category.
    /// </summary>
    [FluentPreset("Grouped", ChartBarMode.Grouped, Summary = "Places bars of one category side by side.")]
    [FluentPreset("Stacked", ChartBarMode.Stacked, Summary = "Stacks bars of one category.")]
    public ChartBarMode Mode {
        get => _barMode;
        set {
            if (!Enum.IsDefined(typeof(ChartBarMode), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown bar mode.");
            _barMode = value;
        }
    }


    /// <summary>
    /// Gets or sets the visual treatment used by bar and horizontal-bar renderers.
    /// </summary>
    public ChartBarStyle Style {
        get => VisualStyle.Kind;
        set {
            if (!Enum.IsDefined(typeof(ChartBarStyle), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown bar style.");
            VisualStyle = value switch {
                ChartBarStyle.SegmentedCapsule => ChartBarVisualStyle.DashboardCapsule(),
                ChartBarStyle.Flat => ChartBarVisualStyle.Flat(),
                _ => ChartBarVisualStyle.Solid()
            };
        }
    }


    /// <summary>
    /// Gets or sets reusable visual tokens for bar and horizontal-bar renderers.
    /// </summary>
    [NoFluent]
    public ChartBarVisualStyle VisualStyle {
        get => _barVisualStyle;
        set { _barVisualStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone(); _hasExplicitBarStyle = true; }
    }


    /// <summary>Snapshots the chart's canonical bar treatment for detached preparation.</summary>
    internal ChartBarVisualStyle ResolvePreparedVisualStyle() => _barVisualStyle.Clone();


    internal bool HasPreparedCornerRadius => _hasExplicitBarStyle || _barVisualStyle.HasExplicitCornerRadius;


    internal void ConfigureVisualStyle(Action<ChartBarVisualStyle> configure) =>
        _barVisualStyle = ChartOptionConfiguration.Configure(_barVisualStyle, configure, style => style.Clone());


    /// <summary>
    /// Gets or sets a value indicating whether stacked bar totals are rendered above each category.
    /// </summary>
    public bool ShowTotals { get; set; }

    private double? _maximumBarWidth;


    /// <summary>Maximum regular bar thickness in drawing pixels, or null to use the theme's 64-pixel default. Histograms retain their bin widths.</summary>
    public double? MaxWidth {
        get => _maximumBarWidth;
        set {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value <= 0))
                throw new ArgumentOutOfRangeException(nameof(value), "Use a positive finite bar width.");
            _maximumBarWidth = value;
        }
    }
}
