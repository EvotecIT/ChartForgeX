using System;
using ChartForgeX.Terminal;

namespace ChartForgeX.Themes;

/// <summary>Applies shared core design tokens to Stories-owned terminal presentation.</summary>
public static class TerminalDesignTokenExtensions {
    /// <summary>
    /// Maps shared surfaces, monospace typography and output tones onto an existing terminal theme.
    /// Semantic output uses status inks suitable for text on the content surface, rather than mark fills.
    /// Apply caller-specific overrides after this mapping.
    /// </summary>
    public static TerminalTheme ApplyTo(this VisualDesignTokens tokens, TerminalTheme theme) {
        if (tokens == null) throw new ArgumentNullException(nameof(tokens));
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        theme.PageBackground = tokens.Background;
        theme.Background = tokens.ElevatedSurface;
        theme.HeaderBackground = tokens.Surface;
        theme.Border = tokens.Border;
        theme.Text = tokens.Foreground;
        theme.Muted = tokens.MutedForeground;
        theme.Accent = tokens.Accent;
        theme.Success = tokens.Status.Pass.Ink;
        theme.Warning = tokens.Status.Medium.Ink;
        theme.Error = tokens.Status.Critical.Ink;
        theme.Cursor = tokens.Foreground;
        theme.FontFamily = tokens.MonospaceFontFamily;
        return theme;
    }
}
