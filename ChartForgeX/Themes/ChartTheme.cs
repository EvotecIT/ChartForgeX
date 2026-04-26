using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>
/// Defines colors, typography, and surface styling used by chart renderers.
/// </summary>
public sealed class ChartTheme {
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
    public ChartColor[] Palette { get; set; } = {
        ChartColor.FromRgb(37,99,235), ChartColor.FromRgb(14,165,233), ChartColor.FromRgb(16,185,129),
        ChartColor.FromRgb(245,158,11), ChartColor.FromRgb(239,68,68), ChartColor.FromRgb(139,92,246),
        ChartColor.FromRgb(236,72,153), ChartColor.FromRgb(20,184,166)
    };

    /// <summary>
    /// Gets or sets a value indicating whether renderers should draw an outer card surface.
    /// </summary>
    public bool UseCard { get; set; } = true;

    /// <summary>
    /// Gets or sets the outer card corner radius.
    /// </summary>
    public double CornerRadius { get; set; } = 18;

    /// <summary>
    /// Gets or sets the plot area corner radius.
    /// </summary>
    public double PlotCornerRadius { get; set; } = 14;

    /// <summary>
    /// Gets or sets the default stroke width used by capable renderers.
    /// </summary>
    public double StrokeWidth { get; set; } = 3;

    /// <summary>
    /// Gets or sets the opacity used for the SVG card shadow.
    /// </summary>
    public double ShadowOpacity { get; set; } = 0.14;

    /// <summary>
    /// Gets or sets the title font size used by SVG and HTML renderers.
    /// </summary>
    public double TitleFontSize { get; set; } = 26;

    /// <summary>
    /// Gets or sets the subtitle font size used by SVG and HTML renderers.
    /// </summary>
    public double SubtitleFontSize { get; set; } = 13;

    /// <summary>
    /// Gets or sets the axis title font size used by SVG renderers.
    /// </summary>
    public double AxisTitleFontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the tick label font size used by SVG renderers.
    /// </summary>
    public double TickLabelFontSize { get; set; } = 11;

    /// <summary>
    /// Gets or sets the legend font size used by SVG renderers.
    /// </summary>
    public double LegendFontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the data label font size used by SVG renderers.
    /// </summary>
    public double DataLabelFontSize { get; set; } = 11;

    /// <summary>
    /// Gets or sets the marker radius used by SVG line and scatter renderers.
    /// </summary>
    public double MarkerRadius { get; set; } = 3.25;

    /// <summary>
    /// Gets or sets the CSS font-family used by vector and HTML renderers.
    /// </summary>
    public string FontFamily { get; set; } = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

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
}
