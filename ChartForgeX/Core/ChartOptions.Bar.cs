using System;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartGridLineStyle _gridLineStyle = ChartGridLineStyle.Default();
    private bool _hasExplicitGridStyle;

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

    internal void ConfigureGridLineStyle(Action<ChartGridLineStyle> configure) =>
        _gridLineStyle = ChartOptionConfiguration.Configure(_gridLineStyle, configure, style => style.Clone());
}
