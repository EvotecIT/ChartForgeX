using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Terminal;

/// <summary>Terminal-specific cursor and title-bar treatments, independent of shared colors and fonts.</summary>
public sealed class TerminalStyleOptions {
    /// <summary>Overrides the title-bar surface.</summary>
    public ChartColor? HeaderBackground { get; set; }
    /// <summary>Overrides the cursor paint.</summary>
    public ChartColor? Cursor { get; set; }
    /// <summary>Creates an independent copy of the local options.</summary>
    public TerminalStyleOptions Clone() => (TerminalStyleOptions)MemberwiseClone();
}

internal sealed partial class TerminalThemeProjection {
    internal static ResolvedTerminalStyle Resolve(VisualTheme theme, VisualThemeMode mode, TerminalStyleOptions? style = null) {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        var colors = theme.Resolve(mode);
        return new ResolvedTerminalStyle {
            PageBackground = colors.Background, Background = colors.ElevatedSurface,
            HeaderBackground = style?.HeaderBackground ?? colors.Surface, Border = colors.Border,
            Text = colors.Foreground, Muted = colors.MutedForeground, Accent = colors.Accent,
            Success = colors.Status.Pass.Ink, Warning = colors.Status.Medium.Ink,
            Error = colors.Status.Critical.Ink, Cursor = style?.Cursor ?? colors.Foreground,
            FontFamily = theme.Typography.MonospaceFamily
        };
    }
}

internal sealed partial class ResolvedTerminalStyle {
    internal static ResolvedTerminalStyle FromTheme(VisualTheme theme, VisualThemeMode mode) => TerminalThemeProjection.Resolve(theme, mode);
}

public sealed partial class TerminalStory {
    /// <summary>Chooses the initial tab's explicit light or dark appearance before timeline authoring.</summary>
    public TerminalStory WithThemeMode(VisualThemeMode mode) {
        EnsureInitialConfiguration(); InitialTab.ThemeMode = mode; return this;
    }
    /// <summary>Sets independent cursor and title-bar options before timeline authoring.</summary>
    public TerminalStory WithStyle(TerminalStyleOptions style) {
        EnsureInitialConfiguration(); InitialTab.Style = (style ?? throw new ArgumentNullException(nameof(style))).Clone(); return this;
    }
}

/// <summary>Authored terminal palette recipes represented by the shared immutable theme type.</summary>
public static class TerminalThemes {
    /// <summary>Creates the restrained terminal palette in both light and dark modes.</summary>
    public static VisualTheme Default() => Pair(ResolvedTerminalStyle.Light(), ResolvedTerminalStyle.Dark());
    /// <summary>Creates a PowerShell-oriented paired palette.</summary>
    public static VisualTheme PowerShell() => Pair(ResolvedTerminalStyle.Light(), ResolvedTerminalStyle.PowerShell());
    /// <summary>Creates the classic blue Windows PowerShell palette with a printable light mode.</summary>
    public static VisualTheme WindowsPowerShell() => Pair(ResolvedTerminalStyle.Light(), ResolvedTerminalStyle.WindowsPowerShell());
    /// <summary>Creates the Ubuntu aubergine palette with a printable light mode.</summary>
    public static VisualTheme Ubuntu() => Pair(ResolvedTerminalStyle.Light(), ResolvedTerminalStyle.Ubuntu());
    /// <summary>Creates the Campbell palette with a printable light mode.</summary>
    public static VisualTheme Campbell() => Pair(ResolvedTerminalStyle.Light(), ResolvedTerminalStyle.Campbell());
    /// <summary>Creates a classic near-black palette with a printable light mode.</summary>
    public static VisualTheme Classic() => Pair(ResolvedTerminalStyle.Light(), ResolvedTerminalStyle.Classic());

    private static VisualTheme Pair(ResolvedTerminalStyle light, ResolvedTerminalStyle dark) =>
        new(Tokens(light), Tokens(dark), new VisualTypography(monospaceFamily: dark.FontFamily));

    private static VisualDesignTokens Tokens(ResolvedTerminalStyle style) {
        var tokens = new VisualDesignTokens {
            Background = style.PageBackground, Surface = style.HeaderBackground, ElevatedSurface = style.Background,
            Border = style.Border, Foreground = style.Text, MutedForeground = style.Muted, Accent = style.Accent
        };
        tokens.Status.Pass = new VisualTokenColor(style.Success, style.Success);
        tokens.Status.Medium = new VisualTokenColor(style.Warning, style.Warning);
        tokens.Status.Critical = new VisualTokenColor(style.Error, style.Error);
        return tokens;
    }
}
