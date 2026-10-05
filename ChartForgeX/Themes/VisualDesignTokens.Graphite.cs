using ChartForgeX.Primitives;

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
        ChartColor C(string light, string lifted) => ChartColor.FromHex(dark ? lifted : light);
        var tokens = new VisualDesignTokens {
            UseGraphiteLayout = true,
            Background = C("#F2F3F4", "#0C0D10"),
            Surface = C("#FFFFFF", "#16181C"), ElevatedSurface = C("#FFFFFF", "#16181C"),
            Foreground = C("#16181C", "#F2F3F5"), MutedForeground = C("#4D525B", "#B0B4BC"),
            Muted = C("#626770", "#8A8F98"), Border = C("#E2E4E7", "#272A30"),
            Grid = C("#ECEEF0", "#23262B"), Axis = C("#C4C8CE", "#3D424A"),
            Accent = C("#2A78D6", "#4A8FE6"), SecondaryAccent = C("#0F9F8C", "#22B5A0"),
            Positive = C("#1D8A52", "#4CC78A"), Warning = C("#C78404", "#EEB640"),
            Negative = C("#D4302F", "#F47171"), Info = C("#0A728B", "#3EC3DD"),
            Quiet = C("#9FCDB3", "#2F6A4C"), QuietLine = C("#A3A9B1", "#6B717B"),
            Neutral = C("#C9CDD3", "#4A4F57"), Neutral2 = C("#E4E6E9", "#2E3137"),
            Neutral3 = C("#F1F2F4", "#22252A"), Disabled = C("#A3A9B1", "#6B717B"),
            Palette = dark
                ? ChartPalettes.FromHex("#4A8FE6", "#22B5A0", "#EC8D3C", "#9A86F0", "#E067A8", "#3FB6E6", "#A3B43C", "#8695A8")
                : ChartPalettes.FromHex("#2A78D6", "#0F9F8C", "#D9731A", "#7B61E8", "#C2418A", "#1C9BD1", "#7F8F1F", "#5B6B7F"),
            SequentialRamp = dark
                ? ChartPalettes.FromHex("#1B2330", "#1F3655", "#24508A", "#2F6FC0", "#5A98E6", "#9CC3F2")
                : ChartPalettes.FromHex("#EEF4FC", "#C7DCF5", "#8FB9EA", "#4F8FDC", "#2A6BC0", "#1A4C8F"),
            DivergingRamp = new VisualDivergingRamp(
                new[] { C("#EAB27A", "#7A4A22"), C("#B65A12", "#EC8D3C") },
                C("#ECEEF0", "#2A2D33"), new[] { C("#8FB9EA", "#24508A"), C("#2A6BC0", "#5A98E6") }),
            FontFamily = "Calibri, Carlito, \"Segoe UI\", system-ui, sans-serif",
            CornerRadius = 8, StrokeWidth = 2
        };
        tokens.Status = dark ? Dark().Status : new VisualStatusTokens();
        return tokens;
    }
}
