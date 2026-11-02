using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

public sealed partial class VisualTheme {
    /// <summary>Creates a paired light and dark theme for reports and generated HTML.</summary>
    public static VisualTheme Report() => ClassicPreset(
        ClassicPresetColors(VisualThemeMode.Light), ClassicPresetColors(VisualThemeMode.Dark));

    /// <summary>Creates a restrained executive report palette with framed surfaces.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme Executive() => SinglePalettePreset(VisualThemeMode.Light, colors => {
        colors.ElevatedSurface = ChartColor.White;
        colors.Surface = ChartColor.FromHex("#F8FAFC");
        colors.Border = ChartColor.FromRgba(100, 116, 139, 74);
        colors.PlotBorder = ChartColor.FromRgba(148, 163, 184, 60);
        colors.Foreground = ChartColor.FromHex("#0F172A");
        colors.MutedForeground = ChartColor.FromHex("#475569");
        colors.Grid = ChartColor.FromRgba(148, 163, 184, 82);
        colors.Axis = ChartColor.FromRgba(71, 85, 105, 150);
        colors.Palette = ChartPalettes.FromHex("#1E40AF", "#0F766E", "#9333EA", "#B45309", "#BE123C", "#475569", "#0284C7", "#16A34A");
        SetPresetSemantics(colors, ChartColor.FromHex("#059669"), ChartColor.FromHex("#D97706"), ChartColor.FromHex("#DC2626"));
    }).Configure(builder => {
        builder.CardRadius = 8;
        builder.PlotRadius = builder.BarRadius = 6;
        builder.CardShadowOpacity = 0;
    });

    /// <summary>Creates a vivid product dashboard palette with floating surfaces.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme Product() => SinglePalettePreset(VisualThemeMode.Light, colors => {
        colors.ElevatedSurface = ChartColor.White;
        colors.Surface = ChartColor.FromHex("#F0F9FF");
        colors.Border = ChartColor.FromRgba(14, 165, 233, 64);
        colors.PlotBorder = ChartColor.FromRgba(20, 184, 166, 54);
        colors.Foreground = ChartColor.FromHex("#111827");
        colors.MutedForeground = ChartColor.FromHex("#4F46E5");
        colors.Grid = ChartColor.FromRgba(125, 211, 252, 90);
        colors.Axis = ChartColor.FromRgba(79, 70, 229, 140);
        colors.Palette = ChartPalettes.FromHex("#0EA5E9", "#EC4899", "#14B8A6", "#A855F7", "#F59E0B", "#6366F1", "#22C55E", "#F43F5E");
        SetPresetSemantics(colors, ChartColor.FromHex("#14B8A6"), ChartColor.FromHex("#F59E0B"), ChartColor.FromHex("#F43F5E"));
    }).Configure(builder => {
        builder.Typography = ClassicPresetTypography(ChartFontStacks.Geometric);
        builder.CardRadius = 24;
        builder.PlotRadius = builder.BarRadius = 16;
        builder.CardShadowOpacity = 0.24;
    });

    /// <summary>Creates a framed report palette with distinct colors for colorblind readers.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme Accessible() => SinglePalettePreset(VisualThemeMode.Light, colors => {
        colors.ElevatedSurface = ChartColor.White;
        colors.Surface = ChartColor.FromHex("#FAFAF7");
        colors.Border = ChartColor.FromRgba(82, 82, 82, 68);
        colors.PlotBorder = ChartColor.FromRgba(82, 82, 82, 52);
        colors.Foreground = ChartColor.FromHex("#18181B");
        colors.MutedForeground = ChartColor.FromHex("#52525B");
        colors.Grid = ChartColor.FromRgba(113, 113, 122, 76);
        colors.Axis = ChartColor.FromRgba(39, 39, 42, 160);
        colors.Palette = ChartPalettes.Colorblind;
        SetPresetSemantics(colors, ChartColor.FromHex("#009E73"), ChartColor.FromHex("#E69F00"), ChartColor.FromHex("#D55E00"));
    }).Configure(builder => {
        builder.CardRadius = 8;
        builder.PlotRadius = builder.BarRadius = 6;
        builder.CardShadowOpacity = 0;
    });

    /// <summary>Creates a light report palette with distinct colors for colorblind readers.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme Colorblind() => SinglePalettePreset(VisualThemeMode.Light, colors => {
        colors.ElevatedSurface = ChartColor.FromRgb(255, 255, 255);
        colors.Surface = ChartColor.FromRgb(250, 250, 247);
        colors.Border = ChartColor.FromRgba(82, 82, 82, 58);
        colors.PlotBorder = ChartColor.FromRgba(82, 82, 82, 44);
        colors.Foreground = ChartColor.FromRgb(24, 24, 27);
        colors.MutedForeground = ChartColor.FromRgb(82, 82, 91);
        colors.Grid = ChartColor.FromRgba(113, 113, 122, 66);
        colors.Axis = ChartColor.FromRgba(39, 39, 42, 150);
        colors.Palette = ChartPalettes.Colorblind;
        SetPresetSemantics(colors, ChartColor.FromRgb(0, 158, 115), ChartColor.FromRgb(230, 159, 0), ChartColor.FromRgb(213, 94, 0));
    });

    /// <summary>Creates a vibrant dark palette for product and status dashboards.</summary>
    /// <remarks>The authored dark palette is used in both modes.</remarks>
    public static VisualTheme Aurora() => SinglePalettePreset(VisualThemeMode.Dark, colors => {
        colors.ElevatedSurface = ChartColor.FromRgb(11, 18, 32);
        colors.Surface = ChartColor.FromRgb(7, 12, 24);
        colors.Border = ChartColor.FromRgba(125, 211, 252, 38);
        colors.PlotBorder = ChartColor.FromRgba(45, 212, 191, 34);
        colors.Foreground = ChartColor.FromRgb(240, 249, 255);
        colors.MutedForeground = ChartColor.FromRgb(186, 230, 253);
        colors.Grid = ChartColor.FromRgba(56, 189, 248, 42);
        colors.Axis = ChartColor.FromRgba(165, 243, 252, 120);
        colors.Palette = ChartPalettes.Vivid;
        SetPresetSemantics(colors, ChartColor.FromRgb(45, 212, 191), ChartColor.FromRgb(250, 204, 21), ChartColor.FromRgb(251, 113, 133));
    }).Configure(builder => {
        builder.Typography = ClassicPresetTypography(ChartFontStacks.Geometric);
        builder.CardShadowOpacity = 0.24;
        builder.SeriesStrokeWidth = 3.4;
        builder.MarkerRadius = 3.6;
    });

    /// <summary>Creates a warm publication palette with serif typography.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme Editorial() => SinglePalettePreset(VisualThemeMode.Light, colors => {
        colors.ElevatedSurface = ChartColor.FromRgb(255, 252, 247);
        colors.Surface = ChartColor.FromRgb(250, 246, 238);
        colors.Border = ChartColor.FromRgba(120, 113, 108, 72);
        colors.PlotBorder = ChartColor.FromRgba(168, 162, 158, 58);
        colors.Foreground = ChartColor.FromRgb(28, 25, 23);
        colors.MutedForeground = ChartColor.FromRgb(87, 83, 78);
        colors.Grid = ChartColor.FromRgba(168, 162, 158, 74);
        colors.Axis = ChartColor.FromRgba(68, 64, 60, 140);
        colors.Palette = ChartPalettes.Editorial;
        SetPresetSemantics(colors, ChartColor.FromRgb(22, 101, 52), ChartColor.FromRgb(180, 83, 9), ChartColor.FromRgb(185, 28, 28));
    }).Configure(builder => {
        builder.Typography = ClassicPresetTypography(ChartFontStacks.Serif);
        builder.CardRadius = 8;
        builder.PlotRadius = builder.BarRadius = 6;
        builder.CardShadowOpacity = 0.08;
    });

    /// <summary>Creates a soft light palette with rounded typography and surfaces.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme Candy() => SinglePalettePreset(VisualThemeMode.Light, colors => {
        colors.ElevatedSurface = ChartColor.FromRgb(255, 255, 255);
        colors.Surface = ChartColor.FromRgb(253, 246, 255);
        colors.Border = ChartColor.FromRgba(217, 70, 239, 46);
        colors.PlotBorder = ChartColor.FromRgba(14, 165, 233, 46);
        colors.Foreground = ChartColor.FromRgb(49, 46, 129);
        colors.MutedForeground = ChartColor.FromRgb(99, 102, 241);
        colors.Grid = ChartColor.FromRgba(125, 211, 252, 82);
        colors.Axis = ChartColor.FromRgba(79, 70, 229, 130);
        colors.Palette = ChartPalettes.Pastel;
        SetPresetSemantics(colors, ChartColor.FromRgb(20, 184, 166), ChartColor.FromRgb(245, 158, 11), ChartColor.FromRgb(244, 63, 94));
    }).Configure(builder => {
        builder.Typography = ClassicPresetTypography(ChartFontStacks.Rounded);
        builder.CardRadius = 24;
        builder.PlotRadius = builder.BarRadius = 18;
        builder.CardShadowOpacity = 0.11;
    });

    /// <summary>Creates a cyan and magenta palette for demographic and survey graphics.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme PeopleInfographic() => SinglePalettePreset(VisualThemeMode.Light, colors => {
        colors.ElevatedSurface = ChartColor.White;
        colors.Surface = ChartColor.FromHex("#F8FAFC");
        colors.Border = ChartColor.FromRgba(15, 23, 42, 44);
        colors.PlotBorder = ChartColor.FromRgba(100, 116, 139, 46);
        colors.Foreground = ChartColor.FromHex("#0F172A");
        colors.MutedForeground = ChartColor.FromHex("#475569");
        colors.Grid = ChartColor.FromRgba(148, 163, 184, 86);
        colors.Axis = ChartColor.FromRgba(51, 65, 85, 150);
        colors.Palette = ChartPalettes.PeopleInfographic;
        SetPresetSemantics(colors, ChartColor.FromHex("#06B6D4"), ChartColor.FromHex("#FB923C"), ChartColor.FromHex("#DB2777"));
    }).Configure(builder => {
        builder.Typography = ClassicPresetTypography(ChartFontStacks.Geometric, 24, 12, 11, 10, 11, 10);
        builder.CardRadius = 8;
        builder.PlotRadius = builder.BarRadius = 6;
        builder.CardShadowOpacity = 0;
        builder.SeriesStrokeWidth = 2.7;
        builder.MarkerRadius = 3.1;
    });

    /// <summary>Creates a compact green technical palette with monospace typography.</summary>
    /// <remarks>The authored dark palette is used in both modes.</remarks>
    public static VisualTheme Terminal() => SinglePalettePreset(VisualThemeMode.Dark, colors => {
        colors.ElevatedSurface = ChartColor.FromRgb(6, 10, 16);
        colors.Surface = ChartColor.FromRgb(2, 6, 12);
        colors.Border = ChartColor.FromRgba(34, 197, 94, 48);
        colors.PlotBorder = ChartColor.FromRgba(34, 197, 94, 40);
        colors.Foreground = ChartColor.FromRgb(220, 252, 231);
        colors.MutedForeground = ChartColor.FromRgb(134, 239, 172);
        colors.Grid = ChartColor.FromRgba(34, 197, 94, 38);
        colors.Axis = ChartColor.FromRgba(187, 247, 208, 105);
        colors.Palette = ChartPalettes.Terminal;
        SetPresetSemantics(colors, ChartColor.FromRgb(74, 222, 128), ChartColor.FromRgb(250, 204, 21), ChartColor.FromRgb(248, 113, 113));
    }).Configure(builder => {
        builder.Typography = ClassicPresetTypography(ChartFontStacks.Mono, 23, 12, 11, 10, 11, 10);
        builder.CardRadius = 10;
        builder.PlotRadius = builder.BarRadius = 6;
        builder.CardShadowOpacity = 0.2;
    });

    /// <summary>Creates a transparent dark overlay palette for imagery and wallpapers.</summary>
    /// <remarks>The authored dark palette is used in both modes.</remarks>
    public static VisualTheme TransparentOverlay() => SinglePalettePreset(VisualThemeMode.Dark, colors => {
        colors.Background = ChartColors.Transparent;
        colors.ElevatedSurface = ChartColors.Slate950.WithAlpha(172);
        colors.Surface = ChartColors.Transparent;
        colors.Border = ChartColors.Slate400.WithAlpha(64);
        colors.PlotBorder = ChartColors.Slate400.WithAlpha(34);
        colors.Foreground = ChartColors.White;
        colors.MutedForeground = ChartColors.Slate200.WithAlpha(230);
        colors.Grid = ChartColors.Slate400.WithAlpha(42);
        colors.Axis = ChartColors.Slate200.WithAlpha(110);
        colors.Palette = ChartPalettes.CommandCenter;
        SetPresetSemantics(colors, ChartColors.Emerald400, ChartColors.Orange400, ChartColors.Red400);
    }).Configure(builder => {
        builder.CardRadius = 8;
        builder.PlotRadius = builder.BarRadius = 4;
        builder.CardShadowOpacity = 0;
        builder.SeriesStrokeWidth = 2.7;
        builder.MarkerRadius = 3;
    });

    /// <summary>Creates a crisp light palette for dense business dashboards.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme Minimal() => SinglePalettePreset(VisualThemeMode.Light, colors => {
        colors.ElevatedSurface = ChartColor.FromRgb(255, 255, 255);
        colors.Surface = ChartColor.FromRgb(255, 255, 255);
        colors.Border = ChartColor.FromRgba(15, 23, 42, 34);
        colors.PlotBorder = ChartColor.FromRgba(15, 23, 42, 28);
        colors.Grid = ChartColor.FromRgba(148, 163, 184, 50);
        colors.Axis = ChartColor.FromRgba(51, 65, 85, 120);
        colors.Palette = ChartPalettes.Jewel;
    }).Configure(builder => {
        builder.Typography = ClassicPresetTypography(ChartFontStacks.Humanist);
        builder.CardRadius = 6;
        builder.PlotRadius = builder.BarRadius = 4;
        builder.CardShadowOpacity = 0;
        builder.SeriesStrokeWidth = 2.4;
        builder.MarkerRadius = 2.8;
    });

    /// <summary>Creates a bright operational dashboard palette for KPIs and utilization panels.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme Dashboard() => DashboardPreset(colors => ConfigureDashboardPalette(colors,
        "#F2F5EC", "#FFFFFF", "#F6F8FB", ChartColor.FromHex("#DDE3D1"), ChartColor.FromHex("#DDE3D1"),
        ChartColor.FromHex("#DDE3D1"), "#BFC8B2", "#27C26A", "#F6C445", "#FF3B4F",
        "#DDFB20", "#27C26A", "#356AF4", "#FF3B4F", "#667085"), 14, 0.05);

    /// <summary>Creates a light dashboard palette for revenue trends, targets and driver cards.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme SaasDashboard() => DashboardPreset(colors => ConfigureDashboardPalette(colors,
        "#F3F6FA", "#FCFDFE", "#F6F8FC", ChartColor.FromRgba(199, 210, 226, 46), ChartColor.FromRgba(210, 218, 232, 52),
        ChartColor.FromRgba(214, 222, 234, 142), "#A6B0C0", "#27C26A", "#F6C445", "#FF3B4F",
        "#356AF4", "#667085", "#27C26A", "#FF3B4F"), 12, 0.09, 4.2);

    /// <summary>Creates a soft light dashboard palette for restaurant operations panels.</summary>
    /// <remarks>The authored light palette is used in both modes.</remarks>
    public static VisualTheme RestaurantDashboard() => DashboardPreset(colors => {
        ConfigureDashboardPalette(colors, "#EFEFEF", "#FBFBFA", "#F5F5F3", ChartColor.FromRgba(207, 207, 202, 70),
            ChartColor.FromRgba(219, 219, 214, 92), ChartColor.FromRgba(222, 222, 218, 138), "#A4A29E", "#2CA36B",
            "#D0A400", "#E66A1F", "#C46A23", "#EF7F22", "#FFB074", "#A38300", "#545454");
        colors.Foreground = ChartColor.FromHex("#161616");
        colors.MutedForeground = ChartColor.FromHex("#777776");
    }, 14, 0.05, 4);

    private static VisualTheme SinglePalettePreset(VisualThemeMode mode, Action<VisualDesignTokens> configure) {
        var colors = ClassicPresetColors(mode);
        configure(colors);
        colors.Accent = colors.Palette[0];
        return ClassicPreset(colors, colors);
    }

    private static VisualTheme ClassicPreset(VisualDesignTokens light, VisualDesignTokens dark) => new(light, dark,
        ClassicPresetTypography(), seriesStrokeWidth: 3, markerRadius: 3.25, barRadius: 8,
        cardRadius: 12, cardShadowOpacity: 0.10, cardShadowColor: ChartColor.FromRgb(15, 23, 42), plotRadius: 8);

    private static VisualTheme DashboardPreset(Action<VisualDesignTokens> configure,
        double plotRadius, double shadowOpacity, double markerRadius = 2.8) => SinglePalettePreset(VisualThemeMode.Light, configure)
        .Configure(builder => {
            // Design rule 7: ticks and legends share one secondary size, well below the titles.
            builder.Typography = ClassicPresetTypography(ChartFontStacks.Humanist, 26, 14, 13, 14, 14, 16);
            builder.CardRadius = 24;
            builder.PlotRadius = builder.BarRadius = plotRadius;
            builder.CardShadowOpacity = shadowOpacity;
            builder.SeriesStrokeWidth = 2.4;
            builder.MarkerRadius = markerRadius;
        });

    private static VisualTypography ClassicPresetTypography(string family = ChartFontStacks.SystemSans,
        double title = 25, double subtitle = 12.5, double axisTitle = 11.5, double axis = 10.5,
        double legend = 11.5, double dataLabel = 10.5) => new(family, title, subtitle, axis, legend, dataLabel,
            axisTitleSize: axisTitle);

    private static VisualDesignTokens ClassicPresetColors(VisualThemeMode mode) {
        var colors = Graphite().Resolve(mode).ToTokens();
        var dark = mode == VisualThemeMode.Dark;
        colors.Background = ChartColor.Transparent;
        colors.ElevatedSurface = dark ? ChartColor.FromRgb(16, 24, 39) : ChartColor.FromRgb(255, 255, 255);
        colors.Surface = dark ? ChartColor.FromRgb(8, 13, 24) : ChartColor.FromRgb(248, 250, 252);
        colors.Border = dark ? ChartColor.FromRgba(148, 163, 184, 34) : ChartColor.FromRgba(148, 163, 184, 92);
        colors.PlotBorder = dark ? ChartColor.FromRgba(148, 163, 184, 28) : ChartColor.FromRgba(148, 163, 184, 48);
        colors.Foreground = dark ? ChartColor.FromRgb(248, 250, 252) : ChartColor.FromRgb(15, 23, 42);
        colors.MutedForeground = dark ? ChartColor.FromRgb(186, 198, 214) : ChartColor.FromRgb(100, 116, 139);
        colors.Grid = dark ? ChartColor.FromRgba(148, 163, 184, 45) : ChartColor.FromRgba(148, 163, 184, 70);
        colors.Axis = dark ? ChartColor.FromRgba(203, 213, 225, 105) : ChartColor.FromRgba(71, 85, 105, 160);
        colors.Palette = dark ? ChartPalettes.FromHex("#60A5FA", "#22D3EE", "#34D399", "#FBBF24", "#F87171", "#A78BFA", "#F472B6", "#2DD4BF") : ChartPalettes.Report;
        colors.Accent = colors.Palette[0];
        colors.SequentialRamp = null;
        SetPresetSemantics(colors, dark ? ChartColor.FromRgb(52, 211, 153) : ChartColor.FromRgb(16, 185, 129),
            dark ? ChartColor.FromRgb(251, 191, 36) : ChartColor.FromRgb(245, 158, 11),
            dark ? ChartColor.FromRgb(248, 113, 113) : ChartColor.FromRgb(239, 68, 68));
        return colors;
    }

    private static void SetPresetSemantics(VisualDesignTokens colors, ChartColor positive, ChartColor warning, ChartColor negative) {
        colors.Positive = positive;
        colors.Warning = warning;
        colors.Negative = negative;
        colors.Status.Pass = PresetStatusPair(positive, colors.Status.Pass);
        colors.Status.Medium = PresetStatusPair(warning, colors.Status.Medium);
        colors.Status.Critical = PresetStatusPair(negative, colors.Status.Critical);
    }

    private static VisualTokenColor PresetStatusPair(ChartColor fill, VisualTokenColor canonical) => fill.Equals(canonical.Fill)
        ? canonical : new VisualTokenColor(fill, ChartColorMath.AccessibleTextOnBackground(fill));

    private static void ConfigureDashboardPalette(VisualDesignTokens colors, string background, string card, string plot,
        ChartColor border, ChartColor plotBorder, ChartColor grid, string axis, string positive, string warning,
        string negative, params string[] palette) {
        colors.Background = ChartColor.FromHex(background);
        colors.ElevatedSurface = ChartColor.FromHex(card);
        colors.Surface = ChartColor.FromHex(plot);
        colors.Border = border;
        colors.PlotBorder = plotBorder;
        colors.Foreground = ChartColor.FromHex("#1D2430");
        colors.MutedForeground = ChartColor.FromHex("#8B93A3");
        colors.Grid = grid;
        colors.Axis = ChartColor.FromHex(axis);
        colors.Palette = ChartPalettes.FromHex(palette);
        colors.Accent = colors.Palette[0];
        SetPresetSemantics(colors, ChartColor.FromHex(positive), ChartColor.FromHex(warning), ChartColor.FromHex(negative));
    }
}
