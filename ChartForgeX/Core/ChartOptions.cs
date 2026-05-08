using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>
/// Defines renderer-independent options for a chart.
/// </summary>
public sealed partial class ChartOptions {
    private ChartSize _size = new(1000, 560);
    private ChartPadding _padding = new(76, 78, 36, 74);
    private ChartTheme _theme = ChartTheme.Light();
    private int _tickCount = 6;
    private ChartLabelDensity _xAxisLabelDensity = ChartLabelDensity.Auto;
    private double _xAxisLabelAngle;
    private ChartHeatmapScale _heatmapScale = ChartHeatmapScale.Sequential;
    private ChartLegendPosition _legendPosition = ChartLegendPosition.Bottom;
    private ChartPictorialShape _pictorialShape = ChartPictorialShape.Circle;
    private int _pictorialColumns = 12;
    private double? _pictorialMaximum;
    private double? _pictorialValuePerSymbol;
    private double _pictorialSymbolScale = 1.0;
    private double _pictorialEmptyOpacity = 0.22;
    private string? _pictorialSvgPathData;
    private ChartRect _pictorialSvgPathViewBox = new(0, 0, 24, 24);
    private ChartPictorialShape _pictorialPngFallbackShape = ChartPictorialShape.Circle;
    private double _progressMaximum = 100;
    private double _progressBarThicknessRatio = 0.30;
    private double _progressTrackOpacity = 0.24;
    private double _wordCloudMinFontSize = 12;
    private double _wordCloudMaxFontSize = 46;
    private double[] _wordCloudAngles = { -12, 0, 0, 12, 0 };
    private int? _wordCloudMaximumTerms;
    private double _wordCloudDensity = 1.0;
    private string? _pngFontPath;
    private string? _pngFontFaceName;
    private int? _pngFontCollectionIndex;
    private int _pngSupersamplingScale = 2;
    private int _pngOutputScale = 1;
    private double? _xAxisMinimum;
    private double? _xAxisMaximum;
    private double? _yAxisMinimum;
    private double? _yAxisMaximum;
    private double? _secondaryYAxisMinimum;
    private double? _secondaryYAxisMaximum;
    private double? _ganttToday;
    private double _donutInnerRadiusRatio = 0.58;
    private string? _donutCenterValue;
    private string? _donutCenterLabel;
    private double _circleRadiusScale = 1.0;
    private double _circleStrokeScale = 1.0;
    private double _radialBarRadiusScale = 1.0;
    private double _radialBarStrokeScale = 1.0;

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
    /// Gets or sets the internal supersampling scale used by the PNG renderer.
    /// </summary>
    /// <remarks>
    /// Higher values improve antialiased edges at the cost of render time and memory. The output dimensions stay equal to <see cref="Size"/>.
    /// </remarks>
    public int PngSupersamplingScale {
        get => _pngSupersamplingScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG supersampling scale must be between one and four.");
            _pngSupersamplingScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the output pixel multiplier used by the PNG renderer.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="PngSupersamplingScale"/>, this changes the emitted PNG dimensions while preserving the chart's logical layout.
    /// </remarks>
    public int PngOutputScale {
        get => _pngOutputScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG output scale must be between one and four.");
            _pngOutputScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit minimum value for the cartesian x-axis.
    /// </summary>
    public double? XAxisMinimum {
        get => _xAxisMinimum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(value, _xAxisMaximum, nameof(value), "X-axis");
            _xAxisMinimum = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit maximum value for the cartesian x-axis.
    /// </summary>
    public double? XAxisMaximum {
        get => _xAxisMaximum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(_xAxisMinimum, value, nameof(value), "X-axis");
            _xAxisMaximum = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit minimum value for the cartesian y-axis.
    /// </summary>
    public double? YAxisMinimum {
        get => _yAxisMinimum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(value, _yAxisMaximum, nameof(value), "Y-axis");
            _yAxisMinimum = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit maximum value for the cartesian y-axis.
    /// </summary>
    public double? YAxisMaximum {
        get => _yAxisMaximum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(_yAxisMinimum, value, nameof(value), "Y-axis");
            _yAxisMaximum = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit minimum value for the secondary cartesian y-axis.
    /// </summary>
    public double? SecondaryYAxisMinimum {
        get => _secondaryYAxisMinimum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(value, _secondaryYAxisMaximum, nameof(value), "Secondary y-axis");
            _secondaryYAxisMinimum = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit maximum value for the secondary cartesian y-axis.
    /// </summary>
    public double? SecondaryYAxisMaximum {
        get => _secondaryYAxisMaximum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(_secondaryYAxisMinimum, value, nameof(value), "Secondary y-axis");
            _secondaryYAxisMaximum = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the legend is rendered.
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether capable legends should list individual points instead of series.
    /// </summary>
    public bool ShowPointLegend { get; set; }

    /// <summary>
    /// Gets or sets where the legend is placed relative to the plot area.
    /// </summary>
    public ChartLegendPosition LegendPosition {
        get => _legendPosition;
        set {
            if (!Enum.IsDefined(typeof(ChartLegendPosition), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown legend position.");
            _legendPosition = value;
        }
    }

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
    /// Gets or sets a value indicating whether the x-axis, x tick labels, and x-axis title are rendered when axes are enabled.
    /// </summary>
    public bool ShowXAxis { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the y-axis, y tick labels, and y-axis title are rendered when axes are enabled.
    /// </summary>
    public bool ShowYAxis { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether axis rules and zero-axis lines are rendered when axes are enabled.
    /// </summary>
    public bool ShowAxisLines { get; set; } = true;

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
    /// Gets or sets the optional Gantt current-date marker as an OLE Automation date or numeric schedule value.
    /// </summary>
    public double? GanttToday {
        get => _ganttToday;
        set {
            ValidateNullableFinite(value, nameof(value));
            _ganttToday = value;
        }
    }

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
    /// Gets or sets a value indicating whether heatmap scale legends are rendered.
    /// </summary>
    public bool ShowHeatmapScale { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether heatmap column labels are rendered.
    /// </summary>
    public bool ShowHeatmapColumnLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets the built-in symbol shape used by pictorial charts.
    /// </summary>
    public ChartPictorialShape PictorialShape {
        get => _pictorialShape;
        set {
            if (!Enum.IsDefined(typeof(ChartPictorialShape), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown pictorial shape.");
            _pictorialShape = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of repeated symbols used per pictorial chart row.
    /// </summary>
    public int PictorialColumns {
        get => _pictorialColumns;
        set {
            if (value < 1 || value > 100) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial columns must be between one and one hundred.");
            _pictorialColumns = value;
        }
    }

    /// <summary>
    /// Gets or sets the optional scale maximum used by pictorial chart rows.
    /// </summary>
    public double? PictorialMaximum {
        get => _pictorialMaximum;
        set {
            ValidateNullableFinite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial maximum must be greater than zero.");
            _pictorialMaximum = value;
        }
    }

    /// <summary>
    /// Gets or sets the data value represented by one full pictorial symbol.
    /// </summary>
    public double? PictorialValuePerSymbol {
        get => _pictorialValuePerSymbol;
        set {
            ValidateNullableFinite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial value per symbol must be greater than zero.");
            _pictorialValuePerSymbol = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether numeric value labels are rendered for pictorial rows.
    /// </summary>
    public bool ShowPictorialValues { get; set; } = true;

    /// <summary>
    /// Gets or sets the relative pictorial symbol size inside each symbol slot.
    /// </summary>
    public double PictorialSymbolScale {
        get => _pictorialSymbolScale;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.4 || value > 1.4) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial symbol scale must be between 0.4 and 1.4.");
            _pictorialSymbolScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the opacity used by unfilled pictorial symbols.
    /// </summary>
    public double PictorialEmptyOpacity {
        get => _pictorialEmptyOpacity;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial empty opacity must be between zero and one.");
            _pictorialEmptyOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum value used by progress-bar charts.
    /// </summary>
    public double ProgressMaximum {
        get => _progressMaximum;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Progress maximum must be greater than zero.");
            _progressMaximum = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether numeric value labels are rendered for progress-bar rows.
    /// </summary>
    public bool ShowProgressValues { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether progress-bar rows render slider handles.
    /// </summary>
    public bool ShowProgressHandles { get; set; } = true;

    /// <summary>
    /// Gets or sets the progress-bar thickness as a ratio of each row height.
    /// </summary>
    public double ProgressBarThicknessRatio {
        get => _progressBarThicknessRatio;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.16 || value > 0.72) throw new ArgumentOutOfRangeException(nameof(value), value, "Progress-bar thickness ratio must be between 0.16 and 0.72.");
            _progressBarThicknessRatio = value;
        }
    }

    /// <summary>
    /// Gets or sets the opacity used by progress-bar tracks.
    /// </summary>
    public double ProgressTrackOpacity {
        get => _progressTrackOpacity;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Progress track opacity must be between zero and one.");
            _progressTrackOpacity = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether donut charts render the center total and series label.
    /// </summary>
    public bool ShowDonutCenterLabel { get; set; } = true;

    /// <summary>
    /// Gets or sets the ratio of the donut hole radius to the outer radius.
    /// </summary>
    public double DonutInnerRadiusRatio {
        get => _donutInnerRadiusRatio;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.35 || value > 0.82) throw new ArgumentOutOfRangeException(nameof(value), value, "Donut inner radius ratio must be between 0.35 and 0.82.");
            _donutInnerRadiusRatio = value;
        }
    }

    /// <summary>
    /// Gets or sets optional custom primary text rendered in the center of donut charts.
    /// </summary>
    public string? DonutCenterValue {
        get => _donutCenterValue;
        set => _donutCenterValue = value == null || string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Gets or sets optional custom secondary text rendered in the center of donut charts.
    /// </summary>
    public string? DonutCenterLabel {
        get => _donutCenterLabel;
        set => _donutCenterLabel = value == null || string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Gets or sets a value indicating whether radial-bar charts render the center average and series label.
    /// </summary>
    public bool ShowRadialBarCenterLabel { get; set; } = true;

    /// <summary>
    /// Gets or sets the relative radius used by circle charts.
    /// </summary>
    public double CircleRadiusScale {
        get => _circleRadiusScale;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.65 || value > 1.35) throw new ArgumentOutOfRangeException(nameof(value), value, "Circle radius scale must be between 0.65 and 1.35.");
            _circleRadiusScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the relative stroke thickness used by circle charts.
    /// </summary>
    public double CircleStrokeScale {
        get => _circleStrokeScale;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.55 || value > 1.8) throw new ArgumentOutOfRangeException(nameof(value), value, "Circle stroke scale must be between 0.55 and 1.8.");
            _circleStrokeScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the relative outer radius used by radial-bar charts.
    /// </summary>
    public double RadialBarRadiusScale {
        get => _radialBarRadiusScale;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.65 || value > 1.35) throw new ArgumentOutOfRangeException(nameof(value), value, "Radial-bar radius scale must be between 0.65 and 1.35.");
            _radialBarRadiusScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the relative stroke thickness used by radial-bar charts.
    /// </summary>
    public double RadialBarStrokeScale {
        get => _radialBarStrokeScale;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.55 || value > 1.8) throw new ArgumentOutOfRangeException(nameof(value), value, "Radial-bar stroke scale must be between 0.55 and 1.8.");
            _radialBarStrokeScale = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether circle charts render the status marker and status label.
    /// </summary>
    public bool ShowCircleStatusLabel { get; set; } = true;

    /// <summary>
    /// Gets or sets optional SVG path data used as the pictorial symbol in SVG and HTML output.
    /// </summary>
    public string? PictorialSvgPathData {
        get => _pictorialSvgPathData;
        set {
            if (value == null) {
                _pictorialSvgPathData = null;
                return;
            }

            var trimmed = value.Trim();
            ValidateSvgPathData(trimmed, nameof(value));
            _pictorialSvgPathData = trimmed;
        }
    }

    /// <summary>
    /// Gets or sets the viewBox used to scale <see cref="PictorialSvgPathData"/>.
    /// </summary>
    public ChartRect PictorialSvgPathViewBox {
        get => _pictorialSvgPathViewBox;
        set {
            if (value.Width <= 0 || value.Height <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Pictorial SVG path viewBox must have positive dimensions.");
            _pictorialSvgPathViewBox = value;
        }
    }

    /// <summary>
    /// Gets or sets the built-in shape used by PNG output when <see cref="PictorialSvgPathData"/> is configured.
    /// </summary>
    public ChartPictorialShape PictorialPngFallbackShape {
        get => _pictorialPngFallbackShape;
        set {
            if (!Enum.IsDefined(typeof(ChartPictorialShape), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown pictorial fallback shape.");
            _pictorialPngFallbackShape = value;
        }
    }

    /// <summary>
    /// Gets or sets the smallest font size used by word cloud terms.
    /// </summary>
    public double WordCloudMinFontSize {
        get => _wordCloudMinFontSize;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0 || value >= _wordCloudMaxFontSize) throw new ArgumentOutOfRangeException(nameof(value), value, "Word cloud minimum font size must be greater than zero and smaller than the maximum.");
            _wordCloudMinFontSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the largest font size used by word cloud terms.
    /// </summary>
    public double WordCloudMaxFontSize {
        get => _wordCloudMaxFontSize;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= _wordCloudMinFontSize) throw new ArgumentOutOfRangeException(nameof(value), value, "Word cloud maximum font size must be greater than the minimum.");
            _wordCloudMaxFontSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the deterministic rotation angles used by word cloud terms.
    /// </summary>
    public double[] WordCloudAngles {
        get => (double[])_wordCloudAngles.Clone();
        set {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length == 0) throw new ArgumentException("Word cloud angles must contain at least one angle.", nameof(value));
            var copy = (double[])value.Clone();
            for (var i = 0; i < copy.Length; i++) {
                ChartGuards.Finite(copy[i], nameof(value));
                if (copy[i] < -90 || copy[i] > 90) throw new ArgumentOutOfRangeException(nameof(value), copy[i], "Word cloud angles must be between -90 and 90 degrees.");
            }

            _wordCloudAngles = copy;
        }
    }

    /// <summary>
    /// Gets or sets the optional maximum number of positive-weight terms rendered in a word cloud.
    /// </summary>
    public int? WordCloudMaximumTerms {
        get => _wordCloudMaximumTerms;
        set {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Word cloud maximum terms must be greater than zero.");
            _wordCloudMaximumTerms = value;
        }
    }

    /// <summary>
    /// Gets or sets the word cloud packing density from 0.5 to 2.0.
    /// </summary>
    public double WordCloudDensity {
        get => _wordCloudDensity;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.5 || value > 2.0) throw new ArgumentOutOfRangeException(nameof(value), value, "Word cloud density must be between 0.5 and 2.0.");
            _wordCloudDensity = value;
        }
    }

    /// <summary>
    /// Gets or sets a formatter used for y-axis ticks, data labels, stack totals, and donut totals.
    /// </summary>
    public Func<double, string>? ValueFormatter { get; set; }

    /// <summary>
    /// Gets or sets a formatter used for secondary y-axis ticks.
    /// </summary>
    public Func<double, string>? SecondaryYAxisValueFormatter { get; set; }

    /// <summary>
    /// Gets or sets a formatter used for generated numeric x-axis tick labels.
    /// </summary>
    public Func<double, string>? XAxisValueFormatter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the chart is rendered as a compact sparkline.
    /// </summary>
    public bool IsSparkline { get; set; }

    /// <summary>
    /// Gets explicit labels for x-axis values.
    /// </summary>
    public List<ChartAxisLabel> XAxisLabels { get; } = new();

    internal List<string> SankeyNodeLabels { get; } = new();

    internal List<string> TreeNodeLabels { get; } = new();

    internal void SetXAxisBounds(double minimum, double maximum) {
        ChartGuards.Finite(minimum, nameof(minimum));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "X-axis maximum must be greater than minimum.");
        _xAxisMinimum = minimum;
        _xAxisMaximum = maximum;
    }

    internal void ClearXAxisBounds() {
        _xAxisMinimum = null;
        _xAxisMaximum = null;
    }

    internal void SetYAxisBounds(double minimum, double maximum) {
        ChartGuards.Finite(minimum, nameof(minimum));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Y-axis maximum must be greater than minimum.");
        _yAxisMinimum = minimum;
        _yAxisMaximum = maximum;
    }

    internal void ClearYAxisBounds() {
        _yAxisMinimum = null;
        _yAxisMaximum = null;
    }

    internal void SetSecondaryYAxisBounds(double minimum, double maximum) {
        ChartGuards.Finite(minimum, nameof(minimum));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Secondary y-axis maximum must be greater than minimum.");
        _secondaryYAxisMinimum = minimum;
        _secondaryYAxisMaximum = maximum;
    }

    internal void ClearSecondaryYAxisBounds() {
        _secondaryYAxisMinimum = null;
        _secondaryYAxisMaximum = null;
    }

    internal void SetWordCloudFontRange(double minimum, double maximum) {
        ChartGuards.Finite(minimum, nameof(minimum));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (minimum <= 0) throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Word cloud minimum font size must be greater than zero.");
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Word cloud maximum font size must be greater than the minimum.");
        _wordCloudMinFontSize = minimum;
        _wordCloudMaxFontSize = maximum;
    }

    internal void SetPictorialSvgPath(string pathData, ChartRect viewBox, ChartPictorialShape pngFallbackShape) {
        PictorialSvgPathData = pathData;
        PictorialSvgPathViewBox = viewBox;
        PictorialPngFallbackShape = pngFallbackShape;
    }

    private static void ValidateNullableFinite(double? value, string parameterName) {
        if (value.HasValue) ChartGuards.Finite(value.Value, parameterName);
    }

    private static void ValidateAxisBounds(double? minimum, double? maximum, string parameterName, string axisName) {
        if (minimum.HasValue && maximum.HasValue && maximum.Value <= minimum.Value) {
            throw new ArgumentOutOfRangeException(parameterName, maximum.Value, axisName + " maximum must be greater than minimum.");
        }
    }

    private static void ValidateSvgPathData(string value, string parameterName) {
        if (value.Length == 0) throw new ArgumentException("Pictorial SVG path data must not be empty.", parameterName);
        if (value.Length > 4096) throw new ArgumentException("Pictorial SVG path data must be 4096 characters or fewer.", parameterName);
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            if (char.IsWhiteSpace(ch) || char.IsDigit(ch) || ch == ',' || ch == '.' || ch == '-' || ch == '+') continue;
            if ("MmZzLlHhVvCcSsQqTtAaEe".IndexOf(ch) >= 0) continue;
            throw new ArgumentException("Pictorial SVG path data contains unsupported characters.", parameterName);
        }
    }
}
