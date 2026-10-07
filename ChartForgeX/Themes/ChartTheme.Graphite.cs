using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

public sealed partial class ChartTheme {
    /// <summary>Gets or sets whether the shared Graphite frame, axes, legend, and family geometry are used.</summary>
    public bool UseGraphiteLayout { get; set; }
    /// <summary>Gets or sets whether filled marks and surfaces use flat colour without highlights or shadows.</summary>
    public bool FlatMarks { get; set; }
    /// <summary>Gets or sets the secondary text colour used for data labels and legends.</summary>
    public ChartColor Text2 { get; set; } = ChartColor.FromHex("#4D525B");
    /// <summary>Gets or sets the informational state colour, independent of the categorical palette.</summary>
    public ChartColor Info { get; set; } = ChartColor.FromHex("#0A728B");
    /// <summary>Gets or sets the quiet healthy fill colour.</summary>
    public ChartColor Quiet { get; set; } = ChartColor.FromHex("#9FCDB3");
    /// <summary>Gets or sets the understated healthy line colour.</summary>
    public ChartColor QuietLine { get; set; } = ChartColor.FromHex("#A3A9B1");
    /// <summary>Gets or sets the neutral aggregate colour.</summary>
    public ChartColor Neutral { get; set; } = ChartColor.FromHex("#C9CDD3");
    /// <summary>Gets or sets the neutral track colour.</summary>
    public ChartColor Neutral2 { get; set; } = ChartColor.FromHex("#E4E6E9");
    /// <summary>Gets or sets the neutral zero-cell colour.</summary>
    public ChartColor Neutral3 { get; set; } = ChartColor.FromHex("#F1F2F4");

    /// <summary>Creates the default flat Graphite light theme.</summary>
    public static ChartTheme GraphiteLight() => VisualDesignTokens.GraphiteLight().ApplyTo(new ChartTheme());
    /// <summary>Creates the lifted Graphite dark theme with the same semantic roles.</summary>
    public static ChartTheme GraphiteDark() => VisualDesignTokens.GraphiteDark().ApplyTo(new ChartTheme());
}
