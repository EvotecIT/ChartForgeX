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

    internal static ChartColor GanttProgressFill(VisualThemeColors colors, ChartColor fill) =>
        ChartColorMath.Blend(fill, colors.Foreground, GanttProgressTint);

    internal static ChartColor SectionBandFill(VisualThemeColors colors, int section) =>
        ChartColorMath.Blend(colors.Background, colors.Foreground, section % 2 == 0 ? .035 : .07);

    internal static VisualTheme Apply(VisualTheme theme) {
        var source = theme.Typography;
        if (theme.SeriesStrokeWidth > ConnectorStrokeWidth) theme = theme.Configure(builder => builder.SeriesStrokeWidth = ConnectorStrokeWidth);
        return theme.WithTypography(new VisualTypography(source.Family, Math.Max(20, source.TitleSize), Math.Max(14, source.SubtitleSize),
            source.AxisSize, source.LegendSize, Math.Max(13, source.DataLabelSize), source.ScalarValueSize,
            source.CenterValueSize, source.AxisTitleSize, source.MonospaceFamily));
    }
}
