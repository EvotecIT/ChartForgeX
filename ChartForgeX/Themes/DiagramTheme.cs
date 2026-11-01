using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

/// <summary>Owns diagram presentation defaults while retaining the active theme's font request and color tokens.</summary>
internal static class DiagramTheme {
    internal const double CardCornerRadius = 6;

    internal static ChartColor SectionBandFill(VisualThemeColors colors, int section) =>
        ChartColorMath.Blend(colors.Background, colors.Foreground, section % 2 == 0 ? .035 : .07);

    internal static VisualTheme Apply(VisualTheme theme) {
        var source = theme.Typography;
        return theme.WithTypography(new VisualTypography(source.Family, Math.Max(20, source.TitleSize), Math.Max(14, source.SubtitleSize),
            source.AxisSize, source.LegendSize, Math.Max(13, source.DataLabelSize), source.ScalarValueSize,
            source.CenterValueSize, source.AxisTitleSize, source.MonospaceFamily));
    }
}
