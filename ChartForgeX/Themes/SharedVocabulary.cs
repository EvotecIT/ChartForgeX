using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>Resolves the shared <see cref="ThemeMode"/> for output that cannot follow the host.</summary>
internal static class ThemeModes {
    /// <summary>Static output has no host preference, so <see cref="ThemeMode.Auto"/> paints as <see cref="ThemeMode.Light"/>.</summary>
    internal static ThemeMode Resolve(ThemeMode mode) => mode == ThemeMode.Dark ? ThemeMode.Dark : ThemeMode.Light;
}

/// <summary>Resolves a theme-defined <see cref="Tone"/> to one of the theme's colour roles.</summary>
internal static class ToneRoles {
    /// <summary>
    /// Finds the <see cref="VisualThemeColorToken"/> a named tone refers to, ignoring case, hyphens, underscores and dots
    /// (<c>accent</c>, <c>maintenance</c>, <c>muted-foreground</c>). Built-in tones and names the theme does not define return false.
    /// </summary>
    internal static bool TryResolveNamed(Tone tone, out VisualThemeColorToken token) {
        token = default;
        if (!tone.IsNamed) return false;
        var key = tone.Name.Replace("-", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty);
        // Names start with a letter, so a numeric name never selects a role by its underlying value.
        return key.Length > 0 && char.IsLetter(key[0]) && Enum.TryParse(key, ignoreCase: true, out token) && Enum.IsDefined(typeof(VisualThemeColorToken), token);
    }

    /// <summary>Finds the colour of the theme role a named tone refers to.</summary>
    internal static bool TryResolveNamed(VisualThemeColors colors, Tone tone, out VisualThemeColorToken token, out ChartColor color) {
        color = default;
        if (!TryResolveNamed(tone, out token)) return false;
        color = Color(colors, token);
        return true;
    }

    internal static ChartColor Color(VisualThemeColors colors, VisualThemeColorToken token) => token switch {
        VisualThemeColorToken.Background => colors.Background,
        VisualThemeColorToken.Surface => colors.Surface,
        VisualThemeColorToken.ElevatedSurface => colors.ElevatedSurface,
        VisualThemeColorToken.Foreground => colors.Foreground,
        VisualThemeColorToken.MutedForeground => colors.MutedForeground,
        VisualThemeColorToken.Border => colors.Border,
        VisualThemeColorToken.Accent => colors.Accent,
        VisualThemeColorToken.Positive => colors.Positive,
        VisualThemeColorToken.Warning => colors.Warning,
        VisualThemeColorToken.Negative => colors.Negative,
        VisualThemeColorToken.Maintenance => colors.Status.Maintenance.Fill,
        VisualThemeColorToken.Info => colors.Info,
        _ => colors.Neutral
    };
}
