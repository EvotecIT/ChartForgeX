using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>
/// Defines renderer-independent options for a chart.
/// </summary>
public sealed class ChartOptions {
    private ChartSize _size = new(1000, 560);
    private ChartPadding _padding = new(76, 78, 36, 74);
    private ChartTheme _theme = ChartTheme.Light();
    private int _tickCount = 6;
    private ChartLabelDensity _xAxisLabelDensity = ChartLabelDensity.Auto;
    private double _xAxisLabelAngle;
    private ChartBarMode _barMode = ChartBarMode.Grouped;
    private ChartHeatmapScale _heatmapScale = ChartHeatmapScale.Sequential;
    private string? _pngFontPath;
    private string? _pngFontFaceName;
    private int? _pngFontCollectionIndex;

    /// <summary>
    /// Gets or sets the rendered chart size in pixels.
    /// </summary>
    public ChartSize Size {
        get => _size;
        set {
            if (value.Width <= 0 || value.Height <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Chart size must have positive dimensions.");
            _size = value;
        }
    }

    /// <summary>
    /// Gets or sets the chart padding around the plot area.
    /// </summary>
    public ChartPadding Padding {
        get => _padding;
        set {
            ChartGuards.Finite(value.Left, nameof(value));
            ChartGuards.Finite(value.Top, nameof(value));
            ChartGuards.Finite(value.Right, nameof(value));
            ChartGuards.Finite(value.Bottom, nameof(value));
            if (value.Left < 0 || value.Top < 0 || value.Right < 0 || value.Bottom < 0) throw new ArgumentOutOfRangeException(nameof(value), "Chart padding values must be non-negative.");
            _padding = value;
        }
    }

    /// <summary>
    /// Gets or sets the visual theme used by renderers.
    /// </summary>
    public ChartTheme Theme {
        get => _theme;
        set => _theme = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the preferred TrueType font file or TrueType collection used by the PNG renderer.
    /// </summary>
    /// <remarks>
    /// SVG and HTML use <see cref="ChartTheme.FontFamily"/>. PNG rendering uses this .ttf or .ttc file when it can be loaded and falls back to an auto-detected platform font or the built-in tiny font.
    /// </remarks>
    public string? PngFontPath {
        get => _pngFontPath;
        set {
            if (value != null && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("PNG font path must not be empty.", nameof(value));
            _pngFontPath = value;
        }
    }

    /// <summary>
    /// Gets or sets the optional family, subfamily, full, or PostScript face name used when selecting a PNG font.
    /// </summary>
    public string? PngFontFaceName {
        get => _pngFontFaceName;
        set {
            if (value != null && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("PNG font face name must not be empty.", nameof(value));
            _pngFontFaceName = value;
        }
    }

    /// <summary>
    /// Gets or sets the optional face index used when <see cref="PngFontPath"/> points to a TrueType collection.
    /// </summary>
    public int? PngFontCollectionIndex {
        get => _pngFontCollectionIndex;
        set {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG font collection index must be non-negative.");
            _pngFontCollectionIndex = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the legend is rendered.
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the title and subtitle are rendered.
    /// </summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the outer card surface is rendered.
    /// </summary>
    public bool ShowCard { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the plot background surface is rendered.
    /// </summary>
    public bool ShowPlotBackground { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether grid lines are rendered.
    /// </summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether axes, tick labels, and axis titles are rendered.
    /// </summary>
    public bool ShowAxes { get; set; } = true;

    /// <summary>
    /// Gets or sets the preferred number of axis ticks.
    /// </summary>
    public int TickCount {
        get => _tickCount;
        set {
            if (value < 2) throw new ArgumentOutOfRangeException(nameof(value), value, "Tick count must be at least two.");
            _tickCount = value;
        }
    }

    /// <summary>
    /// Gets or sets the density used for explicit x-axis labels.
    /// </summary>
    public ChartLabelDensity XAxisLabelDensity {
        get => _xAxisLabelDensity;
        set {
            if (!Enum.IsDefined(typeof(ChartLabelDensity), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown x-axis label density.");
            _xAxisLabelDensity = value;
        }
    }

    /// <summary>
    /// Gets or sets the x-axis label rotation angle in degrees for capable renderers.
    /// </summary>
    public double XAxisLabelAngle {
        get => _xAxisLabelAngle;
        set {
            ChartGuards.Finite(value, nameof(value));
            _xAxisLabelAngle = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the chart background should be transparent.
    /// </summary>
    public bool TransparentBackground { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether point and bar values are rendered as labels.
    /// </summary>
    public bool ShowDataLabels { get; set; }

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
    /// Gets or sets a value indicating whether stacked bar totals are rendered above each category.
    /// </summary>
    public bool ShowStackTotals { get; set; }

    /// <summary>
    /// Gets or sets how heatmap values are converted into cell colors.
    /// </summary>
    public ChartHeatmapScale HeatmapScale {
        get => _heatmapScale;
        set {
            if (!Enum.IsDefined(typeof(ChartHeatmapScale), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown heatmap scale.");
            _heatmapScale = value;
        }
    }

    /// <summary>
    /// Gets or sets a formatter used for y-axis ticks, data labels, stack totals, and donut totals.
    /// </summary>
    public Func<double, string>? ValueFormatter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the chart is rendered as a compact sparkline.
    /// </summary>
    public bool IsSparkline { get; set; }

    /// <summary>
    /// Gets explicit labels for x-axis values.
    /// </summary>
    public List<ChartAxisLabel> XAxisLabels { get; } = new();
}
