using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

public sealed partial class VisualDesignTokens {
    /// <summary>Gets or sets whether charts use the flat Graphite look when these tokens are applied.</summary>
    public bool UseGraphiteLayout { get; set; }
    /// <summary>Gets or sets muted annotation text; null retains the secondary foreground mapping.</summary>
    public ChartColor? Muted { get; set; }
    /// <summary>Gets or sets horizontal plot guides; null retains the border-derived mapping.</summary>
    public ChartColor? Grid { get; set; }
    /// <summary>Gets or sets the zero baseline; null retains the secondary-text-derived mapping.</summary>
    public ChartColor? Axis { get; set; }
    /// <summary>Gets or sets the informational state colour.</summary>
    public ChartColor? Info { get; set; }
    /// <summary>Gets or sets a quiet healthy fill.</summary>
    public ChartColor? Quiet { get; set; }
    /// <summary>Gets or sets an understated healthy line.</summary>
    public ChartColor? QuietLine { get; set; }
    /// <summary>Gets or sets the neutral aggregate fill.</summary>
    public ChartColor? Neutral { get; set; }
    /// <summary>Gets or sets the neutral track fill.</summary>
    public ChartColor? Neutral2 { get; set; }
    /// <summary>Gets or sets the neutral zero-cell fill.</summary>
    public ChartColor? Neutral3 { get; set; }

    /// <summary>Creates the approved Graphite light colour roles and ramps.</summary>
    public static VisualDesignTokens GraphiteLight() => Graphite(false);
    /// <summary>Creates the lifted Graphite dark colour roles and ramps.</summary>
    public static VisualDesignTokens GraphiteDark() => Graphite(true);

    private static VisualDesignTokens Graphite(bool dark) {
        var tokens = VisualTheme.Graphite().Resolve(dark ? VisualThemeMode.Dark : VisualThemeMode.Light).ToTokens();
        tokens.UseGraphiteLayout = true;
        tokens.FontFamily = VisualTheme.Graphite().Typography.Family;
        tokens.CornerRadius = VisualTheme.Graphite().CardRadius;
        tokens.StrokeWidth = VisualTheme.Graphite().SeriesStrokeWidth;
        return tokens;
    }
}
