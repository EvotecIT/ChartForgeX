using System;

namespace ChartForgeX.Themes;

/// <summary>Shares diagram role sizes while retaining the active theme's font request and color tokens.</summary>
internal static class DiagramTheme {
    internal static VisualTheme Apply(VisualTheme theme) {
        var source = theme.Typography;
        return theme.WithTypography(new VisualTypography(source.Family, Math.Max(20, source.TitleSize), Math.Max(14, source.SubtitleSize),
            source.AxisSize, source.LegendSize, Math.Max(13, source.DataLabelSize), source.ScalarValueSize,
            source.CenterValueSize, source.AxisTitleSize, source.MonospaceFamily));
    }
}
