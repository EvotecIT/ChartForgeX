using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>
/// Defines colors, typography, and surface styling used by chart renderers.
/// </summary>
public sealed partial class ChartTheme {
    private ChartColor[] _palette = ChartPalettes.Report;
    private double _cornerRadius = 12;
    private double _plotCornerRadius = 8;
    private double _strokeWidth = 3;
    private double _shadowOpacity = 0.10;
    private double _titleFontSize = 25;
    private double _subtitleFontSize = 12.5;
    private double _axisTitleFontSize = 11.5;
    private double _tickLabelFontSize = 10.5;
    private double _legendFontSize = 11.5;
    private double _dataLabelFontSize = 10.5;
    private double _markerRadius = 3.25;
    private string _fontFamily = ChartFontStacks.SystemSans;

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
    public ChartColor CardBorder { get; set; } = ChartColor.FromRgba(148,163,184,92);

    /// <summary>
    /// Gets or sets the plot area stroke color.
    /// </summary>
    public ChartColor PlotBorder { get; set; } = ChartColor.FromRgba(148,163,184,48);

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
            _palette = (ChartColor[])value.Clone();
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
    /// Applies a default series palette.
    /// </summary>
    /// <param name="colors">The palette colors.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithPalette(params ChartColor[] colors) {
        Palette = colors;
        return this;
    }

    /// <summary>
    /// Applies a default series palette from hexadecimal color strings.
    /// </summary>
    /// <param name="colors">The color strings in #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithPalette(params string[] colors) {
        Palette = ChartPalettes.FromHex(colors);
        return this;
    }

    /// <summary>
    /// Applies a reusable brand kit.
    /// </summary>
    /// <param name="brandKit">The brand kit to apply.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithBrandKit(ChartBrandKit brandKit) {
        if (brandKit == null) throw new ArgumentNullException(nameof(brandKit));
        return brandKit.ApplyTo(this);
    }

    /// <summary>
    /// Applies background and surface colors.
    /// </summary>
    /// <param name="background">The full chart background.</param>
    /// <param name="cardBackground">The outer card background.</param>
    /// <param name="plotBackground">The plot area background.</param>
    /// <param name="cardBorder">The outer card border.</param>
    /// <param name="plotBorder">The plot area border.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithSurfaceColors(ChartColor background, ChartColor cardBackground, ChartColor plotBackground, ChartColor cardBorder, ChartColor plotBorder) {
        Background = background;
        CardBackground = cardBackground;
        PlotBackground = plotBackground;
        CardBorder = cardBorder;
        PlotBorder = plotBorder;
        return this;
    }

    /// <summary>
    /// Applies primary and secondary text colors.
    /// </summary>
    /// <param name="text">The primary text color.</param>
    /// <param name="mutedText">The secondary text color.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithTextColors(ChartColor text, ChartColor mutedText) {
        Text = text;
        MutedText = mutedText;
        return this;
    }

    /// <summary>
    /// Applies grid and axis guide colors.
    /// </summary>
    /// <param name="grid">The grid line color.</param>
    /// <param name="axis">The axis line color.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithGuideColors(ChartColor grid, ChartColor axis) {
        Grid = grid;
        Axis = axis;
        return this;
    }

    /// <summary>
    /// Applies semantic status colors.
    /// </summary>
    /// <param name="positive">The positive status color.</param>
    /// <param name="warning">The warning status color.</param>
    /// <param name="negative">The negative status color.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithSemanticColors(ChartColor positive, ChartColor warning, ChartColor negative) {
        Positive = positive;
        Warning = warning;
        Negative = negative;
        return this;
    }

    /// <summary>
    /// Applies a CSS font-family stack.
    /// </summary>
    /// <param name="fontFamily">The CSS font-family stack.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithFontFamily(string fontFamily) {
        FontFamily = fontFamily;
        return this;
    }

    /// <summary>
    /// Applies the main typography sizes used by renderers.
    /// </summary>
    /// <param name="title">The title font size.</param>
    /// <param name="subtitle">The subtitle font size.</param>
    /// <param name="axisTitle">The axis title font size.</param>
    /// <param name="tickLabel">The tick label font size.</param>
    /// <param name="legend">The legend font size.</param>
    /// <param name="dataLabel">The data label font size.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithTypography(double title, double subtitle, double axisTitle, double tickLabel, double legend, double dataLabel) {
        TitleFontSize = title;
        SubtitleFontSize = subtitle;
        AxisTitleFontSize = axisTitle;
        TickLabelFontSize = tickLabel;
        LegendFontSize = legend;
        DataLabelFontSize = dataLabel;
        return this;
    }

    /// <summary>
    /// Applies chart and plot corner radii.
    /// </summary>
    /// <param name="card">The outer card corner radius.</param>
    /// <param name="plot">The plot area corner radius.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithCornerRadius(double card, double plot) {
        CornerRadius = card;
        PlotCornerRadius = plot;
        return this;
    }

    /// <summary>
    /// Applies the default line stroke width.
    /// </summary>
    /// <param name="strokeWidth">The stroke width.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithStrokeWidth(double strokeWidth) {
        StrokeWidth = strokeWidth;
        return this;
    }

    /// <summary>
    /// Applies the marker radius used by point-capable renderers.
    /// </summary>
    /// <param name="markerRadius">The marker radius.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithMarkerRadius(double markerRadius) {
        MarkerRadius = markerRadius;
        return this;
    }

    /// <summary>
    /// Applies the SVG card shadow opacity.
    /// </summary>
    /// <param name="shadowOpacity">The shadow opacity from zero to one.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithShadowOpacity(double shadowOpacity) {
        ShadowOpacity = shadowOpacity;
        return this;
    }

    /// <summary>
    /// Applies a reusable card and plot surface style.
    /// </summary>
    /// <param name="style">The surface style preset.</param>
    /// <returns>The current theme.</returns>
    public ChartTheme WithSurfaceStyle(ChartSurfaceStyle style) {
        if (!Enum.IsDefined(typeof(ChartSurfaceStyle), style)) throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown chart surface style.");
        if (style == ChartSurfaceStyle.Default) {
            UseCard = true;
            CornerRadius = 18;
            PlotCornerRadius = 14;
            ShadowOpacity = 0.14;
        } else if (style == ChartSurfaceStyle.Flat) {
            UseCard = true;
            CornerRadius = 0;
            PlotCornerRadius = 0;
            ShadowOpacity = 0;
        } else if (style == ChartSurfaceStyle.Framed) {
            UseCard = true;
            CornerRadius = 8;
            PlotCornerRadius = 6;
            ShadowOpacity = 0;
        } else if (style == ChartSurfaceStyle.Floating) {
            UseCard = true;
            CornerRadius = 24;
            PlotCornerRadius = 16;
            ShadowOpacity = 0.24;
        } else if (style == ChartSurfaceStyle.Glass) {
            UseCard = true;
            CornerRadius = 22;
            PlotCornerRadius = 16;
            ShadowOpacity = 0.18;
            CardBackground = WithAlpha(CardBackground, 210);
            PlotBackground = WithAlpha(PlotBackground, 178);
            CardBorder = WithMinimumAlpha(CardBorder, 92);
            PlotBorder = WithMinimumAlpha(PlotBorder, 72);
        } else if (style == ChartSurfaceStyle.Bare) {
            UseCard = false;
            CornerRadius = 0;
            PlotCornerRadius = 0;
            ShadowOpacity = 0;
            PlotBackground = ChartColor.Transparent;
            CardBorder = ChartColor.Transparent;
            PlotBorder = ChartColor.Transparent;
        } else if (style == ChartSurfaceStyle.Compact) {
            UseCard = true;
            CornerRadius = 6;
            PlotCornerRadius = 4;
            ShadowOpacity = 0.04;
            StrokeWidth = 2.2;
            MarkerRadius = 2.8;
            WithTypography(22, 12, 11, 10, 11, 10);
        }

        return this;
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

    /// <summary>
    /// Creates a colorblind-friendly light theme using a high-contrast qualitative palette.
    /// </summary>
    /// <returns>A colorblind-friendly chart theme.</returns>
    public static ChartTheme Colorblind() => new() {
        CardBackground = ChartColor.FromRgb(255,255,255),
        PlotBackground = ChartColor.FromRgb(250,250,247),
        CardBorder = ChartColor.FromRgba(82,82,82,58),
        PlotBorder = ChartColor.FromRgba(82,82,82,44),
        Text = ChartColor.FromRgb(24,24,27),
        MutedText = ChartColor.FromRgb(82,82,91),
        Grid = ChartColor.FromRgba(113,113,122,66),
        Axis = ChartColor.FromRgba(39,39,42,150),
        Positive = ChartColor.FromRgb(0,158,115),
        Warning = ChartColor.FromRgb(230,159,0),
        Negative = ChartColor.FromRgb(213,94,0),
        Palette = ChartPalettes.Colorblind
    };

    /// <summary>
    /// Creates a vibrant dark theme for high-impact product and status dashboards.
    /// </summary>
    /// <returns>A vibrant dark chart theme.</returns>
    public static ChartTheme Aurora() => new() {
        Background = ChartColor.Transparent,
        CardBackground = ChartColor.FromRgb(11,18,32),
        PlotBackground = ChartColor.FromRgb(7,12,24),
        CardBorder = ChartColor.FromRgba(125,211,252,38),
        PlotBorder = ChartColor.FromRgba(45,212,191,34),
        Text = ChartColor.FromRgb(240,249,255),
        MutedText = ChartColor.FromRgb(186,230,253),
        Grid = ChartColor.FromRgba(56,189,248,42),
        Axis = ChartColor.FromRgba(165,243,252,120),
        Positive = ChartColor.FromRgb(45,212,191),
        Warning = ChartColor.FromRgb(250,204,21),
        Negative = ChartColor.FromRgb(251,113,133),
        ShadowOpacity = 0.24,
        StrokeWidth = 3.4,
        MarkerRadius = 3.6,
        FontFamily = ChartFontStacks.Geometric,
        Palette = ChartPalettes.Vivid
    };

    /// <summary>
    /// Creates a refined editorial theme with serif typography.
    /// </summary>
    /// <returns>An editorial chart theme.</returns>
    public static ChartTheme Editorial() => new() {
        CardBackground = ChartColor.FromRgb(255,252,247),
        PlotBackground = ChartColor.FromRgb(250,246,238),
        CardBorder = ChartColor.FromRgba(120,113,108,72),
        PlotBorder = ChartColor.FromRgba(168,162,158,58),
        Text = ChartColor.FromRgb(28,25,23),
        MutedText = ChartColor.FromRgb(87,83,78),
        Grid = ChartColor.FromRgba(168,162,158,74),
        Axis = ChartColor.FromRgba(68,64,60,140),
        Positive = ChartColor.FromRgb(22,101,52),
        Warning = ChartColor.FromRgb(180,83,9),
        Negative = ChartColor.FromRgb(185,28,28),
        CornerRadius = 8,
        PlotCornerRadius = 6,
        ShadowOpacity = 0.08,
        FontFamily = ChartFontStacks.Serif,
        Palette = ChartPalettes.Editorial
    };

    /// <summary>
    /// Creates a soft, playful light theme with rounded typography.
    /// </summary>
    /// <returns>A playful chart theme.</returns>
    public static ChartTheme Candy() => new() {
        CardBackground = ChartColor.FromRgb(255,255,255),
        PlotBackground = ChartColor.FromRgb(253,246,255),
        CardBorder = ChartColor.FromRgba(217,70,239,46),
        PlotBorder = ChartColor.FromRgba(14,165,233,46),
        Text = ChartColor.FromRgb(49,46,129),
        MutedText = ChartColor.FromRgb(99,102,241),
        Grid = ChartColor.FromRgba(125,211,252,82),
        Axis = ChartColor.FromRgba(79,70,229,130),
        Positive = ChartColor.FromRgb(20,184,166),
        Warning = ChartColor.FromRgb(245,158,11),
        Negative = ChartColor.FromRgb(244,63,94),
        CornerRadius = 24,
        PlotCornerRadius = 18,
        ShadowOpacity = 0.11,
        FontFamily = ChartFontStacks.Rounded,
        Palette = ChartPalettes.Pastel
    };

    /// <summary>
    /// Creates a bright people-infographic theme for demographic and survey dashboards.
    /// </summary>
    /// <returns>A people-infographic chart theme.</returns>
    public static ChartTheme PeopleInfographic() => Minimal()
        .WithBrandKit(ChartBrandKit.PeopleInfographic())
        .WithTypography(24, 12, 11, 10, 11, 10)
        .WithStrokeWidth(2.7)
        .WithMarkerRadius(3.1);

    /// <summary>
    /// Creates a compact technical theme for operations and infrastructure charts.
    /// </summary>
    /// <returns>A technical chart theme.</returns>
    public static ChartTheme Terminal() => new() {
        Background = ChartColor.Transparent,
        CardBackground = ChartColor.FromRgb(6,10,16),
        PlotBackground = ChartColor.FromRgb(2,6,12),
        CardBorder = ChartColor.FromRgba(34,197,94,48),
        PlotBorder = ChartColor.FromRgba(34,197,94,40),
        Text = ChartColor.FromRgb(220,252,231),
        MutedText = ChartColor.FromRgb(134,239,172),
        Grid = ChartColor.FromRgba(34,197,94,38),
        Axis = ChartColor.FromRgba(187,247,208,105),
        Positive = ChartColor.FromRgb(74,222,128),
        Warning = ChartColor.FromRgb(250,204,21),
        Negative = ChartColor.FromRgb(248,113,113),
        CornerRadius = 10,
        PlotCornerRadius = 6,
        ShadowOpacity = 0.2,
        FontFamily = ChartFontStacks.Mono,
        TitleFontSize = 23,
        SubtitleFontSize = 12,
        AxisTitleFontSize = 11,
        TickLabelFontSize = 10,
        LegendFontSize = 11,
        DataLabelFontSize = 10,
        Palette = ChartPalettes.Terminal
    };

    /// <summary>
    /// Creates a transparent dark overlay theme for charts rendered on existing imagery or wallpapers.
    /// </summary>
    /// <returns>A transparent overlay chart theme.</returns>
    public static ChartTheme TransparentOverlayDark() => new() {
        Background = ChartColors.Transparent,
        CardBackground = ChartColors.Slate950.WithAlpha(172),
        PlotBackground = ChartColors.Transparent,
        CardBorder = ChartColors.Slate400.WithAlpha(64),
        PlotBorder = ChartColors.Slate400.WithAlpha(34),
        Text = ChartColors.White,
        MutedText = ChartColors.Slate200.WithAlpha(230),
        Grid = ChartColors.Slate400.WithAlpha(42),
        Axis = ChartColors.Slate200.WithAlpha(110),
        Positive = ChartColors.Emerald400,
        Warning = ChartColors.Orange400,
        Negative = ChartColors.Red400,
        CornerRadius = 8,
        PlotCornerRadius = 4,
        ShadowOpacity = 0,
        StrokeWidth = 2.7,
        MarkerRadius = 3,
        FontFamily = ChartFontStacks.SystemSans,
        Palette = ChartPalettes.CommandCenter
    };

    /// <summary>
    /// Creates a crisp minimal theme for dense business dashboards.
    /// </summary>
    /// <returns>A minimal chart theme.</returns>
    public static ChartTheme Minimal() => new() {
        CardBackground = ChartColor.FromRgb(255,255,255),
        PlotBackground = ChartColor.FromRgb(255,255,255),
        CardBorder = ChartColor.FromRgba(15,23,42,34),
        PlotBorder = ChartColor.FromRgba(15,23,42,28),
        Grid = ChartColor.FromRgba(148,163,184,50),
        Axis = ChartColor.FromRgba(51,65,85,120),
        CornerRadius = 6,
        PlotCornerRadius = 4,
        ShadowOpacity = 0,
        StrokeWidth = 2.4,
        MarkerRadius = 2.8,
        FontFamily = ChartFontStacks.Humanist,
        Palette = ChartPalettes.Jewel
    };

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

    private static ChartColor WithAlpha(ChartColor color, byte alpha) => color.WithAlpha(alpha);

    private static ChartColor WithMinimumAlpha(ChartColor color, byte alpha) => color.WithAlpha(color.A < alpha ? alpha : color.A);
}
