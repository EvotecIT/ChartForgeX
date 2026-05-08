using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartBarMode _barMode = ChartBarMode.Grouped;
    private ChartBarVisualStyle _barVisualStyle = ChartBarVisualStyle.Solid();
    private ChartGridLineStyle _gridLineStyle = ChartGridLineStyle.Default();

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
        get => _barVisualStyle.Kind;
        set {
            if (!Enum.IsDefined(typeof(ChartBarStyle), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown bar style.");
            BarVisualStyle = value == ChartBarStyle.SegmentedCapsule ? ChartBarVisualStyle.DashboardCapsule() : ChartBarVisualStyle.Solid();
        }
    }

    /// <summary>
    /// Gets or sets reusable visual tokens for bar and horizontal-bar renderers.
    /// </summary>
    public ChartBarVisualStyle BarVisualStyle {
        get => _barVisualStyle;
        set => _barVisualStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone();
    }

    /// <summary>
    /// Gets or sets reusable visual tokens for cartesian grid and guide lines.
    /// </summary>
    public ChartGridLineStyle GridLineStyle {
        get => _gridLineStyle;
        set => _gridLineStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone();
    }

    /// <summary>
    /// Gets or sets a value indicating whether stacked bar totals are rendered above each category.
    /// </summary>
    public bool ShowStackTotals { get; set; }
}
