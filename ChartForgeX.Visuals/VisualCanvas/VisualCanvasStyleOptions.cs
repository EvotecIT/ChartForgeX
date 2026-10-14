using System;
using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Composition;

/// <summary>Canvas-specific presentation overrides. Unassigned colors and fonts come from <see cref="VisualTheme"/>.</summary>
public sealed class VisualCanvasStyleOptions {
    private TextMeasurementMode _textMeasurementMode;
    /// <summary>Chooses portable text measurement or an explicit host-font opt-in.</summary>
    public TextMeasurementMode TextMeasurementMode { get => _textMeasurementMode; set { VisualCanvas.ValidateEnum(value, nameof(value)); _textMeasurementMode = value; } }
    /// <summary>Overrides the top glass-tile paint.</summary>
    public ChartColor? TileGlassTop { get; set; }
    /// <summary>Overrides the bottom glass-tile paint.</summary>
    public ChartColor? TileGlassBottom { get; set; }
    /// <summary>Overrides the glass-tile inner highlight.</summary>
    public ChartColor? TileInnerStroke { get; set; }
    /// <summary>Overrides tile label text independently of the shared secondary foreground. Null inherits the selected theme mode.</summary>
    public ChartColor? TileLabelColor { get; set; }
    /// <summary>Overrides tile value text independently of the shared foreground. Null inherits the selected theme mode.</summary>
    public ChartColor? TileValueColor { get; set; }
    /// <summary>Overrides tile detail text independently of the shared secondary foreground. Null inherits the selected theme mode.</summary>
    public ChartColor? TileDetailColor { get; set; }
    /// <summary>Overrides the progress rail paint.</summary>
    public ChartColor? TileProgressTrackColor { get; set; }
    /// <summary>Overrides the mini-chart fill treatment.</summary>
    public ChartColor? TileMiniChartFillColor { get; set; }
    /// <summary>Overrides the mini-chart guide treatment.</summary>
    public ChartColor? TileMiniChartTrackColor { get; set; }
    /// <summary>Overrides the hero-badge glow.</summary>
    public ChartColor? HeroBadgeGlowColor { get; set; }
    /// <summary>Overrides the top hero-badge paint.</summary>
    public ChartColor? HeroBadgeTop { get; set; }
    /// <summary>Overrides the bottom hero-badge paint.</summary>
    public ChartColor? HeroBadgeBottom { get; set; }
    /// <summary>Overrides hero-badge symbol text independently of the shared foreground. Null inherits the selected theme mode.</summary>
    public ChartColor? HeroBadgeTextColor { get; set; }
    /// <summary>Overrides an image placeholder's surface.</summary>
    public ChartColor? ImagePlaceholderFill { get; set; }
    /// <summary>Overrides an image placeholder's border.</summary>
    public ChartColor? ImagePlaceholderStroke { get; set; }
    /// <summary>Overrides feature-strip dividers.</summary>
    public ChartColor? FeatureDividerColor { get; set; }
    /// <summary>Overrides the optional technology backdrop horizon.</summary>
    public ChartColor? TechHorizonFill { get; set; }
    /// <summary>Creates an independent copy of these local overrides.</summary>
    public VisualCanvasStyleOptions Clone() => (VisualCanvasStyleOptions)MemberwiseClone();
}

public sealed partial class VisualCanvas {
    /// <summary>Gets or sets the explicit light or dark appearance.</summary>
    public VisualThemeMode ThemeMode { get => _themeMode; set { ValidateEnum(value, nameof(value)); _themeMode = value; } }
    /// <summary>Gets or sets local canvas styling without replacing the shared theme.</summary>
    public VisualCanvasStyleOptions Style { get => _style; set => _style = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Changes appearance without changing the theme snapshot.</summary>
    public VisualCanvas WithThemeMode(VisualThemeMode mode) { ThemeMode = mode; return this; }
    /// <summary>Sets canvas-specific surface and measurement options.</summary>
    public VisualCanvas WithStyle(VisualCanvasStyleOptions style) { Style = style; return this; }
    /// <summary>Installs independent canvas-specific surface and measurement options after successful configuration.</summary>
    /// <remarks>Callback exceptions preserve the owner. Previous options and retained callback values remain independent of the installed result.</remarks>
    public VisualCanvas ConfigureStyle(Action<VisualCanvasStyleOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Style = ChartOptionConfiguration.Configure(Style, configure, options => options.Clone()); return this;
    }

    internal ResolvedVisualCanvasStyle ResolveStyle() => ResolveStyle(Theme, ThemeMode, Style);

    internal static ResolvedVisualCanvasStyle ResolveStyle(VisualTheme theme, VisualThemeMode mode, VisualCanvasStyleOptions? style = null) {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        var colors = theme.Resolve(mode);
        style ??= new VisualCanvasStyleOptions();
        return new ResolvedVisualCanvasStyle {
            FontFamily = theme.Typography.Family, MonospaceFontFamily = theme.Typography.MonospaceFamily,
            TextMeasurementMode = style.TextMeasurementMode,
            Accent = colors.Accent, SecondaryAccent = colors.SecondaryAccent,
            HeroTitleColor = colors.Foreground, HeroTitleAccentColor = colors.Accent,
            SubtitleColor = colors.MutedForeground, TileLabelColor = style.TileLabelColor ?? colors.MutedForeground,
            TileValueColor = style.TileValueColor ?? colors.Foreground, TileDetailColor = style.TileDetailColor ?? colors.MutedForeground,
            HeroBadgeTextColor = style.HeroBadgeTextColor ?? colors.Foreground, FeatureLabelColor = colors.Foreground,
            TileGlassTop = style.TileGlassTop ?? colors.ElevatedSurface.WithOpacity(.96),
            TileGlassBottom = style.TileGlassBottom ?? colors.Surface.WithOpacity(.94),
            TileInnerStroke = style.TileInnerStroke ?? colors.Border.WithOpacity(.35),
            TileProgressTrackColor = style.TileProgressTrackColor ?? colors.Border.WithOpacity(.52),
            TileMiniChartFillColor = style.TileMiniChartFillColor ?? colors.Accent.WithOpacity(.18),
            TileMiniChartTrackColor = style.TileMiniChartTrackColor ?? colors.Border.WithOpacity(.30),
            HeroBadgeGlowColor = style.HeroBadgeGlowColor ?? colors.SecondaryAccent.WithOpacity(.12),
            HeroBadgeTop = style.HeroBadgeTop ?? colors.ElevatedSurface,
            HeroBadgeBottom = style.HeroBadgeBottom ?? colors.Surface,
            ImagePlaceholderFill = style.ImagePlaceholderFill ?? colors.Surface.WithOpacity(.68),
            ImagePlaceholderStroke = style.ImagePlaceholderStroke ?? colors.Accent.WithOpacity(.34),
            FeatureDividerColor = style.FeatureDividerColor ?? colors.Border.WithOpacity(.32),
            TechHorizonFill = style.TechHorizonFill ?? colors.Background.WithOpacity(.78)
        };
    }

    private static VisualTheme CreateDefaultTheme() => VisualTheme.Graphite().Configure(builder => {
        builder.Dark.Background = ChartColor.FromHex("#030712");
        builder.Dark.Surface = ChartColor.FromHex("#07182F");
        builder.Dark.ElevatedSurface = ChartColor.FromHex("#0B1C3A");
        builder.Dark.Foreground = ChartColor.FromHex("#F8FAFC");
        builder.Dark.MutedForeground = ChartColor.FromHex("#D8E3F4");
        builder.Dark.Border = ChartColor.FromHex("#8AA9D6");
        builder.Dark.Accent = ChartColor.FromHex("#2F80FF");
        builder.Dark.SecondaryAccent = ChartColor.FromHex("#22A7FF");
        builder.Typography = new VisualTypography("Segoe UI, Arial, sans-serif", monospaceFamily: "Cascadia Mono, Consolas, monospace");
    });
}
