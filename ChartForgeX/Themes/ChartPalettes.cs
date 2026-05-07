using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>
/// Provides reusable qualitative color palettes for chart series.
/// </summary>
public static class ChartPalettes {
    /// <summary>
    /// Creates a palette from named color tokens or hexadecimal color strings.
    /// </summary>
    /// <param name="colors">The color strings as ChartForgeX color names, #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.</param>
    /// <returns>A chart color palette.</returns>
    public static ChartColor[] FromHex(params string[] colors) {
        if (colors == null) throw new System.ArgumentNullException(nameof(colors));
        if (colors.Length == 0) throw new System.ArgumentException("Palette must contain at least one color.", nameof(colors));
        var palette = new ChartColor[colors.Length];
        for (var i = 0; i < colors.Length; i++) palette[i] = ChartColor.Parse(colors[i]);
        return palette;
    }

    /// <summary>
    /// Gets the default report palette.
    /// </summary>
    public static ChartColor[] Report => new[] {
        ChartColor.FromRgb(37,99,235), ChartColor.FromRgb(14,165,233), ChartColor.FromRgb(16,185,129),
        ChartColor.FromRgb(245,158,11), ChartColor.FromRgb(239,68,68), ChartColor.FromRgb(139,92,246),
        ChartColor.FromRgb(236,72,153), ChartColor.FromRgb(20,184,166)
    };

    /// <summary>
    /// Gets a colorblind-friendly high-contrast palette.
    /// </summary>
    public static ChartColor[] Colorblind => new[] {
        ChartColor.FromRgb(0,114,178), ChartColor.FromRgb(230,159,0), ChartColor.FromRgb(0,158,115),
        ChartColor.FromRgb(204,121,167), ChartColor.FromRgb(86,180,233), ChartColor.FromRgb(213,94,0),
        ChartColor.FromRgb(240,228,66), ChartColor.FromRgb(0,0,0)
    };

    /// <summary>
    /// Gets a vivid palette for high-impact dashboards.
    /// </summary>
    public static ChartColor[] Vivid => new[] {
        ChartColor.FromRgb(34,211,238), ChartColor.FromRgb(168,85,247), ChartColor.FromRgb(244,114,182),
        ChartColor.FromRgb(45,212,191), ChartColor.FromRgb(250,204,21), ChartColor.FromRgb(96,165,250),
        ChartColor.FromRgb(251,113,133), ChartColor.FromRgb(129,140,248)
    };

    /// <summary>
    /// Gets a soft palette for playful or education-oriented reports.
    /// </summary>
    public static ChartColor[] Pastel => new[] {
        ChartColor.FromRgb(96,165,250), ChartColor.FromRgb(244,114,182), ChartColor.FromRgb(94,234,212),
        ChartColor.FromRgb(253,186,116), ChartColor.FromRgb(196,181,253), ChartColor.FromRgb(125,211,252),
        ChartColor.FromRgb(251,113,133), ChartColor.FromRgb(134,239,172)
    };

    /// <summary>
    /// Gets a bright cyan and magenta palette for demographic infographic dashboards.
    /// </summary>
    public static ChartColor[] PeopleInfographic => new[] {
        ChartColor.FromRgb(6,182,212), ChartColor.FromRgb(219,39,119), ChartColor.FromRgb(45,212,191),
        ChartColor.FromRgb(124,58,237), ChartColor.FromRgb(14,165,233), ChartColor.FromRgb(244,114,182),
        ChartColor.FromRgb(251,146,60), ChartColor.FromRgb(71,85,105)
    };

    /// <summary>
    /// Gets a refined publication palette for editorial output.
    /// </summary>
    public static ChartColor[] Editorial => new[] {
        ChartColor.FromRgb(30,64,175), ChartColor.FromRgb(190,18,60), ChartColor.FromRgb(5,150,105),
        ChartColor.FromRgb(146,64,14), ChartColor.FromRgb(88,28,135), ChartColor.FromRgb(15,118,110),
        ChartColor.FromRgb(127,29,29), ChartColor.FromRgb(51,65,85)
    };

    /// <summary>
    /// Gets a jewel-tone palette for rich static report visuals.
    /// </summary>
    public static ChartColor[] Jewel => new[] {
        ChartColor.FromRgb(37,99,235), ChartColor.FromRgb(5,150,105), ChartColor.FromRgb(190,18,60),
        ChartColor.FromRgb(124,58,237), ChartColor.FromRgb(217,119,6), ChartColor.FromRgb(8,145,178),
        ChartColor.FromRgb(71,85,105), ChartColor.FromRgb(219,39,119)
    };

    /// <summary>
    /// Gets a terminal-inspired technical palette.
    /// </summary>
    public static ChartColor[] Terminal => new[] {
        ChartColor.FromRgb(34,197,94), ChartColor.FromRgb(56,189,248), ChartColor.FromRgb(250,204,21),
        ChartColor.FromRgb(192,132,252), ChartColor.FromRgb(248,113,113), ChartColor.FromRgb(45,212,191),
        ChartColor.FromRgb(251,146,60), ChartColor.FromRgb(163,230,53)
    };

    /// <summary>
    /// Gets a bright overlay palette for transparent operational dashboards and wallpapers.
    /// </summary>
    public static ChartColor[] CommandCenter => new[] {
        ChartColors.Blue400, ChartColors.Cyan400, ChartColors.Emerald400, ChartColors.Orange400,
        ChartColors.Red400, ChartColors.Violet400, ChartColors.Pink400, ChartColors.Teal400
    };
}
