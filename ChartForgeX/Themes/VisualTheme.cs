using System;
using System.Collections.Generic;
using System.IO;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Themes;

/// <summary>A paired immutable theme shared by charts, diagrams and future composition packages.</summary>
public sealed partial class VisualTheme {
    private const double DefaultCardRadius = 3;
    private static readonly Lazy<VisualTheme> Default = new(CreateGraphite);
    private readonly VisualThemeColors _light;
    private readonly VisualThemeColors _dark;
    /// <summary>Creates a paired theme from independent token snapshots and geometry settings.</summary>
    public VisualTheme(VisualDesignTokens light, VisualDesignTokens dark, VisualTypography? typography = null,
        double spacing = 12, double seriesStrokeWidth = 2, double markerRadius = 3,
        double areaOpacity = 0.18, double barRadius = 3, double gridStrokeWidth = 1, double axisStrokeWidth = 1,
        double cardRadius = DefaultCardRadius, double cardShadowOpacity = 0, ChartColor? cardShadowColor = null,
        double gaugeStrokeWidth = 14, double gaugeBandWidth = 4, double plotRadius = 3) {
        _light = new VisualThemeColors(light ?? throw new ArgumentNullException(nameof(light)));
        _dark = new VisualThemeColors(dark ?? throw new ArgumentNullException(nameof(dark)));
        Typography = typography ?? new VisualTypography();
        Spacing = NonNegative(spacing, nameof(spacing)); SeriesStrokeWidth = NonNegative(seriesStrokeWidth, nameof(seriesStrokeWidth));
        MarkerRadius = NonNegative(markerRadius, nameof(markerRadius)); BarRadius = NonNegative(barRadius, nameof(barRadius));
        GridStrokeWidth = NonNegative(gridStrokeWidth, nameof(gridStrokeWidth)); AxisStrokeWidth = NonNegative(axisStrokeWidth, nameof(axisStrokeWidth));
        CardRadius = NonNegative(cardRadius, nameof(cardRadius));
        GaugeStrokeWidth = NonNegative(gaugeStrokeWidth, nameof(gaugeStrokeWidth));
        GaugeBandWidth = NonNegative(gaugeBandWidth, nameof(gaugeBandWidth));
        PlotRadius = NonNegative(plotRadius, nameof(plotRadius));
        if (double.IsNaN(cardShadowOpacity) || cardShadowOpacity < 0 || cardShadowOpacity > 1) throw new ArgumentOutOfRangeException(nameof(cardShadowOpacity));
        CardShadowOpacity = cardShadowOpacity;
        CardShadowColor = cardShadowColor ?? ChartColor.FromRgb(15, 23, 42);
        if (double.IsNaN(areaOpacity) || areaOpacity < 0 || areaOpacity > 1) throw new ArgumentOutOfRangeException(nameof(areaOpacity));
        AreaOpacity = areaOpacity;
    }
    /// <summary>Gets the typography scale.</summary>
    public VisualTypography Typography { get; }
    /// <summary>Gets the standard layout gap.</summary>
    public double Spacing { get; }
    /// <summary>Gets the default data-line width.</summary>
    public double SeriesStrokeWidth { get; }
    /// <summary>Gets the default point radius.</summary>
    public double MarkerRadius { get; }
    /// <summary>Gets the area fill opacity.</summary>
    public double AreaOpacity { get; }
    /// <summary>Gets the bar corner radius.</summary>
    public double BarRadius { get; }
    /// <summary>Gets the shared outer card corner radius, independent of plot and data mark radii.</summary>
    public double CardRadius { get; }
    /// <summary>Gets the plot surface corner radius, independent of card and mark radii.</summary>
    public double PlotRadius { get; }
    /// <summary>Gets the shared card shadow opacity. Zero keeps the default flat surface.</summary>
    public double CardShadowOpacity { get; }
    /// <summary>Gets the shared card shadow color, including its authored alpha.</summary>
    public ChartColor CardShadowColor { get; }
    /// <summary>Gets the guide width.</summary>
    public double GridStrokeWidth { get; }
    /// <summary>Gets the axis width.</summary>
    public double AxisStrokeWidth { get; }
    /// <summary>Gets the gauge track and value stroke width.</summary>
    public double GaugeStrokeWidth { get; }
    /// <summary>Gets the gauge threshold band width.</summary>
    public double GaugeBandWidth { get; }
    /// <summary>Resolves the selected immutable color snapshot.</summary>
    public VisualThemeColors Resolve(VisualThemeMode mode) => mode switch {
        VisualThemeMode.Light => _light, VisualThemeMode.Dark => _dark,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
    /// <summary>Gets the Graphite layout preset with the canonical HtmlForgeX light/dark colors.</summary>
    public static VisualTheme Graphite() => Default.Value;
    /// <summary>Creates an independent paired theme with the supplied typography and the same colors, geometry and effects.</summary>
    /// <param name="typography">The complete font scale to use for the new theme.</param>
    public VisualTheme WithTypography(VisualTypography typography) => new(_light.ToTokens(), _dark.ToTokens(),
        typography ?? throw new ArgumentNullException(nameof(typography)), Spacing, SeriesStrokeWidth, MarkerRadius,
        AreaOpacity, BarRadius, GridStrokeWidth, AxisStrokeWidth, CardRadius, CardShadowOpacity, CardShadowColor,
        GaugeStrokeWidth, GaugeBandWidth, PlotRadius);
    /// <summary>Creates a new immutable theme by configuring an independent builder.</summary>
    /// <remarks>The callback cannot mutate this theme. Invalid or throwing callbacks leave the original snapshot intact.</remarks>
    public VisualTheme Configure(Action<VisualThemeBuilder> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var builder = new VisualThemeBuilder(this);
        configure(builder);
        return builder.Build();
    }
    /// <summary>Creates a theme with the supplied family and all other typography preserved.</summary>
    public VisualTheme WithFontFamily(string family) => WithTypography(Typography.WithFamily(family));
    /// <summary>Creates a theme with a categorical palette for one explicit mode.</summary>
    public VisualTheme WithPalette(VisualThemeMode mode, params ChartColor[] colors) => Configure(builder => builder.Colors(mode).Palette = colors);
    /// <summary>Creates a theme with the supplied categorical palette in both modes.</summary>
    public VisualTheme WithPalette(params ChartColor[] colors) => Configure(builder => { builder.Light.Palette = colors; builder.Dark.Palette = colors; });
    /// <summary>Creates a theme from generated HtmlForgeX color palettes without changing geometry.</summary>
    public static VisualTheme FromPaletteJson(string json, VisualTypography? typography = null) => new(
        VisualDesignTokens.FromJson(json, VisualThemeMode.Light), VisualDesignTokens.FromJson(json, VisualThemeMode.Dark), typography);
    internal ResolvedVisualTheme ResolvePresentation(VisualThemeMode mode) => new(this, mode);
    private static VisualTheme CreateGraphite() {
        using var stream = typeof(VisualTheme).Assembly.GetManifestResourceStream("ChartForgeX.Themes.Tokens.evotec.chartforgex.tokens.json")
            ?? throw new InvalidOperationException("The canonical theme resource is missing.");
        using var reader = new StreamReader(stream);
        return FromPaletteJson(reader.ReadToEnd());
    }
    private static double NonNegative(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }
}

/// <summary>Immutable semantic color roles for one theme mode.</summary>
public sealed class VisualThemeColors {
    private readonly VisualDesignTokens _tokens;
    internal VisualThemeColors(VisualDesignTokens tokens) { _tokens = tokens.Clone(); Palette = Array.AsReadOnly(_tokens.Palette); }
    internal VisualDesignTokens ToTokens() => _tokens.Clone();
    /// <summary>Gets the canvas color.</summary>
    public ChartColor Background => _tokens.Background;
    /// <summary>Gets the plot surface color.</summary>
    public ChartColor Surface => _tokens.Surface;
    /// <summary>Gets the card surface color.</summary>
    public ChartColor ElevatedSurface => _tokens.ElevatedSurface;
    /// <summary>Gets the primary text color.</summary>
    public ChartColor Foreground => _tokens.Foreground;
    /// <summary>Gets the secondary text color.</summary>
    public ChartColor MutedForeground => _tokens.MutedForeground;
    /// <summary>Gets the guide and border color.</summary>
    public ChartColor Border => _tokens.Border;
    /// <summary>Gets the axis rule color, falling back to the shared border role when unspecified.</summary>
    public ChartColor Axis => _tokens.Axis ?? _tokens.Border;
    /// <summary>Gets the plot grid color, falling back to the shared border role when unspecified.</summary>
    public ChartColor Grid => _tokens.Grid ?? _tokens.Border;
    /// <summary>Gets the weakest neutral data surface.</summary>
    public ChartColor Neutral => _tokens.Neutral ?? _tokens.Surface;
    /// <summary>Gets the middle neutral data surface.</summary>
    public ChartColor Neutral2 => _tokens.Neutral2 ?? _tokens.Border;
    /// <summary>Gets the strongest neutral data surface.</summary>
    public ChartColor Neutral3 => _tokens.Neutral3 ?? _tokens.MutedForeground;
    /// <summary>Gets the independent plot border, falling back to the shared border when unspecified.</summary>
    public ChartColor PlotBorder => _tokens.PlotBorder ?? _tokens.Border;
    /// <summary>Gets the secondary annotation color.</summary>
    public ChartColor Muted => _tokens.Muted ?? _tokens.MutedForeground;
    /// <summary>Gets the informational mark color.</summary>
    public ChartColor Info => _tokens.Info ?? _tokens.Status.Info.Fill;
    /// <summary>Gets the subdued positive fill.</summary>
    public ChartColor Quiet => _tokens.Quiet ?? _tokens.Status.Pass.Fill;
    /// <summary>Gets the subdued line color.</summary>
    public ChartColor QuietLine => _tokens.QuietLine ?? _tokens.Border;
    /// <summary>Gets the positive mark color independently of status ink.</summary>
    public ChartColor Positive => _tokens.Positive;
    /// <summary>Gets the warning mark color independently of status ink.</summary>
    public ChartColor Warning => _tokens.Warning;
    /// <summary>Gets the negative mark color independently of status ink.</summary>
    public ChartColor Negative => _tokens.Negative;
    /// <summary>Gets the disabled mark color.</summary>
    public ChartColor Disabled => _tokens.Disabled;
    /// <summary>Gets the secondary accent color.</summary>
    public ChartColor SecondaryAccent => _tokens.SecondaryAccent;
    /// <summary>Gets the accent color.</summary>
    public ChartColor Accent => _tokens.Accent;
    /// <summary>Gets the ordered categorical palette.</summary>
    public IReadOnlyList<ChartColor> Palette { get; }
    /// <summary>Gets an independent copy of the severity, outcome and state color pairs.</summary>
    public VisualStatusTokens Status => _tokens.Status.Clone();
    /// <summary>Gets the sequential ramp, weakest to strongest.</summary>
    public IReadOnlyList<ChartColor> SequentialRamp => Array.AsReadOnly(_tokens.SequentialRamp ?? Array.Empty<ChartColor>());
    /// <summary>Gets the immutable diverging ramp.</summary>
    public VisualDivergingRamp? DivergingRamp => _tokens.DivergingRamp;
}

/// <summary>The shared logical typography scale; the default follows the chart look specification.</summary>
public sealed class VisualTypography {
    /// <summary>Creates typography with the existing chart-role sizes and default tile sizes.</summary>
    public VisualTypography(string family, double titleSize, double subtitleSize, double axisSize,
        double legendSize, double dataLabelSize, double scalarValueSize, double centerValueSize,
        double? axisTitleSize, string monospaceFamily)
        : this(family, titleSize, subtitleSize, axisSize, legendSize, dataLabelSize,
            scalarValueSize, centerValueSize, axisTitleSize, monospaceFamily, 14, 22, 13) { }

    /// <summary>Creates a validated immutable font scale including VisualCanvas tile sizes.</summary>
    public VisualTypography(string family = "Calibri, Carlito, Segoe UI, Arial, system-ui, sans-serif",
        double titleSize = 17, double subtitleSize = 13.5, double axisSize = 12, double legendSize = 13, double dataLabelSize = 12,
        double scalarValueSize = 34, double centerValueSize = 20, double? axisTitleSize = null,
        string monospaceFamily = "Cascadia Mono, Consolas, monospace",
        double tileLabelSize = 14, double tileValueSize = 22, double tileDetailSize = 13) {
        if (string.IsNullOrWhiteSpace(family)) throw new ArgumentException("A font family is required.", nameof(family));
        if (string.IsNullOrWhiteSpace(monospaceFamily)) throw new ArgumentException("A monospace family is required.", nameof(monospaceFamily));
        MonospaceFamily = monospaceFamily; AxisTitleSize = Positive(axisTitleSize ?? axisSize, nameof(axisTitleSize));
        Family = family; TitleSize = Positive(titleSize, nameof(titleSize)); SubtitleSize = Positive(subtitleSize, nameof(subtitleSize));
        AxisSize = Positive(axisSize, nameof(axisSize)); LegendSize = Positive(legendSize, nameof(legendSize)); DataLabelSize = Positive(dataLabelSize, nameof(dataLabelSize));
        ScalarValueSize = Positive(scalarValueSize, nameof(scalarValueSize)); CenterValueSize = Positive(centerValueSize, nameof(centerValueSize));
        TileLabelSize = Positive(tileLabelSize, nameof(tileLabelSize)); TileValueSize = Positive(tileValueSize, nameof(tileValueSize));
        TileDetailSize = Positive(tileDetailSize, nameof(tileDetailSize));
    }
    /// <summary>Gets the fallback family stack.</summary>
    public string Family { get; }
    /// <summary>Gets the monospace fallback family stack.</summary>
    public string MonospaceFamily { get; }
    /// <summary>Gets the independent axis title size.</summary>
    public double AxisTitleSize { get; }
    /// <summary>Creates a font request for the normal or monospace family.</summary>
    public FontSpec ToFontSpec(bool monospace = false) => new() { Family = monospace ? MonospaceFamily : Family };
    /// <summary>Creates an independent scale with a different proportional family.</summary>
    public VisualTypography WithFamily(string family) => new(family, TitleSize, SubtitleSize, AxisSize, LegendSize,
        DataLabelSize, ScalarValueSize, CenterValueSize, AxisTitleSize, MonospaceFamily, TileLabelSize, TileValueSize, TileDetailSize);
    /// <summary>Gets the title size.</summary>
    public double TitleSize { get; }
    /// <summary>Gets the subtitle size.</summary>
    public double SubtitleSize { get; }
    /// <summary>Gets the axis size.</summary>
    public double AxisSize { get; }
    /// <summary>Gets the legend size.</summary>
    public double LegendSize { get; }
    /// <summary>Gets the data-label size.</summary>
    public double DataLabelSize { get; }
    /// <summary>Gets the primary value size for scalar charts.</summary>
    public double ScalarValueSize { get; }
    /// <summary>Gets the value size inside a radial chart.</summary>
    public double CenterValueSize { get; }
    /// <summary>Gets the default VisualCanvas information-tile label size.</summary>
    public double TileLabelSize { get; }
    /// <summary>Gets the default VisualCanvas information-tile value size.</summary>
    public double TileValueSize { get; }
    /// <summary>Gets the default VisualCanvas information-tile detail size.</summary>
    public double TileDetailSize { get; }
    private static double Positive(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }
}
