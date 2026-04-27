using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>
/// Defines colors, typography, and surface styling used by chart renderers.
/// </summary>
public sealed class ChartTheme {
    private ChartColor[] _palette = {
        ChartColor.FromRgb(37,99,235), ChartColor.FromRgb(14,165,233), ChartColor.FromRgb(16,185,129),
        ChartColor.FromRgb(245,158,11), ChartColor.FromRgb(239,68,68), ChartColor.FromRgb(139,92,246),
        ChartColor.FromRgb(236,72,153), ChartColor.FromRgb(20,184,166)
    };
    private double _cornerRadius = 18;
    private double _plotCornerRadius = 14;
    private double _strokeWidth = 3;
    private double _shadowOpacity = 0.14;
    private double _titleFontSize = 26;
    private double _subtitleFontSize = 13;
    private double _axisTitleFontSize = 12;
    private double _tickLabelFontSize = 11;
    private double _legendFontSize = 12;
    private double _dataLabelFontSize = 11;
    private double _markerRadius = 3.25;
    private string _fontFamily = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

    /// <summary>
    /// Gets or sets the full chart background color.
    /// </summary>
    public ChartColor Background { get; set; } = ChartColor.Transparent;

    /// <summary>
    /// Gets or sets the outer card background color.
    /// </summary>
    public ChartColor CardBackground { get; set; } = ChartColor.FromRgb(255,255,255);

    /// <summary>
    /// Gets or sets the plot area background color.
    /// </summary>
    public ChartColor PlotBackground { get; set; } = ChartColor.FromRgb(248,250,252);

    /// <summary>
    /// Gets or sets the outer card stroke color.
    /// </summary>
    public ChartColor CardBorder { get; set; } = ChartColor.FromRgba(148,163,184,80);

    /// <summary>
    /// Gets or sets the plot area stroke color.
    /// </summary>
    public ChartColor PlotBorder { get; set; } = ChartColor.FromRgba(148,163,184,55);

    /// <summary>
    /// Gets or sets the primary text color.
    /// </summary>
    public ChartColor Text { get; set; } = ChartColor.FromRgb(15,23,42);

    /// <summary>
    /// Gets or sets the secondary text color.
    /// </summary>
    public ChartColor MutedText { get; set; } = ChartColor.FromRgb(100,116,139);

    /// <summary>
    /// Gets or sets the grid line color.
    /// </summary>
    public ChartColor Grid { get; set; } = ChartColor.FromRgba(148,163,184,70);

    /// <summary>
    /// Gets or sets the axis line color.
    /// </summary>
    public ChartColor Axis { get; set; } = ChartColor.FromRgba(71,85,105,160);

    /// <summary>
    /// Gets or sets the semantic color used for positive status indicators.
    /// </summary>
    public ChartColor Positive { get; set; } = ChartColor.FromRgb(16,185,129);

    /// <summary>
    /// Gets or sets the semantic color used for warning status indicators.
    /// </summary>
    public ChartColor Warning { get; set; } = ChartColor.FromRgb(245,158,11);

    /// <summary>
    /// Gets or sets the semantic color used for negative status indicators.
    /// </summary>
    public ChartColor Negative { get; set; } = ChartColor.FromRgb(239,68,68);

    /// <summary>
    /// Gets or sets the default series palette.
    /// </summary>
    public ChartColor[] Palette {
        get => _palette;
        set {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length == 0) throw new ArgumentException("Palette must contain at least one color.", nameof(value));
            _palette = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether renderers should draw an outer card surface.
    /// </summary>
    public bool UseCard { get; set; } = true;

    /// <summary>
    /// Gets or sets the outer card corner radius.
    /// </summary>
    public double CornerRadius {
        get => _cornerRadius;
        set => _cornerRadius = NonNegative(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the plot area corner radius.
    /// </summary>
    public double PlotCornerRadius {
        get => _plotCornerRadius;
        set => _plotCornerRadius = NonNegative(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the default stroke width used by capable renderers.
    /// </summary>
    public double StrokeWidth {
        get => _strokeWidth;
        set => _strokeWidth = ValidatePositive(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the opacity used for the SVG card shadow.
    /// </summary>
    public double ShadowOpacity {
        get => _shadowOpacity;
        set => _shadowOpacity = UnitInterval(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the title font size used by SVG and HTML renderers.
    /// </summary>
    public double TitleFontSize {
        get => _titleFontSize;
        set => _titleFontSize = ValidatePositive(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the subtitle font size used by SVG and HTML renderers.
    /// </summary>
    public double SubtitleFontSize {
        get => _subtitleFontSize;
        set => _subtitleFontSize = ValidatePositive(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the axis title font size used by SVG renderers.
    /// </summary>
    public double AxisTitleFontSize {
        get => _axisTitleFontSize;
        set => _axisTitleFontSize = ValidatePositive(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the tick label font size used by SVG renderers.
    /// </summary>
    public double TickLabelFontSize {
        get => _tickLabelFontSize;
        set => _tickLabelFontSize = ValidatePositive(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the legend font size used by SVG renderers.
    /// </summary>
    public double LegendFontSize {
        get => _legendFontSize;
        set => _legendFontSize = ValidatePositive(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the data label font size used by SVG renderers.
    /// </summary>
    public double DataLabelFontSize {
        get => _dataLabelFontSize;
        set => _dataLabelFontSize = ValidatePositive(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the marker radius used by SVG line and scatter renderers.
    /// </summary>
    public double MarkerRadius {
        get => _markerRadius;
        set => _markerRadius = NonNegative(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the CSS font-family used by vector and HTML renderers.
    /// </summary>
    public string FontFamily {
        get => _fontFamily;
        set => _fontFamily = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Creates the default light report theme.
    /// </summary>
    /// <returns>A light chart theme.</returns>
    public static ChartTheme Light() => new();

    /// <summary>
    /// Creates a polished light theme for static reports and generated HTML.
    /// </summary>
    /// <returns>A light report chart theme.</returns>
    public static ChartTheme ReportLight() => Light();

    /// <summary>
    /// Creates the default dark report theme.
    /// </summary>
    /// <returns>A dark chart theme.</returns>
    public static ChartTheme Dark() => new() {
        Background = ChartColor.Transparent,
        CardBackground = ChartColor.FromRgb(16,24,39),
        PlotBackground = ChartColor.FromRgb(8,13,24),
        CardBorder = ChartColor.FromRgba(148,163,184,34),
        PlotBorder = ChartColor.FromRgba(148,163,184,28),
        Text = ChartColor.FromRgb(248,250,252),
        MutedText = ChartColor.FromRgb(186,198,214),
        Grid = ChartColor.FromRgba(148,163,184,45),
        Axis = ChartColor.FromRgba(203,213,225,105),
        Positive = ChartColor.FromRgb(52,211,153),
        Warning = ChartColor.FromRgb(251,191,36),
        Negative = ChartColor.FromRgb(248,113,113),
        ShadowOpacity = 0.22,
        Palette = new[] {
            ChartColor.FromRgb(96,165,250), ChartColor.FromRgb(34,211,238), ChartColor.FromRgb(52,211,153),
            ChartColor.FromRgb(251,191,36), ChartColor.FromRgb(248,113,113), ChartColor.FromRgb(167,139,250),
            ChartColor.FromRgb(244,114,182), ChartColor.FromRgb(45,212,191)
        }
    };

    /// <summary>
    /// Creates a polished dark theme for static reports and generated HTML.
    /// </summary>
    /// <returns>A dark report chart theme.</returns>
    public static ChartTheme ReportDark() => Dark();

    private static double ValidatePositive(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and greater than zero.");
        return value;
    }

    private static double NonNegative(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and non-negative.");
        return value;
    }

    private static double UnitInterval(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be between zero and one.");
        return value;
    }
}
