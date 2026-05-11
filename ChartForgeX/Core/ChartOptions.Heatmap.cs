using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private double? _heatmapCellGap;
    private double? _heatmapCellRadius;
    private ChartHeatmapValueTextMode _heatmapValueTextMode = ChartHeatmapValueTextMode.Auto;

    /// <summary>
    /// Gets or sets an optional heatmap cell gap in pixels.
    /// </summary>
    public double? HeatmapCellGap {
        get => _heatmapCellGap;
        set {
            ValidateNullableFinite(value, nameof(value));
            if (value < 0 || value > 24) throw new ArgumentOutOfRangeException(nameof(value), value, "Heatmap cell gap must be between zero and twenty-four pixels.");
            _heatmapCellGap = value;
        }
    }

    /// <summary>
    /// Gets or sets an optional heatmap cell corner radius in pixels.
    /// </summary>
    public double? HeatmapCellRadius {
        get => _heatmapCellRadius;
        set {
            ValidateNullableFinite(value, nameof(value));
            if (value < 0 || value > 24) throw new ArgumentOutOfRangeException(nameof(value), value, "Heatmap cell radius must be between zero and twenty-four pixels.");
            _heatmapCellRadius = value;
        }
    }

    /// <summary>
    /// Gets or sets when heatmap cell values are rendered as text.
    /// </summary>
    public ChartHeatmapValueTextMode HeatmapValueTextMode {
        get => _heatmapValueTextMode;
        set {
            if (!Enum.IsDefined(typeof(ChartHeatmapValueTextMode), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown heatmap value text mode.");
            _heatmapValueTextMode = value;
        }
    }
}
