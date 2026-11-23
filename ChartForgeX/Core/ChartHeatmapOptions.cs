using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Heatmap scales, cells and labels. Configure it through <see cref="ChartOptions.Heatmap"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartHeatmapOptions {
    internal ChartHeatmapOptions Clone() {
        var copy = (ChartHeatmapOptions)MemberwiseClone();
        copy._heatmapColorDimension = _heatmapColorDimension.Clone();
        return copy;
    }
    /// <summary>Gets the name and number display policy for numeric rectangular, hexagonal and calendar heatmap measurements.</summary>
    public ChartColorDimensionOptions ColorDimension {
        get => _heatmapColorDimension;
        set => _heatmapColorDimension = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartColorDimensionOptions _heatmapColorDimension = new();

    private ChartHeatmapScale _heatmapScale = ChartHeatmapScale.Sequential;


    /// <summary>
    /// Gets or sets how heatmap values are converted into cell colors.
    /// </summary>
    public ChartHeatmapScale Scale {
        get => _heatmapScale;
        set {
            if (!Enum.IsDefined(typeof(ChartHeatmapScale), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown heatmap scale.");
            _heatmapScale = value;
        }
    }


    private ChartHeatmapColumnWindow _heatmapColumnWindow = ChartHeatmapColumnWindow.Latest;


    /// <summary>Gets or sets whether rectangular heatmaps may grow beyond the requested frame to fit readable rows.</summary>
    public bool NaturalHeight { get; set; }


    /// <summary>Gets or sets the policy for rectangular heatmap columns that do not fit at a readable width.</summary>
    public ChartHeatmapColumnWindow ColumnWindow {
        get => _heatmapColumnWindow;
        set {
            if (!Enum.IsDefined(typeof(ChartHeatmapColumnWindow), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _heatmapColumnWindow = value;
        }
    }


    /// <summary>
    /// Gets or sets whether categorical heatmap state keys are rendered when the chart legend is enabled. Numeric keys use HeatmapColorDimension.ShowLegend.
    /// </summary>
    public bool ShowCategoryLegend { get; set; } = true;


    /// <summary>
    /// Gets or sets a value indicating whether heatmap column labels are rendered.
    /// </summary>
    public bool ShowColumnLabels { get; set; } = true;

    /// <summary>Gets or sets whether numeric heatmap keys add automatic intermediate step labels where they fit. Authored scale captions remain authoritative. Default: true.</summary>
    public bool ShowScaleStepLabels { get; set; } = true;


    private double? _heatmapCellGap;

    private double? _heatmapCellRadius;

    private ChartHeatmapValueTextMode _heatmapValueTextMode = ChartHeatmapValueTextMode.Auto;


    /// <summary>
    /// Gets or sets the numeric color scale shared by rectangular, hexagonal and calendar heatmaps.
    /// Null retains the family's default coloring. A scale uses all observed values, including zero, unless its
    /// range is fixed; explicit cell colors take precedence. Categorical matrices do not support a numeric scale.
    /// </summary>
    /// <remarks>
    /// An explicit scale overrides <see cref="ChartHeatmapOptions.Scale"/> and <see cref="ChartHeatmapOptions.RelativeScale"/> without changing
    /// those settings. Masked matrix cells remain absent. Missing calendar days use the scale's explicit no-data
    /// color, or their default missing-day paint when none is supplied.
    /// </remarks>
    public ChartColorScale? ColorScale { get; set; }


    /// <summary>
    /// Gets or sets an optional heatmap cell gap in pixels.
    /// </summary>
    public double? CellGap {
        get => _heatmapCellGap;
        set {
            ChartOptions.ValidateNullableFinite(value, nameof(value));
            if (value < 0 || value > 24) throw new ArgumentOutOfRangeException(nameof(value), value, "Heatmap cell gap must be between zero and twenty-four pixels.");
            _heatmapCellGap = value;
        }
    }


    /// <summary>
    /// Gets or sets an optional heatmap cell corner radius in pixels.
    /// </summary>
    public double? CellRadius {
        get => _heatmapCellRadius;
        set {
            ChartOptions.ValidateNullableFinite(value, nameof(value));
            if (value < 0 || value > 24) throw new ArgumentOutOfRangeException(nameof(value), value, "Heatmap cell radius must be between zero and twenty-four pixels.");
            _heatmapCellRadius = value;
        }
    }


    /// <summary>
    /// Gets or sets when heatmap cell values are rendered as text.
    /// </summary>
    public ChartHeatmapValueTextMode ValueTextMode {
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
    /// unless <see cref="ChartHeatmapOptions.ColorScale"/> supplies the coloring instead.
    /// </summary>
    public bool RelativeScale { get; set; }
}
