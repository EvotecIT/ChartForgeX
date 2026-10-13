using System;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

/// <summary>
/// Provides topology SVG theme tokens.
/// </summary>
public sealed class TopologyTheme {
    private VisualTypography _typography = new("Inter, Segoe UI, system-ui, sans-serif");
    internal TopologyTheme Clone() => (TopologyTheme)MemberwiseClone();

    /// <summary>Gets or sets the background color.</summary>
    public string Background { get; set; } = "#FFFFFF";

    /// <summary>Gets or sets the foreground text color.</summary>
    public string Foreground { get; set; } = "#0F172A";

    /// <summary>Gets or sets the muted text color.</summary>
    public string MutedForeground { get; set; } = "#475569";

    /// <summary>Gets or sets the card fill color.</summary>
    public string Card { get; set; } = "#FFFFFF";

    /// <summary>Gets or sets the subtle surface fill color.</summary>
    public string Surface { get; set; } = "#F8FAFC";

    /// <summary>Gets or sets the border color.</summary>
    public string Border { get; set; } = "#CBD5E1";

    /// <summary>Gets or sets the accent color.</summary>
    public string Accent { get; set; } = "#2563EB";

    /// <summary>Gets or sets the healthy status color.</summary>
    public string Healthy { get; set; } = "#16A34A";

    /// <summary>Gets or sets the warning status color.</summary>
    public string Warning { get; set; } = "#F97316";

    /// <summary>Gets or sets the critical status color.</summary>
    public string Critical { get; set; } = "#EF4444";

    /// <summary>Gets or sets the unknown status color.</summary>
    public string Unknown { get; set; } = "#64748B";

    /// <summary>Gets or sets the disabled status color.</summary>
    public string Disabled { get; set; } = "#94A3B8";

    /// <summary>Gets or sets the logical typography used for static text measurement and export.</summary>
    /// <remarks>An explicit rendering context supplied to TopologyChart.Prepare takes precedence.</remarks>
    public VisualTypography Typography {
        get => _typography;
        set => _typography = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the font family while preserving the logical typography sizes.</summary>
    public string FontFamily {
        get => _typography.Family;
        set => _typography = new VisualTypography(value, _typography.TitleSize, _typography.SubtitleSize,
            _typography.AxisSize, _typography.LegendSize, _typography.DataLabelSize,
            _typography.ScalarValueSize, _typography.CenterValueSize);
    }

    /// <summary>
    /// Creates the default light topology theme.
    /// </summary>
    /// <returns>A light topology theme.</returns>
    public static TopologyTheme Light() => new();

    /// <summary>
    /// Creates a dark topology theme.
    /// </summary>
    /// <returns>A dark topology theme.</returns>
    public static TopologyTheme Dark() => new() {
        Background = "#0B1120",
        Foreground = "#E5E7EB",
        MutedForeground = "#A5B4FC",
        Card = "#111827",
        Surface = "#172033",
        Border = "#334155",
        Accent = "#60A5FA",
        Healthy = "#22C55E",
        Warning = "#FB923C",
        Critical = "#F87171",
        Unknown = "#94A3B8",
        Disabled = "#64748B"
    };

    /// <summary>
    /// Resolves a status color.
    /// </summary>
    /// <param name="status">The health status.</param>
    /// <returns>A CSS color.</returns>
    public string StatusColor(TopologyHealthStatus status) {
        return status switch {
            TopologyHealthStatus.Healthy => Healthy,
            TopologyHealthStatus.Warning => Warning,
            TopologyHealthStatus.Critical => Critical,
            TopologyHealthStatus.Disabled => Disabled,
            _ => Unknown
        };
    }
}
