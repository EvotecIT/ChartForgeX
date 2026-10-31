using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartBarMode _barMode = ChartBarMode.Grouped;
    private ChartBarVisualStyle _barVisualStyle = ChartBarVisualStyle.Flat();
    private ChartGridLineStyle _gridLineStyle = ChartGridLineStyle.Default();
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
        get => _barVisualStyle;
        set { _barVisualStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone(); _hasExplicitBarStyle = true; }
    }

    /// <summary>
    /// Gets or sets reusable visual tokens for cartesian grid and guide lines.
    /// </summary>
    public ChartGridLineStyle GridLineStyle {
        get => _gridLineStyle;
        set { _gridLineStyle = (value ?? throw new ArgumentNullException(nameof(value))).Clone(); _hasExplicitGridStyle = true; }
    }

    /// <summary>Snapshots the chart's canonical grid treatment for detached preparation.</summary>
    internal ChartGridLineStyle ResolvePreparedGridLineStyle() => _gridLineStyle.Clone();

    /// <summary>Whether the grid style was set as a whole; schedules then honour its row lines literally.</summary>
    internal bool HasExplicitGridLineStyle => _hasExplicitGridStyle;

    internal bool HasPreparedGridStrokeWidth => _hasExplicitGridStyle || _gridLineStyle.HasExplicitStrokeWidth;

    /// <summary>Snapshots the chart's canonical bar treatment for detached preparation.</summary>
    internal ChartBarVisualStyle ResolvePreparedBarVisualStyle() => _barVisualStyle.Clone();

    internal bool HasPreparedBarCornerRadius => _hasExplicitBarStyle || _barVisualStyle.HasExplicitCornerRadius;

    internal void ConfigureBarVisualStyle(Action<ChartBarVisualStyle> configure) =>
        _barVisualStyle = ChartOptionConfiguration.Configure(_barVisualStyle, configure, style => style.Clone());

    internal void ConfigureGridLineStyle(Action<ChartGridLineStyle> configure) =>
        _gridLineStyle = ChartOptionConfiguration.Configure(_gridLineStyle, configure, style => style.Clone());

    /// <summary>
    /// Gets or sets a value indicating whether stacked bar totals are rendered above each category.
    /// </summary>
    public bool ShowStackTotals { get; set; }
}
