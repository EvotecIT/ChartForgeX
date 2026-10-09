using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Stories;

namespace ChartForgeX.Themes;

/// <summary>Applies shared core design tokens to Stories-owned scene presentation.</summary>
public static class VisualStoryDesignTokenExtensions {
    /// <summary>
    /// Maps shared surfaces, typography and text roles onto an existing story theme. Success uses the
    /// status ink for text; syntax uses the data palette, falling back to primary text when a palette
    /// color has less than 4.5:1 contrast on the panel. Apply caller-specific overrides after this mapping.
    /// </summary>
    public static VisualStoryTheme ApplyTo(this VisualDesignTokens tokens, VisualStoryTheme theme) {
        if (tokens == null) throw new ArgumentNullException(nameof(tokens));
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        theme.Background = tokens.Background;
        theme.Panel = tokens.ElevatedSurface;
        theme.Border = tokens.Border;
        theme.Text = tokens.Foreground;
        theme.Muted = tokens.MutedForeground;
        theme.Accent = tokens.Accent;
        theme.Success = tokens.Status.Pass.Ink;
        theme.FontFamily = tokens.FontFamily;
        theme.MonospaceFontFamily = tokens.MonospaceFontFamily;
        var palette = tokens.Palette;
        var panel = ChartColorMath.CompositeOverOpaque(theme.Panel, theme.Background);
        ChartColor SyntaxInk(ChartColor color) => ChartColorMath.ContrastRatio(
            ChartColorMath.CompositeOverOpaque(color, panel), panel) >= 4.5 ? color : theme.Text;
        ChartColor SeriesInk(int index) => SyntaxInk(palette[index % palette.Length]);
        theme.Syntax = new StorySyntaxPalette {
            Plain = theme.Text,
            Keyword = SeriesInk(2),
            Type = SeriesInk(1),
            Command = SyntaxInk(theme.Accent),
            Parameter = SeriesInk(0),
            Variable = SeriesInk(0),
            Property = SeriesInk(3),
            String = SeriesInk(4),
            Number = SeriesInk(5),
            Comment = theme.Muted,
            Operator = theme.Text,
            Punctuation = theme.Muted
        };
        return theme;
    }
}
