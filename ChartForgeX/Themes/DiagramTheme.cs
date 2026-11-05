using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

/// <summary>Owns diagram presentation defaults while retaining the active theme's font request and color tokens.</summary>
internal static class DiagramTheme {
    internal const double CardCornerRadius = 6;
    /// <summary>Wrapping limit for card descriptions; shorter cards keep their natural width.</summary>
    internal const double CardTextMaxWidth = 190;
    internal const double GanttProgressTint = .18;
    /// <summary>Connector weight: relationship lines read as one step heavier than 1 px outlines, never as data series.</summary>
    internal const double ConnectorStrokeWidth = 1.5;
    /// <summary>The thinnest a default connector paints when a fitted diagram scales down.</summary>
    internal const double MinimumConnectorStrokeWidth = .75;

    internal static ChartColor GanttProgressFill(VisualThemeColors colors, ChartColor fill) =>
        ChartColorMath.Blend(fill, colors.Foreground, GanttProgressTint);

    internal static ChartColor SectionBandFill(VisualThemeColors colors, int section) =>
        ChartColorMath.Blend(colors.Background, colors.Foreground, section % 2 == 0 ? .035 : .07);

    /// <summary>How far node surfaces step from the canvas toward the ink, so cards read as raised in both modes.</summary>
    internal const double NodeSurfaceLift = .04;

    /// <summary>The smallest node and edge caption size; diagram text is read, not scanned like tick labels.</summary>
    internal const double MinimumCaptionSize = 13;

    /// <summary>Diagrams sit on the theme's card surface, like charts, so every family shares one canvas.</summary>
    /// <remarks>Titles keep the theme's chart heading scale, including its compact size on narrow canvases.</remarks>
    internal static VisualTheme Apply(VisualTheme theme) {
        var typography = theme.Typography.MapSizes(source => new VisualTypography(source.Family, source.TitleSize, source.SubtitleSize,
            source.AxisSize, source.LegendSize, Math.Max(MinimumCaptionSize, source.DataLabelSize), source.ScalarValueSize,
            source.CenterValueSize, source.AxisTitleSize, source.MonospaceFamily, source.TileLabelSize, source.TileValueSize, source.TileDetailSize));
        return theme.Configure(builder => {
            Canvas(builder.Light);
            Canvas(builder.Dark);
            if (builder.SeriesStrokeWidth > ConnectorStrokeWidth) builder.SeriesStrokeWidth = ConnectorStrokeWidth;
        }).WithTypography(typography);
    }

    private static void Canvas(VisualDesignTokens tokens) {
        tokens.Background = tokens.ElevatedSurface;
        tokens.Surface = ChartColorMath.Blend(tokens.ElevatedSurface, tokens.Foreground, NodeSurfaceLift);
    }
}
