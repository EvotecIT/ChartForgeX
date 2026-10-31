namespace ChartForgeX.Core;

/// <summary>Independent endpoint glyph overrides for a dumbbell series, rendered through the shared numeric marker owner.</summary>
/// <remarks>Unspecified values inherit the series' marker settings, then the family defaults. Explicit observation colors retain their existing precedence over marker fill. Endpoint configuration never changes authored values, order or observation identity.</remarks>
public sealed class ChartDumbbellOptions {
    /// <summary>Gets the authored Start endpoint's marker overrides. The default fill remains the theme's muted foreground.</summary>
    public ChartMarkerOptions StartMarker { get; private set; } = new();

    /// <summary>Gets the authored End endpoint's marker overrides. The default fill remains the resolved series or observation paint.</summary>
    public ChartMarkerOptions EndMarker { get; private set; } = new();

    /// <summary>Gets or sets the legend name of the Start endpoint ("Before" by default). The legend names both ends (design rule 8).</summary>
    public string StartLabel { get; set; } = "Before";

    /// <summary>Gets or sets the legend name of the End endpoint ("After" by default).</summary>
    public string EndLabel { get; set; } = "After";

    internal bool IsConfigured => StartMarker.IsConfigured || EndMarker.IsConfigured;

    /// <summary>Copies both mutable endpoint configurations for callback isolation.</summary>
    internal ChartDumbbellOptions Clone() => new() { StartMarker = StartMarker.Clone(), EndMarker = EndMarker.Clone(), StartLabel = StartLabel, EndLabel = EndLabel };
}
