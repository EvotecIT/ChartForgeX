using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    private static VisualArtifactInterchangeTheme MapTopologyTheme(TopologyChart source) {
        var captured = MapTheme(source.ResolvedTheme, source.Theme.Typography.TitleSize);
        if (!source.Theme.HasTopologyDefaultExtras()) captured.Definition = source.Theme;
        if (source.ThemeMode != VisualThemeMode.Light) captured.Mode = source.ThemeMode;
        return captured;
    }

    private static VisualTheme RestoreCompleteTopologyTheme(VisualArtifactInterchangeTheme source) {
        // Existing host CSS bindings remain nonportable; a literal base definition cannot replace their authored paint.
        foreach (var paint in new[] { source.Background, source.Foreground, source.MutedForeground, source.Card, source.Surface,
            source.Border, source.Accent, source.Healthy, source.Warning, source.Critical, source.Unknown, source.Disabled }) TopologyThemeColor(paint);
        if (source.Definition != null) return source.Definition;
        var typography = new VisualTypography(family: source.FontFamily,
            titleSize: source.TitleSize ?? VisualTheme.Graphite().Typography.TitleSize,
            dataLabelSize: source.DataLabelSize ?? VisualTheme.Graphite().Typography.DataLabelSize,
            subtitleSize: source.SubtitleSize ?? VisualTheme.Graphite().Typography.SubtitleSize,
            legendSize: source.LegendSize ?? VisualTheme.Graphite().Typography.LegendSize);
        var graphite = VisualTheme.Graphite().WithTypography(typography);
        var ordinary = MapTheme(new TopologyPaint(graphite, source.Mode ?? VisualThemeMode.Light), typography.TitleSize);
        if (SameLegacyThemeColors(source, ordinary)) return graphite;
        return RestoreTopologyTheme(source);
    }

    private static bool SameLegacyThemeColors(VisualArtifactInterchangeTheme a, VisualArtifactInterchangeTheme b) =>
        a.Background == b.Background && a.Foreground == b.Foreground && a.MutedForeground == b.MutedForeground
        && a.Card == b.Card && a.Surface == b.Surface && a.Border == b.Border && a.Accent == b.Accent
        && a.Healthy == b.Healthy && a.Warning == b.Warning && a.Critical == b.Critical && a.Unknown == b.Unknown && a.Disabled == b.Disabled;
}
