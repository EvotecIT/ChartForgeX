using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartBarMode _barMode = ChartBarMode.Grouped;
    private ChartBarVisualStyle _barVisualStyle = ChartBarVisualStyle.Solid();
    private ChartGridLineStyle _gridLineStyle = ChartGridLineStyle.Default();
    private readonly ChartBarVisualStyle _flatBarVisualStyle = ChartBarVisualStyle.Flat();
    private readonly ChartGridLineStyle _graphiteGridLineStyle = new() { ShowVerticalLines = false };
    private bool _hasExplicitBarStyle;
    private bool _hasExplicitGridStyle;

    /// <summary>
    /// Gets or sets how multiple bar series are arranged within each category.
    /// </summary>
    public ChartBarMode BarMode {
        get => _barMode;
        set {
            if (!Enum.IsDefined(typeof(ChartBarMode), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown bar mode.");
            _barMode = value;
        }
    }

    /// <summary>
    /// Gets or sets the visual treatment used by bar and horizontal-bar renderers.
    /// </summary>
    public ChartBarStyle BarStyle {
        get => BarVisualStyle.Kind;
        set {
            if (!Enum.IsDefined(typeof(ChartBarStyle), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown bar style.");
            BarVisualStyle = value switch {
                ChartBarStyle.SegmentedCapsule => ChartBarVisualStyle.DashboardCapsule(),
                ChartBarStyle.Flat => ChartBarVisualStyle.Flat(),
                _ => ChartBarVisualStyle.Solid()
            };
        }
    }

    /// <summary>
    /// Gets or sets reusable visual tokens for bar and horizontal-bar renderers.
    /// </summary>
    public ChartBarVisualStyle BarVisualStyle {
        get => !_hasExplicitBarStyle && Theme.FlatMarks ? _flatBarVisualStyle : _barVisualStyle;
        set { _barVisualStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone(); _hasExplicitBarStyle = true; }
    }

    /// <summary>
    /// Gets or sets reusable visual tokens for cartesian grid and guide lines.
    /// </summary>
    public ChartGridLineStyle GridLineStyle {
        get => !_hasExplicitGridStyle && Theme.UseGraphiteLayout ? _graphiteGridLineStyle : _gridLineStyle;
        set { _gridLineStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone(); _hasExplicitGridStyle = true; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether stacked bar totals are rendered above each category.
    /// </summary>
    public bool ShowStackTotals { get; set; }
}
