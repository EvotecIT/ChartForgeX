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

    /// <summary>Snapshots explicit grid settings over the prepared pipeline's theme-independent guide defaults.</summary>
    /// <remarks>Direct getter mutations are compared against their original legacy defaults. Assign a complete style when a value equal to a legacy default must override a different prepared default.</remarks>
    internal ChartGridLineStyle ResolvePreparedGridLineStyle() {
        if (_hasExplicitGridStyle) return _gridLineStyle.Clone();
        var prepared = new ChartGridLineStyle { ShowVerticalLines = false };
        ApplyDifferences(_gridLineStyle, ChartGridLineStyle.Default());
        ApplyDifferences(_graphiteGridLineStyle, new ChartGridLineStyle { ShowVerticalLines = false });
        return prepared;

        void ApplyDifferences(ChartGridLineStyle configured, ChartGridLineStyle original) {
            if (configured.ShowHorizontalLines != original.ShowHorizontalLines) prepared.ShowHorizontalLines = configured.ShowHorizontalLines;
            if (configured.ShowVerticalLines != original.ShowVerticalLines) prepared.ShowVerticalLines = configured.ShowVerticalLines;
            if (configured.HorizontalOpacity != original.HorizontalOpacity) prepared.HorizontalOpacity = configured.HorizontalOpacity;
            if (configured.VerticalOpacity != original.VerticalOpacity) prepared.VerticalOpacity = configured.VerticalOpacity;
            if (configured.StrokeWidth != original.StrokeWidth) prepared.StrokeWidth = configured.StrokeWidth;
            if (configured.Dash != original.Dash) prepared.Dash = configured.Dash;
            if (configured.Gap != original.Gap) prepared.Gap = configured.Gap;
        }
    }

    internal bool HasPreparedGridStrokeWidth => _hasExplicitGridStyle || _gridLineStyle.StrokeWidth != 1 || _graphiteGridLineStyle.StrokeWidth != 1;

    internal bool HasPreparedSegmentedBars => _hasExplicitBarStyle ? _barVisualStyle.Kind == ChartBarStyle.SegmentedCapsule
        : _barVisualStyle.Kind == ChartBarStyle.SegmentedCapsule || _flatBarVisualStyle.Kind == ChartBarStyle.SegmentedCapsule;

    /// <summary>
    /// Gets or sets a value indicating whether stacked bar totals are rendered above each category.
    /// </summary>
    public bool ShowStackTotals { get; set; }
}
