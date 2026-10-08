using System;
using ChartForgeX.Composition;

namespace ChartForgeX.Themes;

/// <summary>Applies shared core design tokens to Visuals-owned canvas presentation.</summary>
public static class VisualCanvasDesignTokenExtensions {
    /// <summary>Maps shared colors and typography onto an existing canvas theme.</summary>
    public static VisualCanvasTheme ApplyTo(this VisualDesignTokens tokens, VisualCanvasTheme theme) {
        if (tokens == null) throw new ArgumentNullException(nameof(tokens));
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        theme.Accent = tokens.Accent;
        theme.SecondaryAccent = tokens.SecondaryAccent;
        theme.HeroTitleColor = tokens.Foreground;
        theme.HeroTitleAccentColor = tokens.Accent;
        theme.SubtitleColor = tokens.MutedForeground;
        theme.TileGlassTop = tokens.ElevatedSurface.WithOpacity(0.96);
        theme.TileGlassBottom = tokens.Surface.WithOpacity(0.94);
        theme.TileInnerStroke = tokens.Border.WithOpacity(0.35);
        theme.TileLabelColor = tokens.MutedForeground;
        theme.TileValueColor = tokens.Foreground;
        theme.TileDetailColor = tokens.MutedForeground;
        theme.TileProgressTrackColor = tokens.Border.WithOpacity(0.52);
        theme.TileMiniChartFillColor = tokens.Accent.WithOpacity(0.18);
        theme.TileMiniChartTrackColor = tokens.Border.WithOpacity(0.30);
        theme.HeroBadgeGlowColor = tokens.SecondaryAccent.WithOpacity(0.12);
        theme.HeroBadgeTop = tokens.ElevatedSurface;
        theme.HeroBadgeBottom = tokens.Surface;
        theme.HeroBadgeTextColor = tokens.Foreground;
        theme.ImagePlaceholderFill = tokens.Surface.WithOpacity(0.68);
        theme.ImagePlaceholderStroke = tokens.Accent.WithOpacity(0.34);
        theme.FeatureDividerColor = tokens.Border.WithOpacity(0.32);
        theme.FeatureLabelColor = tokens.Foreground;
        theme.FontFamily = tokens.FontFamily;
        theme.MonospaceFontFamily = tokens.MonospaceFontFamily;
        return theme;
    }
}
