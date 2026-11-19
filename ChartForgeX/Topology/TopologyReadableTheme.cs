using System;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

/// <summary>
/// Readable dense layouts draw node and edge captions at least at the theme's minimum readable size. Layout and paint
/// both resolve the theme here, so caption footprints and painted captions stay the same size.
/// </summary>
internal static class TopologyReadableTheme {
    internal static VisualTheme Resolve(VisualTheme theme, TopologyRenderOptions? options) {
        if (options?.ReadableDenseLayout != true) return theme;
        var minimum = theme.Typography.MinimumReadableTextSize;
        if (theme.Typography.DataLabelSize >= minimum) return theme;
        return theme.WithTypography(theme.Typography.MapSizes(source => new VisualTypography(source.Family, source.TitleSize, source.SubtitleSize,
            source.AxisSize, source.LegendSize, Math.Max(minimum, source.DataLabelSize), source.ScalarValueSize, source.CenterValueSize,
            source.AxisTitleSize, source.MonospaceFamily, source.TileLabelSize, source.TileValueSize, source.TileDetailSize)));
    }
}
