using System;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.Themes;

/// <summary>
/// Defines product-neutral brand, semantic color, typography, and shape tokens that can be applied across ChartForgeX visual surfaces.
/// </summary>
public sealed class VisualDesignTokens {
    private ChartColor[] _palette = ChartPalettes.Report;
    private string _fontFamily = ChartFontStacks.SystemSans;
    private string _monospaceFontFamily = "Cascadia Mono, Consolas, monospace";
    private double _cornerRadius = 12;
    private double _strokeWidth = 2;

    /// <summary>Gets or sets the page or canvas background color.</summary>
    public ChartColor Background { get; set; } = ChartColor.FromHex("#FFFFFF");
    /// <summary>Gets or sets the recessed surface color.</summary>
    public ChartColor Surface { get; set; } = ChartColor.FromHex("#F8FAFC");
    /// <summary>Gets or sets the elevated card surface color.</summary>
    public ChartColor ElevatedSurface { get; set; } = ChartColor.FromHex("#FFFFFF");
    /// <summary>Gets or sets the primary foreground color.</summary>
    public ChartColor Foreground { get; set; } = ChartColor.FromHex("#0F172A");
    /// <summary>Gets or sets the secondary foreground color.</summary>
    public ChartColor MutedForeground { get; set; } = ChartColor.FromHex("#475569");
    /// <summary>Gets or sets the guide and border color.</summary>
    public ChartColor Border { get; set; } = ChartColor.FromHex("#CBD5E1");
    /// <summary>Gets or sets the primary accent color.</summary>
    public ChartColor Accent { get; set; } = ChartColor.FromHex("#2563EB");
    /// <summary>Gets or sets the secondary accent color.</summary>
    public ChartColor SecondaryAccent { get; set; } = ChartColor.FromHex("#06B6D4");
    /// <summary>Gets or sets the positive semantic color.</summary>
    public ChartColor Positive { get; set; } = ChartColor.FromHex("#16A34A");
    /// <summary>Gets or sets the warning semantic color.</summary>
    public ChartColor Warning { get; set; } = ChartColor.FromHex("#F59E0B");
    /// <summary>Gets or sets the negative semantic color.</summary>
    public ChartColor Negative { get; set; } = ChartColor.FromHex("#EF4444");
    /// <summary>Gets or sets the disabled semantic color.</summary>
    public ChartColor Disabled { get; set; } = ChartColor.FromHex("#94A3B8");

    /// <summary>Gets or sets the qualitative data palette.</summary>
    public ChartColor[] Palette {
        get => (ChartColor[])_palette.Clone();
        set {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length == 0) throw new ArgumentException("Palette must contain at least one color.", nameof(value));
            _palette = (ChartColor[])value.Clone();
        }
    }

    /// <summary>Gets or sets the primary CSS-compatible font family stack.</summary>
    public string FontFamily {
        get => _fontFamily;
        set => _fontFamily = RequiredText(value, nameof(value));
    }

    /// <summary>Gets or sets the CSS-compatible monospace font family stack.</summary>
    public string MonospaceFontFamily {
        get => _monospaceFontFamily;
        set => _monospaceFontFamily = RequiredText(value, nameof(value));
    }

    /// <summary>Gets or sets the base corner radius.</summary>
    public double CornerRadius {
        get => _cornerRadius;
        set => _cornerRadius = NonNegativeFinite(value, nameof(value));
    }

    /// <summary>Gets or sets the base data-mark stroke width.</summary>
    public double StrokeWidth {
        get => _strokeWidth;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Stroke width must be finite and greater than zero.");
            _strokeWidth = value;
        }
    }

    /// <summary>Creates an independent copy.</summary>
    public VisualDesignTokens Clone() => new() {
        Background = Background,
        Surface = Surface,
        ElevatedSurface = ElevatedSurface,
        Foreground = Foreground,
        MutedForeground = MutedForeground,
        Border = Border,
        Accent = Accent,
        SecondaryAccent = SecondaryAccent,
        Positive = Positive,
        Warning = Warning,
        Negative = Negative,
        Disabled = Disabled,
        Palette = Palette,
        FontFamily = FontFamily,
        MonospaceFontFamily = MonospaceFontFamily,
        CornerRadius = CornerRadius,
        StrokeWidth = StrokeWidth
    };

    /// <summary>Applies the shared tokens to a chart renderer theme.</summary>
    public ChartTheme ApplyTo(ChartTheme theme) {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        theme.Background = Background;
        theme.CardBackground = ElevatedSurface;
        theme.PlotBackground = Surface;
        theme.CardBorder = Border;
        theme.PlotBorder = Border.WithOpacity(0.72);
        theme.Text = Foreground;
        theme.MutedText = MutedForeground;
        theme.Grid = Border.WithOpacity(0.55);
        theme.Axis = MutedForeground.WithOpacity(0.75);
        theme.Positive = Positive;
        theme.Warning = Warning;
        theme.Negative = Negative;
        theme.Palette = Palette;
        theme.FontFamily = FontFamily;
        theme.CornerRadius = CornerRadius;
        theme.PlotCornerRadius = Math.Max(0, CornerRadius * 0.72);
        theme.StrokeWidth = StrokeWidth;
        return theme;
    }

    /// <summary>Applies the shared tokens to a visual-canvas renderer theme.</summary>
    public VisualCanvasTheme ApplyTo(VisualCanvasTheme theme) {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        theme.Accent = Accent;
        theme.SecondaryAccent = SecondaryAccent;
        theme.HeroTitleColor = Foreground;
        theme.HeroTitleAccentColor = Accent;
        theme.SubtitleColor = MutedForeground;
        theme.TileGlassTop = ElevatedSurface.WithOpacity(0.96);
        theme.TileGlassBottom = Surface.WithOpacity(0.94);
        theme.TileInnerStroke = Border.WithOpacity(0.35);
        theme.TileLabelColor = MutedForeground;
        theme.TileValueColor = Foreground;
        theme.TileDetailColor = MutedForeground;
        theme.TileProgressTrackColor = Border.WithOpacity(0.52);
        theme.TileMiniChartFillColor = Accent.WithOpacity(0.18);
        theme.TileMiniChartTrackColor = Border.WithOpacity(0.30);
        theme.HeroBadgeGlowColor = SecondaryAccent.WithOpacity(0.12);
        theme.HeroBadgeTop = ElevatedSurface;
        theme.HeroBadgeBottom = Surface;
        theme.HeroBadgeTextColor = Foreground;
        theme.ImagePlaceholderFill = Surface.WithOpacity(0.68);
        theme.ImagePlaceholderStroke = Accent.WithOpacity(0.34);
        theme.FeatureDividerColor = Border.WithOpacity(0.32);
        theme.FeatureLabelColor = Foreground;
        theme.FontFamily = FontFamily;
        theme.MonospaceFontFamily = MonospaceFontFamily;
        return theme;
    }

    /// <summary>Applies the shared tokens to a topology renderer theme.</summary>
    public TopologyTheme ApplyTo(TopologyTheme theme) {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        theme.Background = Background.ToCss();
        theme.Foreground = Foreground.ToCss();
        theme.MutedForeground = MutedForeground.ToCss();
        theme.Card = ElevatedSurface.ToCss();
        theme.Surface = Surface.ToCss();
        theme.Border = Border.ToCss();
        theme.Accent = Accent.ToCss();
        theme.Healthy = Positive.ToCss();
        theme.Warning = Warning.ToCss();
        theme.Critical = Negative.ToCss();
        theme.Unknown = MutedForeground.ToCss();
        theme.Disabled = Disabled.ToCss();
        theme.FontFamily = FontFamily;
        return theme;
    }

    /// <summary>Creates a dark token set suitable for reports, dashboards, and wallpapers.</summary>
    public static VisualDesignTokens Dark() => new() {
        Background = ChartColor.FromHex("#0B1120"),
        Surface = ChartColor.FromHex("#111827"),
        ElevatedSurface = ChartColor.FromHex("#172033"),
        Foreground = ChartColor.FromHex("#F8FAFC"),
        MutedForeground = ChartColor.FromHex("#CBD5E1"),
        Border = ChartColor.FromHex("#334155"),
        Accent = ChartColor.FromHex("#60A5FA"),
        SecondaryAccent = ChartColor.FromHex("#22D3EE"),
        Positive = ChartColor.FromHex("#34D399"),
        Warning = ChartColor.FromHex("#FBBF24"),
        Negative = ChartColor.FromHex("#F87171"),
        Palette = ChartPalettes.Report
    };

    private static string RequiredText(string value, string parameterName) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value must not be empty.", parameterName) : value.Trim();
    private static double NonNegativeFinite(double value, string parameterName) => double.IsNaN(value) || double.IsInfinity(value) || value < 0 ? throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and non-negative.") : value;
}
