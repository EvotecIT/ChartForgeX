using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

// CSS-facing colors are a view of the shared immutable theme, never a second configurable theme.
internal sealed class TopologyPaint {
    internal TopologyPaint(VisualTheme theme, VisualThemeMode mode, SvgColorVariables? bindings = null) {
        var colors = theme.Resolve(mode);
        string Paint(VisualThemeColorToken token, ChartForgeX.Primitives.ChartColor color) => bindings?.ResolveThemeToken(token, color) ?? color.ToCss();
        Background = Paint(VisualThemeColorToken.Background, colors.Background); Foreground = Paint(VisualThemeColorToken.Foreground, colors.Foreground);
        MutedForeground = Paint(VisualThemeColorToken.MutedForeground, colors.MutedForeground); Card = Paint(VisualThemeColorToken.Surface, colors.Surface);
        Surface = Paint(VisualThemeColorToken.ElevatedSurface, colors.ElevatedSurface); Border = Paint(VisualThemeColorToken.Border, colors.Border); Accent = Paint(VisualThemeColorToken.Accent, colors.Accent);
        var status = colors.Status;
        Healthy = Paint(VisualThemeColorToken.Positive, status.Pass.Fill); Warning = Paint(VisualThemeColorToken.Warning, status.Medium.Fill);
        Critical = Paint(VisualThemeColorToken.Negative, status.Critical.Fill); Unknown = Paint(VisualThemeColorToken.Neutral, status.Neutral.Fill); Disabled = Paint(VisualThemeColorToken.Maintenance, status.Maintenance.Fill);
        Typography = theme.Typography;
    }
    internal static TopologyPaint Default => new(VisualTheme.Graphite(), VisualThemeMode.Light);
    internal string Background { get; }
    internal string Foreground { get; }
    internal string MutedForeground { get; }
    internal string Card { get; }
    internal string Surface { get; }
    internal string Border { get; }
    internal string Accent { get; }
    internal string Healthy { get; }
    internal string Warning { get; }
    internal string Critical { get; }
    internal string Unknown { get; }
    internal string Disabled { get; }
    internal VisualTypography Typography { get; }
    internal string FontFamily => Typography.Family;
    internal string StatusColor(TopologyHealthStatus status) => status switch {
        TopologyHealthStatus.Healthy => Healthy,
        TopologyHealthStatus.Warning => Warning,
        TopologyHealthStatus.Critical => Critical,
        TopologyHealthStatus.Disabled => Disabled,
        _ => Unknown
    };
}
