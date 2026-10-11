using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private double? _heatmapCellGap;
    private double? _heatmapCellRadius;
    private ChartHeatmapValueTextMode _heatmapValueTextMode = ChartHeatmapValueTextMode.Auto;

    /// <summary>
    /// Gets or sets the numeric color scale shared by rectangular, hexagonal and calendar heatmaps.
    /// Null retains the family's default coloring. A scale uses all observed values, including zero, unless its
    /// range is fixed; explicit cell colors take precedence. Categorical matrices do not support a numeric scale.
    /// </summary>
    /// <remarks>
    /// An explicit scale overrides <see cref="HeatmapScale"/> and <see cref="HeatmapRelativeScale"/> without changing
    /// those settings. Masked matrix cells remain absent. Missing calendar days use the scale's explicit no-data
    /// color, or their default missing-day paint when none is supplied.
    /// </remarks>
    public ChartColorScale? HeatmapColorScale { get; set; }

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

    /// <summary>
    /// Gets or sets a value indicating whether heatmap intensity is relative to the observed value range (from zero for
    /// non-negative data). By default, heatmaps whose values all fall between 0 and 100 are read as percentages; enable
    /// this for counts and other magnitudes. <see cref="Chart.AddHourWeekdayHeatmap"/> enables it. Relative heatmaps are
    /// drawn with the neutral sequential scale; combining this with <see cref="ChartHeatmapScale.Semantic"/> is rejected
    /// unless <see cref="HeatmapColorScale"/> supplies the coloring instead.
    /// </summary>
    public bool HeatmapRelativeScale { get; set; }
}
